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
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;

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
        public List<Frontend> Frontends
        {
            get
            {
                if (_Frontends == null) return new List<Frontend>();
                return new List<Frontend>(_Frontends.Values);
            }
        }

        /// <summary>
        /// Retrieve the list of backends in memory.
        /// </summary>
        public List<Backend> Backends
        {
            get
            {
                if (_Backends == null) return new List<Backend>();
                return new List<Backend>(_Backends.Values);
            }
        }

        #endregion

        #region Private-Members

        private readonly string _Header = "[HealthCheckService] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private ServiceContext _Services = null;
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private Random _Random = new Random(Guid.NewGuid().GetHashCode());
        private bool _Disposed = false;

        private int _IntervalMs = 5000;

        private ConcurrentDictionary<string, Frontend> _Frontends = new ConcurrentDictionary<string, Frontend>();
        private ConcurrentDictionary<string, Backend> _Backends = new ConcurrentDictionary<string, Backend>();

        // Per-backend task management
        private ConcurrentDictionary<string, Task> _BackendTasks = new ConcurrentDictionary<string, Task>();
        private ConcurrentDictionary<string, CancellationTokenSource> _BackendTokenSources = new ConcurrentDictionary<string, CancellationTokenSource>();

        // Round-robin index tracking per frontend
        private ConcurrentDictionary<string, int> _FrontendRoundRobinIndex = new ConcurrentDictionary<string, int>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Health check service.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="services">Service context.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        internal HealthCheckService(
            OllamaFlowSettings settings,
            LoggingModule logging,
            Serializer serializer,
            ServiceContext services,
            CancellationTokenSource tokenSource = default)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Services = services ?? throw new ArgumentNullException(nameof(services));
            _TokenSource = tokenSource ?? new CancellationTokenSource();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
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

                    // Clear round-robin tracking
                    _FrontendRoundRobinIndex?.Clear();

                    _Random = null;
                    _Serializer = null;
                    _Logging = null;
                    _Settings = null;
                }

                _Disposed = true;
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
        /// Test if a frontend is healthy.
        /// </summary>
        /// <param name="identifier">Identifier.</param>
        /// <returns>True if healthy.</returns>
        public bool IsHealthy(string identifier)
        {
            if (_Backends.TryGetValue(identifier, out Backend backend))
            {
                return backend.Healthy;
            }
            return false;
        }

        #endregion

        #region Internal-Methods

        /// <summary>
        /// Initialize.
        /// </summary>
        internal void Initialize()
        {
            InitializeExistingNodes();

            _Logging.Debug(_Header + "initialized");
        }

        /// <summary>
        /// Retrieve the next backend that should be used for a request to a given frontend.
        /// </summary>
        /// <param name="frontend">Frontend.</param>
        /// <param name="requestType">Type of request to filter backends by capability.</param>
        /// <returns>Backend.</returns>
        /// <exception cref="ArgumentNullException">Thrown when frontend is null.</exception>
        internal Backend GetNextBackend(Frontend frontend, RequestTypeEnum requestType)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));

            List<Backend> candidates = new List<Backend>();

            if (frontend.Backends != null && frontend.Backends.Count > 0)
            {
                foreach (string backendId in frontend.Backends)
                {
                    if (_Backends.TryGetValue(backendId, out Backend backend))
                    {
                        if (backend.Active && backend.Healthy)
                        {
                            if (RequestTypeHelper.IsEmbeddingsRequest(requestType) && !backend.AllowEmbeddings) continue;
                            if (RequestTypeHelper.IsCompletionsRequest(requestType) && !backend.AllowCompletions) continue;
                            candidates.Add(backend);
                        }
                    }
                }
            }

            if (candidates.Count > 0)
            {
                if (frontend.LoadBalancing == LoadBalancingMode.RoundRobin)
                {
                    int currentIndex = _FrontendRoundRobinIndex.GetOrAdd(frontend.Identifier, 0);

                    int nextIndex = Interlocked.Increment(ref currentIndex);
                    _FrontendRoundRobinIndex[frontend.Identifier] = nextIndex;

                    int index = 0;
                    try
                    {
                        index = nextIndex % candidates.Count;

                        if (index < 0 || index >= candidates.Count)
                        {
                            _Logging.Info($"{_Header}round-robin index out of bounds, resetting to 0 for frontend {frontend.Identifier}");
                            index = 0;
                            _FrontendRoundRobinIndex[frontend.Identifier] = 0;
                        }

                        _Logging.Debug($"{_Header}round-robin returning index {index} of {candidates.Count} candidates for frontend {frontend.Identifier}");
                        return candidates[index];
                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        _Logging.Warn($"{_Header}round-robin index out of bounds (exception), resetting to 0 for frontend {frontend.Identifier}{Environment.NewLine}{ex.ToString()}");
                        _FrontendRoundRobinIndex[frontend.Identifier] = 0;
                        return candidates[0];
                    }
                }
                else if (frontend.LoadBalancing == LoadBalancingMode.Random)
                {
                    int index = _Random.Next(0, candidates.Count);
                    _Logging.Debug($"{_Header}returning random index {index} of {candidates.Count} for frontend {frontend.Identifier}");
                    return candidates[index];
                }
                else
                {
                    throw new ArgumentException("Unknown load-balancing mode " + frontend.LoadBalancing.ToString() + ".");
                }
            }
            else
            {
                _Logging.Warn($"{_Header}unable to find backend for frontend {frontend.Identifier}");
                return null;
            }
        }

        /// <summary>
        /// Retrieve the next backend that should be used for a request to a given frontend, considering sticky sessions.
        /// </summary>
        /// <param name="req">Request context.</param>
        /// <param name="frontend">Frontend.</param>
        /// <returns>Backend.</returns>
        internal Backend GetNextBackend(RequestContext req, Frontend frontend)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));

            if (frontend.UseStickySessions)
            {
                string stickyBackendId = _Services.SessionStickiness.GetStickyBackend(req.ClientIdentifier, frontend.Identifier);

                if (!String.IsNullOrEmpty(stickyBackendId))
                {
                    #region Sticky-Session-Exists

                    if (_Backends.TryGetValue(stickyBackendId, out Backend stickyBackend))
                    {
                        if (stickyBackend.Active && stickyBackend.Healthy)
                        {
                            // Check if this backend supports the request type
                            bool supportsRequestType = true;
                            if (req.IsEmbeddingsRequest && !stickyBackend.AllowEmbeddings) supportsRequestType = false;
                            if (req.IsCompletionsRequest && !stickyBackend.AllowCompletions) supportsRequestType = false;

                            if (supportsRequestType)
                            {
                                // Touch the session to extend its expiration
                                _Services.SessionStickiness.TouchSession(req.ClientIdentifier, frontend.Identifier, frontend.StickySessionExpirationMs);
                                _Logging.Debug($"{_Header}using sticky backend {stickyBackendId} for client {req.ClientIdentifier} frontend {frontend.Identifier}");

                                stickyBackend.IsSticky = true;
                                return stickyBackend;
                            }
                            else
                            {
                                _Logging.Debug($"{_Header}sticky backend {stickyBackendId} does not support request type {req.RequestType.ToString()}, selecting new backend");
                            }
                        }
                        else
                        {
                            // Backend is unhealthy or inactive, remove the sticky session
                            _Services.SessionStickiness.RemoveSession(req.ClientIdentifier, frontend.Identifier);
                            _Logging.Debug($"{_Header}sticky backend {stickyBackendId} is unhealthy or inactive, removed session for client {req.ClientIdentifier} frontend {frontend.Identifier}");
                        }
                    }
                    else
                    {
                        // Backend no longer exists, remove the sticky session
                        _Services.SessionStickiness.RemoveSession(req.ClientIdentifier, frontend.Identifier);
                        _Logging.Debug($"{_Header}sticky backend {stickyBackendId} no longer exists, removed session for client {req.ClientIdentifier} frontend {frontend.Identifier}");
                    }

                    #endregion
                }

                // No valid sticky session, select a new backend using normal load balancing
                Backend selectedBackend = GetNextBackend(frontend, req.RequestType);

                if (selectedBackend != null)
                {
                    // Create new sticky session
                    _Services.SessionStickiness.SetStickyBackend(req.ClientIdentifier, frontend.Identifier, selectedBackend.Identifier, frontend.StickySessionExpirationMs);
                    selectedBackend.IsSticky = frontend.UseStickySessions;
                }

                return selectedBackend;
            }
            else
            {
                // Sticky sessions not enabled, use normal load balancing
                return GetNextBackend(frontend, req.RequestType);
            }
        }

        /// <summary>
        /// Update a backend if cached.
        /// </summary>
        /// <param name="backend">Backend.</param>
        /// <exception cref="ArgumentNullException">Thrown when backend is null.</exception>
        internal void UpdateBackend(Backend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));

            if (_Backends.TryGetValue(backend.Identifier, out Backend cached))
            {
                bool needsRestart = 
                    cached.Hostname != backend.Hostname ||
                    cached.Port != backend.Port ||
                    cached.Ssl != backend.Ssl ||
                    cached.HealthCheckMethod != backend.HealthCheckMethod ||
                    cached.HealthCheckUrl != backend.HealthCheckUrl;

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
                cached.ApiFormat = backend.ApiFormat;
                cached.AllowEmbeddings = backend.AllowEmbeddings;
                cached.AllowCompletions = backend.AllowCompletions;
                cached.PinnedEmbeddingsProperties = backend.PinnedEmbeddingsProperties;
                cached.PinnedCompletionsProperties = backend.PinnedCompletionsProperties;
                cached.Active = backend.Active;

                if (needsRestart)
                {
                    _Logging.Debug($"{_Header}restarting health check task for backend {backend.Identifier} due to configuration changes");
                    StopBackendHealthCheckTask(backend.Identifier);
                    StartBackendHealthCheckTask(cached);
                }

                _Logging.Debug($"{_Header}updated cached backend {backend.Identifier}");
            }
        }

        /// <summary>
        /// Add a new backend to health monitoring.
        /// </summary>
        /// <param name="backend">Backend to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when backend is null.</exception>
        internal void AddBackend(Backend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));

            Backend newBackend = new Backend
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
                ApiFormat = backend.ApiFormat,
                AllowEmbeddings = backend.AllowEmbeddings,
                AllowCompletions = backend.AllowCompletions,
                PinnedEmbeddingsProperties = backend.PinnedEmbeddingsProperties,
                PinnedCompletionsProperties = backend.PinnedCompletionsProperties,
                Active = backend.Active,
                UnhealthySinceUtc = DateTime.UtcNow,
                Healthy = false
            };

            if (_Backends.TryAdd(newBackend.Identifier, newBackend))
            {
                StartBackendHealthCheckTask(newBackend);
                _Logging.Debug($"{_Header}added backend {backend.Identifier} to health monitoring");
            }
        }

        /// <summary>
        /// Remove a backend from health monitoring.
        /// </summary>
        /// <param name="identifier">Backend identifier to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown when identifier is null.</exception>
        internal void RemoveBackend(string identifier)
        {
            if (String.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));

            if (_Backends.TryRemove(identifier, out Backend removed))
            {
                StopBackendHealthCheckTask(identifier);
                int removedSessions = _Services.SessionStickiness.RemoveBackendSessions(identifier);

                _Logging.Debug($"{_Header}removed backend {identifier} from health monitoring with {removedSessions} sticky sessions");
            }
        }

        /// <summary>
        /// Update a frontend if cached.
        /// </summary>
        /// <param name="frontend">Frontend to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when frontend is null.</exception>
        internal void UpdateFrontend(Frontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));

            if (_Frontends.TryGetValue(frontend.Identifier, out Frontend cached))
            {
                cached.Name = frontend.Name;
                cached.Hostname = frontend.Hostname;
                cached.TimeoutMs = frontend.TimeoutMs;
                cached.LoadBalancing = frontend.LoadBalancing;
                cached.BlockHttp10 = frontend.BlockHttp10;
                cached.MaxRequestBodySize = frontend.MaxRequestBodySize;
                cached.Backends = frontend.Backends;
                cached.RequiredModels = frontend.RequiredModels;
                cached.LogRequestFull = frontend.LogRequestFull;
                cached.LogRequestBody = frontend.LogRequestBody;
                cached.LogResponseBody = frontend.LogResponseBody;
                cached.UseStickySessions = frontend.UseStickySessions;
                cached.StickySessionExpirationMs = frontend.StickySessionExpirationMs;
                cached.AllowEmbeddings = frontend.AllowEmbeddings;
                cached.AllowCompletions = frontend.AllowCompletions;
                cached.AllowRetries = frontend.AllowRetries;
                cached.PinnedEmbeddingsProperties = frontend.PinnedEmbeddingsProperties;
                cached.PinnedCompletionsProperties = frontend.PinnedCompletionsProperties;
                cached.Active = frontend.Active;

                _Logging.Debug(_Header + "updated cached frontend " + frontend.Identifier);
            }
        }

        /// <summary>
        /// Add a new frontend to monitoring.
        /// </summary>
        /// <param name="frontend">Frontend to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when frontend is null.</exception>
        internal void AddFrontend(Frontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));

            _Frontends.TryAdd(frontend.Identifier, frontend);
            _Logging.Debug($"{_Header}added frontend {frontend.Identifier} to monitoring");
        }

        /// <summary>
        /// Remove a frontend from monitoring.
        /// </summary>
        /// <param name="identifier">Frontend identifier to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown when identifier is null.</exception>
        internal void RemoveFrontend(string identifier)
        {
            if (String.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));

            if (_Frontends.TryRemove(identifier, out Frontend removed))
            {
                _FrontendRoundRobinIndex.TryRemove(identifier, out _);
                int removedSessions = _Services.SessionStickiness.RemoveFrontendSessions(identifier);

                _Logging.Debug($"{_Header}removed frontend {identifier} from monitoring with {removedSessions} sticky sessions");
            }
        }

        #endregion

        #region Private-Methods

        private void InitializeExistingNodes()
        {
            try
            {
                // Load existing frontends
                List<Frontend> frontends = _Services.Frontend.GetAll()?.ToList() ?? new List<Frontend>();
                foreach (Frontend frontend in frontends)
                {
                    _Frontends.TryAdd(frontend.Identifier, frontend);
                }

                // Load existing backends and start health check tasks
                List<Backend> backends = _Services.Backend.GetAll()?.ToList() ?? new List<Backend>();
                foreach (Backend backend in backends)
                {
                    backend.UnhealthySinceUtc = DateTime.UtcNow;
                    backend.Healthy = false;

                    if (_Backends.TryAdd(backend.Identifier, backend))
                    {
                        StartBackendHealthCheckTask(backend);
                    }
                }

                _Logging.Debug($"{_Header}initialized {frontends.Count} frontends and {backends.Count} backends with dedicated tasks");
            }
            catch (Exception ex)
            {
                _Logging.Error($"{_Header}error initializing existing nodes:{Environment.NewLine}{ex.ToString()}");
            }
        }

        private void StartBackendHealthCheckTask(Backend backend)
        {
            if (backend == null || String.IsNullOrEmpty(backend.Identifier)) return;

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            _BackendTokenSources.TryAdd(backend.Identifier, tokenSource);

            CancellationToken combinedToken = CancellationTokenSource.CreateLinkedTokenSource(_TokenSource.Token, tokenSource.Token).Token;

            Task healthCheckTask = Task.Run(async () => await BackendHealthCheckLoop(backend, combinedToken), combinedToken);
            _BackendTasks.TryAdd(backend.Identifier, healthCheckTask);

            _Logging.Debug($"{_Header}started health check task for backend {backend.Identifier}");
        }

        private void StopBackendHealthCheckTask(string identifier)
        {
            if (String.IsNullOrEmpty(identifier)) return;

            if (_BackendTokenSources.TryRemove(identifier, out CancellationTokenSource tokenSource))
            {
                try
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                }
                catch (Exception ex)
                {
                    _Logging.Warn($"{_Header}error cancelling token for backend {identifier}:{Environment.NewLine}{ex.ToString()}");
                }
            }

            if (_BackendTasks.TryRemove(identifier, out Task task))
            {
                try
                {
                    if (!task.Wait(TimeSpan.FromSeconds(5)))
                    {
                        _Logging.Warn($"{_Header}health check task for backend {identifier} did not terminate within timeout");
                    }
                }
                catch (Exception ex)
                {
                    _Logging.Warn($"{_Header}error waiting for health check task for backend {identifier}:{Environment.NewLine}{ex.ToString()}");
                }
            }

            _Logging.Debug($"{_Header}stopped and cleaned up health check task for backend {identifier}");
        }

        private async Task BackendHealthCheckLoop(Backend backend, CancellationToken token)
        {
            string healthCheckUrl = (backend.Ssl ? "https://" : "http://") + backend.Hostname + ":" + backend.Port + backend.HealthCheckUrl;

            _Logging.Debug($"{_Header}starting health check for backend {backend.Identifier} at {healthCheckUrl}");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_IntervalMs, token).ConfigureAwait(false);

                    if (token.IsCancellationRequested) break;

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
                    _Logging.Error($"{_Header}error in health check for backend {backend.Identifier}:{Environment.NewLine}{ex.ToString()}");

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

            _Logging.Debug($"{_Header}health check terminated for backend {backend.Identifier}");
        }

        private async Task PerformHealthCheck(Backend backend, string healthCheckUrl, CancellationToken token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMilliseconds(_IntervalMs / 2); // Half of interval for timeout

                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethodHelper.ToHttpMethod(backend.HealthCheckMethod), healthCheckUrl);
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

        private void HandleHealthCheckSuccess(Backend backend, string healthCheckUrl)
        {
            if (_Backends.TryGetValue(backend.Identifier, out Backend cached))
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

                        _Logging.Debug($"{_Header}health check success for backend {backend.Identifier} at {healthCheckUrl}");
                        _Logging.Info($"{_Header}backend {cached.Identifier} ({cached.Name}) is now healthy");
                    }
                    else
                    {
                        _Logging.Debug($"{_Header}health check success for backend {backend.Identifier} at {healthCheckUrl}");
                    }
                }
            }
        }

        private void HandleHealthCheckFailure(Backend backend, string healthCheckUrl, string reason)
        {
            if (_Backends.TryGetValue(backend.Identifier, out Backend cached))
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

                        _Logging.Warn($"{_Header}backend {cached.Identifier} ({cached.Name}) is now unhealthy");
                    }
                }
            }

            _Logging.Debug($"{_Header}health check failure for backend {backend.Identifier} at {healthCheckUrl}: {reason}");
        }

        #endregion
    }
}