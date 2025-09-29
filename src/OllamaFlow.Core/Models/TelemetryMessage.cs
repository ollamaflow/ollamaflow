namespace OllamaFlow.Core.Models
{
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Telemetry message.
    /// </summary>
    public class TelemetryMessage
    {
        /// <summary>
        /// Gets or sets the unique identifier for this request.
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Request type.
        /// </summary>
        public RequestTypeEnum RequestType { get; set; } = RequestTypeEnum.Unknown;

        /// <summary>
        /// API format of the incoming request.
        /// </summary>
        public ApiFormatEnum ApiFormat { get; set; } = ApiFormatEnum.Ollama;

        /// <summary>
        /// Gets or sets the conversation/session identifier this request belongs to.
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the backend server that handled this request.
        /// </summary>
        public string BackendServerId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the client that initiated this request.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the transformation identifier if request/response transformation was performed.
        /// </summary>
        public string TransformationId { get; set; }

        // Public Properties - Request Configuration

        /// <summary>
        /// Request body size in bytes.
        /// Must be non-negative.
        /// </summary>
        public long RequestBodySize
        {
            get => _RequestBodySize;
            set => _RequestBodySize = (value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(RequestBodySize)));
        }

        /// <summary>
        /// Gets or sets the UTC timestamp when the request arrived at the load balancer.
        /// </summary>
        public DateTime RequestArrivalUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the UTC timestamp when a backend server was selected to handle the request.
        /// </summary>
        public DateTime? BackendSelectedUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the request was sent to the backend server.
        /// </summary>
        public DateTime? BackendRequestSentUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the first token was received from the backend server.
        /// </summary>
        public DateTime? FirstTokenTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the last token was received from the backend server.
        /// </summary>
        public DateTime? LastTokenTimeUtc { get; set; }

        /// <summary>
        /// Gets the time in milliseconds between request arrival and backend server selection.
        /// Returns null if either timestamp is not set.
        /// </summary>
        public double? BackendSelectionTimeMs
        {
            get
            {
                if (BackendSelectedUtc.HasValue)
                    return (BackendSelectedUtc.Value - RequestArrivalUtc).TotalMilliseconds;
                return null;
            }
        }

        /// <summary>
        /// Gets the time in milliseconds between sending the request to backend and receiving the first token.
        /// Returns null if either timestamp is not set.
        /// </summary>
        public double? TimeToFirstTokenMs
        {
            get
            {
                if (BackendRequestSentUtc.HasValue && FirstTokenTimeUtc.HasValue)
                    return (FirstTokenTimeUtc.Value - BackendRequestSentUtc.Value).TotalMilliseconds;
                return null;
            }
        }

        /// <summary>
        /// Gets the time in milliseconds between sending the request to backend and receiving the last token.
        /// Returns null if either timestamp is not set.
        /// </summary>
        public double? TimeToCompletionMs
        {
            get
            {
                if (BackendRequestSentUtc.HasValue && LastTokenTimeUtc.HasValue)
                    return (LastTokenTimeUtc.Value - BackendRequestSentUtc.Value).TotalMilliseconds;
                return null;
            }
        }

        /// <summary>
        /// Gets the total end-to-end request time in milliseconds (from arrival to last token).
        /// Returns null if last token timestamp is not set.
        /// </summary>
        public double? TotalRequestTimeMs
        {
            get
            {
                if (LastTokenTimeUtc.HasValue)
                    return (LastTokenTimeUtc.Value - RequestArrivalUtc).TotalMilliseconds;
                return null;
            }
        }

        private long _RequestBodySize = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryMessage"/> class.
        /// </summary>
        public TelemetryMessage()
        {
        }
    }
}