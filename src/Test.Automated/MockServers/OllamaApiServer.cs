namespace Test.Automated.MockServers
{
    using OllamaFlow.Core.Models.Ollama;
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
    /// Mock Ollama API server for testing protocol correctness.
    /// Supports /api/generate, /api/chat, and /api/embeddings endpoints with streaming and non-streaming responses.
    /// </summary>
    internal class OllamaApiServer : IDisposable
    {
        private Webserver _Server = null!;
        private Serializer _Serializer = new Serializer();
        private bool _Disposed = false;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="hostname">Hostname to bind to.</param>
        /// <param name="port">Port to bind to.</param>
        public OllamaApiServer(string hostname, int port)
        {
            WebserverSettings settings = new WebserverSettings
            {
                Hostname = hostname,
                Port = port
            };

            _Server = new Webserver(settings, DefaultRoute);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/api/generate", GenerateRoute);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/api/chat", ChatRoute);
            _Server.Routes.PreAuthentication.Static.Add(WatsonWebserver.Core.HttpMethod.POST, "/api/embeddings", EmbeddingsRoute);
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
                    _Server = null!;
                }
            }

            _Disposed = true;
        }

        private async Task DefaultRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 404;
            await ctx.Response.Send("Not found").ConfigureAwait(false);
        }

        private async Task GenerateRoute(HttpContextBase ctx)
        {
            string requestBody = null!;
            if (ctx.Request.Data != null && ctx.Request.ContentLength > 0)
            {
                using (StreamReader reader = new StreamReader(ctx.Request.Data, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            OllamaGenerateCompletion request = _Serializer.DeserializeJson<OllamaGenerateCompletion>(requestBody);

            bool stream = request.Stream ?? true;

            if (stream)
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/x-ndjson";

                string[] chunks = new string[] { "This ", "is ", "a ", "mock ", "response." };

                for (int i = 0; i < chunks.Length; i++)
                {
                    OllamaStreamingCompletionResult chunk = new OllamaStreamingCompletionResult
                    {
                        Model = request.Model,
                        CreatedAt = DateTime.UtcNow,
                        Response = chunks[i],
                        Done = false
                    };

                    string json = _Serializer.SerializeJson(chunk, false) + "\n";
                    await ctx.Response.SendChunk(Encoding.UTF8.GetBytes(json), false).ConfigureAwait(false);
                }

                OllamaStreamingCompletionResult final = new OllamaStreamingCompletionResult
                {
                    Model = request.Model,
                    CreatedAt = DateTime.UtcNow,
                    Response = "",
                    Done = true
                };

                string finalJson = _Serializer.SerializeJson(final, false) + "\n";
                await ctx.Response.SendChunk(Encoding.UTF8.GetBytes(finalJson), true).ConfigureAwait(false);
            }
            else
            {
                OllamaGenerateCompletionResult result = new OllamaGenerateCompletionResult
                {
                    Model = request.Model,
                    CreatedAt = DateTime.UtcNow,
                    Response = "This is a mock response.",
                    Context = new List<int> { 1, 2, 3, 4, 5 },
                    TotalDuration = 1000000000,
                    LoadDuration = 100000000,
                    PromptEvalCount = 10,
                    PromptEvalDuration = 200000000,
                    EvalCount = 15,
                    EvalDuration = 700000000
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(_Serializer.SerializeJson(result, false)).ConfigureAwait(false);
            }
        }

        private async Task ChatRoute(HttpContextBase ctx)
        {
            string requestBody = null!;
            if (ctx.Request.Data != null && ctx.Request.ContentLength > 0)
            {
                using (StreamReader reader = new StreamReader(ctx.Request.Data, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            OllamaGenerateChatCompletionRequest request = _Serializer.DeserializeJson<OllamaGenerateChatCompletionRequest>(requestBody);

            bool stream = request.Stream ?? true;

            if (stream)
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/x-ndjson";

                string[] chunks = new string[] { "This ", "is ", "a ", "mock ", "chat ", "response." };

                for (int i = 0; i < chunks.Length; i++)
                {
                    OllamaStreamingChatCompletionResult chunk = new OllamaStreamingChatCompletionResult
                    {
                        Model = request.Model,
                        CreatedAt = DateTime.UtcNow,
                        Message = new OllamaChatMessage
                        {
                            Role = "assistant",
                            Content = chunks[i]
                        },
                        Done = false
                    };

                    string json = _Serializer.SerializeJson(chunk, false) + "\n";
                    await ctx.Response.SendChunk(Encoding.UTF8.GetBytes(json), false).ConfigureAwait(false);
                }

                OllamaGenerateChatCompletionResult final = new OllamaGenerateChatCompletionResult
                {
                    Model = request.Model,
                    CreatedAt = DateTime.UtcNow,
                    Message = new OllamaChatMessage
                    {
                        Role = "assistant",
                        Content = ""
                    },
                    Done = true,
                    DoneReason = "stop",
                    TotalDuration = 1000000000,
                    LoadDuration = 100000000,
                    PromptEvalCount = 10,
                    PromptEvalDuration = 200000000,
                    EvalCount = 15,
                    EvalDuration = 700000000
                };

                string finalJson = _Serializer.SerializeJson(final, false) + "\n";
                await ctx.Response.SendChunk(Encoding.UTF8.GetBytes(finalJson), true).ConfigureAwait(false);
            }
            else
            {
                OllamaGenerateChatCompletionResult result = new OllamaGenerateChatCompletionResult
                {
                    Model = request.Model,
                    CreatedAt = DateTime.UtcNow,
                    Message = new OllamaChatMessage
                    {
                        Role = "assistant",
                        Content = "This is a mock chat response."
                    },
                    Done = true,
                    DoneReason = "stop",
                    TotalDuration = 1000000000,
                    LoadDuration = 100000000,
                    PromptEvalCount = 10,
                    PromptEvalDuration = 200000000,
                    EvalCount = 15,
                    EvalDuration = 700000000
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(_Serializer.SerializeJson(result, false)).ConfigureAwait(false);
            }
        }

        private async Task EmbeddingsRoute(HttpContextBase ctx)
        {
            string requestBody = null!;
            if (ctx.Request.Data != null && ctx.Request.ContentLength > 0)
            {
                using (StreamReader reader = new StreamReader(ctx.Request.Data, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            OllamaGenerateEmbeddingsRequest request = _Serializer.DeserializeJson<OllamaGenerateEmbeddingsRequest>(requestBody);

            List<string> inputs = request.GetInputs();
            int numInputs = inputs?.Count ?? 1;

            List<List<float>> embeddings = new List<List<float>>();
            for (int i = 0; i < numInputs; i++)
            {
                List<float> embedding = new List<float>();
                for (int j = 0; j < 384; j++)
                {
                    embedding.Add(0.1f * (i + 1) * (j + 1));
                }
                embeddings.Add(embedding);
            }

            OllamaGenerateEmbeddingsResult result = new OllamaGenerateEmbeddingsResult
            {
                Model = request.Model,
                Embeddings = embeddings,
                TotalDuration = 500000000,
                LoadDuration = 50000000,
                PromptEvalCount = numInputs * 5
            };

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(_Serializer.SerializeJson(result, false)).ConfigureAwait(false);
        }
    }
}
