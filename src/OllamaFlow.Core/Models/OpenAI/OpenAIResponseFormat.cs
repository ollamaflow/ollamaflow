namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI response format specification.
    /// </summary>
    public class OpenAIResponseFormat
    {
        /// <summary>
        /// Type of response format.
        /// Valid values: "text", "json_object", "json_schema"
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = null;

        /// <summary>
        /// JSON schema for structured output (when type is "json_schema").
        /// </summary>
        [JsonPropertyName("json_schema")]
        public object JsonSchema { get; set; } = null;

        /// <summary>
        /// OpenAI response format.
        /// </summary>
        public OpenAIResponseFormat()
        {
        }
    }
}