namespace Test.Automated.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Core.Settings;
    using RestWrapper;

    /// <summary>
    /// Base class for tests.
    /// </summary>
    public abstract class TestBase
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

        /// <summary>
        /// Name of the test.
        /// </summary>
        public string Name
        {
            get => _Name;
            set => _Name = (!String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Name)));
        }

        /// <summary>
        /// Test environment variables.
        /// </summary>
        public TestEnvironment TestEnvironment
        {
            get => _TestEnvironment;
            set => _TestEnvironment = (value != null ? value : throw new ArgumentNullException(nameof(TestEnvironment)));
        }

        /// <summary>
        /// OllamaFlow settings.
        /// </summary>
        public OllamaFlowSettings OllamaFlowSettings
        {
            get => _OllamaFlowSettings;
            set => _OllamaFlowSettings = (value != null ? value : throw new ArgumentNullException(nameof(OllamaFlowSettings)));
        }

        /// <summary>
        /// OllamaFlow daemon.
        /// </summary>
        public OllamaFlowDaemon OllamaFlowDaemon
        {
            get => _OllamaFlowDaemon;
            set => _OllamaFlowDaemon = (value != null ? value : throw new ArgumentNullException(nameof(OllamaFlowDaemon)));
        }

        /// <summary>
        /// Serializer.
        /// </summary>
        public Serializer Serializer
        {
            get => _Serializer;
        }

        private string _Name = "My test";
        private TestEnvironment _TestEnvironment = new TestEnvironment();
        private OllamaFlowSettings _OllamaFlowSettings = new OllamaFlowSettings();
        private OllamaFlowDaemon _OllamaFlowDaemon = null;
        private Serializer _Serializer = new Serializer();
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();

        /// <summary>
        /// Run the test.
        /// </summary>
        /// <param name="result">Test result.</param>
        /// <returns>Task.</returns>
        public abstract Task Run(TestResult result);

        /// <summary>
        /// Initialize the environment.
        /// </summary>
        /// <param name="removeDefaultRecords">Boolean indicating if default frontend and backend records should be removed.</param>
        /// <returns>Task.</returns>
        public void InitializeTestEnvironment(bool removeDefaultRecords = true)
        {
            OllamaFlowSettings = new OllamaFlowSettings
            {
                Logging = new LoggingSettings(),
                Webserver = new WatsonWebserver.Core.WebserverSettings
                {
                    Hostname = TestEnvironment.OllamaFlowHostname,
                    Port = TestEnvironment.OllamaFlowPort
                },
                DatabaseFilename = TestEnvironment.DatabaseFilename,
                AdminBearerTokens = TestEnvironment.AdminBearerTokens,
                StickyHeaders = TestEnvironment.StickyHeaders
            };

            OllamaFlowDaemon = new OllamaFlowDaemon(OllamaFlowSettings, _TokenSource);

            if (removeDefaultRecords)
            {
                foreach (Frontend frontend in TestEnvironment.Frontends)
                {
                    OllamaFlowDaemon.Frontends.Delete(frontend.Identifier);
                }

                foreach (Backend backend in TestEnvironment.Backends)
                {
                    OllamaFlowDaemon.Backends.Delete(backend.Identifier);
                }
            }

            if (TestEnvironment.Backends != null)
            {
                foreach (Backend backend in TestEnvironment.Backends)
                {
                    OllamaFlowDaemon.Backends.Create(backend);
                }
            }

            if (TestEnvironment.Frontends != null)
            {
                foreach (Frontend frontend in TestEnvironment.Frontends)
                {
                    OllamaFlowDaemon.Frontends.Create(frontend);
                }
            }
        }

        /// <summary>
        /// Send a REST request.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="method">Method.</param>
        /// <param name="url">URL.</param>
        /// <param name="data">Data.</param>
        /// <param name="contentType">Content-type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>RestResponse.</returns>
        public async Task<RestResponse> SendRestRequest<T>(
            HttpMethod method,
            string url,
            T data,
            string contentType,
            CancellationToken token = default)
        {
            using (RestRequest req = new RestRequest(url, method))
            {
                RestResponse resp = null;

                if (data != null)
                {
                    if (data is not string)
                    {
                        string json = Serializer.SerializeJson(data, true);
                        req.ContentType = contentType;
                        resp = await req.SendAsync(json, token).ConfigureAwait(false);
                    }
                    else
                    {
                        req.ContentType = contentType;
                        resp = await req.SendAsync(data as string, token).ConfigureAwait(false);
                    }
                }
                else
                {
                    resp = await req.SendAsync(token).ConfigureAwait(false);
                }

                return resp;
            }
        }

        /// <summary>
        /// Send a REST request.
        /// </summary>
        /// <param name="method">Method.</param>
        /// <param name="url">URL.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>RestResponse.</returns>
        public async Task<RestResponse> SendRestRequest(
            HttpMethod method,
            string url,
            CancellationToken token = default)
        {
            using (RestRequest req = new RestRequest(url, method))
            {
                return await req.SendAsync();
            }
        }

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
