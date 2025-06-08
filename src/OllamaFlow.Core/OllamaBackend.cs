namespace OllamaFlow.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;   
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Origin server.
    /// </summary>
    public class OllamaBackend
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for this origin server.
        /// </summary>
        public string Identifier { get; set; } = null;

        /// <summary>
        /// Name for this origin server.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Hostname.
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
        /// TCP port.
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
            set
            {
                if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException(nameof(Port));
                _Port = value;
            }
        }

        /// <summary>
        /// Enable or disable SSL.
        /// </summary>
        public bool Ssl { get; set; } = false;

        /// <summary>
        /// Interval at which the list of available models is refreshed. 
        /// Default is 30 seconds (30000).  Minimum is 1 second (1000).
        /// </summary>
        public int ModelRefreshIntervalMs
        {
            get
            {
                return _ModelRefreshIntervalMs;
            }
            set
            {
                if (value < 1000) throw new ArgumentOutOfRangeException(nameof(ModelRefreshIntervalMs));
                _ModelRefreshIntervalMs = value;
            }
        }

        /// <summary>
        /// Interval at which health is checked against this server.
        /// Default is 5 seconds (5000).  Minimum is 1 second (1000).
        /// </summary>
        public int HealthCheckIntervalMs
        {
            get
            {
                return _HealthCheckIntervalMs;
            }
            set
            {
                if (value < 1000) throw new ArgumentOutOfRangeException(nameof(HealthCheckIntervalMs));
                _HealthCheckIntervalMs = value;
            }
        }

        /// <summary>
        /// Number of consecutive failed health checks before marking a server as unhealthy.
        /// </summary>
        public int UnhealthyThreshold
        {
            get
            {
                return _UnhealthyThreshold;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(UnhealthyThreshold));
                _UnhealthyThreshold = value;
            }
        }

        /// <summary>
        /// Number of consecutive successful health checks before marking a server as healthy.
        /// </summary>
        public int HealthyThreshold
        {
            get
            {
                return _HealthyThreshold;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(HealthyThreshold));
                _HealthyThreshold = value;
            }
        }

        /// <summary>
        /// HTTP method to use when performing a healthcheck.
        /// Default is GET.
        /// </summary>
        public HttpMethod HealthCheckMethod
        {
            get
            {
                return _HealthCheckMethod;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(HealthCheckMethod));
                _HealthCheckMethod = value;
            }
        }

        /// <summary>
        /// URL to use when performing a healthcheck.
        /// Default is /.
        /// </summary>
        public string HealthCheckUrl
        {
            get
            {
                return _HealthCheckUrl;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) value = "/";
                _HealthCheckUrl = value;
            }
        }

        /// <summary>
        /// Maximum number of parallel requests to this backend.
        /// </summary>
        public int MaxParallelRequests
        {
            get
            {
                return _MaxParallelRequests;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxParallelRequests));
                _MaxParallelRequests = value;
            }
        }

        /// <summary>
        /// True to log the request body.
        /// </summary>
        public bool LogRequestBody { get; set; } = false;

        /// <summary>
        /// True to log the response body.
        /// </summary>
        public bool LogResponseBody { get; set; } = false;

        /// <summary>
        /// URL prefix.
        /// </summary>
        [JsonIgnore]
        public string UrlPrefix
        {
            get
            {
                return (Ssl ? "https://" : "http://") + Hostname + ":" + Port;
            }
        }

        #endregion

        #region Internal-Members

        internal readonly object Lock = new object();
        internal int HealthCheckSuccess = 0;
        internal int HealthCheckFailure = 0;
        internal bool Healthy = false;
        internal bool ModelsDiscovered = false;
        internal List<string> Models
        {
            get
            {
                return _Models;
            }
            set
            {
                if (value == null) value = new List<string>();
                _Models = value;
            }
        }
        internal SemaphoreSlim Semaphore
        {
            get
            {
                if (_Semaphore == null) _Semaphore = new SemaphoreSlim(_MaxParallelRequests, _MaxParallelRequests);
                return _Semaphore;
            }
            set
            {
                _Semaphore = value;
            }
        }

        #endregion

        #region Private-Members

        private string _Hostname = "localhost";
        private int _Port = 8000;
        private int _ModelRefreshIntervalMs = 30000;
        private int _HealthCheckIntervalMs = 5000;
        private int _UnhealthyThreshold = 2;
        private int _HealthyThreshold = 2;
        private int _MaxParallelRequests = 4;
        private HttpMethod _HealthCheckMethod = HttpMethod.Get;
        private string _HealthCheckUrl = "/";
        private List<string> _Models = new List<string>();
        private SemaphoreSlim _Semaphore = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Ollama backend.
        /// </summary>
        public OllamaBackend()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
