namespace OllamaFlow.Core.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Core.Services;
    using SyslogLogging;

    /// <summary>
    /// Handler context.
    /// </summary>
    public class HandlerContext : IDisposable
    {
        /// <summary>
        /// Admin API handler.
        /// </summary>
        public AdminApiHandler AdminApi
        {
            get => _AdminApi;
        }

        /// <summary>
        /// Static route handler.
        /// </summary>
        public StaticRouteHandler StaticRoute
        {
            get => _StaticRoute;
        }

        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private ServiceContext _Services = null;
        private CancellationTokenSource _TokenSource = null;

        private AdminApiHandler _AdminApi = null;
        private StaticRouteHandler _StaticRoute = null;
        private bool _Disposed = false;

        /// <summary>
        /// Handler context.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="services">Service context.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public HandlerContext(
            OllamaFlowSettings settings,
            LoggingModule logging,
            Serializer serializer,
            ServiceContext services,
            CancellationTokenSource tokenSource)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Services = services ?? throw new ArgumentNullException(nameof(services));
            _TokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
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
                    _AdminApi?.Dispose();
                    _StaticRoute?.Dispose();
                }

                _AdminApi = null;
                _StaticRoute = null;

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

        /// <summary>
        /// Add admin API handler.
        /// </summary>
        /// <returns>Handler context.</returns>
        public HandlerContext AddAdminApi()
        {
            _AdminApi = new AdminApiHandler(_Settings, _Logging, _Serializer, _Services, _TokenSource);
            return this;
        }

        /// <summary>
        /// Add static route handler/
        /// </summary>
        /// <returns>Handler context.</returns>
        public HandlerContext AddStaticRoute()
        {
            _StaticRoute = new StaticRouteHandler(_Settings, _Logging, _Serializer, _TokenSource);
            return this;
        }

        /// <summary>
        /// Initialize handlers.
        /// </summary>
        /// <returns>Handler context.</returns>
        public HandlerContext Initialize()
        {
            _StaticRoute.Initialize();
            _AdminApi.Initialize();
            return this;
        }
    }
}
