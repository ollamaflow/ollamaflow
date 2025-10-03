namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI tool definition.
    /// </summary>
    public class OpenAITool
    {
        /// <summary>
        /// Type of tool (currently only "function" is supported).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        /// <summary>
        /// Function definition.
        /// </summary>
        [JsonPropertyName("function")]
        public OpenAIToolFunction Function { get; set; } = null;

        /// <summary>
        /// OpenAI tool.
        /// </summary>
        public OpenAITool()
        {
        }
    }
}