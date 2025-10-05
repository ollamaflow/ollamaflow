namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI completion chunk (single chunk from streaming response).
    /// </summary>
    public class OpenAICompletionChunk
    {
        /// <summary>
        /// Unique identifier for the completion.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Object type (always "text_completion").
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; }

        /// <summary>
        /// Unix timestamp when the completion was created.
        /// </summary>
        [JsonPropertyName("created")]
        public long? Created { get; set; }

        /// <summary>
        /// Model used for the completion.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// System fingerprint.
        /// </summary>
        [JsonPropertyName("system_fingerprint")]
        public string SystemFingerprint { get; set; }

        /// <summary>
        /// List of completion choices.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<OpenAICompletionChoice> Choices { get; set; }

        /// <summary>
        /// OpenAI completion chunk.
        /// </summary>
        public OpenAICompletionChunk()
        {
        }
    }
}
