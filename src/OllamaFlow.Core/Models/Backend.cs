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
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Core.Enums;

    /// <summary>
    /// Origin server.
    /// </summary>
    public class Backend
    {
        #region Public-Members

        /// <summary>
        /// Serializer.
        /// </summary>
        [JsonIgnore]
        public static Serializer Serializer = new Serializer();

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
        /// Number of consecutive failed health checks before marking a server as unhealthy.
        /// Default is 2.
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
        /// Default is 2.
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
        /// Default is 4.
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
        /// Threshold at which 429 rate limit responses are sent.
        /// Default is 10.
        /// </summary>
        public int RateLimitRequestsThreshold
        {
            get
            {
                return _RateLimitRequestsThreshold;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(RateLimitRequestsThreshold));
                _RateLimitRequestsThreshold = value;
            }
        }

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

        /// <summary>
        /// Boolean indicating if the full request should be logged.
        /// </summary>
        public bool LogRequestFull { get; set; } = false;

        /// <summary>
        /// Boolean indicating if the request body should be logged.
        /// </summary>
        public bool LogRequestBody { get; set; } = false;

        /// <summary>
        /// Boolean indicating if the response body should be logged.
        /// </summary>
        public bool LogResponseBody { get; set; } = false;

        /// <summary>
        /// API format supported by this backend.
        /// Default is Ollama.
        /// </summary>
        public ApiFormatEnum ApiFormat { get; set; } = ApiFormatEnum.Ollama;

        /// <summary>
        /// Boolean indicating if the object is active or not.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Creation timestamp, in UTC time.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp, in UTC time.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp at which the backend was seen as healthy, in UTC time.
        /// </summary>
        public DateTime? HealthySinceUtc { get; set; }

        /// <summary>
        /// Timestamp at which the backend was seen as unhealthy, in UTC time.
        /// </summary>
        public DateTime? UnhealthySinceUtc { get; set; }

        /// <summary>
        /// Uptime for the backend.
        /// </summary>
        public TimeSpan? Uptime
        {
            get
            {
                if (HealthySinceUtc != null) return DateTime.UtcNow - HealthySinceUtc.Value;
                return null;
            }
        }

        /// <summary>
        /// Downtime for the backend.
        /// </summary>
        public TimeSpan? Downtime
        {
            get
            {
                if (UnhealthySinceUtc != null) return DateTime.UtcNow - UnhealthySinceUtc.Value;
                return null;
            }
        }

        /// <summary>
        /// Number of active requests.
        /// </summary>
        public int ActiveRequests
        {
            get
            {
                return _ActiveRequests;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(ActiveRequests));
                _ActiveRequests = value;
            }
        }

        /// <summary>
        /// Boolean indicating whether or not the backend was chosen due to stickiness.
        /// </summary>
        public bool IsSticky { get; set; } = false;

        #endregion

        #region Internal-Members

        internal readonly object Lock = new object();
        internal bool Healthy = false;
        internal int HealthCheckSuccess = 0;
        internal int HealthCheckFailure = 0;
        internal bool ModelsDiscovered = false;
        internal int _ActiveRequests = 0;
        internal int _PendingRequests = 0;
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
        private int _UnhealthyThreshold = 2;
        private int _HealthyThreshold = 2;
        private int _MaxParallelRequests = 4;
        private int _RateLimitRequestsThreshold = 10;
        private HttpMethod _HealthCheckMethod = HttpMethod.Get;
        private string _HealthCheckUrl = "/";
        private List<string> _Models = new List<string>();
        private SemaphoreSlim _Semaphore = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Backend server.
        /// </summary>
        public Backend()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
