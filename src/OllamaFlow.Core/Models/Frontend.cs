namespace OllamaFlow.Core
{
    using OllamaFlow.Core.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using Watson.ORM.Core;

    /// <summary>
    /// API endpoint.
    /// </summary>
    public class Frontend
    {
        #region Public-Members

        /// <summary>
        /// Serializer.
        /// </summary>
        [JsonIgnore]
        public static Serializer Serializer = new Serializer();

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
        /// Use * to specify that this frontend is a catch-all.
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
        /// Default is RoundRobin.
        /// </summary>
        public LoadBalancingMode LoadBalancing { get; set; } = LoadBalancingMode.RoundRobin;

        /// <summary>
        /// True to terminate HTTP/1.0 requests.
        /// </summary>
        public bool BlockHttp10 { get; set; } = true;

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
                _BackendsString = Serializer.SerializeJson(value, false);
                _Backends = value;
            }
        }

        /// <summary>
        /// String containing JSON-serialized list of backend identifiers.
        /// Used by the database layer.
        /// </summary>
        [JsonIgnore]
        public string BackendsString
        {
            get
            {
                return _BackendsString;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) value = "[]";
                _BackendsString = value;
                _Backends = Serializer.DeserializeJson<List<string>>(value);
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
                _RequiredModelsString = Serializer.SerializeJson(value, false);
                _RequiredModels = value;
            }
        }

        /// <summary>
        /// String containing JSON-serialized list of required models.
        /// Used by the database layer.
        /// </summary>
        [JsonIgnore]
        public string RequiredModelsString
        {
            get
            {
                return _RequiredModelsString;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) value = "[]";
                _RequiredModelsString = value;
                _RequiredModels = Serializer.DeserializeJson<List<string>>(value);
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
        /// Boolean indicating whether sticky sessions should be used for this frontend.
        /// When enabled, clients will be routed to the same backend for subsequent requests.
        /// Default is false.
        /// </summary>
        public bool UseStickySessions { get; set; } = false;

        /// <summary>
        /// Duration in milliseconds for how long a sticky session should remain active.
        /// Default is 1800000 milliseconds (30 minutes).
        /// Minimum is 10000 milliseconds (10 seconds).
        /// Maximum is 86400000 milliseconds (24 hours).
        /// </summary>
        public int StickySessionExpirationMs
        {
            get
            {
                return _StickySessionExpirationMs;
            }
            set
            {
                if (value < 10000) throw new ArgumentOutOfRangeException(nameof(StickySessionExpirationMs), "Minimum value is 10000 milliseconds (10 seconds)");
                if (value > 86400000) throw new ArgumentOutOfRangeException(nameof(StickySessionExpirationMs), "Maximum value is 86400000 milliseconds (24 hours)");
                _StickySessionExpirationMs = value;
            }
        }

        /// <summary>
        /// String containing JSON-serialized pinned properties, which will be applied to every embeddings request.
        /// </summary>
        [JsonIgnore]
        public string PinnedEmbeddingsPropertiesString
        {
            get
            {
                return Serializer.SerializeJson(_PinnedEmbeddingsProperties, false);
            }
            set
            {
                if (String.IsNullOrEmpty(value)) value = "{}";
                _PinnedEmbeddingsProperties = Serializer.DeserializeJson<Dictionary<string, object>>(value);
            }
        }

        /// <summary>
        /// Dictionary containing pinned properties, which will be applied to every embeddings request.
        /// </summary>
        public Dictionary<string, object> PinnedEmbeddingsProperties
        {
            get
            {
                return _PinnedEmbeddingsProperties;
            }
            set
            {
                if (value == null) value = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
                _PinnedEmbeddingsProperties = value;
            }
        }

        /// <summary>
        /// String containing JSON-serialized pinned properties, which will be applied to every completions request.
        /// </summary>
        [JsonIgnore]
        public string PinnedCompletionsPropertiesString
        {
            get
            {
                return Serializer.SerializeJson(_PinnedCompletionsProperties, false);
            }
            set
            {
                if (String.IsNullOrEmpty(value)) value = "{}";
                _PinnedCompletionsProperties = Serializer.DeserializeJson<Dictionary<string, object>>(value);
            }
        }

        /// <summary>
        /// Dictionary containing pinned properties, which will be applied to every completions request.
        /// </summary>
        public Dictionary<string, object> PinnedCompletionsProperties
        {
            get
            {
                return _PinnedCompletionsProperties;
            }
            set
            {
                if (value == null) value = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
                _PinnedCompletionsProperties = value;
            }
        }

        /// <summary>
        /// Boolean indicating if embeddings requests are allowed.
        /// </summary>
        public bool AllowEmbeddings { get; set; } = true;

        /// <summary>
        /// Boolean indicating if completions requests are allowed.
        /// </summary>
        public bool AllowCompletions { get; set; } = true;

        /// <summary>
        /// Allow OllamaFlow to retry failed requests.
        /// </summary>
        public bool AllowRetries { get; set; } = true;

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

        #endregion

        #region Internal-Members

        internal readonly object Lock = new object();
        internal int LastBackendIndex = 0;

        #endregion

        #region Private-Members

        private string _Hostname = "*";
        private int _TimeoutMs = 60000;
        private int _MaxRequestBodySize = (512 * 1024 * 1024);
        private List<string> _Backends = new List<string>();
        private string _BackendsString = "[]";
        private List<string> _RequiredModels = new List<string>();
        private string _RequiredModelsString = "[]";
        private int _StickySessionExpirationMs = 1800000; // 30 minutes
        private Dictionary<string, object> _PinnedEmbeddingsProperties = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<string, object> _PinnedCompletionsProperties = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Frontend()
        {

        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
