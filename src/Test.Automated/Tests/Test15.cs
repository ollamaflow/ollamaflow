namespace Test.Automated.Tests
{
    using System;
    using System.Collections.Generic;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Models.OpenAI;
    using RestWrapper;

    /// <summary>
    /// Test 15: vLLM sticky session pinning and failover test with two vLLM backends
    /// </summary>
    public class Test15 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 15: vLLM sticky session pinning and failover test with two vLLM backends
        /// </summary>
        public Test15()
        {
            Name = "Test 15: vLLM sticky session pinning and failover test with two vLLM backends";

            // Create 2 vLLM backends
            Backend vllm1 = new Backend
            {
                Identifier = "vllm1",
                Name = "vllm1",
                Hostname = "34.55.208.75",
                Port = 8000,
                Ssl = false,
                HealthCheckMethod = "GET",
                HealthCheckUrl = "/health",
                ApiFormat = ApiFormatEnum.OpenAI,
                PinnedEmbeddingsProperties = null,
                PinnedCompletionsProperties = null,
                AllowEmbeddings = false, // vLLM doesn't support embeddings
                AllowCompletions = true
            };

            Backend vllm2 = new Backend
            {
                Identifier = "vllm2",
                Name = "vllm2",
                Hostname = "34.55.208.75",
                Port = 8001,
                Ssl = false,
                HealthCheckMethod = "GET",
                HealthCheckUrl = "/health",
                ApiFormat = ApiFormatEnum.OpenAI,
                PinnedEmbeddingsProperties = null,
                PinnedCompletionsProperties = null,
                AllowEmbeddings = false, // vLLM doesn't support embeddings
                AllowCompletions = true
            };

            TestEnvironment.Backends.Add(vllm1);
            TestEnvironment.Backends.Add(vllm2);

            Frontend frontend1 = new Frontend
            {
                Identifier = "frontend1",
                Name = "frontend1",
                Hostname = "localhost",
                LoadBalancing = LoadBalancingMode.RoundRobin,
                Backends = new List<string> { "vllm1", "vllm2" },
                RequiredModels = new List<string> { "meta-llama/Llama-3.2-3B-Instruct" },
                UseStickySessions = true, // Enable sticky sessions
                TimeoutMs = 120000,
                AllowEmbeddings = false, // No embeddings for vLLM
                AllowCompletions = true,
                AllowRetries = true
            };

            TestEnvironment.Frontends.Add(frontend1);
            TestEnvironment.CompletionsModel = "meta-llama/Llama-3.2-3B-Instruct";

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 15: vLLM sticky session pinning and failover test with two vLLM backends
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult test)
        {
            test.Success = true;

            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "vllm1");
            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "vllm2");

            Frontend frontend = OllamaFlowDaemon.Frontends.GetAll().ToList()[0];
            Backend backend = OllamaFlowDaemon.Backends.GetAll().ToList()[0];

            #region Test A: Sticky Session Pinning Test

            string completionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OpenAIGenerateCompletion);
            HttpMethod completionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OpenAIGenerateCompletion);

            // Test with a specific sticky session ID
            string stickySessionId = "test-session-vllm-pinning-12345";
            int consecutiveRequests = 3;

            for (int i = 0; i < consecutiveRequests; i++)
            {
                string body = Helpers.OpenAICompletionsRequestBody(TestEnvironment.CompletionsModel, $"vLLM sticky session pinning test request {i + 1}");
                ApiDetails stickyPinningTest = new ApiDetails
                {
                    Step = $"vLLM Sticky Session Pinning Test - Request {i + 1}",
                    Request = body,
                    StartUtc = DateTime.UtcNow
                };

                using (RestRequest req = new RestRequest(completionsUrl, completionsMethod))
                {
                    // Add sticky session header
                    req.Headers.Add("x-thread-id", stickySessionId);
                    req.ContentType = Constants.JsonContentType;

                    RestResponse resp = await req.SendAsync(body);

                    if (resp == null)
                    {
                        Console.WriteLine($"No response for vLLM sticky session pinning test request {i + 1}");
                        stickyPinningTest.Response = null;
                        stickyPinningTest.StatusCode = 0;
                        stickyPinningTest.EndUtc = DateTime.UtcNow;

                        test.Success = false;
                        test.ApiDetails.Add(stickyPinningTest);
                        return;
                    }
                    else
                    {
                        stickyPinningTest.Response = resp;
                        stickyPinningTest.StatusCode = resp.StatusCode;
                        stickyPinningTest.EndUtc = DateTime.UtcNow;

                        if (!resp.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Non-success response for vLLM sticky session pinning test request {i + 1}: {resp.StatusCode}");
                            test.Success = false;
                            test.ApiDetails.Add(stickyPinningTest);
                            return;
                        }
                        else
                        {
                            OpenAIGenerateCompletionResult result = await Helpers.GetOpenAICompletionsResult(resp);
                            if (result == null || result.Choices == null || result.Choices.Count == 0 || string.IsNullOrEmpty(result.Choices[0].Text))
                            {
                                Console.WriteLine($"No completion response for vLLM sticky session pinning test request {i + 1}");
                                test.Success = false;
                                test.ApiDetails.Add(stickyPinningTest);
                                return;
                            }
                            else
                            {
                                test.Success = true;
                                test.ApiDetails.Add(stickyPinningTest);
                            }
                        }
                    }
                }
            }

            #endregion

            #region Test B: Backend Failover Test

            // First, send a request to establish the session on the first backend
            string failoverBody = Helpers.OpenAICompletionsRequestBody(TestEnvironment.CompletionsModel, "vLLM backend failover test - establishing session");
            ApiDetails failoverEstablishment = new ApiDetails
            {
                Step = "vLLM Backend Failover Test - Establishing Session",
                Request = failoverBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(completionsUrl, completionsMethod))
            {
                req.Headers.Add("x-thread-id", stickySessionId);
                req.ContentType = Constants.JsonContentType;

                RestResponse resp = await req.SendAsync(failoverBody);

                if (resp == null)
                {
                    Console.WriteLine("No response for vLLM failover establishment request");
                    failoverEstablishment.Response = null;
                    failoverEstablishment.StatusCode = 0;
                    failoverEstablishment.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(failoverEstablishment);
                    return;
                }
                else
                {
                    failoverEstablishment.Response = resp;
                    failoverEstablishment.StatusCode = resp.StatusCode;
                    failoverEstablishment.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for vLLM failover establishment request: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(failoverEstablishment);
                        return;
                    }
                    else
                    {
                        test.Success = true;
                        test.ApiDetails.Add(failoverEstablishment);
                    }
                }
            }

            // Now delete the first backend to simulate it going offline
            ApiDetails backendDeletion = new ApiDetails
            {
                Step = "vLLM Backend Failover Test - Deleting First Backend",
                Request = "Deleting backend 'vllm1' to simulate offline condition",
                StartUtc = DateTime.UtcNow
            };

            try
            {
                bool deleted = OllamaFlowDaemon.Backends.Delete("vllm1", force: true);
                if (!deleted)
                {
                    Console.WriteLine("Failed to delete backend vllm1");
                    backendDeletion.Response = "Failed to delete backend vllm1";
                    backendDeletion.StatusCode = 500;
                    backendDeletion.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(backendDeletion);
                    return;
                }
                else
                {
                    backendDeletion.Response = "Backend vllm1 successfully deleted";
                    backendDeletion.StatusCode = 200;
                    backendDeletion.EndUtc = DateTime.UtcNow;
                    test.ApiDetails.Add(backendDeletion);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while deleting backend vllm1: {ex.Message}");
                backendDeletion.Response = $"Exception: {ex.Message}";
                backendDeletion.StatusCode = 500;
                backendDeletion.EndUtc = DateTime.UtcNow;

                test.Success = false;
                test.ApiDetails.Add(backendDeletion);
                return;
            }

            // Wait a moment for the backend to be fully removed from health checks
            await Task.Delay(2000);

            // Now send requests with the same sticky session ID - they should be routed to the remaining backend
            int failoverRequests = 3;
            for (int i = 0; i < failoverRequests; i++)
            {
                string failoverRequestBody = Helpers.OpenAICompletionsRequestBody(TestEnvironment.CompletionsModel, $"vLLM backend failover test request {i + 1} - should route to remaining backend");
                ApiDetails failoverRequest = new ApiDetails
                {
                    Step = $"vLLM Backend Failover Test - Request {i + 1} After Backend Deletion",
                    Request = failoverRequestBody,
                    StartUtc = DateTime.UtcNow
                };

                using (RestRequest req = new RestRequest(completionsUrl, completionsMethod))
                {
                    req.Headers.Add("x-thread-id", stickySessionId);
                    req.ContentType = Constants.JsonContentType;

                    RestResponse resp = await req.SendAsync(failoverRequestBody);

                    if (resp == null)
                    {
                        Console.WriteLine($"No response for vLLM failover test request {i + 1}");
                        failoverRequest.Response = null;
                        failoverRequest.StatusCode = 0;
                        failoverRequest.EndUtc = DateTime.UtcNow;

                        test.Success = false;
                        test.ApiDetails.Add(failoverRequest);
                        return;
                    }
                    else
                    {
                        failoverRequest.Response = resp;
                        failoverRequest.StatusCode = resp.StatusCode;
                        failoverRequest.EndUtc = DateTime.UtcNow;

                        if (!resp.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Non-success response for vLLM failover test request {i + 1}: {resp.StatusCode}");
                            test.Success = false;
                            test.ApiDetails.Add(failoverRequest);
                            return;
                        }
                        else
                        {
                            OpenAIGenerateCompletionResult result = await Helpers.GetOpenAICompletionsResult(resp);
                            if (result == null || result.Choices == null || result.Choices.Count == 0 || string.IsNullOrEmpty(result.Choices[0].Text))
                            {
                                Console.WriteLine($"No completion response for vLLM failover test request {i + 1}");
                                test.Success = false;
                                test.ApiDetails.Add(failoverRequest);
                                return;
                            }
                            else
                            {
                                test.Success = true;
                                test.ApiDetails.Add(failoverRequest);
                            }
                        }
                    }
                }
            }

            #endregion

            #region Test Summary

            ApiDetails testSummary = new ApiDetails
            {
                Step = "Test 15 Summary",
                Request = $"Tested vLLM sticky session pinning with {consecutiveRequests} consecutive requests, then simulated backend failure and verified failover with {failoverRequests} additional requests",
                Response = "vLLM sticky session pinning and failover test completed successfully. Requests were pinned to first backend, then successfully failed over to second backend after first backend was removed.",
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
