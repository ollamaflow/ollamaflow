namespace OllamaFlow.Core.Models.Agnostic.Common
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a streaming chunk in an agnostic format.
    /// </summary>
    public class AgnosticStreamChunk
    {
        /// <summary>
        /// Unique identifier for this chunk.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Object type, typically "chat.completion.chunk".
        /// </summary>
        public string Object { get; set; } = "chat.completion.chunk";

        /// <summary>
        /// Unix timestamp when the chunk was created.
        /// </summary>
        public long Created { get; set; }

        /// <summary>
        /// The model used for the completion.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// The completion choices in this chunk.
        /// </summary>
        public List<AgnosticChoice> Choices { get; set; } = new List<AgnosticChoice>();

        /// <summary>
        /// Usage statistics (typically only in the final chunk).
        /// </summary>
        public AgnosticUsage Usage { get; set; }

        /// <summary>
        /// Whether this is the final chunk in the stream.
        /// </summary>
        public bool IsComplete { get; set; } = false;

        /// <summary>
        /// Whether the response is done (for Ollama compatibility).
        /// </summary>
        public bool Done { get; set; } = false;

        /// <summary>
        /// The response text for this chunk (for Ollama compatibility).
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// Extension data for API-specific chunk features.
        /// </summary>
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
    }
}