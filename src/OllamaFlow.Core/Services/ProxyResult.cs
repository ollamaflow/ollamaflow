namespace OllamaFlow.Core.Services
{
    using System.Collections.Specialized;

    /// <summary>
    /// Result of a proxy request operation.
    /// </summary>
    public class ProxyResult
    {
        /// <summary>
        /// Whether a response was received from the backend.
        /// </summary>
        public bool ResponseReceived { get; set; }

        /// <summary>
        /// The HTTP status code returned by the backend.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// The response body as bytes.
        /// </summary>
        public byte[] ResponseBody { get; set; }

        /// <summary>
        /// The response content type.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The response headers.
        /// </summary>
        public NameValueCollection Headers { get; set; }

        /// <summary>
        /// Whether the response was sent to the client (for non-transformed responses).
        /// </summary>
        public bool AlreadySent { get; set; }
    }
}