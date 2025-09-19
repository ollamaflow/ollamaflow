namespace OllamaFlow.Core.Models
{
    using System;

    /// <summary>
    /// Represents a sticky session binding a client to a specific backend.
    /// </summary>
    public class StickySession
    {
        /// <summary>
        /// Client identifier (typically IP address).
        /// </summary>
        public string ClientId { get; set; } = null;

        /// <summary>
        /// Frontend identifier associated with this session.
        /// </summary>
        public string FrontendId { get; set; } = null;

        /// <summary>
        /// Backend identifier this client is bound to.
        /// </summary>
        public string BackendId { get; set; } = null;

        /// <summary>
        /// Creation timestamp, in UTC time.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Expiration timestamp, in UTC time.
        /// </summary>
        public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last access timestamp, in UTC time.
        /// Updated each time the session is used.
        /// </summary>
        public DateTime LastAccessUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public StickySession()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="frontendId">Frontend identifier.</param>
        /// <param name="backendId">Backend identifier.</param>
        /// <param name="expirationMs">Expiration duration in milliseconds.</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when expirationMs is less than or equal to zero.</exception>
        public StickySession(string clientId, string frontendId, string backendId, int expirationMs)
        {
            if (String.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));
            if (String.IsNullOrEmpty(frontendId)) throw new ArgumentNullException(nameof(frontendId));
            if (String.IsNullOrEmpty(backendId)) throw new ArgumentNullException(nameof(backendId));
            if (expirationMs <= 0) throw new ArgumentOutOfRangeException(nameof(expirationMs));

            ClientId = clientId;
            FrontendId = frontendId;
            BackendId = backendId;
            CreatedUtc = DateTime.UtcNow;
            ExpiresUtc = DateTime.UtcNow.AddMilliseconds(expirationMs);
            LastAccessUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Check if the session has expired.
        /// </summary>
        /// <returns>True if the session has expired, false otherwise.</returns>
        public bool IsExpired()
        {
            return DateTime.UtcNow > ExpiresUtc;
        }

        /// <summary>
        /// Update the last access timestamp and extend expiration if needed.
        /// </summary>
        /// <param name="expirationMs">New expiration duration in milliseconds.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when expirationMs is less than or equal to zero.</exception>
        public void Touch(int expirationMs)
        {
            if (expirationMs <= 0) throw new ArgumentOutOfRangeException(nameof(expirationMs));

            LastAccessUtc = DateTime.UtcNow;
            ExpiresUtc = DateTime.UtcNow.AddMilliseconds(expirationMs);
        }

        /// <summary>
        /// Create a unique session key for storage.
        /// </summary>
        /// <returns>Session key combining client and frontend identifiers.</returns>
        public string GetSessionKey()
        {
            return $"{ClientId}:{FrontendId}";
        }
    }
}