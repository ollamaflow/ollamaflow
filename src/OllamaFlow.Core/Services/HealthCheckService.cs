namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Serialization;
    using SyslogLogging;

    /// <summary>
    /// Health check service.
    /// </summary>
    public class HealthCheckService : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Interval, in milliseconds.
        /// Default is 5000.
        /// Minimum is 1000.
        /// </summary>
        public int IntervalMs
        {
            get
            {
                return _IntervalMs;
            }
            set
            {
                if (value < 1000) throw new ArgumentOutOfRangeException(nameof(IntervalMs));
                _IntervalMs = value;
            }
        }

        /// <summary>
        /// Retrieve the list of frontends in memory.
        /// </summary>
        public List<OllamaFrontend> Frontends
        {
            get
            {
                if (_Frontends == null) return new List<OllamaFrontend>();
                return _Frontends.Values.ToList();
            }
        }

        /// <summary>
        /// Retrieve the list of backends in memory.
        /// </summary>
        public List<OllamaBackend> Backends
        {
            get
            {
                if (_Backends == null) return new List<OllamaBackend>();
                return _Backends.Values.ToList();
            }
        }

        #endregion

        #region Private-Members

        private readonly string _Header = "[HealthCheckService] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private Random _Random = new Random(Guid.NewGuid().GetHashCode());
        private bool _IsDisposed = false;

        private FrontendService _FrontendService = null;
        private BackendService _BackendService = null;
        private SessionStickinessService _SessionStickiness = null;

        private CancellationTokenSource _TokenSource = new CancellationTokenSource();

        private int _IntervalMs = 5000;

        private ConcurrentDictionary<string, OllamaFrontend> _Frontends = new ConcurrentDictionary<string, OllamaFrontend>();
        private ConcurrentDictionary<string, OllamaBackend> _Backends = new ConcurrentDictionary<string, OllamaBackend>();

        // Per-backend task management
        private ConcurrentDictionary<string, Task> _BackendTasks = new ConcurrentDictionary<string, Task>();
        private ConcurrentDictionary<string, CancellationTokenSource> _BackendTokenSources = new ConcurrentDictionary<string, CancellationTokenSource>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Health check service.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="frontend">Frontend service.</param>
        /// <param name="backend">Backend service.</param>
        /// <param name="sessionStickiness">Session stickiness service.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public HealthCheckService(
            OllamaFlowSettings settings,
            LoggingModule logging,
            Serializer serializer,
            FrontendService frontend,
            BackendService backend,
            SessionStickinessService sessionStickiness,
            CancellationTokenSource tokenSource = default)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _FrontendService = frontend ?? throw new ArgumentNullException(nameof(frontend));
            _BackendService = backend ?? throw new ArgumentNullException(nameof(backend));
            _SessionStickiness = sessionStickiness ?? throw new ArgumentNullException(nameof(sessionStickiness));
            _TokenSource = tokenSource ?? new CancellationTokenSource();

            // Initialize existing frontends and backends from database
            InitializeExistingNodes();

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
                        StopBackendHealthCheckTask(backendId);
                    }

                    // Cancel main token source
                    if (_TokenSource != null && !_TokenSource.IsCancellationRequested)
                    {
                        _TokenSource.Cancel();
                        _TokenSource.Dispose();
                    }

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

        /// <summary>
        /// Retrieve the next backend that should be used for a request to a given frontend.
        /// </summary>
        /// <param name="frontend">Frontend.</param>
        /// <returns>Backend.</returns>
        /// <exception cref="ArgumentNullException">Thrown when frontend is null.</exception>
        public OllamaBackend GetNextBackend(OllamaFrontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));

            List<OllamaBackend> candidates = new List<OllamaBackend>();

            if (frontend.Backends != null && frontend.Backends.Count > 0)
            {
                foreach (string backendId in frontend.Backends)
                {
                    if (_Backends.TryGetValue(backendId, out OllamaBackend backend))
                    {
                        if (backend.Active && backend.Healthy) candidates.Add(backend);
                    }
                }
            }

            if (candidates.Count > 0)
            {
                if (frontend.LoadBalancing == LoadBalancingMode.RoundRobin)
                {
                    int index = _Random.Next(0, candidates.Count);
                    _Logging.Debug(_Header + "returning index " + index + " of " + candidates.Count + " candidates for frontend " + frontend.Identifier + " " + frontend.Name);
                    return candidates[index];
                }
                else if (frontend.LoadBalancing == LoadBalancingMode.Random)
                {
                    int index = _Random.Next(0, candidates.Count);
                    _Logging.Debug(_Header + "returning index " + index + " of " + candidates.Count + " candidates for frontend " + frontend.Identifier + " " + frontend.Name);
                    return candidates[index];
                }
                else
                {
                    throw new ArgumentException("Unknown load-balancing mode " + frontend.LoadBalancing.ToString() + ".");
                }
            }
            else
            {
                _Logging.Warn(_Header + "unable to find backend for frontend " + frontend.Identifier);
                return null;
            }
        }

        /// <summary>
        /// Retrieve the next backend that should be used for a request to a given frontend, considering sticky sessions.
        /// </summary>
        /// <param name="frontend">Frontend.</param>
        /// <param name="clientId">Client identifier for sticky session lookup.</param>
        /// <returns>Backend.</returns>
        /// <exception cref="ArgumentNullException">Thrown when frontend or clientId is null.</exception>
        public OllamaBackend GetNextBackend(OllamaFrontend frontend, string clientId)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));
            if (String.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));

            // Check if sticky sessions are enabled for this frontend
            if (frontend.UseStickySessions)
            {
                // Try to get existing sticky backend
                string stickyBackendId = _SessionStickiness.GetStickyBackend(clientId, frontend.Identifier);

                if (!String.IsNullOrEmpty(stickyBackendId))
                {
                    // Verify the sticky backend is still available and healthy
                    if (_Backends.TryGetValue(stickyBackendId, out OllamaBackend stickyBackend))
                    {
                        if (stickyBackend.Active && stickyBackend.Healthy)
                        {
                            // Touch the session to extend its expiration
                            _SessionStickiness.TouchSession(clientId, frontend.Identifier, frontend.StickySessionExpirationMs);
                            _Logging.Debug(_Header + "using sticky backend " + stickyBackendId + " for client " + clientId + " frontend " + frontend.Identifier);
                            
                            stickyBackend.IsSticky = true;
                            return stickyBackend;
                        }
                        else
                        {
                            // Backend is unhealthy or inactive, remove the sticky session
                            _SessionStickiness.RemoveSession(clientId, frontend.Identifier);
                            _Logging.Debug(_Header + "sticky backend " + stickyBackendId + " is unhealthy/inactive, removed session for client " + clientId + " frontend " + frontend.Identifier);
                        }
                    }
                    else
                    {
                        // Backend no longer exists, remove the sticky session
                        _SessionStickiness.RemoveSession(clientId, frontend.Identifier);
                        _Logging.Debug(_Header + "sticky backend " + stickyBackendId + " no longer exists, removed session for client " + clientId + " frontend " + frontend.Identifier);
                    }
                }

                // No valid sticky session, select a new backend using normal load balancing
                OllamaBackend selectedBackend = GetNextBackend(frontend);

                if (selectedBackend != null)
                {
                    // Create new sticky session
                    _SessionStickiness.SetStickyBackend(clientId, frontend.Identifier, selectedBackend.Identifier, frontend.StickySessionExpirationMs);
                    selectedBackend.IsSticky = frontend.UseStickySessions;
                }

                return selectedBackend;
            }
            else
            {
                // Sticky sessions not enabled, use normal load balancing
                return GetNextBackend(frontend);
            }
        }

        /// <summary>
        /// Update a backend if cached.
        /// </summary>
        /// <param name="backend">Backend.</param>
        /// <exception cref="ArgumentNullException">Thrown when backend is null.</exception>
        public void UpdateBackend(OllamaBackend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));

            if (_Backends.TryGetValue(backend.Identifier, out OllamaBackend cached))
            {
                cached.Name = backend.Name;
                cached.Hostname = backend.Hostname;
                cached.Port = backend.Port;
                cached.Ssl = backend.Ssl;
                cached.UnhealthyThreshold = backend.UnhealthyThreshold;
                cached.HealthyThreshold = backend.HealthyThreshold;
                cached.HealthCheckMethod = backend.HealthCheckMethod;
                cached.HealthCheckUrl = backend.HealthCheckUrl;
                cached.MaxParallelRequests = backend.MaxParallelRequests;
                cached.RateLimitRequestsThreshold = backend.RateLimitRequestsThreshold;
                cached.LogRequestFull = backend.LogRequestFull;
                cached.LogRequestBody = backend.LogRequestBody;
                cached.LogResponseBody = backend.LogResponseBody;
                cached.Active = backend.Active;

                _Logging.Debug(_Header + "updated cached backend " + backend.Identifier);
            }
        }

        /// <summary>
        /// Add a new backend to health monitoring.
        /// </summary>
        /// <param name="backend">Backend to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when backend is null.</exception>
        public void AddBackend(OllamaBackend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));

            OllamaBackend newBackend = new OllamaBackend
            {
                Identifier = backend.Identifier,
                Name = backend.Name,
                Hostname = backend.Hostname,
                Port = backend.Port,
                Ssl = backend.Ssl,
                UnhealthyThreshold = backend.UnhealthyThreshold,
                HealthyThreshold = backend.HealthyThreshold,
                HealthCheckMethod = backend.HealthCheckMethod,
                HealthCheckUrl = backend.HealthCheckUrl,
                MaxParallelRequests = backend.MaxParallelRequests,
                RateLimitRequestsThreshold = backend.RateLimitRequestsThreshold,
                LogRequestFull = backend.LogRequestFull,
                LogRequestBody = backend.LogRequestBody,
                LogResponseBody = backend.LogResponseBody,
                Active = backend.Active,
                UnhealthySinceUtc = DateTime.UtcNow,
                Healthy = false
            };

            if (_Backends.TryAdd(newBackend.Identifier, newBackend))
            {
                // Start dedicated health check task for this backend
                StartBackendHealthCheckTask(newBackend);
                _Logging.Debug(_Header + "added backend " + backend.Identifier + " to health monitoring with dedicated task");
            }
        }

        /// <summary>
        /// Remove a backend from health monitoring.
        /// </summary>
        /// <param name="identifier">Backend identifier to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown when identifier is null.</exception>
        public void RemoveBackend(string identifier)
        {
            if (String.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));

            if (_Backends.TryRemove(identifier, out OllamaBackend removed))
            {
                // Stop and cleanup the dedicated health check task for this backend
                StopBackendHealthCheckTask(identifier);

                // Remove all sticky sessions associated with this backend
                int removedSessions = _SessionStickiness.RemoveBackendSessions(identifier);

                _Logging.Debug(_Header + "removed backend " + identifier + " from health monitoring and stopped dedicated task, removed " + removedSessions + " sticky sessions");
            }
        }

        /// <summary>
        /// Update a frontend if cached.
        /// </summary>
        /// <param name="frontend">Frontend to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when frontend is null.</exception>
        public void UpdateFrontend(OllamaFrontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));

            if (_Frontends.TryGetValue(frontend.Identifier, out OllamaFrontend cached))
            {
                cached.Name = frontend.Name;
                cached.Hostname = frontend.Hostname;
                cached.LoadBalancing = frontend.LoadBalancing;
                cached.Backends = frontend.Backends;
                cached.RequiredModels = frontend.RequiredModels;

                _Logging.Debug(_Header + "updated cached frontend " + frontend.Identifier);
            }
        }

        /// <summary>
        /// Add a new frontend to monitoring.
        /// </summary>
        /// <param name="frontend">Frontend to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when frontend is null.</exception>
        public void AddFrontend(OllamaFrontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));

            _Frontends.TryAdd(frontend.Identifier, frontend);
            _Logging.Debug(_Header + "added frontend " + frontend.Identifier + " to monitoring");
        }

        /// <summary>
        /// Remove a frontend from monitoring.
        /// </summary>
        /// <param name="identifier">Frontend identifier to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown when identifier is null.</exception>
        public void RemoveFrontend(string identifier)
        {
            if (String.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));

            if (_Frontends.TryRemove(identifier, out OllamaFrontend removed))
            {
                // Remove all sticky sessions associated with this frontend
                int removedSessions = _SessionStickiness.RemoveFrontendSessions(identifier);

                _Logging.Debug(_Header + "removed frontend " + identifier + " from monitoring, removed " + removedSessions + " sticky sessions");
            }
        }

        #endregion

        #region Private-Methods

        private void InitializeExistingNodes()
        {
            try
            {
                // Load existing frontends
                List<OllamaFrontend> frontends = _FrontendService.GetAll()?.ToList() ?? new List<OllamaFrontend>();
                foreach (OllamaFrontend frontend in frontends)
                {
                    _Frontends.TryAdd(frontend.Identifier, frontend);
                }

                // Load existing backends and start health check tasks
                List<OllamaBackend> backends = _BackendService.GetAll()?.ToList() ?? new List<OllamaBackend>();
                foreach (OllamaBackend backend in backends)
                {
                    backend.UnhealthySinceUtc = DateTime.UtcNow;
                    backend.Healthy = false;

                    if (_Backends.TryAdd(backend.Identifier, backend))
                    {
                        StartBackendHealthCheckTask(backend);
                    }
                }

                _Logging.Debug(_Header + $"initialized {frontends.Count} frontends and {backends.Count} backends with dedicated tasks");
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + "error initializing existing nodes: " + ex.Message);
            }
        }

        private void StartBackendHealthCheckTask(OllamaBackend backend)
        {
            if (backend == null || String.IsNullOrEmpty(backend.Identifier)) return;

            // Create cancellation token source for this backend
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            _BackendTokenSources.TryAdd(backend.Identifier, tokenSource);

            // Create combined token that respects both service shutdown and individual backend cancellation
            CancellationToken combinedToken = CancellationTokenSource.CreateLinkedTokenSource(_TokenSource.Token, tokenSource.Token).Token;

            // Start the dedicated health check task
            Task healthCheckTask = Task.Run(async () => await BackendHealthCheckLoop(backend, combinedToken), combinedToken);
            _BackendTasks.TryAdd(backend.Identifier, healthCheckTask);

            _Logging.Debug(_Header + $"started dedicated health check task for backend {backend.Identifier}");
        }

        private void StopBackendHealthCheckTask(string identifier)
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
                        _Logging.Warn(_Header + $"health check task for backend {identifier} did not terminate within timeout");
                    }
                }
                catch (Exception ex)
                {
                    _Logging.Warn(_Header + $"error waiting for health check task for backend {identifier}: {ex.Message}");
                }
            }

            _Logging.Debug(_Header + $"stopped and cleaned up health check task for backend {identifier}");
        }

        private async Task BackendHealthCheckLoop(OllamaBackend backend, CancellationToken token)
        {
            string healthCheckUrl = (backend.Ssl ? "https://" : "http://") + backend.Hostname + ":" + backend.Port + backend.HealthCheckUrl;

            _Logging.Debug(_Header + $"starting health check for backend {backend.Identifier} at {healthCheckUrl}");

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

                    await PerformHealthCheck(backend, healthCheckUrl, token).ConfigureAwait(false);
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
                    _Logging.Error(_Header + $"error in health check for backend {backend.Identifier}: {ex.Message}");

                    // Brief delay before retrying to avoid tight error loops
                    try
                    {
                        await Task.Delay(5000, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _Logging.Debug(_Header + $"health check terminated for backend {backend.Identifier}");
        }

        private async Task PerformHealthCheck(OllamaBackend backend, string healthCheckUrl, CancellationToken token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMilliseconds(_IntervalMs / 2); // Half of interval for timeout

                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(backend.HealthCheckMethod, healthCheckUrl);
                    HttpResponseMessage response = await client.SendAsync(request, token).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        HandleHealthCheckSuccess(backend, healthCheckUrl);
                    }
                    else
                    {
                        HandleHealthCheckFailure(backend, healthCheckUrl, $"HTTP {response.StatusCode}");
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // Task was cancelled, this is expected
                    throw;
                }
                catch (Exception ex)
                {
                    HandleHealthCheckFailure(backend, healthCheckUrl, ex.Message);
                }
            }
        }

        private void HandleHealthCheckSuccess(OllamaBackend backend, string healthCheckUrl)
        {
            if (_Backends.TryGetValue(backend.Identifier, out OllamaBackend cached))
            {
                lock (cached.Lock)
                {
                    if (cached.HealthCheckSuccess < 99) cached.HealthCheckSuccess++;
                    cached.HealthCheckFailure = 0;

                    if (!cached.Healthy && cached.HealthCheckSuccess >= cached.HealthyThreshold)
                    {
                        cached.Healthy = true;
                        cached.HealthySinceUtc = DateTime.UtcNow;
                        cached.UnhealthySinceUtc = null;

                        _Logging.Info(_Header + $"backend {cached.Identifier} ({cached.Name}) is now healthy");
                    }
                }
            }

            _Logging.Debug(_Header + $"health check success for backend {backend.Identifier} at {healthCheckUrl}");
        }

        private void HandleHealthCheckFailure(OllamaBackend backend, string healthCheckUrl, string reason)
        {
            if (_Backends.TryGetValue(backend.Identifier, out OllamaBackend cached))
            {
                lock (cached.Lock)
                {
                    if (cached.HealthCheckFailure < 99) cached.HealthCheckFailure++;
                    cached.HealthCheckSuccess = 0;

                    if (cached.Healthy && cached.HealthCheckFailure >= cached.UnhealthyThreshold)
                    {
                        cached.Healthy = false;
                        cached.UnhealthySinceUtc = DateTime.UtcNow;
                        cached.HealthySinceUtc = null;

                        _Logging.Warn(_Header + $"backend {cached.Identifier} ({cached.Name}) is now unhealthy");
                    }
                }
            }

            _Logging.Debug(_Header + $"health check failure for backend {backend.Identifier} at {healthCheckUrl}: {reason}");
        }

        #endregion
    }
}