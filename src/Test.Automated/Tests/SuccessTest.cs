namespace Test.Automated
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

    public class SuccessTest : TestBase
    {
        /// <summary>
        /// Test 1: One Ollama backend, Ollama APIs, single embeddings request
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public async Task Test1(TestResult test)
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
    }
}