namespace OllamaFlow.Core.Models.Agnostic.Responses
{
    using System.Collections.Generic;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models.Agnostic.Common;

    /// <summary>
    /// Agnostic embedding response.
    /// </summary>
    public class AgnosticEmbeddingResponse : AgnosticResponse
    {
        /// <summary>
        /// Object type, typically "list".
        /// </summary>
        public string Object { get; set; } = "list";

        /// <summary>
        /// The embedding data.
        /// </summary>
        public List<AgnosticEmbeddingData> Data { get; set; } = new List<AgnosticEmbeddingData>();

        /// <summary>
        /// The model used for generating embeddings.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Usage statistics for the embedding generation.
        /// </summary>
        public AgnosticUsage Usage { get; set; }

        /// <summary>
        /// The embedding vector (for single embedding requests).
        /// </summary>
        public List<double> Embedding { get; set; }
    }

    /// <summary>
    /// Individual embedding data item.
    /// </summary>
    public class AgnosticEmbeddingData
    {
        /// <summary>
        /// Object type, typically "embedding".
        /// </summary>
        public string Object { get; set; } = "embedding";

        /// <summary>
        /// Index of this embedding in the list.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The embedding vector.
        /// </summary>
        public List<double> Embedding { get; set; } = new List<double>();
    }
}