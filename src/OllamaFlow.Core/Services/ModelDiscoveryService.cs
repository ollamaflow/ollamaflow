namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using RestWrapper;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Serialization;
    using SyslogLogging;
    using Timestamps;
    using UrlMatcher;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Model discovery service.
    /// </summary>
    public class ModelDiscoveryService : IDisposable
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        /// <summary>
        /// Interval, in milliseconds.
        /// Default is 30000.
        /// Minimum is 5000.
        /// </summary>
        public int IntervalMs
        {
            get
            {
                return _IntervalMs;
            }
            set
            {
                if (value < 5000) throw new ArgumentOutOfRangeException(nameof(IntervalMs));
                _IntervalMs = value;
            }
        }

        #endregion

        #region Private-Members

        private readonly string _Header = "[ModelDiscoveryService] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private Random _Random = new Random(Guid.NewGuid().GetHashCode());
        private bool _IsDisposed = false;

        private FrontendService _FrontendService = null;
        private BackendService _BackendService = null;
        private HealthCheckService _HealthCheckService = null;

        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private Task _ModelDiscoveryTask = null;

        private int _IntervalMs = 30000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Model discovery service.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="frontend">Frontend service.</param>
        /// <param name="backend">Backend service.</param>
        /// <param name="healthCheck">Healthcheck service.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public ModelDiscoveryService(
            OllamaFlowSettings settings,
            LoggingModule logging,
            Serializer serializer,
            FrontendService frontend,
            BackendService backend, 
            HealthCheckService healthCheck,
            CancellationTokenSource tokenSource = default)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _FrontendService = frontend ?? throw new ArgumentNullException(nameof(frontend));
            _BackendService = backend ?? throw new ArgumentNullException(nameof(backend));
            _HealthCheckService = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
            _TokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
            _ModelDiscoveryTask = Task.Run(() => ModelDiscoveryTask(_TokenSource.Token), _TokenSource.Token);

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    _Random = null;
                    _Serializer = null;
                    _Logging = null;
                    _Settings = null;
                }

                _IsDisposed = true;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        private async Task ModelDiscoveryTask(CancellationToken token = default)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_IntervalMs, token);

                    #region Get-Backends

                    List<OllamaBackend> backends = _HealthCheckService.Backends;
                    List<Task> tasks = new List<Task>();

                    #endregion

                    #region Process-Backends

                    if (backends != null && backends.Count > 0)
                    {
                        foreach (OllamaBackend backend in backends)
                        {
                            if (!backend.Active) continue;
                            tasks.Add(Task.Run(() => ModelDiscoveryTask(backend, token = default)));
                        }

                        Task.WaitAll(tasks.ToArray());
                    }

                    #endregion
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    _Logging.Error(_Header + "model discovery task exception:" + Environment.NewLine + e.ToString());
                }
            }

            _Logging.Debug(_Header + "model discovery task terminated");
        }

        private async Task ModelDiscoveryTask(OllamaBackend backend, CancellationToken token = default)
        {
            _Logging.Debug(
                _Header +
                "starting model discovery task for backend " +
                backend.Identifier + " " + backend.Name + " " + backend.Hostname + ":" + backend.Port);

            string discoveryUrl = (backend.Ssl ? "https://" : "http://") + backend.Hostname + ":" + backend.Port + "/api/tags";

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(5);

                try
                {
                    // Get the list of models from /api/tags
                    HttpResponseMessage response = await client.GetAsync(discoveryUrl, token);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        // Parse the JSON response
                        using (JsonDocument document = JsonDocument.Parse(responseBody))
                        {
                            List<string> models = new List<string>();

                            if (document.RootElement.TryGetProperty("models", out JsonElement modelsElement) &&
                                modelsElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement modelElement in modelsElement.EnumerateArray())
                                {
                                    if (modelElement.TryGetProperty("name", out JsonElement nameElement))
                                    {
                                        string modelName = nameElement.GetString();
                                        if (!string.IsNullOrEmpty(modelName))
                                        {
                                            models.Add(modelName);
                                        }
                                    }
                                }
                            }

                            lock (backend.Lock)
                            {
                                backend.Models = models;
                                backend.ModelsDiscovered = true;
                            }

                            _Logging.Debug(
                                _Header + 
                                "discovered " + models.Count + " models on backend " + backend.Identifier + ": " + string.Join(", ", backend.Models));
                        }
                    }
                    else
                    {
                        _Logging.Warn(_Header + "model discovery failed for backend " + backend.Identifier + " with status " + (int)response.StatusCode);
                        // Don't clear the models list on failure - keep the last known good state
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected when cancellation is requested
                    if (!token.IsCancellationRequested)
                    {
                        _Logging.Debug(_Header + "model discovery timeout for backend " + backend.Identifier);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    if (!token.IsCancellationRequested)
                    {
                        _Logging.Debug(_Header + "model discovery timeout for backend " + backend.Identifier);
                    }
                }
                catch (HttpRequestException hre)
                {
                    _Logging.Debug(_Header + "model discovery HTTP request exception for backend " + backend.Identifier + ": " + hre.Message);
                }
                catch (HttpIOException ioe)
                {
                    _Logging.Debug(_Header + "model discovery IO exception for backend " + backend.Identifier + ": " + ioe.Message);
                }
                catch (Exception e)
                {
                    _Logging.Debug(_Header + "model discovery exception for backend " + backend.Identifier + Environment.NewLine + e.ToString());
                    // Don't clear the models list on exception - keep the last known good state
                }
            }
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
