namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI chat completion chunk (single chunk from streaming response).
    /// </summary>
    public class OpenAIChatCompletionChunk
    {
        /// <summary>
        /// Unique identifier for the chat completion.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Object type (always "chat.completion.chunk").
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
        /// List of chat completion choices with delta updates.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<OpenAIChatChoice> Choices { get; set; }

        /// <summary>
        /// Usage statistics for the completion.
        /// Only present in the final chunk when stream_options.include_usage is true.
        /// </summary>
        [JsonPropertyName("usage")]
        public OpenAIUsage Usage { get; set; }

        /// <summary>
        /// OpenAI chat completion chunk.
        /// </summary>
        public OpenAIChatCompletionChunk()
        {
        }
    }
}
