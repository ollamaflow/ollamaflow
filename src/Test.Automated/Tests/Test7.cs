namespace Test.Automated.Tests
{
    using System;
    using System.Collections.Generic;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Models.Ollama;
    using RestWrapper;

    /// <summary>
    /// Test 7: Chat completions test against a single instance of Ollama where the frontend configuration has completions disabled
    /// </summary>
    public class Test7 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 7: Chat completions test against a single instance of Ollama where the frontend configuration has completions disabled
        /// </summary>
        public Test7()
        {
            Name = "Test 7: Chat completions test against a single instance of Ollama where the frontend configuration has completions disabled";

            // Create single Ollama backend
            Backend ollama1 = new Backend
            {
                Identifier = "ollama1",
                Name = "ollama1",
                Hostname = "localhost",
                Port = 11434,
                Ssl = false,
                HealthCheckMethod = "HEAD",
                HealthCheckUrl = "/",
                ApiFormat = ApiFormatEnum.Ollama,
                PinnedEmbeddingsProperties = null,
                PinnedCompletionsProperties = null,
                AllowEmbeddings = true,
                AllowCompletions = true // Backend allows completions
            };

            TestEnvironment.Backends.Add(ollama1);

            // Create frontend with completions disabled
            Frontend frontend1 = new Frontend
            {
                Identifier = "frontend1",
                Name = "frontend1",
                Hostname = "localhost",
                LoadBalancing = LoadBalancingMode.RoundRobin,
                Backends = new List<string> { "ollama1" },
                RequiredModels = new List<string> { "all-minilm", "gemma3:4b" },
                UseStickySessions = false,
                TimeoutMs = 120000,
                AllowEmbeddings = true,
                AllowCompletions = false, // Frontend has completions disabled
                AllowRetries = true
            };

            TestEnvironment.Frontends.Add(frontend1);

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 7: Chat completions test against a single instance of Ollama where the frontend configuration has completions disabled
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult test)
        {
            test.Success = true;

            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "ollama1");
            Frontend frontend = OllamaFlowDaemon.Frontends.GetAll().ToList()[0];
            Backend backend = OllamaFlowDaemon.Backends.GetAll().ToList()[0];

            #region Non-Streaming Chat Completions Request - Should Fail

            string chatCompletionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateChatCompletion);
            HttpMethod chatCompletionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateChatCompletion);

            List<OllamaChatMessage> messages = new List<OllamaChatMessage>
            {
                new OllamaChatMessage { Role = "user", Content = "Hello, how are you? This is a test with completions disabled on frontend." }
            };

            string body = Helpers.OllamaStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages, false);
            ApiDetails nonStreamingChatCompletionsDisabled = new ApiDetails
            {
                Step = "Non-Streaming Chat Completions Request with Disabled Frontend",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(chatCompletionsUrl, chatCompletionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for non-streaming chat completions request with disabled frontend");
                    nonStreamingChatCompletionsDisabled.Response = null;
                    nonStreamingChatCompletionsDisabled.StatusCode = 0;
                    nonStreamingChatCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(nonStreamingChatCompletionsDisabled);
                    return;
                }
                else
                {
                    nonStreamingChatCompletionsDisabled.Response = resp;
                    nonStreamingChatCompletionsDisabled.StatusCode = resp.StatusCode;
                    nonStreamingChatCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    if (resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Unexpected success response for non-streaming chat completions request with disabled frontend - should have been rejected");
                        test.Success = false;
                        test.ApiDetails.Add(nonStreamingChatCompletionsDisabled);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Expected failure for non-streaming chat completions request with disabled frontend: {resp.StatusCode}");
                        test.Success = true;
                        test.ApiDetails.Add(nonStreamingChatCompletionsDisabled);
                    }
                }
            }

            #endregion

            #region Streaming Chat Completions Request - Should Fail

            body = Helpers.OllamaStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages, true);
            ApiDetails streamingChatCompletionsDisabled = new ApiDetails
            {
                Step = "Streaming Chat Completions Request with Disabled Frontend",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(chatCompletionsUrl, chatCompletionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for streaming chat completions request with disabled frontend");
                    streamingChatCompletionsDisabled.Response = null;
                    streamingChatCompletionsDisabled.StatusCode = 0;
                    streamingChatCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(streamingChatCompletionsDisabled);
                    return;
                }
                else
                {
                    streamingChatCompletionsDisabled.Response = resp;
                    streamingChatCompletionsDisabled.StatusCode = resp.StatusCode;
                    streamingChatCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    if (resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Unexpected success response for streaming chat completions request with disabled frontend - should have been rejected");
                        test.Success = false;
                        test.ApiDetails.Add(streamingChatCompletionsDisabled);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Expected failure for streaming chat completions request with disabled frontend: {resp.StatusCode}");
                        test.Success = true;
                        test.ApiDetails.Add(streamingChatCompletionsDisabled);
                    }
                }
            }

            #endregion

            #region Multiple Messages Chat Completions Request - Should Fail

            List<OllamaChatMessage> multipleMessages = new List<OllamaChatMessage>
            {
                new OllamaChatMessage { Role = "user", Content = "What is the capital of France?" },
                new OllamaChatMessage { Role = "assistant", Content = "The capital of France is Paris." },
                new OllamaChatMessage { Role = "user", Content = "What is the population of Paris?" }
            };

            body = Helpers.OllamaStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, multipleMessages, false);
            ApiDetails multipleMessagesChatCompletionsDisabled = new ApiDetails
            {
                Step = "Multiple Messages Chat Completions Request with Disabled Frontend",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(chatCompletionsUrl, chatCompletionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for multiple messages chat completions request with disabled frontend");
                    multipleMessagesChatCompletionsDisabled.Response = null;
                    multipleMessagesChatCompletionsDisabled.StatusCode = 0;
                    multipleMessagesChatCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(multipleMessagesChatCompletionsDisabled);
                    return;
                }
                else
                {
                    multipleMessagesChatCompletionsDisabled.Response = resp;
                    multipleMessagesChatCompletionsDisabled.StatusCode = resp.StatusCode;
                    multipleMessagesChatCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    if (resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Unexpected success response for multiple messages chat completions request with disabled frontend - should have been rejected");
                        test.Success = false;
                        test.ApiDetails.Add(multipleMessagesChatCompletionsDisabled);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Expected failure for multiple messages chat completions request with disabled frontend: {resp.StatusCode}");
                        test.Success = true;
                        test.ApiDetails.Add(multipleMessagesChatCompletionsDisabled);
                    }
                }
            }

            #endregion

            #region Embeddings Request - Should Succeed

            string embeddingsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateEmbeddings);
            HttpMethod embeddingsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateEmbeddings);

            body = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, "test embeddings with completions disabled");
            ApiDetails embeddingsWithCompletionsDisabled = new ApiDetails
            {
                Step = "Embeddings Request with Completions Disabled on Frontend",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(embeddingsUrl, embeddingsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for embeddings request with completions disabled on frontend");
                    embeddingsWithCompletionsDisabled.Response = null;
                    embeddingsWithCompletionsDisabled.StatusCode = 0;
                    embeddingsWithCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(embeddingsWithCompletionsDisabled);
                    return;
                }
                else
                {
                    embeddingsWithCompletionsDisabled.Response = resp;
                    embeddingsWithCompletionsDisabled.StatusCode = resp.StatusCode;
                    embeddingsWithCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for embeddings request with completions disabled on frontend: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(embeddingsWithCompletionsDisabled);
                        return;
                    }
                    else
                    {
                        OllamaGenerateEmbeddingsResult result = await Helpers.GetOllamaEmbeddingsResult(resp);
                        if (result == null || result.Embeddings == null)
                        {
                            Console.WriteLine("No embeddings response for embeddings request with completions disabled on frontend");
                            test.Success = false;
                            test.ApiDetails.Add(embeddingsWithCompletionsDisabled);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Embeddings request succeeded as expected when completions are disabled on frontend");
                            test.Success = true;
                            test.ApiDetails.Add(embeddingsWithCompletionsDisabled);
                        }
                    }
                }
            }

            #endregion

            #region Test Summary

            ApiDetails testSummary = new ApiDetails
            {
                Step = "Test 7 Summary",
                Request = "Tested chat completions requests with frontend completions disabled",
                Response = "All chat completions requests correctly rejected, embeddings still work",
                StatusCode = 200,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow
            };

            test.ApiDetails.Add(testSummary);

            #endregion
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
