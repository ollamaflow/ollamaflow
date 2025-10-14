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
    /// Test 14: Backend availability, service degradation, and recovery test with two Ollama backends
    /// </summary>
    public class Test14 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 14: Backend availability, service degradation, and recovery test with two Ollama backends
        /// </summary>
        public Test14()
        {
            Name = "Test 14: Backend availability, service degradation, and recovery test with two Ollama backends";

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
                UseStickySessions = false, // No sticky sessions for this test
                TimeoutMs = 120000,
                AllowEmbeddings = true,
                AllowCompletions = true,
                AllowRetries = true
            };

            TestEnvironment.Frontends.Add(frontend1);

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 14: Backend availability, service degradation, and recovery test with two Ollama backends
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

            #region Phase 1: Test Embeddings and Completions with Both Backends Online

            string embeddingsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateEmbeddings);
            HttpMethod embeddingsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateEmbeddings);

            // Test embeddings
            string embeddingsBody = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, "Test embeddings with both backends online");
            ApiDetails embeddingsTest = new ApiDetails
            {
                Step = "Phase 1: Embeddings Test with Both Backends Online",
                Request = embeddingsBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestResponse resp = await SendRestRequest<string>(embeddingsMethod, embeddingsUrl, embeddingsBody, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for embeddings test with both backends online");
                    embeddingsTest.Response = null;
                    embeddingsTest.StatusCode = 0;
                    embeddingsTest.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(embeddingsTest);
                    return;
                }
                else
                {
                    embeddingsTest.Response = resp;
                    embeddingsTest.StatusCode = resp.StatusCode;
                    embeddingsTest.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for embeddings test with both backends online: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(embeddingsTest);
                        return;
                    }
                    else
                    {
                        test.Success = true;
                        test.ApiDetails.Add(embeddingsTest);
                    }
                }
            }

            // Test completions
            string completionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateCompletion);
            HttpMethod completionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateCompletion);

            string completionsBody = Helpers.OllamaStreamingCompletionsRequestBody(TestEnvironment.CompletionsModel, "What is the capital of France? Test with both backends online.", false);
            ApiDetails completionsTest = new ApiDetails
            {
                Step = "Phase 1: Completions Test with Both Backends Online",
                Request = completionsBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestResponse resp = await SendRestRequest<string>(completionsMethod, completionsUrl, completionsBody, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for completions test with both backends online");
                    completionsTest.Response = null;
                    completionsTest.StatusCode = 0;
                    completionsTest.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(completionsTest);
                    return;
                }
                else
                {
                    completionsTest.Response = resp;
                    completionsTest.StatusCode = resp.StatusCode;
                    completionsTest.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for completions test with both backends online: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(completionsTest);
                        return;
                    }
                    else
                    {
                        OllamaGenerateCompletionResult result = await Helpers.GetOllamaCompletionsResult(resp);
                        if (result == null || string.IsNullOrEmpty(result.Response))
                        {
                            Console.WriteLine("No completion response for completions test with both backends online");
                            test.Success = false;
                            test.ApiDetails.Add(completionsTest);
                            return;
                        }
                        else
                        {
                            test.Success = true;
                            test.ApiDetails.Add(completionsTest);
                        }
                    }
                }
            }

            #endregion

            #region Phase 2: Take All Backends Offline

            // Delete both backends to simulate them going offline
            ApiDetails backend1Deletion = new ApiDetails
            {
                Step = "Phase 2: Deleting Backend 1 (ollama1)",
                Request = "Deleting backend 'ollama1' to simulate offline condition",
                StartUtc = DateTime.UtcNow
            };

            try
            {
                bool deleted1 = OllamaFlowDaemon.Backends.Delete("ollama1", force: true);
                if (!deleted1)
                {
                    Console.WriteLine("Failed to delete backend ollama1");
                    backend1Deletion.Response = "Failed to delete backend ollama1";
                    backend1Deletion.StatusCode = 500;
                    backend1Deletion.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(backend1Deletion);
                    return;
                }
                else
                {
                    backend1Deletion.Response = "Backend ollama1 successfully deleted";
                    backend1Deletion.StatusCode = 200;
                    backend1Deletion.EndUtc = DateTime.UtcNow;
                    test.ApiDetails.Add(backend1Deletion);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while deleting backend ollama1: {ex.Message}");
                backend1Deletion.Response = $"Exception: {ex.Message}";
                backend1Deletion.StatusCode = 500;
                backend1Deletion.EndUtc = DateTime.UtcNow;

                test.Success = false;
                test.ApiDetails.Add(backend1Deletion);
                return;
            }

            ApiDetails backend2Deletion = new ApiDetails
            {
                Step = "Phase 2: Deleting Backend 2 (ollama2)",
                Request = "Deleting backend 'ollama2' to simulate offline condition",
                StartUtc = DateTime.UtcNow
            };

            try
            {
                bool deleted2 = OllamaFlowDaemon.Backends.Delete("ollama2", force: true);
                if (!deleted2)
                {
                    Console.WriteLine("Failed to delete backend ollama2");
                    backend2Deletion.Response = "Failed to delete backend ollama2";
                    backend2Deletion.StatusCode = 500;
                    backend2Deletion.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(backend2Deletion);
                    return;
                }
                else
                {
                    backend2Deletion.Response = "Backend ollama2 successfully deleted";
                    backend2Deletion.StatusCode = 200;
                    backend2Deletion.EndUtc = DateTime.UtcNow;
                    test.ApiDetails.Add(backend2Deletion);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while deleting backend ollama2: {ex.Message}");
                backend2Deletion.Response = $"Exception: {ex.Message}";
                backend2Deletion.StatusCode = 500;
                backend2Deletion.EndUtc = DateTime.UtcNow;

                test.Success = false;
                test.ApiDetails.Add(backend2Deletion);
                return;
            }

            // Wait for backends to be fully removed from health checks
            await Task.Delay(3000);

            #endregion

            #region Phase 3: Test 502 Errors with All Backends Offline

            // Test embeddings with no backends available - should get 502
            string offlineEmbeddingsBody = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, "Test embeddings with no backends online - should get 502");
            ApiDetails offlineEmbeddingsTest = new ApiDetails
            {
                Step = "Phase 3: Embeddings Test with All Backends Offline (Expect 502)",
                Request = offlineEmbeddingsBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestResponse resp = await SendRestRequest<string>(embeddingsMethod, embeddingsUrl, offlineEmbeddingsBody, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for embeddings test with all backends offline");
                    offlineEmbeddingsTest.Response = null;
                    offlineEmbeddingsTest.StatusCode = 0;
                    offlineEmbeddingsTest.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(offlineEmbeddingsTest);
                    return;
                }
                else
                {
                    offlineEmbeddingsTest.Response = resp;
                    offlineEmbeddingsTest.StatusCode = resp.StatusCode;
                    offlineEmbeddingsTest.EndUtc = DateTime.UtcNow;

                    if (resp.StatusCode != 502)
                    {
                        Console.WriteLine($"Expected 502 for embeddings test with all backends offline, got {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(offlineEmbeddingsTest);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Correctly received 502 for embeddings test with all backends offline");
                        test.Success = true;
                        test.ApiDetails.Add(offlineEmbeddingsTest);
                    }
                }
            }

            // Test completions with no backends available - should get 502
            string offlineCompletionsBody = Helpers.OllamaStreamingCompletionsRequestBody(TestEnvironment.CompletionsModel, "What is the capital of France? Test with no backends online - should get 502.", false);
            ApiDetails offlineCompletionsTest = new ApiDetails
            {
                Step = "Phase 3: Completions Test with All Backends Offline (Expect 502)",
                Request = offlineCompletionsBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestResponse resp = await SendRestRequest<string>(completionsMethod, completionsUrl, offlineCompletionsBody, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for completions test with all backends offline");
                    offlineCompletionsTest.Response = null;
                    offlineCompletionsTest.StatusCode = 0;
                    offlineCompletionsTest.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(offlineCompletionsTest);
                    return;
                }
                else
                {
                    offlineCompletionsTest.Response = resp;
                    offlineCompletionsTest.StatusCode = resp.StatusCode;
                    offlineCompletionsTest.EndUtc = DateTime.UtcNow;

                    if (resp.StatusCode != 502)
                    {
                        Console.WriteLine($"Expected 502 for completions test with all backends offline, got {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(offlineCompletionsTest);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Correctly received 502 for completions test with all backends offline");
                        test.Success = true;
                        test.ApiDetails.Add(offlineCompletionsTest);
                    }
                }
            }

            #endregion

            #region Phase 4: Bring One Backend Back Online

            // Recreate one backend to bring it back online
            Backend restoredBackend = new Backend
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

            ApiDetails backendRestoration = new ApiDetails
            {
                Step = "Phase 4: Restoring Backend 1 (ollama1)",
                Request = "Recreating backend 'ollama1' to bring it back online",
                StartUtc = DateTime.UtcNow
            };

            try
            {
                Backend createdBackend = OllamaFlowDaemon.Backends.Create(restoredBackend);
                if (createdBackend == null)
                {
                    Console.WriteLine("Failed to restore backend ollama1");
                    backendRestoration.Response = "Failed to restore backend ollama1";
                    backendRestoration.StatusCode = 500;
                    backendRestoration.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(backendRestoration);
                    return;
                }
                else
                {
                    backendRestoration.Response = "Backend ollama1 successfully restored";
                    backendRestoration.StatusCode = 200;
                    backendRestoration.EndUtc = DateTime.UtcNow;
                    test.ApiDetails.Add(backendRestoration);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while restoring backend ollama1: {ex.Message}");
                backendRestoration.Response = $"Exception: {ex.Message}";
                backendRestoration.StatusCode = 500;
                backendRestoration.EndUtc = DateTime.UtcNow;

                test.Success = false;
                test.ApiDetails.Add(backendRestoration);
                return;
            }

            // Wait for the backend to become healthy
            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "ollama1");

            #endregion

            #region Phase 5: Test Service Recovery

            // Test embeddings with one backend back online - should work
            string recoveryEmbeddingsBody = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, "Test embeddings with one backend restored - should work");
            ApiDetails recoveryEmbeddingsTest = new ApiDetails
            {
                Step = "Phase 5: Embeddings Test with One Backend Restored (Expect Success)",
                Request = recoveryEmbeddingsBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestResponse resp = await SendRestRequest<string>(embeddingsMethod, embeddingsUrl, recoveryEmbeddingsBody, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for embeddings test with one backend restored");
                    recoveryEmbeddingsTest.Response = null;
                    recoveryEmbeddingsTest.StatusCode = 0;
                    recoveryEmbeddingsTest.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(recoveryEmbeddingsTest);
                    return;
                }
                else
                {
                    recoveryEmbeddingsTest.Response = resp;
                    recoveryEmbeddingsTest.StatusCode = resp.StatusCode;
                    recoveryEmbeddingsTest.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for embeddings test with one backend restored: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(recoveryEmbeddingsTest);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Successfully received response for embeddings test with one backend restored: {resp.StatusCode}");
                        test.Success = true;
                        test.ApiDetails.Add(recoveryEmbeddingsTest);
                    }
                }
            }

            // Test completions with one backend back online - should work
            string recoveryCompletionsBody = Helpers.OllamaStreamingCompletionsRequestBody(TestEnvironment.CompletionsModel, "What is the capital of France? Test with one backend restored - should work.", false);
            ApiDetails recoveryCompletionsTest = new ApiDetails
            {
                Step = "Phase 5: Completions Test with One Backend Restored (Expect Success)",
                Request = recoveryCompletionsBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestResponse resp = await SendRestRequest<string>(completionsMethod, completionsUrl, recoveryCompletionsBody, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for completions test with one backend restored");
                    recoveryCompletionsTest.Response = null;
                    recoveryCompletionsTest.StatusCode = 0;
                    recoveryCompletionsTest.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(recoveryCompletionsTest);
                    return;
                }
                else
                {
                    recoveryCompletionsTest.Response = resp;
                    recoveryCompletionsTest.StatusCode = resp.StatusCode;
                    recoveryCompletionsTest.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for completions test with one backend restored: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(recoveryCompletionsTest);
                        return;
                    }
                    else
                    {
                        OllamaGenerateCompletionResult result = await Helpers.GetOllamaCompletionsResult(resp);
                        if (result == null || string.IsNullOrEmpty(result.Response))
                        {
                            Console.WriteLine("No completion response for completions test with one backend restored");
                            test.Success = false;
                            test.ApiDetails.Add(recoveryCompletionsTest);
                            return;
                        }
                        else
                        {
                            Console.WriteLine($"Successfully received completion response with one backend restored: {resp.StatusCode}");
                            test.Success = true;
                            test.ApiDetails.Add(recoveryCompletionsTest);
                        }
                    }
                }
            }

            #endregion

            #region Test Summary

            ApiDetails testSummary = new ApiDetails
            {
                Step = "Test 14 Summary",
                Request = "Tested backend availability, service degradation, and recovery scenarios",
                Response = "Successfully tested: 1) Embeddings and completions with both backends online, 2) 502 errors when all backends offline, 3) Service recovery when one backend restored",
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
