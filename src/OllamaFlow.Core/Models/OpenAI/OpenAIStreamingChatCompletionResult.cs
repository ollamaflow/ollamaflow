namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI streaming chat completion result.
    /// </summary>
    public class OpenAIStreamingChatCompletionResult
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
        /// List of chat completion choices with deltas.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<OpenAIChatChoice> Choices { get; set; }

        /// <summary>
        /// Usage statistics (only in final chunk).
        /// </summary>
        [JsonPropertyName("usage")]
        public OpenAIUsage Usage { get; set; }

        /// <summary>
        /// Checks if this is the final chunk.
        /// </summary>
        public bool IsFinalChunk()
        {
            return Choices?.Any(c => c.FinishReason != null) ?? false;
        }

        /// <summary>
        /// OpenAI streaming chat completion result.
        /// </summary>
        public OpenAIStreamingChatCompletionResult()
        {
        }
    }
}