namespace OllamaFlow.Core.Models.Agnostic.Base
{
    using System;
    using System.Collections.Generic;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models;

    /// <summary>
    /// Base class for all agnostic responses.
    /// </summary>
    public abstract class AgnosticResponse
    {
        /// <summary>
        /// Unique identifier for this response.
        /// </summary>
        public string ResponseId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The request ID this response is for.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// The API format where this response originated from.
        /// </summary>
        public ApiFormatEnum SourceFormat { get; set; }

        /// <summary>
        /// The API format this response should be transformed to.
        /// </summary>
        public ApiFormatEnum TargetFormat { get; set; }

        /// <summary>
        /// The generic request type this response is for, independent of API format.
        /// </summary>
        public RequestTypeEnum RequestType { get; set; }

        /// <summary>
        /// Timestamp when the response was created.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Extension data for API-specific features that don't map to agnostic format.
        /// </summary>
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Original backend response object for debugging/logging purposes.
        /// </summary>
        public object OriginalResponse { get; set; }
    }
}