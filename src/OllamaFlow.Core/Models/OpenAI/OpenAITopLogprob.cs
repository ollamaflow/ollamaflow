namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI top logprob entry.
    /// </summary>
    public class OpenAITopLogprob
    {
        /// <summary>
        /// The token.
        /// </summary>
        [JsonPropertyName("token")]
        public string Token { get; set; }

        /// <summary>
        /// Log probability of this token.
        /// </summary>
        [JsonPropertyName("logprob")]
        public float Logprob { get; set; }

        /// <summary>
        /// UTF-8 byte representation of the token.
        /// </summary>
        [JsonPropertyName("bytes")]
        public List<int> Bytes { get; set; }

        /// <summary>
        /// OpenAI top logprob.
        /// </summary>
        public OpenAITopLogprob()
        {
        }
    }
}