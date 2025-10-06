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
    /// Test 2: vLLM backend, OpenAI APIs, completions and chat completions test (no embeddings)
    /// </summary>
    public class Test2 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 2: vLLM backend, OpenAI APIs, completions and chat completions test (no embeddings)
        /// </summary>
        public Test2()
        {
            Name = "Test 2: vLLM backend, OpenAI APIs, completions and chat completions test (no embeddings)";

            Backend vllm1 = new Backend
            {
                Identifier = "vllm1",
                Name = "vllm1",
                Hostname = "localhost",
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

            Frontend frontend1 = new Frontend
            {
                Identifier = "frontend1",
                Name = "frontend1",
                Hostname = "localhost",
                LoadBalancing = LoadBalancingMode.RoundRobin,
                Backends = new List<string> { "vllm1" },
                RequiredModels = new List<string> { "Qwen/Qwen2.5-3B" },
                UseStickySessions = false,
                AllowEmbeddings = false, // No embeddings for vLLM
                AllowCompletions = true,
                AllowRetries = true,
                TimeoutMs = 120000,
            };

            TestEnvironment.Frontends.Add(frontend1);

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 2: vLLM backend, OpenAI APIs, completions and chat completions test (no embeddings)
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult test)
        {
            test.Success = true; // default to true

            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "vllm1");
            Frontend frontend = OllamaFlowDaemon.Frontends.GetAll().ToList()[0];
            Backend backend = OllamaFlowDaemon.Backends.GetAll().ToList()[0];

            #region Completions

            string completionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OpenAIGenerateCompletion);
            HttpMethod completionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OpenAIGenerateCompletion);

            #region Non-Streaming-Completions

            string body = Helpers.OpenAICompletionsRequestBody(TestEnvironment.CompletionsModel, "What is the capital of France?");
            ApiDetails nonStreamingCompletions = new ApiDetails
            {
                Step = "OpenAI Non-Streaming Completions",
                Request = body
            };

            using (RestResponse resp = await SendRestRequest<string>(completionsMethod, completionsUrl, body, Constants.JsonContentType))
            {
                OpenAIGenerateCompletionResult result = await Helpers.GetOpenAICompletionsResult(resp);
                if (result == null)
                {
                    Console.WriteLine("No response for non-streaming completions request");
                    nonStreamingCompletions.Response = null;
                    nonStreamingCompletions.StatusCode = 0;
                    nonStreamingCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(nonStreamingCompletions);
                    return;
                }
                else
                {
                    nonStreamingCompletions.Response = resp;
                    nonStreamingCompletions.StatusCode = resp.StatusCode;
                    nonStreamingCompletions.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for non-streaming completions request");
                        test.Success = false;
                        test.ApiDetails.Add(nonStreamingCompletions);
                        return;
                    }
                    else
                    {
                        if (result.Choices == null || result.Choices.Count == 0 || String.IsNullOrEmpty(result.Choices[0].Text))
                        {
                            Console.WriteLine("No completion text returned for non-streaming completions request");
                            test.Success = false;
                            test.ApiDetails.Add(nonStreamingCompletions);
                            return;
                        }
                        else
                        {
                            test.Success = true;
                            test.ApiDetails.Add(nonStreamingCompletions);
                        }
                    }
                }
            }

            #endregion

            #region Streaming-Completions

            body = Helpers.OpenAIStreamingCompletionsRequestBody(TestEnvironment.CompletionsModel, "What is the capital of Germany?", true);
            ApiDetails streamingCompletions = new ApiDetails
            {
                Step = "OpenAI Streaming Completions",
                Request = body
            };

            using (RestResponse resp = await SendRestRequest<string>(completionsMethod, completionsUrl, body, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for streaming completions request");
                    streamingCompletions.Response = null;
                    streamingCompletions.StatusCode = 0;
                    streamingCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(streamingCompletions);
                    return;
                }
                else
                {
                    streamingCompletions.Response = resp;
                    streamingCompletions.StatusCode = resp.StatusCode;
                    streamingCompletions.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for streaming completions request");
                        test.Success = false;
                        test.ApiDetails.Add(streamingCompletions);
                        return;
                    }
                    else
                    {
                        if (!resp.ServerSentEvents)
                        {
                            Console.WriteLine("Expected server-sent events for streaming completions request");
                            test.Success = false;
                            test.ApiDetails.Add(streamingCompletions);
                            return;
                        }
                        else
                        {
                            test.Success = true;
                            test.ApiDetails.Add(streamingCompletions);
                        }
                    }
                }
            }

            #endregion

            #endregion

            #region Chat Completions

            string chatCompletionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OpenAIGenerateChatCompletion);
            HttpMethod chatCompletionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OpenAIGenerateChatCompletion);

            #region Non-Streaming-Chat-Completions

            List<OpenAIChatMessage> messages = new List<OpenAIChatMessage>
            {
                new OpenAIChatMessage { Role = "user", Content = "Hello, how are you?" }
            };

            body = Helpers.OpenAIChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages);
            ApiDetails nonStreamingChatCompletions = new ApiDetails
            {
                Step = "OpenAI Non-Streaming Chat Completions",
                Request = body
            };

            using (RestResponse resp = await SendRestRequest<string>(chatCompletionsMethod, chatCompletionsUrl, body, Constants.JsonContentType))
            {
                OpenAIGenerateChatCompletionResult result = await Helpers.GetOpenAIChatCompletionsResult(resp);
                if (result == null)
                {
                    Console.WriteLine("No response for non-streaming chat completions request");
                    nonStreamingChatCompletions.Response = null;
                    nonStreamingChatCompletions.StatusCode = 0;
                    nonStreamingChatCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(nonStreamingChatCompletions);
                    return;
                }
                else
                {
                    nonStreamingChatCompletions.Response = resp;
                    nonStreamingChatCompletions.StatusCode = resp.StatusCode;
                    nonStreamingChatCompletions.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for non-streaming chat completions request");
                        test.Success = false;
                        test.ApiDetails.Add(nonStreamingChatCompletions);
                        return;
                    }
                    else
                    {
                        if (result.Choices == null || result.Choices.Count == 0 || result.Choices[0].Message == null || String.IsNullOrEmpty(result.Choices[0].Message?.Content?.ToString()))
                        {
                            Console.WriteLine("No message content returned for non-streaming chat completions request");
                            test.Success = false;
                            test.ApiDetails.Add(nonStreamingChatCompletions);
                            return;
                        }
                        else
                        {
                            test.Success = true;
                            test.ApiDetails.Add(nonStreamingChatCompletions);
                        }
                    }
                }
            }

            #endregion
            
            #region Streaming-Chat-Completions

            body = Helpers.OpenAIStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages, true);
            ApiDetails streamingChatCompletions = new ApiDetails
            {
                Step = "OpenAI Streaming Chat Completions",
                Request = body
            };

            using (RestResponse resp = await SendRestRequest<string>(chatCompletionsMethod, chatCompletionsUrl, body, Constants.JsonContentType))
            {
                if (resp == null)
                {
                    Console.WriteLine("No response for streaming chat completions request");
                    streamingChatCompletions.Response = null;
                    streamingChatCompletions.StatusCode = 0;
                    streamingChatCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(streamingChatCompletions);
                    return;
                }
                else
                {
                    streamingChatCompletions.Response = resp;
                    streamingChatCompletions.StatusCode = resp.StatusCode;
                    streamingChatCompletions.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for streaming chat completions request");
                        test.Success = false;
                        test.ApiDetails.Add(streamingChatCompletions);
                        return;
                    }
                    else
                    {
                        if (!resp.ServerSentEvents)
                        {
                            Console.WriteLine("Expected server-sent events for streaming chat completions request");
                            test.Success = false;
                            test.ApiDetails.Add(streamingChatCompletions);
                            return;
                        }
                        else
                        {
                            test.Success = true;
                            test.ApiDetails.Add(streamingChatCompletions);
                        }
                    }
                }
            }

            #endregion

            #endregion
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
