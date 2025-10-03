namespace OllamaFlow.Core.Services
{
    using System;
    using System.Threading;
    using OllamaFlow.Core.Database;
    using OllamaFlow.Core.Handlers;
    using OllamaFlow.Core.Serialization;
    using SyslogLogging;

    /// <summary>
    /// Service context.
    /// </summary>
    public class ServiceContext : IDisposable
    {
        /// <summary>
        /// Backend service.
        /// </summary>
        public BackendService Backend
        {
            get => _Backend;
        }

        /// <summary>
        /// Frontend service.
        /// </summary>
        public FrontendService Frontend
        {
            get => _Frontend;
        }

        /// <summary>
        /// Gateway service.
        /// </summary>
        public GatewayService Gateway
        {
            get => _Gateway;
        }

        /// <summary>
        /// Healthcheck service.
        /// </summary>
        public HealthCheckService HealthCheck
        {
            get => _HealthCheck;
        }

        /// <summary>
        /// Model synchronization service.
        /// </summary>
        public ModelSynchronizationService ModelSynchronization
        {
            get => _ModelSynchronization;
        }

        /// <summary>
        /// Session stickiness service.
        /// </summary>
        public SessionStickinessService SessionStickiness
        {
            get => _SessionStickiness;
        }

        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private DatabaseDriverBase _Database = null;
        private Serializer _Serializer = null;
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();

        private BackendService _Backend = null;
        private FrontendService _Frontend = null;
        private GatewayService _Gateway = null;
        private HealthCheckService _HealthCheck = null;
        private ModelSynchronizationService _ModelSynchronization = null;
        private SessionStickinessService _SessionStickiness = null;
        private bool _Disposed = false;

        /// <summary>
        /// Service context.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="db">Database.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public ServiceContext(
            OllamaFlowSettings settings,
            LoggingModule logging,
            DatabaseDriverBase db,
            Serializer serializer,
            CancellationTokenSource tokenSource)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Database = db ?? throw new ArgumentNullException(nameof(db));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _TokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        /// <summary>
        /// Add the backend service.
        /// </summary>
        /// <returns>Service context.</returns>
        public ServiceContext AddBackend()
        {
            _Backend = new BackendService(_Settings, _Logging, _Database, this, _TokenSource);
            return this;
        }

        /// <summary>
        /// Add the frontend service.
        /// </summary>
        /// <returns>Service context.</returns>
        public ServiceContext AddFrontend()
        {
            _Frontend = new FrontendService(_Settings, _Logging, _Database, this, _TokenSource);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Service context.</returns>
        public ServiceContext AddHealthCheck()
        {
            _HealthCheck = new HealthCheckService(_Settings, _Logging, _Serializer, this, _TokenSource);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Service context.</returns>
        public ServiceContext AddModelSynchronization()
        {
            _ModelSynchronization = new ModelSynchronizationService(_Settings, _Logging, _Serializer, this, _TokenSource);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Service context.</returns>
        public ServiceContext AddSessionStickiness()
        {
            _SessionStickiness = new SessionStickinessService(_Logging, _TokenSource);
            return this;
        }

        /// <summary>
        /// Add the gateway service.
        /// </summary>
        /// <param name="callbacks">Callbacks.</param>
        /// <param name="handlers">Handler context.</param>
        /// <returns>Service context.</returns>
        public ServiceContext AddGateway(OllamaFlowCallbacks callbacks, HandlerContext handlers)
        {
            _Gateway = new GatewayService(
                _Settings,
                callbacks,
                _Logging,
                _Serializer,
                this,
                handlers,
                _TokenSource);

            return this;
        }

        /// <summary>
        /// Initialize services.
        /// </summary>
        /// <returns>Service context.</returns>
        public ServiceContext Initialize()
        {
            _SessionStickiness.Initialize();
            _Backend.Initialize();
            _Frontend.Initialize();
            _HealthCheck.Initialize();
            _ModelSynchronization.Initialize();
            _Gateway.Initialize();
            return this;
        }

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
                    _Gateway?.Dispose();
                    _ModelSynchronization?.Dispose();
                    _HealthCheck?.Dispose();
                    _Frontend?.Dispose();
                    _Backend?.Dispose();
                    _SessionStickiness?.Dispose();
                }

                _Gateway = null;
                _ModelSynchronization = null;
                _HealthCheck = null;
                _Frontend = null;
                _Backend = null;
                _SessionStickiness = null;

                _Disposed = true;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
