namespace OllamaFlow.Core.Models.Agnostic.Common
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents model information in an agnostic format.
    /// </summary>
    public class AgnosticModel
    {
        /// <summary>
        /// Unique identifier for the model.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Human-readable name of the model.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the model (e.g., "chat", "completion", "embedding").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Description of the model.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Size of the model in bytes.
        /// </summary>
        public long? SizeBytes { get; set; }

        /// <summary>
        /// When the model was created.
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// When the model was last modified.
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// Model family or parent model.
        /// </summary>
        public string Family { get; set; }

        /// <summary>
        /// Model format (e.g., "gguf", "pytorch").
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Parameters or configuration for the model.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Extension data for API-specific model features.
        /// </summary>
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
    }
}