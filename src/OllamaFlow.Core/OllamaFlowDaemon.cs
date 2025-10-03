namespace OllamaFlow.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using OllamaFlow.Core.Database;
    using OllamaFlow.Core.Database.Sqlite;
    using OllamaFlow.Core.Handlers;
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Core.Services;
    using SyslogLogging;
    using WatsonWebserver;

    /// <summary>
    /// OllamaFlow Daemon.
    /// </summary>
    public class OllamaFlowDaemon : IDisposable
    {
        /// <summary>
        /// OllamaFlow callbacks.  Attach handlers to these methods to integrate your application logic into OllamaFlow.
        /// </summary>
        public OllamaFlowCallbacks Callbacks
        {
            get
            {
                return _Callbacks;
            }
            set
            {
                if (value == null) _Callbacks = new OllamaFlowCallbacks();
                _Callbacks = value;
            }
        }

        /// <summary>
        /// Backend service.
        /// </summary>
        public BackendService Backends
        {
            get => _Services.Backend;
        }

        /// <summary>
        /// Frontend service.
        /// </summary>
        public FrontendService Frontends
        {
            get => _Services.Frontend;
        }

        /// <summary>
        /// Gateway service.
        /// </summary>
        public GatewayService Gateway
        {
            get => _Services.Gateway;
        }

        /// <summary>
        /// Healthcheck service.
        /// </summary>
        public HealthCheckService HealthCheck
        {
            get => _Services.HealthCheck;
        }

        /// <summary>
        /// Model synchronization service.
        /// </summary>
        public ModelSynchronizationService ModelSynchronization
        {
            get => _Services.ModelSynchronization;
        }

        /// <summary>
        /// Session stickiness service.
        /// </summary>
        public SessionStickinessService SessionStickiness
        {
            get => _Services.SessionStickiness;
        }

        private static string _Header = "[OllamaFlowDaemon] ";
        private static int _ProcessId = Environment.ProcessId;
        private OllamaFlowSettings _Settings = null;
        private OllamaFlowCallbacks _Callbacks = new OllamaFlowCallbacks();
        private Serializer _Serializer = new Serializer();
        private LoggingModule _Logging = null;

        private DatabaseDriverBase _Database = null;
        private ServiceContext _Services = null;
        private HandlerContext _Handlers = null;
        private Webserver _Webserver = null;

        private bool _Disposed = false;
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private readonly object _DisposeLock = new object();

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public OllamaFlowDaemon(OllamaFlowSettings settings, CancellationTokenSource tokenSource = default)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _TokenSource = tokenSource ?? new CancellationTokenSource();

            InitializeGlobals();

            _Logging.Info(_Header + "OllamaFlow started using process ID " + _ProcessId);
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
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            lock (_DisposeLock)
            {
                if (_Disposed) return;

                if (disposing)
                {
                    try
                    {
                        _Logging?.Info(_Header + "disposing OllamaFlow daemon");

                        // Cancel any ongoing operations and background tasks
                        if (_TokenSource != null && !_TokenSource.IsCancellationRequested)
                        {
                            _TokenSource.Cancel();
                        }

                        // Dispose services and handlers
                        _Services?.Dispose();
                        _Handlers?.Dispose();

                        // Stop and dispose webserver first
                        if (_Webserver != null)
                        {
                            try
                            {
                                _Webserver.Stop();
                                _Webserver.Dispose();
                            }
                            catch (Exception ex)
                            {
                                _Logging?.Warn($"{_Header}error stopping webserver:{Environment.NewLine}{ex.ToString()}");
                            }
                            finally
                            {
                                _Webserver = null;
                            }
                        }

                        // Dispose database
                        if (_Database != null)
                        {
                            try
                            {
                                if (_Database is IDisposable disposableDb)
                                {
                                    disposableDb.Dispose();
                                }
                            }
                            catch (Exception ex)
                            {
                                _Logging?.Warn($"{_Header}error disposing database:{Environment.NewLine}{ex.ToString()}");
                            }
                            finally
                            {
                                _Database = null;
                            }
                        }

                        // Dispose token source
                        if (_TokenSource != null)
                        {
                            try
                            {
                                _TokenSource.Dispose();
                            }
                            catch (Exception ex)
                            {
                                _Logging?.Warn($"{_Header}error disposing token source:{Environment.NewLine}{ex.ToString()}");
                            }
                            finally
                            {
                                _TokenSource = null;
                            }
                        }

                        // Finally dispose logging
                        if (_Logging != null)
                        {
                            try
                            {
                                _Logging.Info(_Header + "OllamaFlow daemon disposed");
                                _Logging.Dispose();
                            }
                            catch
                            {
                                // Can't log this error
                            }
                            finally
                            {
                                _Logging = null;
                            }
                        }

                        // Clear other references
                        _Handlers = null;
                        _Services = null;
                        _Serializer = null;
                        _Settings = null;
                        _Callbacks = null;
                    }
                    catch (Exception ex)
                    {
                        _Logging?.Error(_Header + "unexpected error during disposal: " + ex.ToString());
                    }
                }

                _Disposed = true;
            }
        }

        private void InitializeGlobals()
        {
            #region Logging

            List<SyslogServer> syslogServers = new List<SyslogServer>();

            if (_Settings.Logging.Servers != null && _Settings.Logging.Servers.Count > 0)
            {
                foreach (OllamaFlow.Core.Settings.SyslogServer server in _Settings.Logging.Servers)
                {
                    syslogServers.Add(
                        new SyslogServer
                        {
                            Hostname = server.Hostname,
                            Port = server.Port
                        }
                    );
                }
            }

            if (syslogServers.Count > 0)
                _Logging = new LoggingModule(syslogServers);
            else
                _Logging = new LoggingModule();

            _Logging.Settings.MinimumSeverity = (Severity)_Settings.Logging.MinimumSeverity;
            _Logging.Settings.EnableConsole = _Settings.Logging.ConsoleLogging;
            _Logging.Settings.EnableColors = _Settings.Logging.EnableColors;

            if (!String.IsNullOrEmpty(_Settings.Logging.LogDirectory))
            {
                if (!Directory.Exists(_Settings.Logging.LogDirectory))
                    Directory.CreateDirectory(_Settings.Logging.LogDirectory);

                _Settings.Logging.LogFilename = _Settings.Logging.LogDirectory + _Settings.Logging.LogFilename;
            }

            if (!String.IsNullOrEmpty(_Settings.Logging.LogFilename))
            {
                _Logging.Settings.FileLogging = FileLoggingMode.FileWithDate;
                _Logging.Settings.LogFilename = _Settings.Logging.LogFilename;
            }

            #endregion

            #region Services

            _Logging.Debug(_Header + "initializing database " + _Settings.DatabaseFilename);

            _Database = new SqliteDatabaseDriver(_Settings, _Logging, _Serializer, _Settings.DatabaseFilename);
            _Database.InitializeRepository();

            _Logging.Debug(_Header + "initializing services and handlers");

            _Services = new ServiceContext(_Settings, _Logging, _Database, _Serializer, _TokenSource);
            _Handlers = new HandlerContext(_Settings, _Logging, _Serializer, _Services, _TokenSource);

            _Handlers
                .AddStaticRoute()
                .AddAdminApi()
                .Initialize();

            _Services
                .AddBackend()
                .AddFrontend()
                .AddSessionStickiness()
                .AddHealthCheck()
                .AddModelSynchronization()
                .AddGateway(_Callbacks, _Handlers)
                .Initialize();

            #endregion

            #region Webserver

            _Logging.Debug(_Header + "initializing webserver");

            _Webserver = new Webserver(_Settings.Webserver, _Services.Gateway.DefaultRoute);
            _Webserver.Routes.Preflight = _Services.Gateway.OptionsRoute;
            _Webserver.Routes.PreRouting = _Services.Gateway.PreRoutingHandler;
            _Webserver.Routes.PostRouting = _Services.Gateway.PostRoutingHandler;

            _Logging.Debug(_Header + "webserver routes configured successfully");

            _Webserver.Start();

            _Logging.Info(
                _Header +
                "initialized webserver on "
                + (_Settings.Webserver.Ssl.Enable ? "https://" : "http://")
                + _Settings.Webserver.Hostname
                + ":" + _Settings.Webserver.Port);

            #endregion
        }
    }
}