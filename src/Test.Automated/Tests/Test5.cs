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
    /// Test 5: Embeddings test against a single instance of Ollama where the frontend configuration has embeddings disabled
    /// </summary>
    public class Test5 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 5: Embeddings test against a single instance of Ollama where the frontend configuration has embeddings disabled
        /// </summary>
        public Test5()
        {
            Name = "Test 5: Embeddings test against a single instance of Ollama where the frontend configuration has embeddings disabled";

            // Create single Ollama backend
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
                AllowEmbeddings = true, // Backend allows embeddings
                AllowCompletions = true
            };

            TestEnvironment.Backends.Add(ollama1);

            // Create frontend with embeddings disabled
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
                AllowEmbeddings = false, // Frontend has embeddings disabled
                AllowCompletions = true,
                AllowRetries = true
            };

            TestEnvironment.Frontends.Add(frontend1);

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 5: Embeddings test against a single instance of Ollama where the frontend configuration has embeddings disabled
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult test)
        {
            test.Success = true;

            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "ollama1");
            Frontend frontend = OllamaFlowDaemon.Frontends.GetAll().ToList()[0];
            Backend backend = OllamaFlowDaemon.Backends.GetAll().ToList()[0];

            #region Single Embeddings Request - Should Fail

            string embeddingsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateEmbeddings);
            HttpMethod embeddingsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateEmbeddings);

            string body = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, "test embeddings with disabled frontend");
            ApiDetails singleEmbeddingsDisabled = new ApiDetails
            {
                Step = "Single Embeddings Request with Disabled Frontend",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(embeddingsUrl, embeddingsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for single embeddings request with disabled frontend");
                    singleEmbeddingsDisabled.Response = null;
                    singleEmbeddingsDisabled.StatusCode = 0;
                    singleEmbeddingsDisabled.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(singleEmbeddingsDisabled);
                    return;
                }
                else
                {
                    singleEmbeddingsDisabled.Response = resp;
                    singleEmbeddingsDisabled.StatusCode = resp.StatusCode;
                    singleEmbeddingsDisabled.EndUtc = DateTime.UtcNow;

                    if (resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Unexpected success response for single embeddings request with disabled frontend - should have been rejected");
                        test.Success = false;
                        test.ApiDetails.Add(singleEmbeddingsDisabled);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Expected failure for single embeddings request with disabled frontend: {resp.StatusCode}");
                        test.Success = true;
                        test.ApiDetails.Add(singleEmbeddingsDisabled);
                    }
                }
            }

            #endregion

            #region Multiple Embeddings Request - Should Fail

            body = Helpers.OllamaMultipleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, new List<string> { "hello", "world" });
            ApiDetails multipleEmbeddingsDisabled = new ApiDetails
            {
                Step = "Multiple Embeddings Request with Disabled Frontend",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(embeddingsUrl, embeddingsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for multiple embeddings request with disabled frontend");
                    multipleEmbeddingsDisabled.Response = null;
                    multipleEmbeddingsDisabled.StatusCode = 0;
                    multipleEmbeddingsDisabled.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(multipleEmbeddingsDisabled);
                    return;
                }
                else
                {
                    multipleEmbeddingsDisabled.Response = resp;
                    multipleEmbeddingsDisabled.StatusCode = resp.StatusCode;
                    multipleEmbeddingsDisabled.EndUtc = DateTime.UtcNow;

                    // This should fail because frontend has embeddings disabled
                    if (resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Unexpected success response for multiple embeddings request with disabled frontend - should have been rejected");
                        test.Success = false;
                        test.ApiDetails.Add(multipleEmbeddingsDisabled);
                        return;
                    }
                    else
                    {
                        // Expected failure - embeddings are disabled on frontend
                        Console.WriteLine($"Expected failure for multiple embeddings request with disabled frontend: {resp.StatusCode}");
                        test.Success = true;
                        test.ApiDetails.Add(multipleEmbeddingsDisabled);
                    }
                }
            }

            #endregion

            #region Completions Request - Should Succeed

            string completionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateCompletion);
            HttpMethod completionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateCompletion);

            body = Helpers.OllamaStreamingCompletionsRequestBody(TestEnvironment.CompletionsModel, "What is the capital of France?", false);
            ApiDetails completionsWithEmbeddingsDisabled = new ApiDetails
            {
                Step = "Completions Request with Embeddings Disabled on Frontend",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(completionsUrl, completionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for completions request with embeddings disabled on frontend");
                    completionsWithEmbeddingsDisabled.Response = null;
                    completionsWithEmbeddingsDisabled.StatusCode = 0;
                    completionsWithEmbeddingsDisabled.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(completionsWithEmbeddingsDisabled);
                    return;
                }
                else
                {
                    completionsWithEmbeddingsDisabled.Response = resp;
                    completionsWithEmbeddingsDisabled.StatusCode = resp.StatusCode;
                    completionsWithEmbeddingsDisabled.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for completions request with embeddings disabled on frontend: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(completionsWithEmbeddingsDisabled);
                        return;
                    }
                    else
                    {
                        OllamaGenerateCompletionResult result = await Helpers.GetOllamaCompletionsResult(resp);
                        if (result == null || string.IsNullOrEmpty(result.Response))
                        {
                            Console.WriteLine("No completion response for completions request with embeddings disabled on frontend");
                            test.Success = false;
                            test.ApiDetails.Add(completionsWithEmbeddingsDisabled);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Completions request succeeded as expected when embeddings are disabled on frontend");
                            test.Success = true;
                            test.ApiDetails.Add(completionsWithEmbeddingsDisabled);
                        }
                    }
                }
            }

            #endregion

            #region Chat Completions Request - Should Succeed

            string chatCompletionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateChatCompletion);
            HttpMethod chatCompletionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateChatCompletion);

            List<OllamaChatMessage> messages = new List<OllamaChatMessage>
            {
                new OllamaChatMessage { Role = "user", Content = "Hello, how are you? This is a test with embeddings disabled on frontend." }
            };

            body = Helpers.OllamaStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages, false);
            ApiDetails chatCompletionsWithEmbeddingsDisabled = new ApiDetails
            {
                Step = "Chat Completions Request with Embeddings Disabled on Frontend",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(chatCompletionsUrl, chatCompletionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for chat completions request with embeddings disabled on frontend");
                    chatCompletionsWithEmbeddingsDisabled.Response = null;
                    chatCompletionsWithEmbeddingsDisabled.StatusCode = 0;
                    chatCompletionsWithEmbeddingsDisabled.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(chatCompletionsWithEmbeddingsDisabled);
                    return;
                }
                else
                {
                    chatCompletionsWithEmbeddingsDisabled.Response = resp;
                    chatCompletionsWithEmbeddingsDisabled.StatusCode = resp.StatusCode;
                    chatCompletionsWithEmbeddingsDisabled.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for chat completions request with embeddings disabled on frontend: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(chatCompletionsWithEmbeddingsDisabled);
                        return;
                    }
                    else
                    {
                        OllamaGenerateChatCompletionResult result = await Helpers.GetOllamaChatCompletionsResult(resp);
                        if (result == null || result.Message == null || string.IsNullOrEmpty(result.Message.Content))
                        {
                            Console.WriteLine("No chat completion response for chat completions request with embeddings disabled on frontend");
                            test.Success = false;
                            test.ApiDetails.Add(chatCompletionsWithEmbeddingsDisabled);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Chat completions request succeeded as expected when embeddings are disabled on frontend");
                            test.Success = true;
                            test.ApiDetails.Add(chatCompletionsWithEmbeddingsDisabled);
                        }
                    }
                }
            }

            #endregion

            #region Test Summary

            ApiDetails testSummary = new ApiDetails
            {
                Step = "Test 5 Summary",
                Request = "Tested embeddings requests with frontend embeddings disabled",
                Response = "Embeddings requests correctly rejected, completions and chat completions still work",
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
