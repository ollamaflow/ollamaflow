namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text;
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
    using System.Collections.Concurrent;

    /// <summary>
    /// Health check service.
    /// </summary>
    public class HealthCheckService : IDisposable
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

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

        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private Task _HealthCheckTask = null;

        private int _IntervalMs = 5000;

        private ConcurrentDictionary<string, OllamaFrontend> _Frontends = new ConcurrentDictionary<string, OllamaFrontend>();
        private ConcurrentDictionary<string, OllamaBackend> _Backends = new ConcurrentDictionary<string, OllamaBackend>();

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
        /// <param name="tokenSource">Cancellation token source.</param>
        public HealthCheckService(
            OllamaFlowSettings settings,
            LoggingModule logging,
            Serializer serializer,
            FrontendService frontend,
            BackendService backend,
            CancellationTokenSource tokenSource = default)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _FrontendService = frontend ?? throw new ArgumentNullException(nameof(frontend));
            _BackendService = backend ?? throw new ArgumentNullException(nameof(backend));
            _HealthCheckTask = Task.Run(() => HealthCheckTask(_TokenSource.Token), _TokenSource.Token);

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

        /// <summary>
        /// Retrieve the next backend that should be used for a request to a given frontend.
        /// </summary>
        /// <param name="frontend">Frontend.</param>
        /// <returns>Backend.</returns>
        public OllamaBackend GetNextBackend(OllamaFrontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));
            if (frontend.Backends == null) return null;

            List<OllamaBackend> candidates = _Backends
               .Where(kvp => frontend.Backends.Contains(kvp.Key) && kvp.Value.Healthy && kvp.Value.Active)
               .Select(kvp => kvp.Value)
               .ToList();

            int index = 0;

            if (candidates.Any())
            {
                if (frontend.LoadBalancing == LoadBalancingMode.Random)
                {
                    index = _Random.Next(0, candidates.Count);

                    lock (frontend.Lock)
                    {
                        frontend.LastBackendIndex = index;
                    }

                    _Logging.Debug(_Header + "returning index " + index + " of " + candidates.Count + " candidates for frontend " + frontend.Identifier + " " + frontend.Name);
                    return candidates[index];
                }
                else if (frontend.LoadBalancing == LoadBalancingMode.RoundRobin)
                {
                    lock (frontend.Lock)
                    {
                        frontend.LastBackendIndex += 1;
                        if (frontend.LastBackendIndex > (candidates.Count - 1)) frontend.LastBackendIndex = 0;
                        index = frontend.LastBackendIndex;
                    }

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
        /// Update a backend if cached.
        /// </summary>
        /// <param name="backend">Backend.</param>
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
            }
        }

        #endregion

        #region Private-Methods

        private async Task HealthCheckTask(CancellationToken token = default)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_IntervalMs, token);

                    #region Get-Frontends

                    List<OllamaFrontend> frontends = _FrontendService.GetAll().ToList();
                    if (frontends != null && frontends.Count > 0)
                    {
                        foreach (OllamaFrontend frontend in frontends)
                        {
                            if (!_Frontends.ContainsKey(frontend.Identifier)) _Frontends.TryAdd(frontend.Identifier, frontend);
                        }
                    }

                    #endregion

                    #region Get-Backends

                    List<OllamaBackend> backends = _BackendService.GetAll()?.ToList() ?? new List<OllamaBackend>();
                    List<Task> tasks = new List<Task>();

                    #endregion

                    #region Process-Backends

                    if (backends != null && backends.Count > 0)
                    {
                        OllamaBackend backend = null;

                        foreach (OllamaBackend be in backends)
                        { 
                            if (!_Backends.ContainsKey(be.Identifier))
                            {
                                backend = be;
                                backend.UnhealthySinceUtc = DateTime.UtcNow;
                                _Backends.TryAdd(backend.Identifier, backend);
                            }
                            else
                            {
                                backend = be;
                            }

                            if (!backend.Active) continue;

                            tasks.Add(Task.Run(() => HealthCheckTask(backend, token = default)));
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
                    _Logging.Error(_Header + "healthcheck task exception:" + Environment.NewLine + e.ToString());
                }
            }

            _Logging.Debug(_Header + "healthcheck task terminated");
        }

        private async Task HealthCheckTask(OllamaBackend backend, CancellationToken token = default)
        {
            _Logging.Debug(
                _Header +
                "starting healthcheck task for backend " +
                backend.Identifier + " " + backend.Name + " " + backend.Hostname + ":" + backend.Port);

            _Backends.TryAdd(backend.Identifier, backend);

            string healthCheckUrl = (backend.Ssl ? "https://" : "http://") + backend.Hostname + ":" + backend.Port + backend.HealthCheckUrl;

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(_IntervalMs);

                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(backend.HealthCheckMethod, healthCheckUrl);
                    HttpResponseMessage response = await client.SendAsync(request, token);

                    if (response.IsSuccessStatusCode)
                    {
                        _Logging.Debug(_Header + "health check succeeded for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl);

                        lock (backend.Lock)
                        {
                            if (backend.HealthCheckSuccess < 99) backend.HealthCheckSuccess++;

                            backend.HealthCheckFailure = 0;
                            backend.UnhealthySinceUtc = null;

                            if (!backend.Healthy && backend.HealthCheckSuccess >= backend.HealthyThreshold)
                            {
                                backend.Healthy = true;
                                _Logging.Info(_Header + "backend " + backend.Identifier + " is now healthy");
                                if (backend.HealthySinceUtc == null) backend.HealthySinceUtc = DateTime.UtcNow; 
                            }
                        }
                    }
                    else
                    {
                        _Logging.Debug(_Header + "health check failed for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl + " with status " + (int)response.StatusCode);

                        lock (backend.Lock)
                        {
                            if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;

                            backend.HealthCheckSuccess = 0;
                            backend.HealthySinceUtc = null;

                            if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                            {
                                backend.Healthy = false;
                                _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (HTTP " + (int)response.StatusCode + ")");
                                if (backend.UnhealthySinceUtc == null) backend.HealthySinceUtc = null;
                            }
                        }
                    }
                }
                catch (HttpRequestException hre)
                {
                    lock (backend.Lock)
                    {
                        if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                        backend.HealthCheckSuccess = 0;

                        if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                        {
                            backend.Healthy = false;
                            _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (timeout)");
                        }
                    }

                    _Logging.Debug(_Header + "health check failed for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl + ": " + hre.Message);
                }
                catch (HttpIOException ioe)
                {
                    lock (backend.Lock)
                    {
                        if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                        backend.HealthCheckSuccess = 0;

                        if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                        {
                            backend.Healthy = false;
                            _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (timeout)");
                        }
                    }

                    _Logging.Debug(_Header + "health check failed for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl + ": " + ioe.Message);
                }
                catch (SocketException se)
                {
                    lock (backend.Lock)
                    {
                        if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                        backend.HealthCheckSuccess = 0;

                        if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                        {
                            backend.Healthy = false;
                            _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (timeout)");
                        }
                    }

                    _Logging.Debug(_Header + "health check failed for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl + ": " + se.Message);
                }
                catch (TaskCanceledException)
                {
                    // Expected when cancellation is requested or timeout occurs
                    if (!token.IsCancellationRequested)
                    {
                        // This was a timeout, not a cancellation
                        lock (backend.Lock)
                        {
                            if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                            backend.HealthCheckSuccess = 0;

                            if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                            {
                                backend.Healthy = false;
                                _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (timeout)");
                            }
                        }

                        _Logging.Debug(_Header + "health check timeout for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested or timeout occurs
                    if (!token.IsCancellationRequested)
                    {
                        // This was a timeout, not a cancellation
                        lock (backend.Lock)
                        {
                            if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                            backend.HealthCheckSuccess = 0;

                            if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                            {
                                backend.Healthy = false;
                                _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (timeout)");
                            }
                        }

                        _Logging.Debug(_Header + "health check timeout for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl);
                    }
                }
                catch (Exception e)
                {
                    lock (backend.Lock)
                    {
                        if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                        backend.HealthCheckSuccess = 0;

                        if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                        {
                            backend.Healthy = false;
                            _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (" + e.GetType().Name + ")");
                        }
                    }

                    _Logging.Debug(_Header + "health check exception for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl + Environment.NewLine + e.ToString());
                }
            }
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
