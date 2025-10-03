namespace Test.Automated.Tests
{
    using System;
    using System.Collections.Specialized;
    using System.Net.WebSockets;
    using System.Runtime.CompilerServices;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Models.Ollama;
    using OllamaFlow.Core.Serialization;
    using RestWrapper;

    public class Test1 
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Test-Setup

        private static string _OllamaFlowHostname = "localhost";
        private static int _OllamaFlowPort = 10000;
        private static string _StickyHeader = "x-thread-id";
        
        private static string _OllamaUrl = "http://astra:11434";
        private static List<string> _OllamaRequiredModels = new List<string> { "all-minilm", "gemma3:4b" };
        private static string _OllamaEmbeddingsModel = "all-minilm";
        private static string _OllamaCompletionsModel = "gemma3:4b";

        private static string _VllmUrl = "http://34.55.208.75:8000";
        private static List<string> _VllmModels = new List<string> { "Qwen/Qwen2.5-3B" };
        private static string _VllmCompletionsModel = "Qwen/Qwen2.5-3B";

        private static Serializer _Serializer = new Serializer();

        private static string _Line = new string('-', Console.WindowWidth - 1);
        private static string _Json = "application/json";

        #endregion

        public static async Task Main(string[] args)
        {
            #region Welcome

            Console.WriteLine();
            Console.WriteLine("OllamaFlow Automated Tests");
            Console.WriteLine(_Line);

            List<TestResult> results = new List<TestResult>();

            #endregion

            #region Tests

            Console.WriteLine("Running tests");
            results.Add(await RunTest("Sample success test", SuccessTest));
            results.Add(await RunTest("Sample failure test", FailureTest));

            // Simple tests
            results.Add(await RunTest("Test 1: One Ollama backend, Ollama APIs, single embeddings request", Test1));

            #endregion

            #region Summary

            Console.WriteLine();
            Console.WriteLine("Summary results");
            Console.WriteLine(_Line);

            int success = 0;
            int failure = 0;

            foreach (TestResult result in results)
            {
                if (result.Success) success++;
                else failure++;
                Console.WriteLine($"| {result.ToString()}");

                if (!result.Success)
                {
                    Console.WriteLine(_Serializer.SerializeJson(result, true));

                    if (result.Exception != null)
                        Console.WriteLine(result.Exception.ToString());

                    Console.WriteLine();
                }
            }

            Console.WriteLine();
            Console.WriteLine($"{success} test(s) passed");
            Console.WriteLine($"{failure} test(s) failed (1 failed test expected, 'Sample failure test')");
            Console.WriteLine();

            if (failure < 2) Console.WriteLine("Test succeeded"); // account for the failure test at the beginning
            else Console.WriteLine("Test failed");
            Console.WriteLine();

            #endregion
        }

        private static async Task<TestResult> RunTest(string name, Func<TestResult, Task> func)
        {
            TestResult result = new TestResult
            {
                Name = name,
                StartUtc = DateTime.Now
            };

            try
            {
                await func(result); // Pass result to the test function
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
            }

            result.EndUtc = DateTime.Now;
            return result;
        }

        #region Sample-Tests

        private static async Task SuccessTest(TestResult test)
        {
            test.Success = true;
            test.Request = "Success request";
            test.Response = "Success response";
            test.Headers = new NameValueCollection();
            test.StatusCode = 200;
            await Task.Delay(250);
            test.EndUtc = DateTime.UtcNow;
        }

        private static async Task FailureTest(TestResult test)
        {
            test.Success = false;
            test.Request = "Failure request";
            test.Response = "Failure response";
            test.Headers = new NameValueCollection();
            test.StatusCode = 400;
            await Task.Delay(100);
            throw new DivideByZeroException();
        }

        #endregion

        #region Tests

        /// <summary>
        /// Test 1: One Ollama backend, Ollama APIs, single embeddings request
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        private static async Task Test1(TestResult test)
        {
            try
            {
                OllamaFlowSettings settings = new OllamaFlowSettings
                {
                    Webserver = new WatsonWebserver.Core.WebserverSettings
                    {
                        Port = _OllamaFlowPort,
                        Hostname = _OllamaFlowHostname
                    },
                    StickyHeaders = new List<string> { _StickyHeader }
                };

                using (OllamaFlowDaemon daemon = new OllamaFlowDaemon(settings))
                {
                    RemoveDefaultRecords(daemon);

                    Backend backend1 = Helpers.CreateOllamaBackend(daemon, "ollama1", "localhost");
                    Frontend frontend1 = Helpers.CreateFrontend(
                        daemon,
                        "frontend1",
                        LoadBalancingMode.RoundRobin,
                        new List<Backend> { backend1 },
                        _OllamaRequiredModels);

                    if (!await Helpers.WaitForHealthyBackend(daemon, backend1))
                    {
                        test.Success = false;
                        throw new TimeoutException($"Backend {backend1.Identifier} failed to become healthy");
                    }

                    // single embeddings request
                    string embeddingsUrl = UrlBuilder.BuildUrl(backend1, RequestTypeEnum.OllamaGenerateEmbeddings);
                    HttpMethod method = UrlBuilder.GetMethod(backend1, RequestTypeEnum.OllamaGenerateEmbeddings);

                    OllamaGenerateEmbeddingsRequest embedReq = new OllamaGenerateEmbeddingsRequest
                    {
                        Model = _OllamaEmbeddingsModel,
                        Input = "test"
                    };

                    test.Request = embedReq;

                    using (RestRequest restReq = new RestRequest(embeddingsUrl, method, _Json))
                    {
                        test.Request = embedReq;

                        using (RestResponse restResp = await restReq.SendAsync(_Serializer.SerializeJson(embedReq, false)))
                        {
                            if (restResp == null)
                            {
                                test.Success = false;
                                throw new IOException($"Unable to connect to frontend {frontend1.Identifier}");
                            }
                            else
                            {
                                if (restResp.StatusCode < 200 || restResp.StatusCode > 299)
                                {
                                    test.Success = false;
                                    throw new IOException($"Bad request returned from frontend {frontend1.Identifier}");
                                }
                                else
                                {
                                    test.Success = true;
                                    test.Response = restResp.DataAsString;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Helpers.Cleanup();
            }
        }

        #endregion

        #region Support-Methods

        private static void RemoveDefaultRecords(OllamaFlowDaemon daemon)
        {

        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}