namespace OllamaFlow.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using OllamaFlow.Core.Settings;
    using WatsonWebserver.Core;

    /// <summary>
    /// Settings.
    /// </summary>
    public class OllamaFlowSettings
    {
        #region Public-Members

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings Logging
        {
            get
            {
                return _Logging;
            }
            set
            {
                if (value == null) value = new LoggingSettings();
                _Logging = value;
            }
        }

        /// <summary>
        /// Webserver settings.
        /// </summary>
        public WebserverSettings Webserver
        {
            get
            {
                return _Webserver;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Webserver));
                _Webserver = value;
            }
        }

        /// <summary>
        /// Database filename.
        /// </summary>
        public string DatabaseFilename
        {
            get
            {
                return _DatabaseFilename;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(DatabaseFilename));
                _DatabaseFilename = value;
            }
        }

        /// <summary>
        /// Administrator bearer tokens.
        /// </summary> 
        public List<string> AdminBearerTokens
        {
            get
            {
                return _AdminBearerTokens;
            }
            set
            {
                if (value == null) value = new List<string>();
                _AdminBearerTokens = value;
            }
        }

        #endregion

        #region Private-Members

        private LoggingSettings _Logging = new LoggingSettings();
        private WebserverSettings _Webserver = new WebserverSettings();
        private List<string> _AdminBearerTokens = new List<string>();
        private string _DatabaseFilename = Constants.DatabaseFilename;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary> 
        public OllamaFlowSettings()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
