namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI usage statistics.
    /// </summary>
    public class OpenAIUsage
    {
        /// <summary>
        /// Number of tokens in the prompt.
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// Number of tokens in the completion.
        /// </summary>
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Total number of tokens used.
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        /// <summary>
        /// Detailed breakdown of prompt tokens (for chat completions with images).
        /// </summary>
        [JsonPropertyName("prompt_tokens_details")]
        public OpenAIPromptTokensDetails PromptTokensDetails { get; set; }

        /// <summary>
        /// Detailed breakdown of completion tokens.
        /// </summary>
        [JsonPropertyName("completion_tokens_details")]
        public OpenAICompletionTokensDetails CompletionTokensDetails { get; set; }

        /// <summary>
        /// OpenAI usage.
        /// </summary>
        public OpenAIUsage()
        {
        }
    }
}