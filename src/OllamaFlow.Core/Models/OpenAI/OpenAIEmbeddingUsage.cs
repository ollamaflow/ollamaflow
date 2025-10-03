namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI embedding usage statistics.
    /// </summary>
    public class OpenAIEmbeddingUsage
    {
        /// <summary>
        /// Number of tokens in the input.
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// Total number of tokens used.
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        /// <summary>
        /// OpenAI embedding usage.
        /// </summary>
        public OpenAIEmbeddingUsage()
        {
        }
    }
}