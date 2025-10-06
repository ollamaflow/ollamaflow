namespace Test.Automated.Tests
{
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Models.Ollama;
    using OllamaFlow.Core.Serialization;
    using RestWrapper;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Net.WebSockets;
    using System.Runtime.CompilerServices;
    using System.Xml.Linq;

    /// <summary>
    /// Test 1: Ollama backend, Ollama APIs, single embeddings, multiple embeddings, completions, and chat completions test
    /// </summary>
    public class Test1 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 1: Ollama backend, Ollama APIs, single embeddings, multiple embeddings, completions, and chat completions test
        /// </summary>
        public Test1()
        {
            Name = "Test 1: Ollama backend, Ollama APIs, single embeddings, multiple embeddings, completions, and chat completions test";

            Backend ollama1 = new Backend
            {
                Identifier = "ollama1",
                Name = "ollama1",
                Hostname = "localhost",
                Port = 11435,
                Ssl = false,
                HealthCheckMethod = "HEAD",
                HealthCheckUrl = "/",
                ApiFormat = ApiFormatEnum.Ollama,
                PinnedEmbeddingsProperties = null,
                PinnedCompletionsProperties = null,
                AllowEmbeddings = true,
                AllowCompletions = true
            };

            TestEnvironment.Backends.Add(ollama1);

            Frontend frontend1 = new Frontend
            {
                Identifier = "frontend1",
                Name = "frontend1",
                Hostname = "localhost",
                LoadBalancing = LoadBalancingMode.RoundRobin,
                Backends = new List<string> { "ollama1" },
                RequiredModels = new List<string> { "all-minilm", "gemma3:4b" },
                UseStickySessions = false,
                AllowEmbeddings = true,
                AllowCompletions = true,
                AllowRetries = true,
                TimeoutMs = 120000,
            };

            TestEnvironment.Frontends.Add(frontend1);

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 1: Ollama backend, Ollama APIs, single embeddings, multiple embeddings, completions, and chat completions test
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult test)
        {
            test.Success = true; // default to true

            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "ollama1");
            Frontend frontend = OllamaFlowDaemon.Frontends.GetAll().ToList()[0];
            Backend backend = OllamaFlowDaemon.Backends.GetAll().ToList()[0];

            #region Embeddings

            string embeddingsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateEmbeddings);
            HttpMethod method = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateEmbeddings);

            #region Single-Embeddings

            string body = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, "test");
            ApiDetails singleEmbeddings = new ApiDetails
            {
                Step = "Ollama Single Embeddings",
                Request = body
            };

            using (RestResponse resp = await SendRestRequest<string>(method, embeddingsUrl, body, Constants.JsonContentType))
            {
                OllamaGenerateEmbeddingsResult result = await Helpers.GetOllamaEmbeddingsResult(resp);
                if (result == null)
                {
                    Console.WriteLine("No response for single embeddings request");
                    singleEmbeddings.Response = null;
                    singleEmbeddings.StatusCode = 0;
                    singleEmbeddings.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(singleEmbeddings);
                    return;
                }
                else
                {
                    singleEmbeddings.Response = resp;
                    singleEmbeddings.StatusCode = resp.StatusCode;
                    singleEmbeddings.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for single embeddings request");
                        test.Success = false;
                        test.ApiDetails.Add(singleEmbeddings);
                        return;
                    }
                    else
                    {
                        singleEmbeddings.Response = resp;
                        singleEmbeddings.StatusCode = resp.StatusCode;

                        if (result.Embeddings == null)
                        {
                            Console.WriteLine("No embeddings returned for single embeddings request");
                            test.Success = false;
                            test.ApiDetails.Add(singleEmbeddings);
                            return;
                        }
                        else
                        {
                            // Ollama always returns array of arrays, even for single input
                            if (result.GetEmbeddingCount() != 1)
                            {
                                Console.WriteLine($"Expected 1 embedding, got {result.GetEmbeddingCount()} for single embeddings request");
                                test.Success = false;
                                test.ApiDetails.Add(singleEmbeddings);
                                return;
                            }
                            else
                            {
                                test.Success = true;
                                test.ApiDetails.Add(singleEmbeddings);
                            }
                        }
                    }
                }
            }

            #endregion

            #region Multiple-Embeddings

            body = Helpers.OllamaMultipleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, new List<string> { "hello", "workd" });
            ApiDetails multiEmbeddings = new ApiDetails
            {
                Step = "Ollama Multi Embeddings",
                Request = body
            };

            using (RestResponse resp = await SendRestRequest<string>(method, embeddingsUrl, body, Constants.JsonContentType))
            {
                OllamaGenerateEmbeddingsResult result = await Helpers.GetOllamaEmbeddingsResult(resp);
                if (result == null)
                {
                    Console.WriteLine("No response for multiple embeddings request");
                    multiEmbeddings.Response = null;
                    multiEmbeddings.StatusCode = 0;
                    multiEmbeddings.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(multiEmbeddings);
                    return;
                }
                else
                {
                    multiEmbeddings.Response = resp;
                    multiEmbeddings.StatusCode = resp.StatusCode;
                    multiEmbeddings.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for multiple embeddings request");
                        test.Success = false;
                        test.ApiDetails.Add(multiEmbeddings);
                        return;
                    }
                    else
                    {
                        multiEmbeddings.Response = resp;
                        multiEmbeddings.StatusCode = resp.StatusCode;

                        if (result.Embeddings == null)
                        {
                            Console.WriteLine("No embeddings returned for multiple embeddings request");
                            test.Success = false;
                            test.ApiDetails.Add(multiEmbeddings);
                            return;
                        }
                        else
                        {
                            if (result.GetEmbeddingCount() != 2)
                            {
                                Console.WriteLine($"Expected 2 embeddings, got {result.GetEmbeddingCount()} for multiple embeddings request");
                                test.Success = false;
                                test.ApiDetails.Add(multiEmbeddings);
                                return;
                            }
                            else
                            {
                                test.Success = true;
                                test.ApiDetails.Add(multiEmbeddings);
                            }
                        }
                    }
                }
            }

            #endregion

            #endregion

            #region Completions

            string completionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateCompletion);
            HttpMethod completionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateCompletion);

            #region Non-Streaming-Completions

            body = Helpers.OllamaStreamingCompletionsRequestBody(TestEnvironment.CompletionsModel, "What is the capital of France?", false);
            ApiDetails nonStreamingCompletions = new ApiDetails
            {
                Step = "Ollama Non-Streaming Completions",
                Request = body
            };

            using (RestResponse resp = await SendRestRequest<string>(completionsMethod, completionsUrl, body, Constants.JsonContentType))
            {
                OllamaGenerateCompletionResult result = await Helpers.GetOllamaCompletionsResult(resp);
                if (result == null)
                {
                    Console.WriteLine("No response for non-streaming completions request");
                    nonStreamingCompletions.Response = null;
                    nonStreamingCompletions.StatusCode = 0;
                    nonStreamingCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(nonStreamingCompletions);
                    return;
                }
                else
                {
                    nonStreamingCompletions.Response = resp;
                    nonStreamingCompletions.StatusCode = resp.StatusCode;
                    nonStreamingCompletions.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for non-streaming completions request");
                        test.Success = false;
                        test.ApiDetails.Add(nonStreamingCompletions);
                        return;
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(result.Response))
                        {
                            Console.WriteLine("No response text returned for non-streaming completions request");
                            test.Success = false;
                            test.ApiDetails.Add(nonStreamingCompletions);
                            return;
                        }
                        else
                        {
                            test.Success = true;
                            test.ApiDetails.Add(nonStreamingCompletions);
                        }
                    }
                }
            }

            #endregion

            #region Streaming-Completions

            body = Helpers.OllamaStreamingCompletionsRequestBody(TestEnvironment.CompletionsModel, "What is the capital of Germany?", true);
            ApiDetails streamingCompletions = new ApiDetails
            {
                Step = "Ollama Streaming Completions",
                Request = body
            };

            using (RestResponse resp = await SendRestRequest<string>(completionsMethod, completionsUrl, body, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for streaming completions request");
                    streamingCompletions.Response = null;
                    streamingCompletions.StatusCode = 0;
                    streamingCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(streamingCompletions);
                    return;
                }
                else
                {
                    streamingCompletions.Response = resp;
                    streamingCompletions.StatusCode = resp.StatusCode;
                    streamingCompletions.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for streaming completions request");
                        test.Success = false;
                        test.ApiDetails.Add(streamingCompletions);
                        return;
                    }
                    else
                    {
                        if (!resp.ChunkedTransferEncoding)
                        {
                            Console.WriteLine("Expected chunked transfer encoding for streaming completions request");
                            test.Success = false;
                            test.ApiDetails.Add(streamingCompletions);
                            return;
                        }
                        else
                        {
                            test.Success = true;
                            test.ApiDetails.Add(streamingCompletions);
                        }
                    }
                }
            }

            #endregion

            #endregion

            #region Chat Completions

            string chatCompletionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateChatCompletion);
            HttpMethod chatCompletionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateChatCompletion);

            #region Non-Streaming-Chat-Completions

            List<OllamaChatMessage> messages = new List<OllamaChatMessage>
            {
                new OllamaChatMessage { Role = "user", Content = "Hello, how are you?" }
            };

            body = Helpers.OllamaStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages, false);
            ApiDetails nonStreamingChatCompletions = new ApiDetails
            {
                Step = "Ollama Non-Streaming Chat Completions",
                Request = body
            };

            using (RestResponse resp = await SendRestRequest<string>(chatCompletionsMethod, chatCompletionsUrl, body, Constants.JsonContentType))
            {
                OllamaGenerateChatCompletionResult result = await Helpers.GetOllamaChatCompletionsResult(resp);
                if (result == null)
                {
                    Console.WriteLine("No response for non-streaming chat completions request");
                    nonStreamingChatCompletions.Response = null;
                    nonStreamingChatCompletions.StatusCode = 0;
                    nonStreamingChatCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(nonStreamingChatCompletions);
                    return;
                }
                else
                {
                    nonStreamingChatCompletions.Response = resp;
                    nonStreamingChatCompletions.StatusCode = resp.StatusCode;
                    nonStreamingChatCompletions.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for non-streaming chat completions request");
                        test.Success = false;
                        test.ApiDetails.Add(nonStreamingChatCompletions);
                        return;
                    }
                    else
                    {
                        if (result.Message == null || String.IsNullOrEmpty(result.Message.Content))
                        {
                            Console.WriteLine("No message content returned for non-streaming chat completions request");
                            test.Success = false;
                            test.ApiDetails.Add(nonStreamingChatCompletions);
                            return;
                        }
                        else
                        {
                            test.Success = true;
                            test.ApiDetails.Add(nonStreamingChatCompletions);
                        }
                    }
                }
            }

            #endregion

            #region Streaming-Chat-Completions

            body = Helpers.OllamaStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages, true);
            ApiDetails streamingChatCompletions = new ApiDetails
            {
                Step = "Ollama Streaming Chat Completions",
                Request = body
            };

            using (RestResponse resp = await SendRestRequest<string>(chatCompletionsMethod, chatCompletionsUrl, body, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for streaming chat completions request");
                    streamingChatCompletions.Response = null;
                    streamingChatCompletions.StatusCode = 0;
                    streamingChatCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(streamingChatCompletions);
                    return;
                }
                else
                {
                    streamingChatCompletions.Response = resp;
                    streamingChatCompletions.StatusCode = resp.StatusCode;
                    streamingChatCompletions.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for streaming chat completions request");
                        test.Success = false;
                        test.ApiDetails.Add(streamingChatCompletions);
                        return;
                    }
                    else
                    {
                        if (!resp.ChunkedTransferEncoding)
                        {
                            Console.WriteLine("Expected chunked transfer encoding for streaming chat completions request");
                            test.Success = false;
                            test.ApiDetails.Add(streamingChatCompletions);
                            return;
                        }
                        else
                        {
                            test.Success = true;
                            test.ApiDetails.Add(streamingChatCompletions);
                        }
                    }
                }
            }

            #endregion

            #endregion
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}