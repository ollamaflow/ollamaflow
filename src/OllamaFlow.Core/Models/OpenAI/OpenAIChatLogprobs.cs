namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI chat logprobs data.
    /// </summary>
    public class OpenAIChatLogprobs
    {
        /// <summary>
        /// Log probability information for each content token.
        /// </summary>
        [JsonPropertyName("content")]
        public List<OpenAIChatLogprobContent> Content { get; set; }

        /// <summary>
        /// OpenAI chat logprobs.
        /// </summary>
        public OpenAIChatLogprobs()
        {
        }
    }
}