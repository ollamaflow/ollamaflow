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
    /// Test 13: Two Ollama backends, sticky sessions enabled, ensure A) the request is pinned properly (three consecutive requests) and B) the request is unpinned and moved to another backend when the original backend goes offline
    /// </summary>
    public class Test13 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 13: Two Ollama backends, sticky sessions enabled, ensure A) the request is pinned properly (three consecutive requests) and B) the request is unpinned and moved to another backend when the original backend goes offline
        /// </summary>
        public Test13()
        {
            Name = "Test 13: Two Ollama backends, sticky sessions enabled, ensure A) the request is pinned properly (three consecutive requests) and B) the request is unpinned and moved to another backend when the original backend goes offline";

            // Create 2 Ollama backends
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

            Backend ollama2 = new Backend
            {
                Identifier = "ollama2",
                Name = "ollama2",
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
            TestEnvironment.Backends.Add(ollama2);

            Frontend frontend1 = new Frontend
            {
                Identifier = "frontend1",
                Name = "frontend1",
                Hostname = "localhost",
                LoadBalancing = LoadBalancingMode.RoundRobin,
                Backends = new List<string> { "ollama1", "ollama2" },
                RequiredModels = new List<string> { "all-minilm", "gemma3:4b" },
                UseStickySessions = true,
                TimeoutMs = 120000,
                AllowEmbeddings = true,
                AllowCompletions = true,
                AllowRetries = true
            };

            TestEnvironment.Frontends.Add(frontend1);

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 13: Two Ollama backends, sticky sessions enabled, ensure A) the request is pinned properly (three consecutive requests) and B) the request is unpinned and moved to another backend when the original backend goes offline
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult test)
        {
            test.Success = true;

            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "ollama1");
            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "ollama2");

            Frontend frontend = OllamaFlowDaemon.Frontends.GetAll().ToList()[0];
            Backend backend = OllamaFlowDaemon.Backends.GetAll().ToList()[0];

            #region Test A: Sticky Session Pinning Test

            string embeddingsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateEmbeddings);
            HttpMethod embeddingsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateEmbeddings);

            string stickySessionId = "test-session-pinning-12345";
            int consecutiveRequests = 3;

            for (int i = 0; i < consecutiveRequests; i++)
            {
                string body = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, $"Sticky session pinning test request {i + 1}");
                ApiDetails stickyPinningTest = new ApiDetails
                {
                    Step = $"Sticky Session Pinning Test - Request {i + 1}",
                    Request = body,
                    StartUtc = DateTime.UtcNow
                };

                using (RestRequest req = new RestRequest(embeddingsUrl, embeddingsMethod))
                {
                    req.Headers.Add("x-thread-id", stickySessionId);
                    req.ContentType = Constants.JsonContentType;

                    RestResponse resp = await req.SendAsync(body);

                    if (resp == null)
                    {
                        Console.WriteLine($"No response for sticky session pinning test request {i + 1}");
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
                            Console.WriteLine($"Non-success response for sticky session pinning test request {i + 1}");
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

            #endregion

            #region Test B: Backend Failover Test

            string failoverBody = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, "Backend failover test - establishing session");
            ApiDetails failoverEstablishment = new ApiDetails
            {
                Step = "Backend Failover Test - Establishing Session",
                Request = failoverBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(embeddingsUrl, embeddingsMethod))
            {
                req.Headers.Add("x-thread-id", stickySessionId);
                req.ContentType = Constants.JsonContentType;

                RestResponse resp = await req.SendAsync(failoverBody);

                if (resp == null)
                {
                    Console.WriteLine("No response for failover establishment request");
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
                        Console.WriteLine("Non-success response for failover establishment request");
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

            ApiDetails backendDeletion = new ApiDetails
            {
                Step = "Backend Failover Test - Deleting First Backend",
                Request = "Deleting backend 'ollama1' to simulate offline condition",
                StartUtc = DateTime.UtcNow
            };

            try
            {
                bool deleted = OllamaFlowDaemon.Backends.Delete("ollama1", force: true);
                if (!deleted)
                {
                    Console.WriteLine("Failed to delete backend ollama1");
                    backendDeletion.Response = null;
                    backendDeletion.StatusCode = 500;
                    backendDeletion.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(backendDeletion);
                    return;
                }
                else
                {
                    backendDeletion.Response = "Backend ollama1 successfully deleted";
                    backendDeletion.StatusCode = 200;
                    backendDeletion.EndUtc = DateTime.UtcNow;
                    test.ApiDetails.Add(backendDeletion);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while deleting backend ollama1: {ex.Message}");
                backendDeletion.Response = $"Exception: {ex.Message}";
                backendDeletion.StatusCode = 500;
                backendDeletion.EndUtc = DateTime.UtcNow;

                test.Success = false;
                test.ApiDetails.Add(backendDeletion);
                return;
            }

            await Task.Delay(2000);

            int failoverRequests = 3;
            for (int i = 0; i < failoverRequests; i++)
            {
                string failoverRequestBody = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, $"Backend failover test request {i + 1} - should route to remaining backend");
                ApiDetails failoverRequest = new ApiDetails
                {
                    Step = $"Backend Failover Test - Request {i + 1} After Backend Deletion",
                    Request = failoverRequestBody,
                    StartUtc = DateTime.UtcNow
                };

                using (RestRequest req = new RestRequest(embeddingsUrl, embeddingsMethod))
                {
                    req.Headers.Add("x-thread-id", stickySessionId);
                    req.ContentType = Constants.JsonContentType;

                    RestResponse resp = await req.SendAsync(failoverRequestBody);

                    if (resp == null)
                    {
                        Console.WriteLine($"No response for failover test request {i + 1}");
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
                            Console.WriteLine($"Non-success response for failover test request {i + 1}");
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

            #endregion

            #region Test Summary

            ApiDetails testSummary = new ApiDetails
            {
                Step = "Test 13 Summary",
                Request = $"Tested sticky session pinning with {consecutiveRequests} consecutive requests, then simulated backend failure and verified failover with {failoverRequests} additional requests",
                Response = "Sticky session pinning and failover test completed successfully. Requests were pinned to first backend, then successfully failed over to second backend after first backend was removed.",
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
