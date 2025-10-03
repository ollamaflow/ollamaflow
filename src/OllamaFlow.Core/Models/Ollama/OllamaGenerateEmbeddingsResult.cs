namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama generate embeddings result.
    /// </summary>
    public class OllamaGenerateEmbeddingsResult
    {
        /// <summary>
        /// The model that generated the embeddings.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// Generated embedding(s).
        /// Can be a single array of floats or an array of arrays of floats.
        /// Use GetEmbedding() for single result or GetEmbeddings() for multiple results.
        /// </summary>
        [JsonPropertyName("embeddings")]
        [JsonConverter(typeof(OllamaEmbeddingsOutputConverter))]
        public object Embeddings
        {
            get => _Embeddings;
            set => _Embeddings = value;
        }

        /// <summary>
        /// Total duration in nanoseconds.
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long? TotalDuration { get; set; }

        /// <summary>
        /// Model load duration in nanoseconds.
        /// </summary>
        [JsonPropertyName("load_duration")]
        public long? LoadDuration { get; set; }

        /// <summary>
        /// Prompt evaluation count.
        /// </summary>
        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; }

        // Private backing fields
        private object _Embeddings;

        /// <summary>
        /// Gets a single embedding array.
        /// Throws if the result contains multiple embeddings.
        /// </summary>
        /// <returns>Single embedding array.</returns>
        public List<float> GetEmbedding()
        {
            if (_Embeddings == null)
                return null;

            if (_Embeddings is List<float> singleEmbedding)
                return singleEmbedding;

            if (_Embeddings is List<List<float>> multipleEmbeddings)
            {
                if (multipleEmbeddings.Count == 1)
                    return multipleEmbeddings[0];
                throw new InvalidOperationException($"Result contains {multipleEmbeddings.Count} embeddings. Use GetEmbeddings() instead.");
            }

            throw new InvalidOperationException($"Embeddings is of unexpected type: {_Embeddings.GetType()}");
        }

        /// <summary>
        /// Gets all embeddings as a list of arrays.
        /// If result is a single embedding, returns a list with one element.
        /// </summary>
        /// <returns>List of embedding arrays.</returns>
        public List<List<float>> GetEmbeddings()
        {
            if (_Embeddings == null)
                return null;

            if (_Embeddings is List<float> singleEmbedding)
                return new List<List<float>> { singleEmbedding };

            if (_Embeddings is List<List<float>> multipleEmbeddings)
                return multipleEmbeddings;

            throw new InvalidOperationException($"Embeddings is of unexpected type: {_Embeddings.GetType()}");
        }

        /// <summary>
        /// Checks if the result contains a single embedding.
        /// </summary>
        /// <returns>True if single embedding, false if multiple.</returns>
        public bool IsSingleEmbedding()
        {
            return _Embeddings is List<float>;
        }

        /// <summary>
        /// Gets the number of embeddings in the result.
        /// </summary>
        /// <returns>Number of embeddings.</returns>
        public int GetEmbeddingCount()
        {
            if (_Embeddings == null)
                return 0;

            if (_Embeddings is List<float>)
                return 1;

            if (_Embeddings is List<List<float>> multipleEmbeddings)
                return multipleEmbeddings.Count;

            return 0;
        }

        /// <summary>
        /// Gets the dimension (length) of the embeddings.
        /// All embeddings in the result should have the same dimension.
        /// </summary>
        /// <returns>Dimension of embeddings, or null if no embeddings.</returns>
        public int? GetEmbeddingDimension()
        {
            if (_Embeddings == null)
                return null;

            if (_Embeddings is List<float> singleEmbedding)
                return singleEmbedding.Count;

            if (_Embeddings is List<List<float>> multipleEmbeddings && multipleEmbeddings.Count > 0)
                return multipleEmbeddings[0].Count;

            return null;
        }

        /// <summary>
        /// Ollama generate embeddings result.
        /// </summary>
        public OllamaGenerateEmbeddingsResult()
        {
        }
    }
}