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
        /// Ollama frontend endpoints, i.e. virtual servers exposed by OllamaFlow.
        /// </summary>
        public List<OllamaFrontend> Frontends
        {
            get
            {
                return _Frontends;
            }
            set
            {
                if (value == null) value = new List<OllamaFrontend>();
                _Frontends = value;
            }
        }

        /// <summary>
        /// Ollama backend endpoints, i.e. Ollama servers.
        /// </summary>
        public List<OllamaBackend> Backends
        {
            get
            {
                return _Backends;
            }
            set
            {
                if (value == null) value = new List<OllamaBackend>();
                _Backends = value;
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

        #endregion

        #region Private-Members

        private LoggingSettings _Logging = new LoggingSettings();
        private List<OllamaFrontend> _Frontends = new List<OllamaFrontend>();
        private List<OllamaBackend> _Backends = new List<OllamaBackend>();
        private WebserverSettings _Webserver = new WebserverSettings();

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
