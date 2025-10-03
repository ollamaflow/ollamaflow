namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI generate completion result.
    /// </summary>
    public class OpenAIGenerateCompletionResult
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
        /// Usage statistics for the completion.
        /// </summary>
        [JsonPropertyName("usage")]
        public OpenAIUsage Usage { get; set; }

        /// <summary>
        /// Gets the created timestamp as DateTime.
        /// </summary>
        public DateTime? GetCreatedDateTime()
        {
            if (!Created.HasValue)
                return null;
            return DateTimeOffset.FromUnixTimeSeconds(Created.Value).DateTime;
        }

        /// <summary>
        /// Gets the primary completion text.
        /// </summary>
        public string GetCompletionText()
        {
            return Choices?.FirstOrDefault()?.Text;
        }

        /// <summary>
        /// Checks if any choice was truncated due to length.
        /// </summary>
        public bool HasTruncatedChoices()
        {
            return Choices?.Any(c => c.FinishReason == "length") ?? false;
        }

        /// <summary>
        /// OpenAI generate completion result.
        /// </summary>
        public OpenAIGenerateCompletionResult()
        {
        }
    }
}