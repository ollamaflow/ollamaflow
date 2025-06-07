namespace OllamaFlow.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;
    using SerializationHelper;
    using OllamaFlow.Core.Services;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;

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

        private HealthCheckService _HealthCheckService = null;
        private ModelDiscoveryService _ModelDiscoveryService = null;
        private ModelSynchronizationService _ModelSynchronizationService = null;
        private GatewayService _GatewayService = null;
        private Webserver _Webserver = null;

        private bool _IsDisposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Settings.</param>
        public OllamaFlowDaemon(OllamaFlowSettings settings)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            InitializeGlobals();

            _Logging.Info(_Header + "OllamaFlow started using process ID " + _ProcessId);
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
                    _GatewayService?.Dispose();
                    _GatewayService = null;

                    _Webserver?.Dispose();
                    _Webserver = null;

                    _Logging?.Dispose();
                    _Logging = null;

                    _Serializer = null;
                    _Settings = null;

                    _IsDisposed = true;
                }
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

            _HealthCheckService = new HealthCheckService(
                _Settings,
                _Logging,
                _Serializer);

            _ModelDiscoveryService = new ModelDiscoveryService(
                _Settings,
                _Logging,
                _Serializer);

            _ModelSynchronizationService = new ModelSynchronizationService(
                _Settings,
                _Logging,
                _Serializer);

            _GatewayService = new GatewayService(
                _Settings, 
                _Callbacks, 
                _Logging, 
                _Serializer);

            #endregion

            #region Webserver

            _Webserver = new Webserver(_Settings.Webserver, _GatewayService.DefaultRoute);
            _Webserver.Routes.Preflight = _GatewayService.OptionsRoute;
            _Webserver.Routes.PreRouting = _GatewayService.PreRoutingHandler;
            _Webserver.Routes.PostRouting = _GatewayService.PostRoutingHandler;
            _Webserver.Routes.AuthenticateRequest = _GatewayService.AuthenticateRequest;

            _GatewayService.InitializeRoutes(_Webserver);

            _Webserver.Start();

            _Logging.Info(
                _Header +
                "webserver started on "
                + (_Settings.Webserver.Ssl.Enable ? "https://" : "http://")
                + _Settings.Webserver.Hostname
                + ":" + _Settings.Webserver.Port);

            #endregion
        }

        #endregion
    }
}
