namespace OllamaFlow.Core.Models.OpenAI
{
    using OllamaFlow.Core.Models.OpenAI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI generate embeddings result.
    /// </summary>
    public class OpenAIGenerateEmbeddingsResult
    {
        /// <summary>
        /// Object type (always "list").
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; }

        /// <summary>
        /// List of embedding objects.
        /// </summary>
        [JsonPropertyName("data")]
        public List<OpenAIEmbedding> Data { get; set; }

        /// <summary>
        /// Model used to generate embeddings.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// Usage statistics.
        /// </summary>
        [JsonPropertyName("usage")]
        public OpenAIEmbeddingUsage Usage { get; set; }

        /// <summary>
        /// Gets a single embedding array.
        /// Throws if the result contains multiple embeddings.
        /// </summary>
        public float[] GetEmbedding()
        {
            if (Data == null || Data.Count == 0)
                return null;

            if (Data.Count > 1)
                throw new InvalidOperationException($"Result contains {Data.Count} embeddings. Use GetEmbeddings() instead.");

            return Data[0].Embedding;
        }

        /// <summary>
        /// Gets all embeddings as a list of arrays.
        /// </summary>
        public List<float[]> GetEmbeddings()
        {
            if (Data == null || Data.Count == 0)
                return new List<float[]>();

            return Data
                .OrderBy(e => e.Index)
                .Select(e => e.Embedding)
                .ToList();
        }

        /// <summary>
        /// Gets all embeddings as a jagged array.
        /// </summary>
        public float[][] GetEmbeddingsArray()
        {
            if (Data == null || Data.Count == 0)
                return new float[0][];

            return Data
                .OrderBy(e => e.Index)
                .Select(e => e.Embedding)
                .ToArray();
        }

        /// <summary>
        /// Checks if the result contains a single embedding.
        /// </summary>
        /// <returns>True if single embedding, false if multiple.</returns>
        public bool IsSingleEmbedding()
        {
            return Data != null && Data.Count == 1;
        }

        /// <summary>
        /// Checks if the result contains multiple embeddings.
        /// </summary>
        /// <returns>True if multiple embeddings, false if single.</returns>
        public bool IsMultiEmbeddings()
        {
            return Data != null && Data.Count > 1;
        }

        /// <summary>
        /// Gets the number of embeddings in the result.
        /// </summary>
        public int GetEmbeddingCount()
        {
            return Data?.Count ?? 0;
        }

        /// <summary>
        /// Gets the dimension of the embeddings.
        /// </summary>
        public int? GetEmbeddingDimension()
        {
            if (Data == null || Data.Count == 0)
                return null;

            return Data[0].Embedding?.Length;
        }

        /// <summary>
        /// OpenAI generate embeddings result.
        /// </summary>
        public OpenAIGenerateEmbeddingsResult()
        {
        }
    }
}