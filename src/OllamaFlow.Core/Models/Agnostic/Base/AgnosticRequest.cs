namespace OllamaFlow.Core.Models.Agnostic.Base
{
    using System;
    using System.Collections.Generic;
    using OllamaFlow.Core.Enums;

    /// <summary>
    /// Base class for all agnostic requests.
    /// </summary>
    public abstract class AgnosticRequest
    {
        /// <summary>
        /// Unique identifier for this request.
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The API format this request originated from.
        /// </summary>
        public ApiFormatEnum SourceFormat { get; set; }

        /// <summary>
        /// Timestamp when the request was created.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Extension data for API-specific features that don't map to agnostic format.
        /// </summary>
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Original request object for debugging/logging purposes.
        /// </summary>
        public object OriginalRequest { get; set; }
    }
}