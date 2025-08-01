namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Serialization;
    using SyslogLogging;

    /// <summary>
    /// Model synchronization service.
    /// </summary>
    public class ModelSynchronizationService : IDisposable
    {
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

        /// <summary>
        /// Maximum concurrent downloads per backend.
        /// Default is 3.
        /// </summary>
        public int MaxConcurrentDownloadsPerBackend
        {
            get
            {
                return _MaxConcurrentDownloadsPerBackend;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxConcurrentDownloadsPerBackend));
                _MaxConcurrentDownloadsPerBackend = value;
            }
        }

        #endregion

        #region Private-Members

        private readonly string _Header = "[ModelSynchronizationService] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private bool _IsDisposed = false;

        private FrontendService _FrontendService = null;
        private BackendService _BackendService = null;
        private HealthCheckService _HealthCheckService = null;

        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private Task _SynchronizationTask = null;

        // Track active pulls per backend
        private ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _ActivePulls =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>();

        // Semaphore per backend to limit concurrent downloads
        private ConcurrentDictionary<string, SemaphoreSlim> _BackendSemaphores =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        private int _IntervalMs = 30000;
        private int _MaxConcurrentDownloadsPerBackend = 3;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Model synchronization service.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="frontend">Frontend service.</param>
        /// <param name="backend">Backend service.</param>
        /// <param name="healthCheck">Healthcheck service.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public ModelSynchronizationService(
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
            _SynchronizationTask = Task.Run(() => SynchronizationTask(_TokenSource.Token), _TokenSource.Token);

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
                    _TokenSource?.Cancel();

                    try
                    {
                        _SynchronizationTask.Wait(TimeSpan.FromSeconds(10));
                    }
                    catch { }

                    _TokenSource?.Dispose();

                    // Dispose all semaphores
                    foreach (var semaphore in _BackendSemaphores.Values)
                    {
                        semaphore?.Dispose();
                    }

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

        private async Task SynchronizationTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_IntervalMs, token);

                    #region Get-Frontends

                    List<OllamaFrontend> frontends = _FrontendService.GetAll().ToList();
                    if (frontends == null || frontends.Count < 1) continue;

                    #endregion

                    #region Get-Backends

                    List<OllamaBackend> backends = _HealthCheckService.Backends;
                    if (backends == null || backends.Count < 1) continue;

                    #endregion

                    #region Process-All-Backends-In-Parallel

                    // Build a dictionary of backend -> required models
                    Dictionary<OllamaBackend, HashSet<string>> backendRequiredModels =
                        new Dictionary<OllamaBackend, HashSet<string>>();

                    foreach (OllamaBackend backend in backends)
                    {
                        HashSet<string> requiredModels = new HashSet<string>();

                        foreach (OllamaFrontend frontend in frontends)
                        {
                            if (frontend.Backends.Contains(backend.Identifier))
                            {
                                if (frontend.RequiredModels != null && frontend.RequiredModels.Count > 0)
                                {
                                    foreach (string model in frontend.RequiredModels)
                                    {
                                        requiredModels.Add(model);
                                    }
                                }
                            }
                        }

                        if (requiredModels.Count > 0)
                        {
                            backendRequiredModels[backend] = requiredModels;
                        }
                    }

                    // Process all backends in parallel
                    List<Task> backendTasks = new List<Task>();
                    foreach (var kvp in backendRequiredModels)
                    {
                        Task backendTask = Task.Run(async () =>
                        {
                            await CheckAndSynchronizeBackendModels(kvp.Key, kvp.Value.ToList(), token);
                        }, token);

                        backendTasks.Add(backendTask);
                    }

                    // Wait for all backend synchronizations to complete
                    if (backendTasks.Count > 0)
                    {
                        await Task.WhenAll(backendTasks);
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
                    _Logging.Error(_Header + "synchronization task exception:" + Environment.NewLine + e.ToString());
                }
            }

            _Logging.Debug(_Header + "synchronization task terminated");
        }

        private async Task CheckAndSynchronizeBackendModels(OllamaBackend backend, List<string> models, CancellationToken token)
        {
            if (models == null || models.Count < 1)
            {
                _Logging.Debug(_Header + "no required models for backend " + backend.Identifier);
                return;
            }

            if (!backend.Healthy)
            {
                _Logging.Debug(_Header + "skipping model synchronization for unhealthy backend " + backend.Identifier);
                return;
            }

            if (!backend.ModelsDiscovered)
            {
                _Logging.Debug(_Header + "skipping model synchronization for backend " + backend.Identifier + ", local models not yet discovered");
                return;
            }

            // Ensure semaphore exists for this backend
            var semaphore = _BackendSemaphores.GetOrAdd(backend.Identifier,
                _ => new SemaphoreSlim(_MaxConcurrentDownloadsPerBackend, _MaxConcurrentDownloadsPerBackend));

            // Create a normalized set of current models (full names with tags)
            HashSet<string> currentModels = new HashSet<string>(backend.Models, StringComparer.OrdinalIgnoreCase);

            // Find missing models by checking both exact matches and base model matches
            List<string> missing = new List<string>();

            foreach (string requiredModel in models)
            {
                bool found = false;

                // Check for exact match first
                if (currentModels.Contains(requiredModel))
                {
                    found = true;
                }
                else
                {
                    // If the required model has a tag, check for exact match only
                    if (requiredModel.Contains(':'))
                    {
                        found = currentModels.Contains(requiredModel);
                    }
                    else
                    {
                        // If no tag specified, check if any version of this model exists
                        string requiredBase = requiredModel;
                        found = currentModels.Any(m =>
                        {
                            string currentBase = m.Contains(':') ? m.Substring(0, m.IndexOf(':')) : m;
                            return currentBase.Equals(requiredBase, StringComparison.OrdinalIgnoreCase);
                        });
                    }
                }

                if (!found)
                {
                    missing.Add(requiredModel);
                }
            }

            if (missing.Count == 0)
            {
                _Logging.Debug(_Header + "all required models present on backend " + backend.Identifier);
                return;
            }

            _Logging.Info(_Header + "found " + missing.Count + " missing required models on backend " + backend.Identifier + ": " + string.Join(", ", missing));

            // Ensure active pulls dictionary exists for this backend
            if (!_ActivePulls.ContainsKey(backend.Identifier))
            {
                _ActivePulls[backend.Identifier] = new ConcurrentDictionary<string, bool>();
            }

            // Pull missing models with controlled concurrency
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

                // Create pull task with semaphore control
                Task pullTask = Task.Run(async () =>
                {
                    await semaphore.WaitAsync(token);
                    try
                    {
                        await PullModelAsync(backend, model, token);
                    }
                    finally
                    {
                        semaphore.Release();
                        _ActivePulls[backend.Identifier].TryRemove(baseModelName, out _);
                    }
                }, token);

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