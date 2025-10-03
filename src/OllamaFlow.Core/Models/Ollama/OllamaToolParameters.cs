namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama tool parameters schema.
    /// </summary>
    public class OllamaToolParameters
    {
        /// <summary>
        /// Type of the parameters object (required).
        /// Should typically be "object".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type
        {
            get => _Type;
            set
            {
                if (value != null && value != "object")
                    throw new ArgumentException("Parameters type must be 'object'", nameof(Type));
                _Type = value;
            }
        }

        /// <summary>
        /// Properties of the parameters object (required).
        /// Each property is a JSON Schema definition.
        /// </summary>
        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; } = null;

        /// <summary>
        /// List of required property names (optional).
        /// </summary>
        [JsonPropertyName("required")]
        public List<string> Required { get; set; } = null;

        // Private backing fields
        private string _Type;

        /// <summary>
        /// Ollama tool parameters.
        /// </summary>
        public OllamaToolParameters()
        {
        }
    }
}