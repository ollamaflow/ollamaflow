namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama tool call made by the assistant.
    /// </summary>
    public class OllamaToolCall
    {
        /// <summary>
        /// Function that was called (required).
        /// </summary>
        [JsonPropertyName("function")]
        public OllamaToolCallFunction Function { get; set; } = null;

        /// <summary>
        /// Ollama tool call.
        /// </summary>
        public OllamaToolCall()
        {
        }
    }
}