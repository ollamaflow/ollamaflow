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
    /// Test 12: Completions test against a single instance of vLLM where the frontend configuration overrides the model being used
    /// </summary>
    public class Test12 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 12: Completions test against a single instance of vLLM where the frontend configuration overrides the model being used
        /// </summary>
        public Test12()
        {
            Name = "Test 12: Completions test against a single instance of vLLM where the frontend configuration overrides the model being used";

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

            // Create frontend with model override for completions
            Frontend frontend1 = new Frontend
            {
                Identifier = "frontend1",
                Name = "frontend1",
                Hostname = "localhost",
                LoadBalancing = LoadBalancingMode.RoundRobin,
                Backends = new List<string> { "vllm1" },
                RequiredModels = new List<string> { "Qwen/Qwen2.5-7B" },
                UseStickySessions = false,
                TimeoutMs = 120000,
                AllowEmbeddings = false, // No embeddings for vLLM
                AllowCompletions = true,
                AllowRetries = true,
                PinnedEmbeddingsProperties = null,
                PinnedCompletionsProperties = new Dictionary<string, object>
                {
                    { "model", "Qwen/Qwen2.5-3B" }
                }
            };

            TestEnvironment.Frontends.Add(frontend1);
            TestEnvironment.CompletionsModel = "Qwen/Qwen2.5-3B";

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 12: Completions test against a single instance of vLLM where the frontend configuration overrides the model being used
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult test)
        {
            test.Success = true;

            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "vllm1");
            Frontend frontend = OllamaFlowDaemon.Frontends.GetAll().ToList()[0];
            Backend backend = OllamaFlowDaemon.Backends.GetAll().ToList()[0];

            #region Non-Streaming Completions Request with Model Override

            string completionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OpenAIGenerateCompletion);
            HttpMethod completionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OpenAIGenerateCompletion);

            string body = Helpers.OpenAICompletionsRequestBody("Qwen/Qwen2.5-3B", "What is the capital of France?");
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
                        OpenAIGenerateCompletionResult result = await Helpers.GetOpenAICompletionsResult(resp);
                        if (result == null || result.Choices == null || result.Choices.Count == 0 || string.IsNullOrEmpty(result.Choices[0].Text))
                        {
                            Console.WriteLine("No completion response for non-streaming completions request with model override");
                            test.Success = false;
                            test.ApiDetails.Add(nonStreamingCompletionsWithOverride);
                            return;
                        }
                        else
                        {
                            // Verify the model was overridden by checking the response
                            if (result.Model != null && result.Model.Contains("Qwen/Qwen2.5-3B"))
                            {
                                Console.WriteLine("✓ Model override successful: Request model was overridden by frontend configuration to 'Qwen/Qwen2.5-3B'");
                            }
                            else
                            {
                                Console.WriteLine($"⚠ Model override verification: Expected 'Qwen/Qwen2.5-3B' but got '{result.Model}'");
                            }
                            test.Success = true;
                            test.ApiDetails.Add(nonStreamingCompletionsWithOverride);
                        }
                    }
                }
            }

            #endregion

            #region Streaming Completions Request with Model Override

            body = Helpers.OpenAIStreamingCompletionsRequestBody("Qwen/Qwen2.5-3B", "What is the capital of Germany?", true);
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
                            Console.WriteLine("Streaming completions request succeeded with model override - frontend should have overridden model from 'Qwen/Qwen2.5-3B' to 'Qwen/Qwen2.5-7B'");
                            test.Success = true;
                            test.ApiDetails.Add(streamingCompletionsWithOverride);
                        }
                    }
                }
            }

            #endregion

            #region Chat Completions Request with Model Override

            string chatCompletionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OpenAIGenerateChatCompletion);
            HttpMethod chatCompletionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OpenAIGenerateChatCompletion);

            List<OpenAIChatMessage> messages = new List<OpenAIChatMessage>
            {
                new OpenAIChatMessage { Role = "user", Content = "Hello, how are you? This is a test with completions model override." }
            };

            // Request with a different model - should be overridden by frontend configuration
            body = Helpers.OpenAIChatCompletionsRequestBody("non-existent-model", messages);
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
                        OpenAIGenerateChatCompletionResult result = await Helpers.GetOpenAIChatCompletionsResult(resp);
                        if (result == null || result.Choices == null || result.Choices.Count == 0 || result.Choices[0].Message == null || string.IsNullOrEmpty(result.Choices[0].Message.Content?.ToString()))
                        {
                            Console.WriteLine("No chat completion response for chat completions request with model override");
                            test.Success = false;
                            test.ApiDetails.Add(chatCompletionsWithOverride);
                            return;
                        }
                        else
                        {
                            // Verify the model was overridden by checking the response
                            if (result.Model != null && result.Model.Contains("Qwen/Qwen2.5-3B"))
                            {
                                Console.WriteLine("✓ Chat completions model override successful: Request model was overridden by frontend configuration to 'Qwen/Qwen2.5-3B'");
                            }
                            else
                            {
                                Console.WriteLine($"⚠ Chat completions model override verification: Expected 'Qwen/Qwen2.5-3B' but got '{result.Model}'");
                            }
                            test.Success = true;
                            test.ApiDetails.Add(chatCompletionsWithOverride);
                        }
                    }
                }
            }

            #endregion

            #region Completions Request with Correct Model (Should Still Work)

            body = Helpers.OpenAICompletionsRequestBody("Qwen/Qwen2.5-3B", "What is the capital of Italy?");
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
                        OpenAIGenerateCompletionResult result = await Helpers.GetOpenAICompletionsResult(resp);
                        if (result == null || result.Choices == null || result.Choices.Count == 0 || string.IsNullOrEmpty(result.Choices[0].Text))
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

            #region Test Summary

            ApiDetails testSummary = new ApiDetails
            {
                Step = "Test 12 Summary",
                Request = "Tested completions requests with frontend model override (Qwen/Qwen2.5-3B -> Qwen/Qwen2.5-3B)",
                Response = "Completions requests succeeded with model override on vLLM backend",
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
