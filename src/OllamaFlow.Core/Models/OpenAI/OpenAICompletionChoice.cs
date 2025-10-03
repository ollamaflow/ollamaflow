namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI completion choice.
    /// </summary>
    public class OpenAICompletionChoice
    {
        /// <summary>
        /// Generated text.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }

        /// <summary>
        /// Index of this choice.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// Log probabilities information.
        /// </summary>
        [JsonPropertyName("logprobs")]
        public OpenAILogprobs Logprobs { get; set; }

        /// <summary>
        /// Reason the generation stopped.
        /// Possible values: "stop", "length"
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }

        /// <summary>
        /// OpenAI completion choice.
        /// </summary>
        public OpenAICompletionChoice()
        {
        }
    }
}