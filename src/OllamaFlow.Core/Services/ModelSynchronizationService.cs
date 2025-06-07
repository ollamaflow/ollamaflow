namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using SyslogLogging;
    using SerializationHelper;
    using System.Threading;
    using System.Net.Http;
    using System.Text.Json;

    /// <summary>
    /// Model synchronization service.
    /// </summary>
    public class ModelSynchronizationService : IDisposable
    {
        #region Private-Members

        private readonly string _Header = "[ModelSynchronizationService] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private bool _IsDisposed = false;

        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private Dictionary<string, Task> _SynchronizationTasks = new Dictionary<string, Task>();

        // Track active pulls to avoid duplicates: backendId -> modelName -> isPulling
        private ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _ActivePulls =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>();

        // Interval to check for missing models (default: 60 seconds)
        private readonly int _SynchronizationIntervalMs = 60000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Model synchronization service.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        public ModelSynchronizationService(
            OllamaFlowSettings settings,
            LoggingModule logging,
            Serializer serializer)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            // Initialize active pulls tracking and start a task for each backend
            foreach (OllamaBackend backend in _Settings.Backends)
            {
                _ActivePulls[backend.Identifier] = new ConcurrentDictionary<string, bool>();

                _SynchronizationTasks.Add(
                    backend.Identifier,
                    Task.Run(() => BackendSynchronizationTask(backend, _TokenSource.Token), _TokenSource.Token));
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
                    _TokenSource?.Cancel();

                    try
                    {
                        Task.WaitAll(_SynchronizationTasks.Values.ToArray(), TimeSpan.FromSeconds(10));
                    }
                    catch { }

                    _TokenSource?.Dispose();

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

        private async Task BackendSynchronizationTask(OllamaBackend backend, CancellationToken token)
        {
            _Logging.Debug(_Header + "starting model synchronization task for backend " +
                backend.Identifier + " " + backend.Name + " " + backend.Hostname + ":" + backend.Port);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_SynchronizationIntervalMs, token);
                    await CheckAndSynchronizeBackendModels(backend, token);
                }
                catch (TaskCanceledException)
                {
                    // Expected when cancellation is requested
                }
                catch (Exception e)
                {
                    _Logging.Error(_Header + "synchronization task error for backend " +
                        backend.Identifier + ": " + e.ToString());
                }
            }

            _Logging.Debug(_Header + "stopping model synchronization task for backend " +
                backend.Identifier + " " + backend.Name + " " + backend.Hostname + ":" + backend.Port);
        }

        private async Task CheckAndSynchronizeBackendModels(OllamaBackend backend, CancellationToken token)
        {
            // Build set of required models for this backend
            HashSet<string> requiredModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (OllamaFrontend frontend in _Settings.Frontends)
            {
                if (frontend.Backends.Contains(backend.Identifier))
                {
                    foreach (string model in frontend.RequiredModels)
                    {
                        requiredModels.Add(model);
                    }
                }
            }

            if (requiredModels.Count == 0)
            {
                _Logging.Debug(_Header + "no required models for backend " + backend.Identifier);
                return;
            }

            // Check if backend is healthy and models have been discovered
            bool isHealthy = false;
            bool modelsDiscovered = false;
            List<string> currentModels = null;

            lock (backend.Lock)
            {
                isHealthy = backend.Healthy;
                modelsDiscovered = backend.ModelsDiscovered;
                currentModels = new List<string>(backend.Models);
            }

            if (!isHealthy)
            {
                _Logging.Debug(_Header + "skipping model synchronization for unhealthy backend " + backend.Identifier);
                return;
            }

            if (!modelsDiscovered)
            {
                _Logging.Debug(_Header + "skipping model synchronization for backend " + backend.Identifier + ", local models not yet discovered");
                return;
            }

            // Find missing models
            HashSet<string> currentBaseModels = new HashSet<string>(
                currentModels.Select(m => m.Contains(':') ? m.Substring(0, m.IndexOf(':')) : m),
                StringComparer.OrdinalIgnoreCase
            );

            List<string> missing = requiredModels.Where(m => !currentBaseModels.Contains(m)).ToList();

            if (missing.Count == 0)
            {
                _Logging.Debug(_Header + "all required models present on backend " + backend.Identifier);
                return;
            }

            _Logging.Info(_Header + "found " + missing.Count + " missing required models on backend " +
                backend.Identifier + ": " + string.Join(", ", missing));

            // Pull missing models
            List<Task> pullTasks = new List<Task>();
            foreach (string model in missing)
            {
                if (token.IsCancellationRequested) break;

                // Check if already pulling this model on this backend
                string baseModelName = model.Contains(':') ? model.Substring(0, model.IndexOf(':')) : model;
                if (!_ActivePulls[backend.Identifier].TryAdd(baseModelName, true))
                {
                    _Logging.Debug(_Header + "model " + baseModelName + " already being pulled on backend " + backend.Identifier);
                    continue;
                }

                // Start pull task
                Task pullTask = PullModelAsync(backend, model, token)
                    .ContinueWith(t =>
                    {
                        _ActivePulls[backend.Identifier].TryRemove(baseModelName, out _);
                    }, TaskScheduler.Default);

                pullTasks.Add(pullTask);
            }

            // Wait for all pulls to complete (or fail)
            if (pullTasks.Count > 0)
            {
                await Task.WhenAll(pullTasks);
            }
        }

        private async Task PullModelAsync(OllamaBackend backend, string model, CancellationToken token)
        {
            string pullUrl = backend.UrlPrefix + "/api/pull";

            try
            {
                _Logging.Info(_Header + "starting pull of model " + model + " on backend " + backend.Identifier);

                var pullRequest = new
                {
                    name = model.Contains(':') ? model : model + ":latest",  // Add :latest if no tag specified
                    stream = true
                };

                string jsonContent = JsonSerializer.Serialize(pullRequest);

                using (HttpClient client = new HttpClient())
                {
                    // No timeout for pull operations
                    client.Timeout = TimeSpan.FromMilliseconds(-1);

                    using (var content = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                    using (HttpResponseMessage response = await client.PostAsync(pullUrl, content, token))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            string errorBody = await response.Content.ReadAsStringAsync();
                            _Logging.Error(_Header + "failed to pull model " + model + " on backend " +
                                backend.Identifier + " - HTTP " + (int)response.StatusCode + ": " + errorBody);
                            return;
                        }

                        // Read streaming response
                        using (Stream stream = await response.Content.ReadAsStreamAsync())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string line;
                            long lastReportedPercent = -1;

                            while ((line = await reader.ReadLineAsync()) != null && !token.IsCancellationRequested)
                            {
                                if (string.IsNullOrWhiteSpace(line)) continue;

                                try
                                {
                                    using (JsonDocument doc = JsonDocument.Parse(line))
                                    {
                                        var root = doc.RootElement;

                                        // Check for error
                                        if (root.TryGetProperty("error", out JsonElement errorElement))
                                        {
                                            string error = errorElement.GetString();
                                            _Logging.Error(_Header + "pull error for model " + model +
                                                " on backend " + backend.Identifier + ": " + error);
                                            return;
                                        }

                                        // Check status
                                        if (root.TryGetProperty("status", out JsonElement statusElement))
                                        {
                                            string status = statusElement.GetString();

                                            // Log progress
                                            if (root.TryGetProperty("completed", out JsonElement completedElement) &&
                                                root.TryGetProperty("total", out JsonElement totalElement) &&
                                                totalElement.GetInt64() > 0)
                                            {
                                                long completed = completedElement.GetInt64();
                                                long total = totalElement.GetInt64();
                                                long percent = (completed * 100) / total;

                                                // Report every 10%
                                                if (percent / 10 != lastReportedPercent / 10)
                                                {
                                                    _Logging.Info(_Header + "pull progress for model " + model +
                                                        " on backend " + backend.Identifier + ": " + percent + "%");
                                                    lastReportedPercent = percent;
                                                }
                                            }

                                            // Check for completion
                                            if (status.Contains("success", StringComparison.OrdinalIgnoreCase))
                                            {
                                                _Logging.Info(_Header + "successfully pulled model " + model +
                                                    " on backend " + backend.Identifier);
                                                return;
                                            }
                                        }
                                    }
                                }
                                catch (JsonException)
                                {
                                    // Ignore malformed JSON lines
                                }
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException hre)
            {
                _Logging.Debug(_Header + "pull failed for model " + model + " on backend " + backend.Identifier + ": " + hre.Message);
            }
            catch (HttpIOException ioe)
            {
                _Logging.Debug(_Header + "pull failed for model " + model + " on backend " + backend.Identifier + ": " + ioe.Message);
            }
            catch (TaskCanceledException)
            {
                _Logging.Info(_Header + "pull task cancelled for model " + model + " on backend " + backend.Identifier);
            }
            catch (OperationCanceledException)
            {
                _Logging.Info(_Header + "pull operation cancelled for model " + model + " on backend " + backend.Identifier);
            }
            catch (Exception e)
            {
                _Logging.Error(_Header + "exception pulling model " + model + " on backend " +
                    backend.Identifier + ": " + e.ToString());
            }
        }

        #endregion
    }
}