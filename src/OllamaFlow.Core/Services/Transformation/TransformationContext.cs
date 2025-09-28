namespace OllamaFlow.Core.Services.Transformation
{
    using System;
    using System.Collections.Generic;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models;

    /// <summary>
    /// Context information for transformation operations.
    /// </summary>
    public class TransformationContext
    {
        /// <summary>
        /// Unique identifier for this transformation operation.
        /// </summary>
        public string TransformationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The source API format.
        /// </summary>
        public ApiFormatEnum SourceFormat { get; set; }

        /// <summary>
        /// The target API format.
        /// </summary>
        public ApiFormatEnum TargetFormat { get; set; }

        /// <summary>
        /// The request type being transformed.
        /// </summary>
        public RequestTypeEnum RequestType { get; set; }

        /// <summary>
        /// Whether this is a streaming request.
        /// </summary>
        public bool IsStreaming { get; set; }

        /// <summary>
        /// The client identifier making the request.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Timestamp when the transformation started.
        /// </summary>
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional metadata for the transformation.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Extension data for transformation-specific context.
        /// </summary>
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
    }
}