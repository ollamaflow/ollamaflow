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
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Serialization;
    using SyslogLogging;
    using OllamaFlow.Core.Models;

    /// <summary>
    /// Model synchronization service.
    /// Only operates on Ollama backends since OpenAI/vLLM backends don't support runtime model pulling.
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
        private ConcurrentDictionary<string, ManualResetEventSlim> _BackendWaitEvents = new ConcurrentDictionary<string, ManualResetEventSlim>();

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
            InitializeExistingBackends();

            _Logging.Debug(_Header + "initialization complete");
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
        /// Only operates on Ollama backends since OpenAI/vLLM don't support runtime model pulling.
        /// </summary>
        /// <param name="backend">Backend that was added.</param>
        /// <exception cref="ArgumentNullException">Thrown when backend is null.</exception>
        public void AddBackend(Backend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));

            // Only start synchronization for Ollama backends
            if (backend.ApiFormat != ApiFormatEnum.Ollama)
            {
                _Logging.Debug(_Header + $"skipping model synchronization for non-Ollama backend {backend.Identifier} (format: {backend.ApiFormat})");
                return;
            }

            _Logging.Debug(_Header + $"adding Ollama backend {backend.Identifier} for model synchronization");

            // Create semaphore for new backend to limit concurrent downloads
            SemaphoreSlim semaphore = new SemaphoreSlim(_MaxConcurrentDownloadsPerBackend, _MaxConcurrentDownloadsPerBackend);
            _BackendSemaphores.TryAdd(backend.Identifier, semaphore);

            // Start dedicated synchronization task for this backend
            StartBackendSynchronizationTask(backend);

            // Trigger immediate sync for the new backend
            if (_BackendWaitEvents.TryGetValue(backend.Identifier, out ManualResetEventSlim waitEvent))
            {
                _Logging.Debug(_Header + "triggering immediate sync for new backend " + backend.Identifier);
                waitEvent.Set();
            }

            _Logging.Debug(_Header + "notified of new backend " + backend.Identifier + " and started synchronization task");
        }

        /// <summary>
        /// Update a backend configuration.
        /// </summary>
        /// <param name="backend">Backend that was updated.</param>
        /// <exception cref="ArgumentNullException">Thrown when backend is null.</exception>
        public void UpdateBackend(Backend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));

            // For Ollama backends, check if configuration changes require task restart
            if (backend.ApiFormat == ApiFormatEnum.Ollama)
            {
                // Get the current backend from database to check what changed
                Backend currentBackend = _BackendService.GetByIdentifier(backend.Identifier);
                if (currentBackend != null)
                {
                    // Check if critical properties that affect synchronization have changed
                    bool needsRestart = _BackendTasks.ContainsKey(backend.Identifier);

                    if (needsRestart)
                    {
                        _Logging.Debug(_Header + "restarting synchronization task for backend " + backend.Identifier + " due to configuration changes");
                        StopBackendSynchronizationTask(backend.Identifier);

                        // Create new semaphore if it doesn't exist
                        if (!_BackendSemaphores.ContainsKey(backend.Identifier))
                        {
                            SemaphoreSlim semaphore = new SemaphoreSlim(_MaxConcurrentDownloadsPerBackend, _MaxConcurrentDownloadsPerBackend);
                            _BackendSemaphores.TryAdd(backend.Identifier, semaphore);
                        }

                        StartBackendSynchronizationTask(backend);

                        // Trigger immediate sync for the updated backend
                        if (_BackendWaitEvents.TryGetValue(backend.Identifier, out ManualResetEventSlim waitEvent))
                        {
                            _Logging.Debug(_Header + "triggering immediate sync for updated backend " + backend.Identifier);
                            waitEvent.Set();
                        }
                    }
                }
            }

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
        public void AddFrontend(Frontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));
            _Logging.Debug(_Header + "notified of new frontend " + frontend.Identifier + " - will sync required models");
        }

        /// <summary>
        /// Update a frontend configuration and sync required models.
        /// </summary>
        /// <param name="frontend">Frontend that was updated.</param>
        /// <exception cref="ArgumentNullException">Thrown when frontend is null.</exception>
        public void UpdateFrontend(Frontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));

            _Logging.Debug(_Header + "notified of updated frontend " + frontend.Identifier);

            // Trigger immediate synchronization for all backends used by this frontend
            if (frontend.Backends != null && frontend.Backends.Count > 0)
            {
                foreach (string backendId in frontend.Backends)
                {
                    // Signal immediate sync by setting the wait event for the backend
                    if (_BackendWaitEvents.TryGetValue(backendId, out ManualResetEventSlim waitEvent))
                    {
                        _Logging.Debug(_Header + "triggering immediate sync for backend " + backendId + " due to frontend " + frontend.Identifier + " update");
                        waitEvent.Set();
                    }
                }
            }
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

        #region Private-Methods

        private void InitializeExistingBackends()
        {
            try
            {
                // Load existing backends and start synchronization tasks
                // Only synchronize models for Ollama backends since OpenAI/vLLM don't support runtime model pulling
                List<Backend> allBackends = _BackendService.GetAll()?.ToList() ?? new List<Backend>();
                List<Backend> backends = allBackends.Where(b => b.ApiFormat == ApiFormatEnum.Ollama).ToList();

                _Logging.Debug(_Header + $"found {allBackends.Count} total backends, {backends.Count} Ollama backends for model synchronization");

                foreach (Backend backend in backends)
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

        private void StartBackendSynchronizationTask(Backend backend)
        {
            if (backend == null || String.IsNullOrEmpty(backend.Identifier)) return;

            // Create cancellation token source for this backend
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            _BackendTokenSources.TryAdd(backend.Identifier, tokenSource);

            // Create wait event for immediate synchronization triggering
            ManualResetEventSlim waitEvent = new ManualResetEventSlim(false);
            _BackendWaitEvents.TryAdd(backend.Identifier, waitEvent);

            // Create combined token that respects both service shutdown and individual backend cancellation
            CancellationToken combinedToken = CancellationTokenSource.CreateLinkedTokenSource(_TokenSource.Token, tokenSource.Token).Token;

            // Start the dedicated synchronization task
            Task syncTask = Task.Run(async () => await BackendSynchronizationLoop(backend, combinedToken), combinedToken);
            _BackendTasks.TryAdd(backend.Identifier, syncTask);
        }

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

            // Cleanup wait event
            if (_BackendWaitEvents.TryRemove(identifier, out ManualResetEventSlim waitEvent))
            {
                try
                {
                    waitEvent.Set(); // Wake any waiting threads
                    waitEvent.Dispose();
                }
                catch (Exception ex)
                {
                    _Logging.Warn(_Header + $"error disposing wait event for backend {identifier}: {ex.Message}");
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

        private async Task BackendSynchronizationLoop(Backend backend, CancellationToken token)
        {
            _Logging.Debug(_Header + $"starting model discovery and synchronization for backend {backend.Identifier}");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Wait for the interval OR until signaled for immediate sync
                    if (_BackendWaitEvents.TryGetValue(backend.Identifier, out ManualResetEventSlim waitEvent))
                    {
                        waitEvent.Wait(_IntervalMs, token);
                        waitEvent.Reset(); // Reset for next cycle
                    }
                    else
                    {
                        await Task.Delay(_IntervalMs, token).ConfigureAwait(false);
                    }

                    if (token.IsCancellationRequested) break;

                    // Skip if backend is not active
                    if (!backend.Active)
                    {
                        await Task.Delay(1000, token).ConfigureAwait(false);
                        continue;
                    }

                    // Check if backend is healthy
                    List<Backend> healthyBackends = _HealthCheckService.Backends.Where(b => b.Identifier == backend.Identifier && b.Healthy && b.Active).ToList();
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

        private async Task SynchronizeModelsForBackend(Backend backend, CancellationToken token)
        {
            try
            {
                // Get all frontends that use this backend
                List<Frontend> frontends = _FrontendService.GetAll()?.ToList() ?? new List<Frontend>();
                List<string> requiredModels = new List<string>();

                foreach (Frontend frontend in frontends)
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

        private async Task PullModelToBackendInternal(Backend backend, string modelName, CancellationToken token)
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

        private async Task<List<string>> DiscoverModelsForBackend(Backend backend, CancellationToken token)
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

        private async Task PerformModelPull(Backend backend, string modelName, CancellationToken token)
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