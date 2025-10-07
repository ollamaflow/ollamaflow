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
    /// Test 10: Embeddings test against a single instance of Ollama where the frontend configuration overrides the model being used in the request
    /// </summary>
    public class Test10 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 10: Embeddings test against a single instance of Ollama where the frontend configuration overrides the model being used in the request
        /// </summary>
        public Test10()
        {
            Name = "Test 10: Embeddings test against a single instance of Ollama where the frontend configuration overrides the model being used in the request";

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

            // Create frontend with model override for embeddings
            Frontend frontend1 = new Frontend
            {
                Identifier = "frontend1",
                Name = "frontend1",
                Hostname = "localhost",
                LoadBalancing = LoadBalancingMode.RoundRobin,
                Backends = new List<string> { "ollama1" },
                RequiredModels = new List<string> { "all-minilm", "nomic-embed-text", "gemma3:4b" },
                UseStickySessions = false,
                TimeoutMs = 120000,
                AllowEmbeddings = true,
                AllowCompletions = true,
                AllowRetries = true,
                PinnedEmbeddingsProperties = new Dictionary<string, object>
                {
                    { "model", "nomic-embed-text" }
                },
                PinnedCompletionsProperties = null
            };

            TestEnvironment.Frontends.Add(frontend1);

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 10: Embeddings test against a single instance of Ollama where the frontend configuration overrides the model being used in the request
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

            string overrideModel = "nomic-embed-text";
            Console.WriteLine($"Waiting for OllamaFlow to synchronize model {overrideModel}...");
            
            bool modelSynchronized = await Helpers.WaitForModelSynchronization(OllamaFlowDaemon, "ollama1", overrideModel, 300000);
            
            if (!modelSynchronized)
            {
                Console.WriteLine($"Model {overrideModel} was not synchronized within timeout period");
                test.Success = false;
                
                ApiDetails modelSyncFailure = new ApiDetails
                {
                    Step = "Model Synchronization Wait",
                    Request = $"Waiting for OllamaFlow to synchronize {overrideModel} model",
                    Response = $"Model {overrideModel} was not synchronized within timeout period",
                    StatusCode = 408,
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

            #region Single Embeddings Request with Model Override

            string embeddingsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateEmbeddings);
            HttpMethod embeddingsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateEmbeddings);

            string body = Helpers.OllamaSingleEmbeddingsRequestBody("all-minilm", "test embeddings with model override");
            ApiDetails singleEmbeddingsWithOverride = new ApiDetails
            {
                Step = "Single Embeddings Request with Model Override",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(embeddingsUrl, embeddingsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for single embeddings request with model override");
                    singleEmbeddingsWithOverride.Response = null;
                    singleEmbeddingsWithOverride.StatusCode = 0;
                    singleEmbeddingsWithOverride.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(singleEmbeddingsWithOverride);
                    return;
                }
                else
                {
                    singleEmbeddingsWithOverride.Response = resp;
                    singleEmbeddingsWithOverride.StatusCode = resp.StatusCode;
                    singleEmbeddingsWithOverride.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for single embeddings request with model override: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(singleEmbeddingsWithOverride);
                        return;
                    }
                    else
                    {
                        OllamaGenerateEmbeddingsResult result = await Helpers.GetOllamaEmbeddingsResult(resp);
                        if (result == null || result.Embeddings == null)
                        {
                            Console.WriteLine("No embeddings response for single embeddings request with model override");
                            test.Success = false;
                            test.ApiDetails.Add(singleEmbeddingsWithOverride);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Single embeddings request succeeded with model override - frontend should have overridden model from 'all-minilm' to 'nomic-embed-text'");
                            test.Success = true;
                            test.ApiDetails.Add(singleEmbeddingsWithOverride);
                        }
                    }
                }
            }

            #endregion

            #region Multiple Embeddings Request with Model Override

            body = Helpers.OllamaMultipleEmbeddingsRequestBody("all-minilm", new List<string> { "hello", "world" });
            ApiDetails multipleEmbeddingsWithOverride = new ApiDetails
            {
                Step = "Multiple Embeddings Request with Model Override",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(embeddingsUrl, embeddingsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for multiple embeddings request with model override");
                    multipleEmbeddingsWithOverride.Response = null;
                    multipleEmbeddingsWithOverride.StatusCode = 0;
                    multipleEmbeddingsWithOverride.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(multipleEmbeddingsWithOverride);
                    return;
                }
                else
                {
                    multipleEmbeddingsWithOverride.Response = resp;
                    multipleEmbeddingsWithOverride.StatusCode = resp.StatusCode;
                    multipleEmbeddingsWithOverride.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for multiple embeddings request with model override: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(multipleEmbeddingsWithOverride);
                        return;
                    }
                    else
                    {
                        OllamaGenerateEmbeddingsResult result = await Helpers.GetOllamaEmbeddingsResult(resp);
                        if (result == null || result.Embeddings == null)
                        {
                            Console.WriteLine("No embeddings response for multiple embeddings request with model override");
                            test.Success = false;
                            test.ApiDetails.Add(multipleEmbeddingsWithOverride);
                            return;
                        }
                        else
                        {
                            if (result.GetEmbeddingCount() != 2)
                            {
                                Console.WriteLine($"Expected 2 embeddings, got {result.GetEmbeddingCount()} for multiple embeddings request with model override");
                                test.Success = false;
                                test.ApiDetails.Add(multipleEmbeddingsWithOverride);
                                return;
                            }
                            else
                            {
                                Console.WriteLine("Multiple embeddings request succeeded with model override - frontend should have overridden model from 'all-minilm' to 'nomic-embed-text'");
                                test.Success = true;
                                test.ApiDetails.Add(multipleEmbeddingsWithOverride);
                            }
                        }
                    }
                }
            }

            #endregion

            #region Embeddings Request with Correct Model (Should Still Work)

            body = Helpers.OllamaSingleEmbeddingsRequestBody("nomic-embed-text", "test embeddings with correct model");
            ApiDetails embeddingsWithCorrectModel = new ApiDetails
            {
                Step = "Embeddings Request with Correct Model",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(embeddingsUrl, embeddingsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for embeddings request with correct model");
                    embeddingsWithCorrectModel.Response = null;
                    embeddingsWithCorrectModel.StatusCode = 0;
                    embeddingsWithCorrectModel.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(embeddingsWithCorrectModel);
                    return;
                }
                else
                {
                    embeddingsWithCorrectModel.Response = resp;
                    embeddingsWithCorrectModel.StatusCode = resp.StatusCode;
                    embeddingsWithCorrectModel.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for embeddings request with correct model: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(embeddingsWithCorrectModel);
                        return;
                    }
                    else
                    {
                        OllamaGenerateEmbeddingsResult result = await Helpers.GetOllamaEmbeddingsResult(resp);
                        if (result == null || result.Embeddings == null)
                        {
                            Console.WriteLine("No embeddings response for embeddings request with correct model");
                            test.Success = false;
                            test.ApiDetails.Add(embeddingsWithCorrectModel);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Embeddings request with correct model succeeded as expected");
                            test.Success = true;
                            test.ApiDetails.Add(embeddingsWithCorrectModel);
                        }
                    }
                }
            }

            #endregion

            #region Completions Request (Should Not Be Affected by Embeddings Override)

            string completionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateCompletion);
            HttpMethod completionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateCompletion);

            body = Helpers.OllamaStreamingCompletionsRequestBody(TestEnvironment.CompletionsModel, "What is the capital of France?", false);
            ApiDetails completionsNotAffectedByOverride = new ApiDetails
            {
                Step = "Completions Request (Not Affected by Embeddings Override)",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(completionsUrl, completionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for completions request");
                    completionsNotAffectedByOverride.Response = null;
                    completionsNotAffectedByOverride.StatusCode = 0;
                    completionsNotAffectedByOverride.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(completionsNotAffectedByOverride);
                    return;
                }
                else
                {
                    completionsNotAffectedByOverride.Response = resp;
                    completionsNotAffectedByOverride.StatusCode = resp.StatusCode;
                    completionsNotAffectedByOverride.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for completions request: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(completionsNotAffectedByOverride);
                        return;
                    }
                    else
                    {
                        OllamaGenerateCompletionResult result = await Helpers.GetOllamaCompletionsResult(resp);
                        if (result == null || string.IsNullOrEmpty(result.Response))
                        {
                            Console.WriteLine("No completion response for completions request");
                            test.Success = false;
                            test.ApiDetails.Add(completionsNotAffectedByOverride);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Completions request succeeded as expected - embeddings model override should not affect completions");
                            test.Success = true;
                            test.ApiDetails.Add(completionsNotAffectedByOverride);
                        }
                    }
                }
            }

            #endregion

            #region Test Summary

            ApiDetails testSummary = new ApiDetails
            {
                Step = "Test 10 Summary",
                Request = "Tested embeddings requests with frontend model override (all-minilm -> nomic-embed-text)",
                Response = "Embeddings requests succeeded with model override, completions not affected",
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
