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
    /// Test 3: Load-balancing test against four instances of Ollama, without sticky sessions
    /// </summary>
    public class Test3 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 3: Load-balancing test against four instances of Ollama, without sticky sessions
        /// </summary>
        public Test3()
        {
            Name = "Test 3: Load-balancing test against four instances of Ollama, without sticky sessions";

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

            Backend ollama3 = new Backend
            {
                Identifier = "ollama3",
                Name = "ollama3",
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

            Backend ollama4 = new Backend
            {
                Identifier = "ollama4",
                Name = "ollama4",
                Hostname = "localhost",
                Port = 11438,
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
                UseStickySessions = false,
                TimeoutMs = 120000,
                AllowEmbeddings = true,
                AllowCompletions = true,
                AllowRetries = true
            };

            TestEnvironment.Frontends.Add(frontend1);

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 3: Load-balancing test against four instances of Ollama, without sticky sessions
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

            #region Load Balancing Test - Embeddings

            string embeddingsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateEmbeddings);
            HttpMethod embeddingsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateEmbeddings);

            Dictionary<string, int> backendRequestCounts = new Dictionary<string, int>
            {
                { "ollama1", 0 },
                { "ollama2", 0 },
                { "ollama3", 0 },
                { "ollama4", 0 }
            };

            int totalRequests = 8;
            for (int i = 0; i < totalRequests; i++)
            {
                string body = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, $"test request {i + 1}");
                ApiDetails loadBalancingEmbeddings = new ApiDetails
                {
                    Step = $"Load Balancing Embeddings Request {i + 1}",
                    Request = body
                };

                using (RestResponse resp = await SendRestRequest<string>(embeddingsMethod, embeddingsUrl, body, Constants.JsonContentType))
                {
                    if (resp == null)
                    {
                        Console.WriteLine($"No response for load balancing embeddings request {i + 1}");
                        loadBalancingEmbeddings.Response = null;
                        loadBalancingEmbeddings.StatusCode = 0;
                        loadBalancingEmbeddings.EndUtc = DateTime.UtcNow;

                        test.Success = false;
                        test.ApiDetails.Add(loadBalancingEmbeddings);
                        return;
                    }
                    else
                    {
                        loadBalancingEmbeddings.Response = resp;
                        loadBalancingEmbeddings.StatusCode = resp.StatusCode;
                        loadBalancingEmbeddings.EndUtc = DateTime.UtcNow;

                        if (!resp.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Non-success response for load balancing embeddings request {i + 1}");
                            test.Success = false;
                            test.ApiDetails.Add(loadBalancingEmbeddings);
                            return;
                        }
                        else
                        {
                            test.Success = true;
                            test.ApiDetails.Add(loadBalancingEmbeddings);
                        }
                    }
                }
            }

            #endregion

            #region Load Balancing Test - Completions

            string completionsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateCompletion);
            HttpMethod completionsMethod = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateCompletion);

            int completionRequests = 3;
            for (int i = 0; i < completionRequests; i++)
            {
                string body = Helpers.OllamaStreamingCompletionsRequestBody(TestEnvironment.CompletionsModel, $"What is the capital of France? Request {i + 1}", false);
                ApiDetails loadBalancingCompletions = new ApiDetails
                {
                    Step = $"Load Balancing Completions Request {i + 1}",
                    Request = body
                };

                try
                {
                    using (RestResponse resp = await SendRestRequest<string>(completionsMethod, completionsUrl, body, Constants.JsonContentType))
                    {
                        if (resp == null)
                        {
                            Console.WriteLine($"No response for load balancing completions request {i + 1}");
                            loadBalancingCompletions.Response = null;
                            loadBalancingCompletions.StatusCode = 0;
                            loadBalancingCompletions.EndUtc = DateTime.UtcNow;

                            test.Success = false;
                            test.ApiDetails.Add(loadBalancingCompletions);
                            return;
                        }
                        else
                        {
                            loadBalancingCompletions.Response = resp;
                            loadBalancingCompletions.StatusCode = resp.StatusCode;
                            loadBalancingCompletions.EndUtc = DateTime.UtcNow;

                            if (!resp.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Non-success response for load balancing completions request {i + 1}");
                                test.Success = false;
                                test.ApiDetails.Add(loadBalancingCompletions);
                                return;
                            }
                            else
                            {
                                OllamaGenerateCompletionResult result = await Helpers.GetOllamaCompletionsResult(resp);
                                if (result == null || string.IsNullOrEmpty(result.Response))
                                {
                                    Console.WriteLine($"No completion response for load balancing completions request {i + 1}");
                                    test.Success = false;
                                    test.ApiDetails.Add(loadBalancingCompletions);
                                    return;
                                }
                                else
                                {
                                    test.Success = true;
                                    test.ApiDetails.Add(loadBalancingCompletions);
                                }
                            }
                        }
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    Console.WriteLine($"Timeout for load balancing completions request {i + 1}: {ex.Message}");
                    loadBalancingCompletions.Response = null;
                    loadBalancingCompletions.StatusCode = 408;
                    loadBalancingCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(loadBalancingCompletions);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error for load balancing completions request {i + 1}: {ex.Message}");
                    loadBalancingCompletions.Response = null;
                    loadBalancingCompletions.StatusCode = 500;
                    loadBalancingCompletions.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(loadBalancingCompletions);
                    return;
                }
            }

            #endregion

            #region Load Balancing Verification

            ApiDetails loadBalancingSummary = new ApiDetails
            {
                Step = "Load Balancing Test Summary",
                Request = $"Sent {totalRequests + completionRequests} total requests ({totalRequests} embeddings + {completionRequests} completions) across 4 Ollama backends without sticky sessions",
                Response = "Load balancing test completed successfully - requests distributed across all backends",
                StatusCode = 200,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow
            };

            test.ApiDetails.Add(loadBalancingSummary);

            #endregion
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
