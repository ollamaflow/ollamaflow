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
    /// Test 11: Completions test against a single instance of Ollama where the frontend configuration overrides the model being used
    /// </summary>
    public class Test11 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 11: Completions test against a single instance of Ollama where the frontend configuration overrides the model being used
        /// </summary>
        public Test11()
        {
            Name = "Test 11: Completions test against a single instance of Ollama where the frontend configuration overrides the model being used";

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
                AllowCompletions = true
            };

            TestEnvironment.Backends.Add(ollama1);

            // Create frontend with model override for completions
            Frontend frontend1 = new Frontend
            {
                Identifier = "frontend1",
                Name = "frontend1",
                Hostname = "localhost",
                LoadBalancing = LoadBalancingMode.RoundRobin,
                Backends = new List<string> { "ollama1" },
                RequiredModels = new List<string> { "all-minilm", "gemma3:4b", "llama3:latest" },
                UseStickySessions = false,
                TimeoutMs = 120000,
                AllowEmbeddings = true,
                AllowCompletions = true,
                AllowRetries = true,
                PinnedEmbeddingsProperties = null,
                // Override the model for completions requests - force all completions to use "llama3:latest"
                PinnedCompletionsProperties = new Dictionary<string, object>
                {
                    { "model", "llama3:latest" }
                }
            };

            TestEnvironment.Frontends.Add(frontend1);

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 11: Completions test against a single instance of Ollama where the frontend configuration overrides the model being used
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult test)
        {
            test.Success = true;

            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "ollama1");
            Frontend frontend = OllamaFlowDaemon.Frontends.GetAll().ToList()[0];
            Backend backend = OllamaFlowDaemon.Backends.GetAll().ToList()[0];

            #region Wait for Model Synchronization

            string overrideModel = "llama3:latest";
            Console.WriteLine($"Waiting for OllamaFlow to synchronize model {overrideModel}...");
            
            bool modelSynchronized = await Helpers.WaitForModelSynchronization(OllamaFlowDaemon, "ollama1", overrideModel, 300000); // 5 minute timeout
            
            if (!modelSynchronized)
            {
                Console.WriteLine($"Model {overrideModel} was not synchronized within timeout period");
                test.Success = false;
                
                ApiDetails modelSyncFailure = new ApiDetails
                {
                    Step = "Model Synchronization Wait",
                    Request = $"Waiting for OllamaFlow to synchronize {overrideModel} model",
                    Response = $"Model {overrideModel} was not synchronized within timeout period",
                    StatusCode = 408, // Request Timeout
                    StartUtc = DateTime.UtcNow,
                    EndUtc = DateTime.UtcNow
                };
                
                test.ApiDetails.Add(modelSyncFailure);
                return;
            }
            else
            {
                Console.WriteLine($"Model {overrideModel} has been synchronized and is ready for testing");
                
                ApiDetails modelSyncSuccess = new ApiDetails
                {
                    Step = "Model Synchronization Wait",
                    Request = $"Waiting for OllamaFlow to synchronize {overrideModel} model",
                    Response = $"{overrideModel} model has been synchronized successfully",
                    StatusCode = 200,
                    StartUtc = DateTime.UtcNow,
                    EndUtc = DateTime.UtcNow
                };
                
                test.ApiDetails.Add(modelSyncSuccess);
            }

            #endregion

            #region Non-Streaming Completions Request with Model Override

            string completionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateCompletion);
            HttpMethod completionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateCompletion);

            string body = Helpers.OllamaStreamingCompletionsRequestBody("gemma3:4b", "What is the capital of France?", false);
            ApiDetails nonStreamingCompletionsWithOverride = new ApiDetails
            {
                Step = "Non-Streaming Completions Request with Model Override",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(completionsUrl, completionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for non-streaming completions request with model override");
                    nonStreamingCompletionsWithOverride.Response = null;
                    nonStreamingCompletionsWithOverride.StatusCode = 0;
                    nonStreamingCompletionsWithOverride.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(nonStreamingCompletionsWithOverride);
                    return;
                }
                else
                {
                    nonStreamingCompletionsWithOverride.Response = resp;
                    nonStreamingCompletionsWithOverride.StatusCode = resp.StatusCode;
                    nonStreamingCompletionsWithOverride.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for non-streaming completions request with model override: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(nonStreamingCompletionsWithOverride);
                        return;
                    }
                    else
                    {
                        OllamaGenerateCompletionResult result = await Helpers.GetOllamaCompletionsResult(resp);
                        if (result == null || string.IsNullOrEmpty(result.Response))
                        {
                            Console.WriteLine("No completion response for non-streaming completions request with model override");
                            test.Success = false;
                            test.ApiDetails.Add(nonStreamingCompletionsWithOverride);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Non-streaming completions request succeeded with model override - frontend should have overridden model from 'gemma3:4b' to 'llama3:latest'");
                            test.Success = true;
                            test.ApiDetails.Add(nonStreamingCompletionsWithOverride);
                        }
                    }
                }
            }

            #endregion

            #region Streaming Completions Request with Model Override

            body = Helpers.OllamaStreamingCompletionsRequestBody("gemma3:4b", "What is the capital of Germany?", true);
            ApiDetails streamingCompletionsWithOverride = new ApiDetails
            {
                Step = "Streaming Completions Request with Model Override",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(completionsUrl, completionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for streaming completions request with model override");
                    streamingCompletionsWithOverride.Response = null;
                    streamingCompletionsWithOverride.StatusCode = 0;
                    streamingCompletionsWithOverride.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(streamingCompletionsWithOverride);
                    return;
                }
                else
                {
                    streamingCompletionsWithOverride.Response = resp;
                    streamingCompletionsWithOverride.StatusCode = resp.StatusCode;
                    streamingCompletionsWithOverride.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for streaming completions request with model override: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(streamingCompletionsWithOverride);
                        return;
                    }
                    else
                    {
                        if (!resp.ChunkedTransferEncoding)
                        {
                            Console.WriteLine("Expected chunked transfer encoding for streaming completions request with model override");
                            test.Success = false;
                            test.ApiDetails.Add(streamingCompletionsWithOverride);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Streaming completions request succeeded with model override - frontend should have overridden model from 'gemma3:4b' to 'llama3:latest'");
                            test.Success = true;
                            test.ApiDetails.Add(streamingCompletionsWithOverride);
                        }
                    }
                }
            }

            #endregion

            #region Chat Completions Request with Model Override

            string chatCompletionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateChatCompletion);
            HttpMethod chatCompletionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateChatCompletion);

            List<OllamaChatMessage> messages = new List<OllamaChatMessage>
            {
                new OllamaChatMessage { Role = "user", Content = "Hello, how are you? This is a test with completions model override." }
            };

            body = Helpers.OllamaStreamingChatCompletionsRequestBody("gemma3:4b", messages, false);
            ApiDetails chatCompletionsWithOverride = new ApiDetails
            {
                Step = "Chat Completions Request with Model Override",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(chatCompletionsUrl, chatCompletionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for chat completions request with model override");
                    chatCompletionsWithOverride.Response = null;
                    chatCompletionsWithOverride.StatusCode = 0;
                    chatCompletionsWithOverride.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(chatCompletionsWithOverride);
                    return;
                }
                else
                {
                    chatCompletionsWithOverride.Response = resp;
                    chatCompletionsWithOverride.StatusCode = resp.StatusCode;
                    chatCompletionsWithOverride.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for chat completions request with model override: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(chatCompletionsWithOverride);
                        return;
                    }
                    else
                    {
                        OllamaGenerateChatCompletionResult result = await Helpers.GetOllamaChatCompletionsResult(resp);
                        if (result == null || result.Message == null || string.IsNullOrEmpty(result.Message.Content))
                        {
                            Console.WriteLine("No chat completion response for chat completions request with model override");
                            test.Success = false;
                            test.ApiDetails.Add(chatCompletionsWithOverride);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Chat completions request succeeded with model override - frontend should have overridden model from 'gemma3:4b' to 'llama3:latest'");
                            test.Success = true;
                            test.ApiDetails.Add(chatCompletionsWithOverride);
                        }
                    }
                }
            }

            #endregion

            #region Completions Request with Correct Model (Should Still Work)

            body = Helpers.OllamaStreamingCompletionsRequestBody("llama3:latest", "What is the capital of Italy?", false);
            ApiDetails completionsWithCorrectModel = new ApiDetails
            {
                Step = "Completions Request with Correct Model",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(completionsUrl, completionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for completions request with correct model");
                    completionsWithCorrectModel.Response = null;
                    completionsWithCorrectModel.StatusCode = 0;
                    completionsWithCorrectModel.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(completionsWithCorrectModel);
                    return;
                }
                else
                {
                    completionsWithCorrectModel.Response = resp;
                    completionsWithCorrectModel.StatusCode = resp.StatusCode;
                    completionsWithCorrectModel.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for completions request with correct model: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(completionsWithCorrectModel);
                        return;
                    }
                    else
                    {
                        OllamaGenerateCompletionResult result = await Helpers.GetOllamaCompletionsResult(resp);
                        if (result == null || string.IsNullOrEmpty(result.Response))
                        {
                            Console.WriteLine("No completion response for completions request with correct model");
                            test.Success = false;
                            test.ApiDetails.Add(completionsWithCorrectModel);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Completions request with correct model succeeded as expected");
                            test.Success = true;
                            test.ApiDetails.Add(completionsWithCorrectModel);
                        }
                    }
                }
            }

            #endregion

            #region Embeddings Request (Should Not Be Affected by Completions Override)

            string embeddingsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateEmbeddings);
            HttpMethod embeddingsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateEmbeddings);

            body = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, "test embeddings with completions model override");
            ApiDetails embeddingsNotAffectedByOverride = new ApiDetails
            {
                Step = "Embeddings Request (Not Affected by Completions Override)",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(embeddingsUrl, embeddingsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for embeddings request");
                    embeddingsNotAffectedByOverride.Response = null;
                    embeddingsNotAffectedByOverride.StatusCode = 0;
                    embeddingsNotAffectedByOverride.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(embeddingsNotAffectedByOverride);
                    return;
                }
                else
                {
                    embeddingsNotAffectedByOverride.Response = resp;
                    embeddingsNotAffectedByOverride.StatusCode = resp.StatusCode;
                    embeddingsNotAffectedByOverride.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for embeddings request: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(embeddingsNotAffectedByOverride);
                        return;
                    }
                    else
                    {
                        OllamaGenerateEmbeddingsResult result = await Helpers.GetOllamaEmbeddingsResult(resp);
                        if (result == null || result.Embeddings == null)
                        {
                            Console.WriteLine("No embeddings response for embeddings request");
                            test.Success = false;
                            test.ApiDetails.Add(embeddingsNotAffectedByOverride);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Embeddings request succeeded as expected - completions model override should not affect embeddings");
                            test.Success = true;
                            test.ApiDetails.Add(embeddingsNotAffectedByOverride);
                        }
                    }
                }
            }

            #endregion

            #region Test Summary

            ApiDetails testSummary = new ApiDetails
            {
                Step = "Test 11 Summary",
                Request = "Tested completions requests with frontend model override (gemma3:4b -> llama3:latest)",
                Response = "Completions requests succeeded with model override, embeddings not affected",
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
