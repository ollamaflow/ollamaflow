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
    /// Test 8: Completions test against a single instance of vLLM where the frontend configuration has completions disabled
    /// </summary>
    public class Test8 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 8: Completions test against a single instance of vLLM where the frontend configuration has completions disabled
        /// </summary>
        public Test8()
        {
            Name = "Test 8: Completions test against a single instance of vLLM where the frontend configuration has completions disabled";

            // Create single vLLM backend
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
                AllowCompletions = true // Backend allows completions
            };

            TestEnvironment.Backends.Add(vllm1);

            // Create frontend with completions disabled
            Frontend frontend1 = new Frontend
            {
                Identifier = "frontend1",
                Name = "frontend1",
                Hostname = "localhost",
                LoadBalancing = LoadBalancingMode.RoundRobin,
                Backends = new List<string> { "vllm1" },
                RequiredModels = new List<string> { "Qwen/Qwen2.5-3B" },
                UseStickySessions = false,
                TimeoutMs = 120000,
                AllowEmbeddings = false, // No embeddings for vLLM
                AllowCompletions = false, // Frontend has completions disabled
                AllowRetries = true
            };

            TestEnvironment.Frontends.Add(frontend1);
            TestEnvironment.CompletionsModel = "Qwen/Qwen2.5-3B";

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 8: Completions test against a single instance of vLLM where the frontend configuration has completions disabled
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult test)
        {
            test.Success = true;

            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "vllm1");
            Frontend frontend = OllamaFlowDaemon.Frontends.GetAll().ToList()[0];
            Backend backend = OllamaFlowDaemon.Backends.GetAll().ToList()[0];

            #region Non-Streaming Completions Request - Should Fail

            string completionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OpenAIGenerateCompletion);
            HttpMethod completionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OpenAIGenerateCompletion);

            string body = Helpers.OpenAIStreamingCompletionsRequestBody(TestEnvironment.CompletionsModel, "What is the capital of France?", false);
            ApiDetails nonStreamingCompletionsDisabled = new ApiDetails
            {
                Step = "OpenAI Non-Streaming Completions Request with Disabled Frontend",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(completionsUrl, completionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for non-streaming completions request with disabled frontend");
                    nonStreamingCompletionsDisabled.Response = null;
                    nonStreamingCompletionsDisabled.StatusCode = 0;
                    nonStreamingCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(nonStreamingCompletionsDisabled);
                    return;
                }
                else
                {
                    nonStreamingCompletionsDisabled.Response = resp;
                    nonStreamingCompletionsDisabled.StatusCode = resp.StatusCode;
                    nonStreamingCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    if (resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Unexpected success response for non-streaming completions request with disabled frontend - should have been rejected");
                        test.Success = false;
                        test.ApiDetails.Add(nonStreamingCompletionsDisabled);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Expected failure for non-streaming completions request with disabled frontend: {resp.StatusCode}");
                        test.Success = true;
                        test.ApiDetails.Add(nonStreamingCompletionsDisabled);
                    }
                }
            }

            #endregion

            #region Streaming Completions Request - Should Fail

            body = Helpers.OpenAIStreamingCompletionsRequestBody(TestEnvironment.CompletionsModel, "What is the capital of Germany?", true);
            ApiDetails streamingCompletionsDisabled = new ApiDetails
            {
                Step = "OpenAI Streaming Completions Request with Disabled Frontend",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(completionsUrl, completionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for streaming completions request with disabled frontend");
                    streamingCompletionsDisabled.Response = null;
                    streamingCompletionsDisabled.StatusCode = 0;
                    streamingCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(streamingCompletionsDisabled);
                    return;
                }
                else
                {
                    streamingCompletionsDisabled.Response = resp;
                    streamingCompletionsDisabled.StatusCode = resp.StatusCode;
                    streamingCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    if (resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Unexpected success response for streaming completions request with disabled frontend - should have been rejected");
                        test.Success = false;
                        test.ApiDetails.Add(streamingCompletionsDisabled);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Expected failure for streaming completions request with disabled frontend: {resp.StatusCode}");
                        test.Success = true;
                        test.ApiDetails.Add(streamingCompletionsDisabled);
                    }
                }
            }

            #endregion

            #region Chat Completions Request - Should Fail

            string chatCompletionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OpenAIGenerateChatCompletion);
            HttpMethod chatCompletionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OpenAIGenerateChatCompletion);

            List<OpenAIChatMessage> messages = new List<OpenAIChatMessage>
            {
                new OpenAIChatMessage { Role = "user", Content = "Hello, how are you? This is a test with completions disabled on frontend." }
            };

            body = Helpers.OpenAIStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages, false);
            ApiDetails chatCompletionsWithCompletionsDisabled = new ApiDetails
            {
                Step = "OpenAI Chat Completions Request with Completions Disabled on Frontend",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(chatCompletionsUrl, chatCompletionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for chat completions request with completions disabled on frontend");
                    chatCompletionsWithCompletionsDisabled.Response = null;
                    chatCompletionsWithCompletionsDisabled.StatusCode = 0;
                    chatCompletionsWithCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(chatCompletionsWithCompletionsDisabled);
                    return;
                }
                else
                {
                    chatCompletionsWithCompletionsDisabled.Response = resp;
                    chatCompletionsWithCompletionsDisabled.StatusCode = resp.StatusCode;
                    chatCompletionsWithCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    if (resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Unexpected success response for chat completions request with completions disabled on frontend - should have been rejected");
                        test.Success = false;
                        test.ApiDetails.Add(chatCompletionsWithCompletionsDisabled);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Expected failure for chat completions request with completions disabled on frontend: {resp.StatusCode}");
                        test.Success = true;
                        test.ApiDetails.Add(chatCompletionsWithCompletionsDisabled);
                    }
                }
            }

            #endregion

            #region Test Summary

            ApiDetails testSummary = new ApiDetails
            {
                Step = "Test 8 Summary",
                Request = "Tested completions requests with vLLM backend and frontend completions disabled",
                Response = "All completions and chat completions requests correctly rejected",
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
