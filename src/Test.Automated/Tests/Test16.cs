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
    /// Test 16: vLLM backend availability, service degradation, and recovery test with two vLLM backends
    /// </summary>
    public class Test16 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 16: vLLM backend availability, service degradation, and recovery test with two vLLM backends
        /// </summary>
        public Test16()
        {
            Name = "Test 16: vLLM backend availability, service degradation, and recovery test with two vLLM backends";

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
                UseStickySessions = false, // No sticky sessions for this test
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
        /// Test 16: vLLM backend availability, service degradation, and recovery test with two vLLM backends
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

            #region Phase 1: Test Completions with Both Backends Online

            string completionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OpenAIGenerateCompletion);
            HttpMethod completionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OpenAIGenerateCompletion);

            // Test completions
            string completionsBody = Helpers.OpenAICompletionsRequestBody(TestEnvironment.CompletionsModel, "vLLM test with both backends online");
            ApiDetails completionsTest = new ApiDetails
            {
                Step = "Phase 1: vLLM Completions Test with Both Backends Online",
                Request = completionsBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestResponse resp = await SendRestRequest<string>(completionsMethod, completionsUrl, completionsBody, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for vLLM completions test with both backends online");
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
                        Console.WriteLine($"Non-success response for vLLM completions test with both backends online: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(completionsTest);
                        return;
                    }
                    else
                    {
                        OpenAIGenerateCompletionResult result = await Helpers.GetOpenAICompletionsResult(resp);
                        if (result == null || result.Choices == null || result.Choices.Count == 0 || string.IsNullOrEmpty(result.Choices[0].Text))
                        {
                            Console.WriteLine("No completion response for vLLM completions test with both backends online");
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

            // Test chat completions
            string chatCompletionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OpenAIGenerateChatCompletion);
            HttpMethod chatCompletionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OpenAIGenerateChatCompletion);

            List<OpenAIChatMessage> messages = new List<OpenAIChatMessage>
            {
                new OpenAIChatMessage { Role = "user", Content = "Hello, how are you? vLLM test with both backends online." }
            };

            string chatCompletionsBody = Helpers.OpenAIChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages);
            ApiDetails chatCompletionsTest = new ApiDetails
            {
                Step = "Phase 1: vLLM Chat Completions Test with Both Backends Online",
                Request = chatCompletionsBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestResponse resp = await SendRestRequest<string>(chatCompletionsMethod, chatCompletionsUrl, chatCompletionsBody, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for vLLM chat completions test with both backends online");
                    chatCompletionsTest.Response = null;
                    chatCompletionsTest.StatusCode = 0;
                    chatCompletionsTest.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(chatCompletionsTest);
                    return;
                }
                else
                {
                    chatCompletionsTest.Response = resp;
                    chatCompletionsTest.StatusCode = resp.StatusCode;
                    chatCompletionsTest.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for vLLM chat completions test with both backends online: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(chatCompletionsTest);
                        return;
                    }
                    else
                    {
                        OpenAIGenerateChatCompletionResult result = await Helpers.GetOpenAIChatCompletionsResult(resp);
                        if (result == null || result.Choices == null || result.Choices.Count == 0 || result.Choices[0].Message == null || string.IsNullOrEmpty(result.Choices[0].Message?.Content?.ToString()))
                        {
                            Console.WriteLine("No chat completion response for vLLM chat completions test with both backends online");
                            test.Success = false;
                            test.ApiDetails.Add(chatCompletionsTest);
                            return;
                        }
                        else
                        {
                            test.Success = true;
                            test.ApiDetails.Add(chatCompletionsTest);
                        }
                    }
                }
            }

            #endregion

            #region Phase 2: Take All Backends Offline

            // Delete both backends to simulate them going offline
            ApiDetails backend1Deletion = new ApiDetails
            {
                Step = "Phase 2: Deleting vLLM Backend 1 (vllm1)",
                Request = "Deleting backend 'vllm1' to simulate offline condition",
                StartUtc = DateTime.UtcNow
            };

            try
            {
                bool deleted1 = OllamaFlowDaemon.Backends.Delete("vllm1", force: true);
                if (!deleted1)
                {
                    Console.WriteLine("Failed to delete backend vllm1");
                    backend1Deletion.Response = "Failed to delete backend vllm1";
                    backend1Deletion.StatusCode = 500;
                    backend1Deletion.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(backend1Deletion);
                    return;
                }
                else
                {
                    backend1Deletion.Response = "Backend vllm1 successfully deleted";
                    backend1Deletion.StatusCode = 200;
                    backend1Deletion.EndUtc = DateTime.UtcNow;
                    test.ApiDetails.Add(backend1Deletion);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while deleting backend vllm1: {ex.Message}");
                backend1Deletion.Response = $"Exception: {ex.Message}";
                backend1Deletion.StatusCode = 500;
                backend1Deletion.EndUtc = DateTime.UtcNow;

                test.Success = false;
                test.ApiDetails.Add(backend1Deletion);
                return;
            }

            ApiDetails backend2Deletion = new ApiDetails
            {
                Step = "Phase 2: Deleting vLLM Backend 2 (vllm2)",
                Request = "Deleting backend 'vllm2' to simulate offline condition",
                StartUtc = DateTime.UtcNow
            };

            try
            {
                bool deleted2 = OllamaFlowDaemon.Backends.Delete("vllm2", force: true);
                if (!deleted2)
                {
                    Console.WriteLine("Failed to delete backend vllm2");
                    backend2Deletion.Response = "Failed to delete backend vllm2";
                    backend2Deletion.StatusCode = 500;
                    backend2Deletion.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(backend2Deletion);
                    return;
                }
                else
                {
                    backend2Deletion.Response = "Backend vllm2 successfully deleted";
                    backend2Deletion.StatusCode = 200;
                    backend2Deletion.EndUtc = DateTime.UtcNow;
                    test.ApiDetails.Add(backend2Deletion);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while deleting backend vllm2: {ex.Message}");
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

            // Test completions with no backends available - should get 502
            string offlineCompletionsBody = Helpers.OpenAICompletionsRequestBody(TestEnvironment.CompletionsModel, "vLLM test with no backends online - should get 502");
            ApiDetails offlineCompletionsTest = new ApiDetails
            {
                Step = "Phase 3: vLLM Completions Test with All Backends Offline (Expect 502)",
                Request = offlineCompletionsBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestResponse resp = await SendRestRequest<string>(completionsMethod, completionsUrl, offlineCompletionsBody, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for vLLM completions test with all backends offline");
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
                        Console.WriteLine($"Expected 502 for vLLM completions test with all backends offline, got {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(offlineCompletionsTest);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Correctly received 502 for vLLM completions test with all backends offline");
                        test.Success = true;
                        test.ApiDetails.Add(offlineCompletionsTest);
                    }
                }
            }

            // Test chat completions with no backends available - should get 502
            string offlineChatCompletionsBody = Helpers.OpenAIChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages);
            ApiDetails offlineChatCompletionsTest = new ApiDetails
            {
                Step = "Phase 3: vLLM Chat Completions Test with All Backends Offline (Expect 502)",
                Request = offlineChatCompletionsBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestResponse resp = await SendRestRequest<string>(chatCompletionsMethod, chatCompletionsUrl, offlineChatCompletionsBody, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for vLLM chat completions test with all backends offline");
                    offlineChatCompletionsTest.Response = null;
                    offlineChatCompletionsTest.StatusCode = 0;
                    offlineChatCompletionsTest.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(offlineChatCompletionsTest);
                    return;
                }
                else
                {
                    offlineChatCompletionsTest.Response = resp;
                    offlineChatCompletionsTest.StatusCode = resp.StatusCode;
                    offlineChatCompletionsTest.EndUtc = DateTime.UtcNow;

                    if (resp.StatusCode != 502)
                    {
                        Console.WriteLine($"Expected 502 for vLLM chat completions test with all backends offline, got {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(offlineChatCompletionsTest);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Correctly received 502 for vLLM chat completions test with all backends offline");
                        test.Success = true;
                        test.ApiDetails.Add(offlineChatCompletionsTest);
                    }
                }
            }

            #endregion

            #region Phase 4: Bring One Backend Back Online

            // Recreate one backend to bring it back online
            Backend restoredBackend = new Backend
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

            ApiDetails backendRestoration = new ApiDetails
            {
                Step = "Phase 4: Restoring vLLM Backend 1 (vllm1)",
                Request = "Recreating backend 'vllm1' to bring it back online",
                StartUtc = DateTime.UtcNow
            };

            try
            {
                Backend createdBackend = OllamaFlowDaemon.Backends.Create(restoredBackend);
                if (createdBackend == null)
                {
                    Console.WriteLine("Failed to restore backend vllm1");
                    backendRestoration.Response = "Failed to restore backend vllm1";
                    backendRestoration.StatusCode = 500;
                    backendRestoration.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(backendRestoration);
                    return;
                }
                else
                {
                    backendRestoration.Response = "Backend vllm1 successfully restored";
                    backendRestoration.StatusCode = 200;
                    backendRestoration.EndUtc = DateTime.UtcNow;
                    test.ApiDetails.Add(backendRestoration);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while restoring backend vllm1: {ex.Message}");
                backendRestoration.Response = $"Exception: {ex.Message}";
                backendRestoration.StatusCode = 500;
                backendRestoration.EndUtc = DateTime.UtcNow;

                test.Success = false;
                test.ApiDetails.Add(backendRestoration);
                return;
            }

            // Wait for the backend to become healthy
            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "vllm1");

            #endregion

            #region Phase 5: Test Service Recovery

            // Test completions with one backend back online - should work
            string recoveryCompletionsBody = Helpers.OpenAICompletionsRequestBody(TestEnvironment.CompletionsModel, "vLLM test with one backend restored - should work");
            ApiDetails recoveryCompletionsTest = new ApiDetails
            {
                Step = "Phase 5: vLLM Completions Test with One Backend Restored (Expect Success)",
                Request = recoveryCompletionsBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestResponse resp = await SendRestRequest<string>(completionsMethod, completionsUrl, recoveryCompletionsBody, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for vLLM completions test with one backend restored");
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
                        Console.WriteLine($"Non-success response for vLLM completions test with one backend restored: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(recoveryCompletionsTest);
                        return;
                    }
                    else
                    {
                        OpenAIGenerateCompletionResult result = await Helpers.GetOpenAICompletionsResult(resp);
                        if (result == null || result.Choices == null || result.Choices.Count == 0 || string.IsNullOrEmpty(result.Choices[0].Text))
                        {
                            Console.WriteLine("No completion response for vLLM completions test with one backend restored");
                            test.Success = false;
                            test.ApiDetails.Add(recoveryCompletionsTest);
                            return;
                        }
                        else
                        {
                            Console.WriteLine($"Successfully received response for vLLM completions test with one backend restored: {resp.StatusCode}");
                            test.Success = true;
                            test.ApiDetails.Add(recoveryCompletionsTest);
                        }
                    }
                }
            }

            // Test chat completions with one backend back online - should work
            string recoveryChatCompletionsBody = Helpers.OpenAIChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages);
            ApiDetails recoveryChatCompletionsTest = new ApiDetails
            {
                Step = "Phase 5: vLLM Chat Completions Test with One Backend Restored (Expect Success)",
                Request = recoveryChatCompletionsBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestResponse resp = await SendRestRequest<string>(chatCompletionsMethod, chatCompletionsUrl, recoveryChatCompletionsBody, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for vLLM chat completions test with one backend restored");
                    recoveryChatCompletionsTest.Response = null;
                    recoveryChatCompletionsTest.StatusCode = 0;
                    recoveryChatCompletionsTest.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(recoveryChatCompletionsTest);
                    return;
                }
                else
                {
                    recoveryChatCompletionsTest.Response = resp;
                    recoveryChatCompletionsTest.StatusCode = resp.StatusCode;
                    recoveryChatCompletionsTest.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Non-success response for vLLM chat completions test with one backend restored: {resp.StatusCode}");
                        test.Success = false;
                        test.ApiDetails.Add(recoveryChatCompletionsTest);
                        return;
                    }
                    else
                    {
                        OpenAIGenerateChatCompletionResult result = await Helpers.GetOpenAIChatCompletionsResult(resp);
                        if (result == null || result.Choices == null || result.Choices.Count == 0 || result.Choices[0].Message == null || string.IsNullOrEmpty(result.Choices[0].Message?.Content?.ToString()))
                        {
                            Console.WriteLine("No chat completion response for vLLM chat completions test with one backend restored");
                            test.Success = false;
                            test.ApiDetails.Add(recoveryChatCompletionsTest);
                            return;
                        }
                        else
                        {
                            Console.WriteLine($"Successfully received chat completion response with one backend restored: {resp.StatusCode}");
                            test.Success = true;
                            test.ApiDetails.Add(recoveryChatCompletionsTest);
                        }
                    }
                }
            }

            #endregion

            #region Test Summary

            ApiDetails testSummary = new ApiDetails
            {
                Step = "Test 16 Summary",
                Request = "Tested vLLM backend availability, service degradation, and recovery scenarios",
                Response = "Successfully tested: 1) Completions and chat completions with both backends online, 2) 502 errors when all backends offline, 3) Service recovery when one backend restored",
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
