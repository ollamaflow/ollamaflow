namespace OllamaFlow.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;

    /// <summary>
    /// API endpoint.
    /// </summary>
    public class OllamaFrontend
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for this API endpoint.
        /// </summary>
        public string Identifier { get; set; } = null;

        /// <summary>
        /// Name for this API endpoint.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Hostname associated with this frontend.
        /// </summary>
        public string Hostname
        {
            get
            {
                return _Hostname;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Hostname));
                _Hostname = value;
            }
        }

        /// <summary>
        /// Number of milliseconds to wait before considering the request to be timed out.
        /// Default is 60 seconds.
        /// </summary>
        public int TimeoutMs
        {
            get
            {
                return _TimeoutMs;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(TimeoutMs));
                _TimeoutMs = value;
            }
        }

        /// <summary>
        /// Load-balancing mode.
        /// </summary>
        public LoadBalancingMode LoadBalancing { get; set; } = LoadBalancingMode.RoundRobin;

        /// <summary>
        /// True to terminate HTTP/1.0 requests.
        /// </summary>
        public bool BlockHttp10 { get; set; } = false;

        /// <summary>
        /// True to enable logging of the full request.
        /// </summary>
        public bool LogRequestFull { get; set; } = false;

        /// <summary>
        /// True to log the request body.
        /// </summary>
        public bool LogRequestBody { get; set; } = false;

        /// <summary>
        /// True to log the response body.
        /// </summary>
        public bool LogResponseBody { get; set; } = false;

        /// <summary>
        /// Maximum request body size.  Default is 512MB.
        /// </summary>
        public int MaxRequestBodySize
        {
            get
            {
                return _MaxRequestBodySize;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxRequestBodySize));
                _MaxRequestBodySize = value;
            }
        }

        /// <summary>
        /// Ollama backend server identifiers.
        /// </summary>
        public List<string> Backends
        {
            get
            {
                return _Backends;
            }
            set
            {
                if (value == null) value = new List<string>();
                _Backends = value;
            }
        }

        /// <summary>
        /// List of models that should be present on each mapped backend Ollama server.
        /// </summary>
        public List<string> RequiredModels
        {
            get
            {
                return _RequiredModels;
            }
            set
            {
                if (value == null) value = new List<string>();
                _RequiredModels = value;
            }
        }

        /// <summary>
        /// Last-used index.
        /// </summary>
        public int LastIndex
        {
            get
            {
                return _LastIndex;
            }
            set
            {
                if (value < 0 || value > (_Backends.Count - 1)) throw new ArgumentOutOfRangeException(nameof(LastIndex));
                _LastIndex = value;
            }
        }

        #endregion

        #region Internal-Members

        internal readonly object Lock = new object();

        #endregion

        #region Private-Members

        private string _Hostname = "*";
        private int _TimeoutMs = 60000;
        private int _MaxRequestBodySize = (512 * 1024 * 1024);
        private List<string> _Backends = new List<string>();
        private List<string> _RequiredModels = new List<string>();
        private int _LastIndex = 0;

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OllamaFrontend()
        {

        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
