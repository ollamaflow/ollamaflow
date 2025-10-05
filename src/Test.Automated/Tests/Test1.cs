namespace Test.Automated.Tests
{
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Models.Ollama;
    using OllamaFlow.Core.Serialization;
    using RestWrapper;
    using System;
    using System.Collections.Specialized;
    using System.Net.WebSockets;
    using System.Runtime.CompilerServices;
    using System.Xml.Linq;

    /// <summary>
    /// Test 1: Ollama backend, Ollama APIs, single embeddings, multiple embeddings, completions, and chat completions test
    /// </summary>
    public class Test1 : TestBase
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Test 1: Ollama backend, Ollama APIs, single embeddings, multiple embeddings, completions, and chat completions test
        /// </summary>
        public Test1()
        {
            Name = "Test 1: Ollama backend, Ollama APIs, single embeddings, multiple embeddings, completions, and chat completions test";

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

            TestEnvironment.Backends.Add(ollama1);

            Frontend frontend1 = new Frontend
            {
                Identifier = "frontend1",
                Name = "frontend1",
                Hostname = "localhost",
                LoadBalancing = LoadBalancingMode.RoundRobin,
                Backends = new List<string> { "ollama1" },
                RequiredModels = new List<string> { "all-minilm", "gemma3:4b" },
                UseStickySessions = false,
                AllowEmbeddings = true,
                AllowCompletions = true,
                AllowRetries = true
            };

            TestEnvironment.Frontends.Add(frontend1);

            InitializeTestEnvironment(true);
        }

        /// <summary>
        /// Test 1: Ollama backend, Ollama APIs, single embeddings, multiple embeddings, completions, and chat completions test
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult test)
        {
            test.Success = true; // default to true

            await Helpers.WaitForHealthyBackend(OllamaFlowDaemon, "ollama1");
            Frontend frontend = OllamaFlowDaemon.Frontends.GetAll().ToList()[0];
            Backend backend = OllamaFlowDaemon.Backends.GetAll().ToList()[0];

            #region Embeddings

            string embeddingsUrl = UrlBuilder.BuildUrl(OllamaFlowSettings, frontend, RequestTypeEnum.OllamaGenerateEmbeddings);
            HttpMethod method = UrlBuilder.GetMethod(backend, RequestTypeEnum.OllamaGenerateEmbeddings);

            #region Single-Embeddings

            string body = Helpers.OllamaSingleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, "test");
            ApiDetails singleEmbeddings = new ApiDetails
            {
                Step = "Ollama Single Embeddings",
                Request = body
            };

            using (RestResponse resp = await SendRestRequest<string>(method, embeddingsUrl, body, Constants.JsonContentType))
            {
                OllamaGenerateEmbeddingsResult result = await Helpers.GetOllamaEmbeddingsResult(resp);
                if (result == null)
                {
                    Console.WriteLine("No response for single embeddings request");
                    singleEmbeddings.Response = null;
                    singleEmbeddings.StatusCode = 0;
                    singleEmbeddings.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(singleEmbeddings);
                    return;
                }
                else
                {
                    singleEmbeddings.Response = resp;
                    singleEmbeddings.StatusCode = resp.StatusCode;
                    singleEmbeddings.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for single embeddings request");
                        test.Success = false;
                        test.ApiDetails.Add(singleEmbeddings);
                        return;
                    }
                    else
                    {
                        singleEmbeddings.Response = resp;
                        singleEmbeddings.StatusCode = resp.StatusCode;

                        if (result.Embeddings == null)
                        {
                            Console.WriteLine("No embeddings returned for single embeddings request");
                            test.Success = false;
                            test.ApiDetails.Add(singleEmbeddings);
                            return;
                        }
                        else
                        {
                            // Ollama always returns array of arrays, even for single input
                            if (result.GetEmbeddingCount() != 1)
                            {
                                Console.WriteLine($"Expected 1 embedding, got {result.GetEmbeddingCount()} for single embeddings request");
                                test.Success = false;
                                test.ApiDetails.Add(singleEmbeddings);
                                return;
                            }
                            else
                            {
                                test.Success = true;
                                test.ApiDetails.Add(singleEmbeddings);
                            }
                        }
                    }
                }
            }

            #endregion

            #region Multiple-Embeddings

            body = Helpers.OllamaMultipleEmbeddingsRequestBody(TestEnvironment.EmbeddingsModel, new List<string> { "hello", "workd" });
            ApiDetails multiEmbeddings = new ApiDetails
            {
                Step = "Ollama Multi Embeddings",
                Request = body
            };

            using (RestResponse resp = await SendRestRequest<string>(method, embeddingsUrl, body, Constants.JsonContentType))
            {
                OllamaGenerateEmbeddingsResult result = await Helpers.GetOllamaEmbeddingsResult(resp);
                if (result == null)
                {
                    Console.WriteLine("No response for multiple embeddings request");
                    multiEmbeddings.Response = null;
                    multiEmbeddings.StatusCode = 0;
                    multiEmbeddings.EndUtc = DateTime.UtcNow;

                    test.Success = false;
                    test.ApiDetails.Add(multiEmbeddings);
                    return;
                }
                else
                {
                    multiEmbeddings.Response = resp;
                    multiEmbeddings.StatusCode = resp.StatusCode;
                    multiEmbeddings.EndUtc = DateTime.UtcNow;

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Non-success response for multiple embeddings request");
                        test.Success = false;
                        test.ApiDetails.Add(multiEmbeddings);
                        return;
                    }
                    else
                    {
                        multiEmbeddings.Response = resp;
                        multiEmbeddings.StatusCode = resp.StatusCode;

                        if (result.Embeddings == null)
                        {
                            Console.WriteLine("No embeddings returned for multiple embeddings request");
                            test.Success = false;
                            test.ApiDetails.Add(multiEmbeddings);
                            return;
                        }
                        else
                        {
                            if (result.GetEmbeddingCount() != 2)
                            {
                                Console.WriteLine($"Expected 2 embeddings, got {result.GetEmbeddingCount()} for multiple embeddings request");
                                test.Success = false;
                                test.ApiDetails.Add(multiEmbeddings);
                                return;
                            }
                            else
                            {
                                test.Success = true;
                                test.ApiDetails.Add(multiEmbeddings);
                            }
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