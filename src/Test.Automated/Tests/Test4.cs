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
    /// Test 4: Load-balancing test against four instances of Ollama, with sticky sessions
    /// </summary>
    public class Test4 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 4: Load-balancing test against four instances of Ollama, with sticky sessions
        /// </summary>
        public Test4()
        {
            Name = "Test 4: Load-balancing test against four instances of Ollama, with sticky sessions";

            // Create 4 Ollama backends
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

            Backend ollama3 = new Backend
            {
                Identifier = "ollama3",
                Name = "ollama3",
                Hostname = "localhost",
                Port = 11436,
                Ssl = false,
                HealthCheckMethod = "HEAD",
                HealthCheckUrl = "/",
                ApiFormat = ApiFormatEnum.Ollama,
                PinnedEmbeddingsProperties = null,
                PinnedCompletionsProperties = null,
                AllowEmbeddings = true,
                AllowCompletions = true
            };

            Backend ollama4 = new Backend
            {
                Identifier = "ollama4",
                Name = "ollama4",
                Hostname = "localhost",
                Port = 11437,
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
            TestEnvironment.Backends.Add(ollama3);
            TestEnvironment.Backends.Add(ollama4);

            Frontend frontend1 = new Frontend
            {
                Identifier = "frontend1",
                Name = "frontend1",
                Hostname = "localhost",
                LoadBalancing = LoadBalancingMode.RoundRobin,
                Backends = new List<string> { "ollama1", "ollama2", "ollama3", "ollama4" },
                RequiredModels = new List<string> { "all-minilm", "gemma3:4b" },
                UseStickySessions = true, // Enable sticky sessions
                TimeoutMs = 120000,
                AllowEmbeddings = true,
                AllowCompletions = true,
                AllowRetries = true
            };

            TestEnvironment.Frontends.Add(frontend1);

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 4: Load-balancing test against four instances of Ollama, with sticky sessions
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult test)
        {
            test.Success = true; 

            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "ollama1");
            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "ollama2");
            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "ollama3");
            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "ollama4");

            Frontend frontend = OllamaFlowDaemon.Frontends.GetAll().ToList()[0];
            Backend backend = OllamaFlowDaemon.Backends.GetAll().ToList()[0];

            #region Sticky Session Test - Embeddings

            string embeddingsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateEmbeddings);
            HttpMethod embeddingsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateEmbeddings);

            // Test with a specific sticky session ID
            string stickySessionId = "test-session-12345";
            Dictionary<string, string> stickyHeaders = new Dictionary<string, string>
            {
                { "x-thread-id", stickySessionId }
            };

            int totalEmbeddingRequests = 5;
            for (int i = 0; i < totalEmbeddingRequests; i++)
            {
                string body = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, $"sticky session test request {i + 1}");
                ApiDetails stickyEmbeddings = new ApiDetails
                {
                    Step = $"Sticky Session Embeddings Request {i + 1}",
                    Request = body,
                    StartUtc = DateTime.UtcNow
                };

                using (RestRequest req = new RestRequest(embeddingsUrl, embeddingsMethod))
                {
                    // Add sticky session header
                    req.Headers.Add("x-thread-id", stickySessionId);
                    req.ContentType = Constants.JsonContentType;
                    
                    RestResponse resp = await req.SendAsync(body);
                    
                    if (resp == null)
                    {
                        Console.WriteLine($"No response for sticky session embeddings request {i + 1}");
                        stickyEmbeddings.Response = null;
                        stickyEmbeddings.StatusCode = 0;
                        stickyEmbeddings.EndUtc = DateTime.UtcNow;

                        test.Success = false;
                        test.ApiDetails.Add(stickyEmbeddings);
                        return;
                    }
                    else
                    {
                        stickyEmbeddings.Response = resp;
                        stickyEmbeddings.StatusCode = resp.StatusCode;
                        stickyEmbeddings.EndUtc = DateTime.UtcNow;

                        if (!resp.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Non-success response for sticky session embeddings request {i + 1}");
                            test.Success = false;
                            test.ApiDetails.Add(stickyEmbeddings);
                            return;
                        }
                        else
                        {
                            test.Success = true;
                            test.ApiDetails.Add(stickyEmbeddings);
                        }
                    }
                }
            }

            #endregion

            #region Sticky Session Test - Completions

            string completionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateCompletion);
            HttpMethod completionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateCompletion);

            int totalCompletionRequests = 3;
            for (int i = 0; i < totalCompletionRequests; i++)
            {
                string body = Helpers.OllamaStreamingCompletionsRequestBody(TestEnvironment.CompletionsModel, $"What is the capital of France? Sticky session request {i + 1}", false);
                ApiDetails stickyCompletions = new ApiDetails
                {
                    Step = $"Sticky Session Completions Request {i + 1}",
                    Request = body,
                    StartUtc = DateTime.UtcNow
                };

                try
                {
                    using (RestRequest req = new RestRequest(completionsUrl, completionsMethod))
                    {
                        // Add sticky session header
                        req.Headers.Add("x-thread-id", stickySessionId);
                        req.ContentType = Constants.JsonContentType;
                        
                        RestResponse resp = await req.SendAsync(body);
                        
                        if (resp == null)
                        {
                            Console.WriteLine($"No response for sticky session completions request {i + 1}");
                            stickyCompletions.Response = null;
                            stickyCompletions.StatusCode = 0;
                            stickyCompletions.EndUtc = DateTime.UtcNow;

                            test.Success = false;
                            test.ApiDetails.Add(stickyCompletions);
                            return;
                        }
                        else
                        {
                            stickyCompletions.Response = resp;
                            stickyCompletions.StatusCode = resp.StatusCode;
                            stickyCompletions.EndUtc = DateTime.UtcNow;

                            if (!resp.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Non-success response for sticky session completions request {i + 1}");
                                test.Success = false;
                                test.ApiDetails.Add(stickyCompletions);
                                return;
                            }
                            else
                            {
                                OllamaGenerateCompletionResult result = await Helpers.GetOllamaCompletionsResult(resp);
                                if (result == null || string.IsNullOrEmpty(result.Response))
                                {
                                    Console.WriteLine($"No completion response for sticky session completions request {i + 1}");
                                    test.Success = false;
                                    test.ApiDetails.Add(stickyCompletions);
                                    return;
                                }
                                else
                                {
                                    test.Success = true;
                                    test.ApiDetails.Add(stickyCompletions);
                                }
                            }
                        }
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    Console.WriteLine($"Timeout for sticky session completions request {i + 1}: {ex.Message}");
                    stickyCompletions.Response = null;
                    stickyCompletions.StatusCode = 408;
                    stickyCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(stickyCompletions);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error for sticky session completions request {i + 1}: {ex.Message}");
                    stickyCompletions.Response = null;
                    stickyCompletions.StatusCode = 500;
                    stickyCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(stickyCompletions);
                    return;
                }
            }

            #endregion

            #region Sticky Session Test - Chat Completions

            string chatCompletionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateChatCompletion);
            HttpMethod chatCompletionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateChatCompletion);

            List<OllamaChatMessage> messages = new List<OllamaChatMessage>
            {
                new OllamaChatMessage { Role = "user", Content = "Hello, how are you? This is a sticky session test." }
            };

            int totalChatRequests = 2;
            for (int i = 0; i < totalChatRequests; i++)
            {
                string body = Helpers.OllamaStreamingChatCompletionsRequestBody(TestEnvironment.CompletionsModel, messages, false);
                ApiDetails stickyChatCompletions = new ApiDetails
                {
                    Step = $"Sticky Session Chat Completions Request {i + 1}",
                    Request = body,
                    StartUtc = DateTime.UtcNow
                };

                try
                {
                    using (RestRequest req = new RestRequest(chatCompletionsUrl, chatCompletionsMethod))
                    {
                        // Add sticky session header
                        req.Headers.Add("x-thread-id", stickySessionId);
                        req.ContentType = Constants.JsonContentType;
                        
                        RestResponse resp = await req.SendAsync(body);
                        
                        if (resp == null)
                        {
                            Console.WriteLine($"No response for sticky session chat completions request {i + 1}");
                            stickyChatCompletions.Response = null;
                            stickyChatCompletions.StatusCode = 0;
                            stickyChatCompletions.EndUtc = DateTime.UtcNow;

                            test.Success = false;
                            test.ApiDetails.Add(stickyChatCompletions);
                            return;
                        }
                        else
                        {
                            stickyChatCompletions.Response = resp;
                            stickyChatCompletions.StatusCode = resp.StatusCode;
                            stickyChatCompletions.EndUtc = DateTime.UtcNow;

                            if (!resp.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Non-success response for sticky session chat completions request {i + 1}");
                                test.Success = false;
                                test.ApiDetails.Add(stickyChatCompletions);
                                return;
                            }
                            else
                            {
                                OllamaGenerateChatCompletionResult result = await Helpers.GetOllamaChatCompletionsResult(resp);
                                if (result == null || result.Message == null || string.IsNullOrEmpty(result.Message.Content))
                                {
                                    Console.WriteLine($"No chat completion response for sticky session chat completions request {i + 1}");
                                    test.Success = false;
                                    test.ApiDetails.Add(stickyChatCompletions);
                                    return;
                                }
                                else
                                {
                                    test.Success = true;
                                    test.ApiDetails.Add(stickyChatCompletions);
                                }
                            }
                        }
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    Console.WriteLine($"Timeout for sticky session chat completions request {i + 1}: {ex.Message}");
                    stickyChatCompletions.Response = null;
                    stickyChatCompletions.StatusCode = 408;
                    stickyChatCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(stickyChatCompletions);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error for sticky session chat completions request {i + 1}: {ex.Message}");
                    stickyChatCompletions.Response = null;
                    stickyChatCompletions.StatusCode = 500;
                    stickyChatCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(stickyChatCompletions);
                    return;
                }
            }

            #endregion

            #region Different Session Test

            string differentStickySessionId = "test-session-67890";  
            string differentBody = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, "Different session test");
            ApiDetails differentSessionTest = new ApiDetails
            {
                Step = "Different Sticky Session Test",
                Request = differentBody,
                StartUtc = DateTime.UtcNow
            };

            using (RestRequest req = new RestRequest(embeddingsUrl, embeddingsMethod))
            {
                // Add different sticky session header
                req.Headers.Add("x-thread-id", differentStickySessionId);
                req.ContentType = Constants.JsonContentType;
                
                RestResponse resp = await req.SendAsync(differentBody);
                
                if (resp == null)
                {
                    Console.WriteLine("No response for different sticky session test");
                    differentSessionTest.Response = null;
                    differentSessionTest.StatusCode = 0;
                    differentSessionTest.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(differentSessionTest);
                    return;
                }
                else
                {
                    differentSessionTest.Response = resp;
                    differentSessionTest.StatusCode = resp.StatusCode;
                    differentSessionTest.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for different sticky session test");
                        test.Success = false;
                        test.ApiDetails.Add(differentSessionTest);
                        return;
                    }
                    else
                    {
                        test.Success = true;
                        test.ApiDetails.Add(differentSessionTest);
                    }
                }
            }

            #endregion

            #region Sticky Session Test Summary

            ApiDetails stickySessionSummary = new ApiDetails
            {
                Step = "Sticky Session Test Summary",
                Request = $"Sent {totalEmbeddingRequests + totalCompletionRequests + totalChatRequests + 1} total requests with sticky sessions enabled",
                Response = $"All requests with session ID '{stickySessionId}' should have been routed to the same backend. Different session ID '{differentStickySessionId}' may route to a different backend.",
                StatusCode = 200,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow
            };

            test.ApiDetails.Add(stickySessionSummary);

            #endregion
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
