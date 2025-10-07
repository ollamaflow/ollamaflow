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
    /// Test 9: Chat completions test against a single instance of vLLM where the frontend configuration has completions disabled
    /// </summary>
    public class Test9 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 9: Chat completions test against a single instance of vLLM where the frontend configuration has completions disabled
        /// </summary>
        public Test9()
        {
            Name = "Test 9: Chat completions test against a single instance of vLLM where the frontend configuration has completions disabled";

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
        /// Test 9: Chat completions test against a single instance of vLLM where the frontend configuration has completions disabled
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult test)
        {
            test.Success = true;

            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "vllm1");
            Frontend frontend = OllamaFlowDaemon.Frontends.GetAll().ToList()[0];
            Backend backend = OllamaFlowDaemon.Backends.GetAll().ToList()[0];

            #region Non-Streaming Chat Completions Request - Should Fail

            string chatCompletionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OpenAIGenerateChatCompletion);
            HttpMethod chatCompletionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OpenAIGenerateChatCompletion);

            List<OpenAIChatMessage> messages = new List<OpenAIChatMessage>
            {
                new OpenAIChatMessage { Role = "user", Content = "Hello, how are you? This is a test with completions disabled on frontend." }
            };

            string body = Helpers.OpenAIStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages, false);
            ApiDetails nonStreamingChatCompletionsDisabled = new ApiDetails
            {
                Step = "OpenAI Non-Streaming Chat Completions Request with Disabled Frontend",
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

            body = Helpers.OpenAIStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages, true);
            ApiDetails streamingChatCompletionsDisabled = new ApiDetails
            {
                Step = "OpenAI Streaming Chat Completions Request with Disabled Frontend",
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

            List<OpenAIChatMessage> multipleMessages = new List<OpenAIChatMessage>
            {
                new OpenAIChatMessage { Role = "user", Content = "What is the capital of France?" },
                new OpenAIChatMessage { Role = "assistant", Content = "The capital of France is Paris." },
                new OpenAIChatMessage { Role = "user", Content = "What is the population of Paris?" }
            };

            body = Helpers.OpenAIStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, multipleMessages, false);
            ApiDetails multipleMessagesChatCompletionsDisabled = new ApiDetails
            {
                Step = "OpenAI Multiple Messages Chat Completions Request with Disabled Frontend",
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

            #region System Message Chat Completions Request - Should Fail

            List<OpenAIChatMessage> systemMessages = new List<OpenAIChatMessage>
            {
                new OpenAIChatMessage { Role = "system", Content = "You are a helpful assistant that provides accurate information." },
                new OpenAIChatMessage { Role = "user", Content = "Tell me about artificial intelligence." }
            };

            body = Helpers.OpenAIStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, systemMessages, false);
            ApiDetails systemMessageChatCompletionsDisabled = new ApiDetails
            {
                Step = "OpenAI System Message Chat Completions Request with Disabled Frontend",
                Request = body,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(chatCompletionsUrl, chatCompletionsMethod))
            {
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(body);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for system message chat completions request with disabled frontend");
                    systemMessageChatCompletionsDisabled.Response = null;
                    systemMessageChatCompletionsDisabled.StatusCode = 0;
                    systemMessageChatCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(systemMessageChatCompletionsDisabled);
                    return;
                }
                else
                {
                    systemMessageChatCompletionsDisabled.Response = resp;
                    systemMessageChatCompletionsDisabled.StatusCode = resp.StatusCode;
                    systemMessageChatCompletionsDisabled.EndUtc = DateTime.UtcNow;

                    if (resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Unexpected success response for system message chat completions request with disabled frontend - should have been rejected");
                        test.Success = false;
                        test.ApiDetails.Add(systemMessageChatCompletionsDisabled);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Expected failure for system message chat completions request with disabled frontend: {resp.StatusCode}");
                        test.Success = true;
                        test.ApiDetails.Add(systemMessageChatCompletionsDisabled);
                    }
                }
            }

            #endregion

            #region Test Summary

            ApiDetails testSummary = new ApiDetails
            {
                Step = "Test 9 Summary",
                Request = "Tested chat completions requests with vLLM backend and frontend completions disabled",
                Response = "All chat completions requests correctly rejected",
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
