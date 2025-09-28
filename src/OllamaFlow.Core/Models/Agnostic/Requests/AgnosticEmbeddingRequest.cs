namespace OllamaFlow.Core.Models.Agnostic.Requests
{
    using System.Collections.Generic;
    using OllamaFlow.Core.Models.Agnostic.Base;

    /// <summary>
    /// Agnostic embedding request.
    /// </summary>
    public class AgnosticEmbeddingRequest : AgnosticRequest
    {
        /// <summary>
        /// The model to use for generating embeddings.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Input text or texts to generate embeddings for.
        /// </summary>
        public object Input { get; set; }

        /// <summary>
        /// The format to return embeddings in.
        /// </summary>
        public string EncodingFormat { get; set; } = "float";

        /// <summary>
        /// Additional options for model-specific parameters.
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();
    }
}