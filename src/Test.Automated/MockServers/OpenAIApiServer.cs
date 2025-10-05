namespace Test.Automated.MockServers
{
    using OllamaFlow.Core.Models.OpenAI;
    using OllamaFlow.Core.Serialization;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Mock OpenAI API server for testing protocol correctness.
    /// Supports /v1/completions, /v1/chat/completions, and /v1/embeddings endpoints with streaming and non-streaming responses.
    /// </summary>
    internal class OpenAIApiServer : IDisposable
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

        private Webserver _Server = null;
        private Serializer _Serializer = new Serializer();
        private bool _Disposed = false;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="hostname">Hostname to bind to.</param>
        /// <param name="port">Port to bind to.</param>
        public OpenAIApiServer(string hostname, int port)
        {
            WebserverSettings settings = new WebserverSettings
            {
                Hostname = hostname,
                Port = port
            };

            _Server = new Webserver(settings, DefaultRoute);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1/completions", CompletionsRoute);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1/chat/completions", ChatCompletionsRoute);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/v1/embeddings", EmbeddingsRoute);
            _Server.Start();
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public void Start()
        {
            if (_Server != null && !_Server.IsListening)
            {
                _Server.Start();
            }
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop()
        {
            if (_Server != null && _Server.IsListening)
            {
                _Server.Stop();
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                if (_Server != null)
                {
                    _Server.Stop();
                    _Server.Dispose();
                    _Server = null;
                }
            }

            _Disposed = true;
        }

        private async Task DefaultRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 404;
            await ctx.Response.Send("Not found").ConfigureAwait(false);
        }

        private async Task CompletionsRoute(HttpContextBase ctx)
        {
            string requestBody = null;
            if (ctx.Request.Data != null && ctx.Request.ContentLength > 0)
            {
                using (StreamReader reader = new StreamReader(ctx.Request.Data, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            OpenAIGenerateCompletionRequest request = _Serializer.DeserializeJson<OpenAIGenerateCompletionRequest>(requestBody);

            bool stream = request.Stream ?? false;

            if (stream)
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "text/event-stream";

                string[] chunks = new string[] { "This ", "is ", "a ", "mock ", "completion." };
                string completionId = "cmpl-" + Guid.NewGuid().ToString();
                long created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                for (int i = 0; i < chunks.Length; i++)
                {
                    OpenAIStreamingCompletionResult chunk = new OpenAIStreamingCompletionResult
                    {
                        Id = completionId,
                        Object = "text_completion",
                        Created = created,
                        Model = request.Model,
                        Choices = new List<OpenAICompletionChoice>
                        {
                            new OpenAICompletionChoice
                            {
                                Index = 0,
                                Text = chunks[i],
                                FinishReason = null
                            }
                        }
                    };

                    string json = "data: " + _Serializer.SerializeJson(chunk, false) + "\n\n";
                    await ctx.Response.SendChunk(Encoding.UTF8.GetBytes(json), false).ConfigureAwait(false);
                }

                OpenAIStreamingCompletionResult final = new OpenAIStreamingCompletionResult
                {
                    Id = completionId,
                    Object = "text_completion",
                    Created = created,
                    Model = request.Model,
                    Choices = new List<OpenAICompletionChoice>
                    {
                        new OpenAICompletionChoice
                        {
                            Index = 0,
                            Text = "",
                            FinishReason = "stop"
                        }
                    }
                };

                string finalJson = "data: " + _Serializer.SerializeJson(final, false) + "\n\n";
                await ctx.Response.SendChunk(Encoding.UTF8.GetBytes(finalJson), false).ConfigureAwait(false);
                await ctx.Response.SendChunk(Encoding.UTF8.GetBytes( "data: [DONE]\n\n"), true).ConfigureAwait(false);
            }
            else
            {
                OpenAIGenerateCompletionResult result = new OpenAIGenerateCompletionResult
                {
                    Id = "cmpl-" + Guid.NewGuid().ToString(),
                    Object = "text_completion",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Model = request.Model,
                    Choices = new List<OpenAICompletionChoice>
                    {
                        new OpenAICompletionChoice
                        {
                            Index = 0,
                            Text = "This is a mock completion.",
                            FinishReason = "stop"
                        }
                    },
                    Usage = new OpenAIUsage
                    {
                        PromptTokens = 10,
                        CompletionTokens = 15,
                        TotalTokens = 25
                    }
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(_Serializer.SerializeJson(result, false)).ConfigureAwait(false);
            }
        }

        private async Task ChatCompletionsRoute(HttpContextBase ctx)
        {
            string requestBody = null;
            if (ctx.Request.Data != null && ctx.Request.ContentLength > 0)
            {
                using (StreamReader reader = new StreamReader(ctx.Request.Data, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            OpenAIGenerateChatCompletionRequest request = _Serializer.DeserializeJson<OpenAIGenerateChatCompletionRequest>(requestBody);

            bool stream = request.Stream ?? false;

            if (stream)
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "text/event-stream";

                string[] chunks = new string[] { "This ", "is ", "a ", "mock ", "chat ", "response." };
                string completionId = "chatcmpl-" + Guid.NewGuid().ToString();
                long created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                for (int i = 0; i < chunks.Length; i++)
                {
                    OpenAIStreamingChatCompletionResult chunk = new OpenAIStreamingChatCompletionResult
                    {
                        Id = completionId,
                        Object = "chat.completion.chunk",
                        Created = created,
                        Model = request.Model,
                        Choices = new List<OpenAIChatChoice>
                        {
                            new OpenAIChatChoice
                            {
                                Index = 0,
                                Delta = new OpenAIChatMessage
                                {
                                    Role = "assistant",
                                    Content = chunks[i]
                                },
                                FinishReason = null
                            }
                        }
                    };

                    string json = "data: " + _Serializer.SerializeJson(chunk, false) + "\n\n";
                    await ctx.Response.SendChunk(Encoding.UTF8.GetBytes(json), false).ConfigureAwait(false);
                }

                OpenAIStreamingChatCompletionResult final = new OpenAIStreamingChatCompletionResult
                {
                    Id = completionId,
                    Object = "chat.completion.chunk",
                    Created = created,
                    Model = request.Model,
                    Choices = new List<OpenAIChatChoice>
                    {
                        new OpenAIChatChoice
                        {
                            Index = 0,
                            Delta = new OpenAIChatMessage(),
                            FinishReason = "stop"
                        }
                    }
                };

                string finalJson = "data: " + _Serializer.SerializeJson(final, false) + "\n\n";
                await ctx.Response.SendChunk(Encoding.UTF8.GetBytes(finalJson), false).ConfigureAwait(false);
                await ctx.Response.SendChunk(Encoding.UTF8.GetBytes("data: [DONE]\n\n"), true).ConfigureAwait(false);
            }
            else
            {
                OpenAIGenerateChatCompletionResult result = new OpenAIGenerateChatCompletionResult
                {
                    Id = "chatcmpl-" + Guid.NewGuid().ToString(),
                    Object = "chat.completion",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Model = request.Model,
                    Choices = new List<OpenAIChatChoice>
                    {
                        new OpenAIChatChoice
                        {
                            Index = 0,
                            Message = new OpenAIChatMessage
                            {
                                Role = "assistant",
                                Content = "This is a mock chat response."
                            },
                            FinishReason = "stop"
                        }
                    },
                    Usage = new OpenAIUsage
                    {
                        PromptTokens = 10,
                        CompletionTokens = 15,
                        TotalTokens = 25
                    }
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(_Serializer.SerializeJson(result, false)).ConfigureAwait(false);
            }
        }

        private async Task EmbeddingsRoute(HttpContextBase ctx)
        {
            string requestBody = null;
            if (ctx.Request.Data != null && ctx.Request.ContentLength > 0)
            {
                using (StreamReader reader = new StreamReader(ctx.Request.Data, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            OpenAIGenerateEmbeddingsRequest request = _Serializer.DeserializeJson<OpenAIGenerateEmbeddingsRequest>(requestBody);

            int numInputs = 1;
            if (request.Input is List<string> inputs)
            {
                numInputs = inputs.Count;
            }

            List<OpenAIEmbedding> embeddings = new List<OpenAIEmbedding>();
            for (int i = 0; i < numInputs; i++)
            {
                List<float> embedding = new List<float>();
                for (int j = 0; j < 1536; j++)
                {
                    embedding.Add(0.01f * (i + 1) * (j + 1));
                }

                embeddings.Add(new OpenAIEmbedding
                {
                    Index = i,
                    Embedding = embedding,
                    Object = "embedding"
                });
            }

            OpenAIGenerateEmbeddingsResult result = new OpenAIGenerateEmbeddingsResult
            {
                Object = "list",
                Data = embeddings,
                Model = request.Model,
                Usage = new OpenAIEmbeddingUsage
                {
                    PromptTokens = numInputs * 5,
                    TotalTokens = numInputs * 5
                }
            };

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(_Serializer.SerializeJson(result, false)).ConfigureAwait(false);
        }

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
