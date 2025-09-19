namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
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

        private int _IntervalMs = 30000;
        private int _MaxConcurrentDownloadsPerBackend = 3;

        // Per-backend task management
        private ConcurrentDictionary<string, Task> _BackendTasks = new ConcurrentDictionary<string, Task>();
        private ConcurrentDictionary<string, CancellationTokenSource> _BackendTokenSources = new ConcurrentDictionary<string, CancellationTokenSource>();

        // Track active pulls per backend
        private ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _ActivePulls =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>();

        // Semaphore per backend to limit concurrent downloads
        private ConcurrentDictionary<string, SemaphoreSlim> _BackendSemaphores =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        // Cached discovered models per backend
        private ConcurrentDictionary<string, List<string>> _BackendModels = new ConcurrentDictionary<string, List<string>>();

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
            _TokenSource = tokenSource ?? new CancellationTokenSource();

            // Initialize existing backends from database
            InitializeExistingBackendsAsync().ConfigureAwait(false);

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
                    // Stop all backend tasks
                    List<string> backendIds = _BackendTasks.Keys.ToList();
                    foreach (string backendId in backendIds)
                    {
                        StopBackendSynchronizationTask(backendId);
                    }

                    // Cancel main token source
                    if (_TokenSource != null && !_TokenSource.IsCancellationRequested)
                    {
                        _TokenSource.Cancel();
                        _TokenSource.Dispose();
                    }

                    // Dispose all remaining semaphores
                    foreach (SemaphoreSlim semaphore in _BackendSemaphores.Values)
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

        /// <summary>
        /// Add a backend and start model synchronization for it.
        /// </summary>
        /// <param name="backend">Backend that was added.</param>
        /// <exception cref="ArgumentNullException">Thrown when backend is null.</exception>
        public void AddBackend(OllamaBackend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));

            // Create semaphore for new backend to limit concurrent downloads
            SemaphoreSlim semaphore = new SemaphoreSlim(_MaxConcurrentDownloadsPerBackend, _MaxConcurrentDownloadsPerBackend);
            _BackendSemaphores.TryAdd(backend.Identifier, semaphore);

            // Start dedicated synchronization task for this backend
            StartBackendSynchronizationTask(backend);

            _Logging.Debug(_Header + "notified of new backend " + backend.Identifier + " and started synchronization task");
        }

        /// <summary>
        /// Update a backend configuration.
        /// </summary>
        /// <param name="backend">Backend that was updated.</param>
        /// <exception cref="ArgumentNullException">Thrown when backend is null.</exception>
        public void UpdateBackend(OllamaBackend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            _Logging.Debug(_Header + "notified of updated backend " + backend.Identifier);
        }

        /// <summary>
        /// Remove a backend and stop model synchronization for it.
        /// </summary>
        /// <param name="identifier">Identifier of backend that was removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when identifier is null.</exception>
        public void RemoveBackend(string identifier)
        {
            if (String.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));

            // Stop and cleanup the synchronization task for this backend
            StopBackendSynchronizationTask(identifier);

            _Logging.Debug(_Header + "notified of removed backend " + identifier + " and stopped synchronization task");
        }

        /// <summary>
        /// Add a frontend and sync its required models.
        /// </summary>
        /// <param name="frontend">Frontend that was added.</param>
        /// <exception cref="ArgumentNullException">Thrown when frontend is null.</exception>
        public void AddFrontend(OllamaFrontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));
            _Logging.Debug(_Header + "notified of new frontend " + frontend.Identifier + " - will sync required models");
        }

        /// <summary>
        /// Update a frontend configuration and sync required models.
        /// </summary>
        /// <param name="frontend">Frontend that was updated.</param>
        /// <exception cref="ArgumentNullException">Thrown when frontend is null.</exception>
        public void UpdateFrontend(OllamaFrontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));
            _Logging.Debug(_Header + "notified of updated frontend " + frontend.Identifier + " - will sync required models");
        }

        /// <summary>
        /// Remove a frontend.
        /// </summary>
        /// <param name="identifier">Identifier of frontend that was removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when identifier is null.</exception>
        public void RemoveFrontend(string identifier)
        {
            if (String.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));
            _Logging.Debug(_Header + "notified of removed frontend " + identifier);
        }

        /// <summary>
        /// Get discovered models for a specific backend.
        /// </summary>
        /// <param name="backendIdentifier">Backend identifier.</param>
        /// <returns>List of model names, or empty list if backend not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when backendIdentifier is null.</exception>
        public List<string> GetBackendModels(string backendIdentifier)
        {
            if (String.IsNullOrEmpty(backendIdentifier)) throw new ArgumentNullException(nameof(backendIdentifier));

            if (_BackendModels.TryGetValue(backendIdentifier, out List<string> models))
            {
                return new List<string>(models); // Return copy to prevent external modification
            }

            return new List<string>();
        }

        #endregion

        #region Internal-Methods

        #endregion

        #region Private-Methods

        /// <summary>
        /// Initialize existing backends from database at startup.
        /// </summary>
        private async Task InitializeExistingBackendsAsync()
        {
            try
            {
                // Load existing backends and start synchronization tasks
                List<OllamaBackend> backends = _BackendService.GetAll()?.ToList() ?? new List<OllamaBackend>();
                foreach (OllamaBackend backend in backends)
                {
                    // Create semaphore for this backend
                    SemaphoreSlim semaphore = new SemaphoreSlim(_MaxConcurrentDownloadsPerBackend, _MaxConcurrentDownloadsPerBackend);
                    _BackendSemaphores.TryAdd(backend.Identifier, semaphore);

                    // Start synchronization task
                    StartBackendSynchronizationTask(backend);
                }

                _Logging.Debug(_Header + $"initialized model synchronization for {backends.Count} backends with dedicated tasks");
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + "error initializing existing backends: " + ex.Message);
            }
        }

        /// <summary>
        /// Start a dedicated model synchronization task for a specific backend.
        /// </summary>
        /// <param name="backend">Backend to start synchronization for.</param>
        private void StartBackendSynchronizationTask(OllamaBackend backend)
        {
            if (backend == null || String.IsNullOrEmpty(backend.Identifier)) return;

            // Create cancellation token source for this backend
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            _BackendTokenSources.TryAdd(backend.Identifier, tokenSource);

            // Create combined token that respects both service shutdown and individual backend cancellation
            CancellationToken combinedToken = CancellationTokenSource.CreateLinkedTokenSource(_TokenSource.Token, tokenSource.Token).Token;

            // Start the dedicated synchronization task
            Task syncTask = Task.Run(async () => await BackendSynchronizationLoop(backend, combinedToken), combinedToken);
            _BackendTasks.TryAdd(backend.Identifier, syncTask);
        }

        /// <summary>
        /// Stop and cleanup the dedicated synchronization task for a specific backend.
        /// </summary>
        /// <param name="identifier">Backend identifier.</param>
        private void StopBackendSynchronizationTask(string identifier)
        {
            if (String.IsNullOrEmpty(identifier)) return;

            // Cancel the backend-specific token
            if (_BackendTokenSources.TryRemove(identifier, out CancellationTokenSource tokenSource))
            {
                try
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                }
                catch (Exception ex)
                {
                    _Logging.Warn(_Header + $"error cancelling token for backend {identifier}: {ex.Message}");
                }
            }

            // Wait for and cleanup the task
            if (_BackendTasks.TryRemove(identifier, out Task task))
            {
                try
                {
                    // Give the task a short time to terminate gracefully
                    if (!task.Wait(TimeSpan.FromSeconds(5)))
                    {
                        _Logging.Warn(_Header + $"synchronization task for backend {identifier} did not terminate within timeout");
                    }
                }
                catch (Exception ex)
                {
                    _Logging.Warn(_Header + $"error waiting for synchronization task for backend {identifier}: {ex.Message}");
                }
            }

            // Remove and dispose semaphore for this backend
            if (_BackendSemaphores.TryRemove(identifier, out SemaphoreSlim semaphore))
            {
                semaphore.Dispose();
            }

            // Remove any active pulls for this backend
            _ActivePulls.TryRemove(identifier, out ConcurrentDictionary<string, bool> activePulls);

            _Logging.Debug(_Header + $"stopped and cleaned up synchronization task for backend {identifier}");
        }

        /// <summary>
        /// Main model discovery and synchronization for a single backend.
        /// </summary>
        /// <param name="backend">Backend to monitor.</param>
        /// <param name="token">Cancellation token.</param>
        private async Task BackendSynchronizationLoop(OllamaBackend backend, CancellationToken token)
        {
            _Logging.Debug(_Header + $"starting model discovery and synchronization for backend {backend.Identifier}");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_IntervalMs, token).ConfigureAwait(false);

                    if (token.IsCancellationRequested) break;

                    // Skip if backend is not active
                    if (!backend.Active)
                    {
                        await Task.Delay(1000, token).ConfigureAwait(false);
                        continue;
                    }

                    // Check if backend is healthy
                    List<OllamaBackend> healthyBackends = _HealthCheckService.Backends.Where(b => b.Identifier == backend.Identifier && b.Healthy && b.Active).ToList();
                    if (healthyBackends.Count == 0)
                    {
                        await Task.Delay(5000, token).ConfigureAwait(false); // Wait longer if unhealthy
                        continue;
                    }

                    await SynchronizeModelsForBackend(backend, token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _Logging.Error(_Header + $"error in synchronization for backend {backend.Identifier}: {ex.Message}");

                    try
                    {
                        await Task.Delay(10000, token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _Logging.Debug(_Header + $"model discovery and synchronization terminated for backend {backend.Identifier}");
        }

        /// <summary>
        /// Discover and synchronize models for a specific backend.
        /// </summary>
        /// <param name="backend">Backend to discover and synchronize models for.</param>
        /// <param name="token">Cancellation token.</param>
        private async Task SynchronizeModelsForBackend(OllamaBackend backend, CancellationToken token)
        {
            try
            {
                // Get all frontends that use this backend
                List<OllamaFrontend> frontends = _FrontendService.GetAll()?.ToList() ?? new List<OllamaFrontend>();
                List<string> requiredModels = new List<string>();

                foreach (OllamaFrontend frontend in frontends)
                {
                    if (frontend.Backends != null && frontend.Backends.Contains(backend.Identifier))
                    {
                        if (frontend.RequiredModels != null)
                        {
                            requiredModels.AddRange(frontend.RequiredModels);
                        }
                    }
                }

                // Remove duplicates
                _Logging.Debug(_Header + $"backend {backend.Identifier} raw required models (before distinct): [{string.Join(", ", requiredModels)}]");
                requiredModels = requiredModels.Distinct().ToList();
                _Logging.Debug(_Header + $"backend {backend.Identifier} required models (after distinct): [{string.Join(", ", requiredModels)}]");

                if (requiredModels.Count == 0)
                {
                    return; // No models required for this backend
                }

                // Discover currently available models for this backend
                List<string> availableModels = await DiscoverModelsForBackend(backend, token).ConfigureAwait(false);

                // Update cached models for this backend
                _BackendModels.AddOrUpdate(backend.Identifier, availableModels, (key, oldValue) => availableModels);

                // Get currently active pulls for this backend
                ConcurrentDictionary<string, bool> backendActivePulls = _ActivePulls.GetOrAdd(backend.Identifier, new ConcurrentDictionary<string, bool>());

                // Debug logging for active pulls
                _Logging.Debug(_Header + $"backend {backend.Identifier} checking active pulls (count: {backendActivePulls.Count})");
                if (backendActivePulls.Count > 0)
                {
                    _Logging.Debug(_Header + $"backend {backend.Identifier} has active pulls: [{string.Join(", ", backendActivePulls.Keys)}]");
                }

                // Find missing models with proper string matching (including partial name matching), excluding models currently being pulled
                List<string> missingModels = requiredModels.Where(required =>
                    !availableModels.Any(available =>
                        available.Equals(required, StringComparison.OrdinalIgnoreCase) ||
                        available.StartsWith(required + ":", StringComparison.OrdinalIgnoreCase)) &&
                    !backendActivePulls.ContainsKey(required)).ToList();

                if (missingModels.Count > 0)
                {
                    _Logging.Info(_Header + $"backend {backend.Identifier} is missing {missingModels.Count} required models: [{string.Join(", ", missingModels)}]");

                    // Pull missing models, but only start pulls for models not already being pulled
                    List<Task> pullTasks = new List<Task>();
                    _Logging.Debug(_Header + $"backend {backend.Identifier} starting process for {missingModels.Count} missing models");

                    foreach (string model in missingModels)
                    {
                        _Logging.Debug(_Header + $"backend {backend.Identifier} checking if {model} is already being pulled...");

                        // Double-check and atomically add to active pulls before starting
                        if (backendActivePulls.TryAdd(model, true))
                        {
                            _Logging.Info(_Header + $"starting pull of model {model} to backend {backend.Identifier}");
                            pullTasks.Add(PullModelToBackendInternal(backend, model, token));
                        }
                        else
                        {
                            _Logging.Info(_Header + $"skipping pull request on backend {backend.Identifier} for model {model}, pull already in progress");
                        }
                    }

                    _Logging.Debug(_Header + $"backend {backend.Identifier} created {pullTasks.Count} pull tasks");

                    if (pullTasks.Count > 0)
                    {
                        await Task.WhenAll(pullTasks).ConfigureAwait(false);
                    }
                }
                else
                {
                    _Logging.Debug(_Header + $"backend {backend.Identifier} has all required models");
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Task was cancelled, this is expected
                throw;
            }
            catch (Exception ex)
            {
                _Logging.Debug(_Header + $"error synchronizing models for backend {backend.Identifier}: {ex.Message}");
            }
        }

        /// <summary>
        /// Pull a specific model to a backend (internal version that assumes active pull tracking is already handled).
        /// </summary>
        /// <param name="backend">Backend to pull model to.</param>
        /// <param name="modelName">Name of model to pull.</param>
        /// <param name="token">Cancellation token.</param>
        private async Task PullModelToBackendInternal(OllamaBackend backend, string modelName, CancellationToken token)
        {
            if (!_BackendSemaphores.TryGetValue(backend.Identifier, out SemaphoreSlim semaphore))
            {
                _Logging.Warn(_Header + $"no semaphore found for backend {backend.Identifier}, cannot pull model {modelName}");
                return;
            }

            ConcurrentDictionary<string, bool> backendPulls = _ActivePulls.GetOrAdd(backend.Identifier, new ConcurrentDictionary<string, bool>());

            try
            {
                await semaphore.WaitAsync(token).ConfigureAwait(false);

                try
                {
                    await PerformModelPull(backend, modelName, token).ConfigureAwait(false);
                }
                finally
                {
                    semaphore.Release();
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Task was cancelled, this is expected
                throw;
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"error pulling model {modelName} to backend {backend.Identifier}: {ex.Message}");
            }
            finally
            {
                // Remove from active pulls
                if (backendPulls.TryRemove(modelName, out bool removed))
                {
                    _Logging.Debug(_Header + $"removed {modelName} from active pulls for backend {backend.Identifier}");
                }
                else
                {
                    _Logging.Warn(_Header + $"failed to remove {modelName} from active pulls for backend {backend.Identifier} - was not found");
                }
            }
        }

        /// <summary>
        /// Discover models for a specific backend.
        /// </summary>
        /// <param name="backend">Backend to discover models for.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of discovered model names.</returns>
        private async Task<List<string>> DiscoverModelsForBackend(OllamaBackend backend, CancellationToken token)
        {
            try
            {
                string baseUrl = (backend.Ssl ? "https://" : "http://") + backend.Hostname + ":" + backend.Port;
                string tagsUrl = baseUrl + "/api/tags";

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, tagsUrl);
                    HttpResponseMessage response = await client.SendAsync(request, token).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        // Parse the JSON response to extract model names
                        List<string> models = ParseModelsFromResponse(responseBody);

                        _Logging.Debug(_Header + $"discovered {models.Count} models for backend {backend.Identifier}: [{string.Join(", ", models)}]");
                        return models;
                    }
                    else
                    {
                        _Logging.Debug(_Header + $"failed to discover models for backend {backend.Identifier}: HTTP {response.StatusCode}");
                        return new List<string>();
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Task was cancelled, this is expected
                throw;
            }
            catch (Exception ex)
            {
                _Logging.Debug(_Header + $"error discovering models for backend {backend.Identifier}: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Parse model names from Ollama /api/tags response.
        /// </summary>
        /// <param name="responseBody">JSON response body.</param>
        /// <returns>List of model names.</returns>
        private List<string> ParseModelsFromResponse(string responseBody)
        {
            List<string> models = new List<string>();

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    if (doc.RootElement.TryGetProperty("models", out JsonElement modelsElement) && modelsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement modelElement in modelsElement.EnumerateArray())
                        {
                            if (modelElement.TryGetProperty("name", out JsonElement nameElement) && nameElement.ValueKind == JsonValueKind.String)
                            {
                                string modelName = nameElement.GetString();
                                if (!String.IsNullOrEmpty(modelName))
                                {
                                    models.Add(modelName);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + $"error parsing models from response: {ex.Message}");
            }

            return models;
        }

        /// <summary>
        /// Perform the actual model pull operation.
        /// </summary>
        /// <param name="backend">Backend to pull model to.</param>
        /// <param name="modelName">Name of model to pull.</param>
        /// <param name="token">Cancellation token.</param>
        private async Task PerformModelPull(OllamaBackend backend, string modelName, CancellationToken token)
        {
            try
            {
                string baseUrl = (backend.Ssl ? "https://" : "http://") + backend.Hostname + ":" + backend.Port;
                string pullUrl = baseUrl + "/api/pull";

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(30); // Long timeout for model pulls

                    string jsonBody = JsonSerializer.Serialize(new { name = modelName });
                    StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");


                    HttpResponseMessage response = await client.PostAsync(pullUrl, content, token).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        _Logging.Debug(_Header + $"successfully initiated pull of model {modelName} to backend {backend.Identifier}");
                    }
                    else
                    {
                        _Logging.Warn(_Header + $"failed to pull model {modelName} to backend {backend.Identifier}: HTTP {response.StatusCode}");
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Task was cancelled, this is expected
                throw;
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"error during model pull {modelName} to backend {backend.Identifier}: {ex.Message}");
            }
        }

        #endregion
    }
}