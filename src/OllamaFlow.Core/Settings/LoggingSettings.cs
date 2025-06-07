namespace OllamaFlow.Core.Settings
{
    using System;
    using System.Collections.Generic;
    using OllamaFlow.Core.Settings;

    /// <summary>
    /// Logging settings.
    /// </summary>
    public class LoggingSettings
    {
        #region Public-Members

        /// <summary>
        /// List of syslog servers.
        /// </summary>
        public List<SyslogServer> Servers
        {
            get
            {
                return _Servers;
            }
            set
            {
                if (value == null) _Servers = new List<SyslogServer>();
                else _Servers = value;
            }
        }

        /// <summary>
        /// Log directory.
        /// </summary>
        public string LogDirectory { get; set; } = Constants.LogDirectory;

        /// <summary>
        /// Log filename.
        /// </summary>
        public string LogFilename { get; set; } = Constants.LogFilename;

        /// <summary>
        /// Enable or disable console logging.
        /// </summary>
        public bool ConsoleLogging { get; set; } = true;

        /// <summary>
        /// Enable colors in logging.
        /// </summary>
        public bool EnableColors { get; set; } = true;

        /// <summary>
        /// Minimum severity.
        /// </summary>
        public int MinimumSeverity
        {
            get
            {
                return _MinimumSeverity;
            }
            set
            {
                if (value < 0 || value > 7) throw new ArgumentOutOfRangeException(nameof(MinimumSeverity));
                _MinimumSeverity = value;
            }
        }

        #endregion

        #region Private-Members

        private int _MinimumSeverity = 0;
        private List<SyslogServer> _Servers = new List<SyslogServer>
        {
            new SyslogServer("127.0.0.1", 514)
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public LoggingSettings()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}