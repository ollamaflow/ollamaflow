namespace OllamaFlow.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using OllamaFlow.Core.Database;
    using OllamaFlow.Core.Database.Sqlite;
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Core.Services;
    using SyslogLogging;
    using WatsonWebserver;

    /// <summary>
    /// OllamaFlow Daemon.
    /// </summary>
    public class OllamaFlowDaemon : IDisposable
    {
        #region Public-Members

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

        #endregion

        #region Private-Members

        private static string _Header = "[OllamaFlowDaemon] ";
        private static int _ProcessId = Environment.ProcessId;
        private OllamaFlowSettings _Settings = null;
        private OllamaFlowCallbacks _Callbacks = new OllamaFlowCallbacks();
        private Serializer _Serializer = new Serializer();
        private LoggingModule _Logging = null;

        private DatabaseDriverBase _Database = null;
        private FrontendService _FrontendService = null;
        private BackendService _BackendService = null;
        private HealthCheckService _HealthCheckService = null;
        private ModelDiscoveryService _ModelDiscoveryService = null;
        private ModelSynchronizationService _ModelSynchronizationService = null;
        private GatewayService _GatewayService = null;
        private Webserver _Webserver = null;

        private bool _IsDisposed = false;
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private readonly object _DisposeLock = new object();

        #endregion

        #region Constructors-and-Factories

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

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            lock (_DisposeLock)
            {
                if (_IsDisposed)
                    return;

                if (disposing)
                {
                    try
                    {
                        _Logging?.Info(_Header + "disposing OllamaFlow daemon");

                        // Cancel any ongoing operations
                        if (_TokenSource != null && !_TokenSource.IsCancellationRequested)
                        {
                            _TokenSource.Cancel();
                        }

                        // Dispose services in reverse order of initialization
                        // This ensures dependencies are properly cleaned up

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
                                _Logging?.Warn(_Header + "error stopping webserver: " + ex.Message);
                            }
                            finally
                            {
                                _Webserver = null;
                            }
                        }

                        // Dispose services
                        DisposeService(ref _GatewayService, "GatewayService");
                        DisposeService(ref _ModelSynchronizationService, "ModelSynchronizationService");
                        DisposeService(ref _ModelDiscoveryService, "ModelDiscoveryService");
                        DisposeService(ref _HealthCheckService, "HealthCheckService");
                        DisposeService(ref _BackendService, "BackendService");
                        DisposeService(ref _FrontendService, "FrontendService");

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
                                _Logging?.Warn(_Header + "error disposing database: " + ex.Message);
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
                                _Logging?.Warn(_Header + "error disposing token source: " + ex.Message);
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
                        _Serializer = null;
                        _Settings = null;
                        _Callbacks = null;
                    }
                    catch (Exception ex)
                    {
                        _Logging?.Error(_Header + "unexpected error during disposal: " + ex.ToString());
                    }
                }

                _IsDisposed = true;
            }
        }

        #endregion

        #region Private-Methods

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

            _Logging.Debug(_Header + "initializing services");

            _FrontendService = new FrontendService(_Settings, _Logging, _Database, _TokenSource);
            _BackendService = new BackendService(_Settings, _Logging, _Database, _TokenSource);

            _HealthCheckService = new HealthCheckService(
                _Settings,
                _Logging,
                _Serializer,
                _FrontendService,
                _BackendService,
                _TokenSource);

            _ModelDiscoveryService = new ModelDiscoveryService(
                _Settings,
                _Logging,
                _Serializer,
                _FrontendService,
                _BackendService,
                _HealthCheckService,
                _TokenSource);

            _ModelSynchronizationService = new ModelSynchronizationService(
                _Settings,
                _Logging,
                _Serializer,
                _FrontendService,
                _BackendService,
                _HealthCheckService,
                _TokenSource);

            _GatewayService = new GatewayService(
                _Settings,
                _Callbacks,
                _Logging,
                _Serializer,
                _FrontendService,
                _BackendService,
                _HealthCheckService,
                _TokenSource);

            #endregion

            #region Webserver

            _Logging.Debug(_Header + "initializing webserver");

            _Webserver = new Webserver(_Settings.Webserver, _GatewayService.DefaultRoute);
            _Webserver.Routes.Preflight = _GatewayService.OptionsRoute;
            _Webserver.Routes.PreRouting = _GatewayService.PreRoutingHandler;
            _Webserver.Routes.PostRouting = _GatewayService.PostRoutingHandler;

            _GatewayService.InitializeRoutes(_Webserver);

            _Webserver.Start();

            _Logging.Info(
                _Header +
                "initialized webserver on "
                + (_Settings.Webserver.Ssl.Enable ? "https://" : "http://")
                + _Settings.Webserver.Hostname
                + ":" + _Settings.Webserver.Port);

            #endregion
        }

        private void DisposeService<T>(ref T service, string serviceName) where T : class
        {
            if (service != null)
            {
                try
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _Logging?.Warn(_Header + $"error disposing {serviceName}: " + ex.Message);
                }
                finally
                {
                    service = null;
                }
            }
        }

        #endregion
    }
}