namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI prompt tokens details.
    /// </summary>
    public class OpenAIPromptTokensDetails
    {
        /// <summary>
        /// Number of tokens from cached content.
        /// </summary>
        [JsonPropertyName("cached_tokens")]
        public int? CachedTokens { get; set; }

        /// <summary>
        /// Number of tokens from audio input.
        /// </summary>
        [JsonPropertyName("audio_tokens")]
        public int? AudioTokens { get; set; }

        /// <summary>
        /// OpenAI prompt tokens details.
        /// </summary>
        public OpenAIPromptTokensDetails()
        {
        }
    }
}