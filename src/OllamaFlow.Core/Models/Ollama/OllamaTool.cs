namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama tool definition.
    /// </summary>
    public class OllamaTool
    {
        /// <summary>
        /// Type of tool (required).
        /// Currently only "function" is supported.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type
        {
            get => _Type;
            set
            {
                if (value != null && value != "function")
                    throw new ArgumentException("Type must be 'function'", nameof(Type));
                _Type = value;
            }
        }

        /// <summary>
        /// Function definition (required when type is "function").
        /// </summary>
        [JsonPropertyName("function")]
        public OllamaToolFunction Function { get; set; } = null;

        // Private backing fields
        private string _Type;

        /// <summary>
        /// Ollama tool.
        /// </summary>
        public OllamaTool()
        {
        }
    }
}