namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI tool call.
    /// </summary>
    public class OpenAIToolCall
    {
        /// <summary>
        /// Unique identifier for the tool call.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null;

        /// <summary>
        /// Type of tool call (currently only "function").
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        /// <summary>
        /// Function call details.
        /// </summary>
        [JsonPropertyName("function")]
        public OpenAIToolCallFunction Function { get; set; } = null;

        /// <summary>
        /// OpenAI tool call.
        /// </summary>
        public OpenAIToolCall()
        {
        }
    }
}