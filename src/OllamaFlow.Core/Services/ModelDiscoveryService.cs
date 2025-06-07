namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using RestWrapper;
    using SerializationHelper;
    using OllamaFlow.Core;
    using SyslogLogging;
    using Timestamps;
    using UrlMatcher;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using System.Threading;
    using System.Net.Http;
    using System.Text.Json;

    /// <summary>
    /// Model discovery service.
    /// </summary>
    public class ModelDiscoveryService : IDisposable
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[ModelDiscoveryService] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private Random _Random = new Random(Guid.NewGuid().GetHashCode());
        private bool _IsDisposed = false;

        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private Dictionary<string, Task> _ModelDiscoveryTasks = new Dictionary<string, Task>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Model discovery service.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        public ModelDiscoveryService(
            OllamaFlowSettings settings,
            LoggingModule logging,
            Serializer serializer)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            foreach (OllamaBackend backend in _Settings.Backends)
            {
                _ModelDiscoveryTasks.Add(
                    backend.Identifier,
                    Task.Run(() => ModelDiscoveryTask(backend, _TokenSource.Token), _TokenSource.Token));
            }
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

        private async Task ModelDiscoveryTask(OllamaBackend backend, CancellationToken token = default)
        {
            bool firstRun = true;

            _Logging.Debug(
                _Header +
                "starting model discovery task for backend " +
                backend.Identifier + " " + backend.Name + " " + backend.Hostname + ":" + backend.Port);

            string discoveryUrl = (backend.Ssl ? "https://" : "http://") + backend.Hostname + ":" + backend.Port + "/api/tags";

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(5);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (!firstRun) await Task.Delay(backend.ModelRefreshIntervalMs, token);
                        else firstRun = false;

                        bool isHealthy = false;
                        lock (backend.Lock)
                        {
                            isHealthy = backend.Healthy;
                        }

                        if (!isHealthy)
                        {
                            _Logging.Debug(_Header + "skipping model discovery for unhealthy backend " + backend.Identifier);
                            continue;
                        }

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
                                    _Logging.Debug(_Header + "discovered " + models.Count + " models on backend " + backend.Identifier);
                                }
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

            _Logging.Debug(
                _Header +
                "stopping model discovery task for backend " +
                backend.Identifier + " " + backend.Name + " " + backend.Hostname + ":" + backend.Port);
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
