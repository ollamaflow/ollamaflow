namespace OllamaFlow.Core.Models.Ollama
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama pull model request.
    /// </summary>
    public class OllamaPullModelRequest
    {
        /// <summary>
        /// Model name to pull (required).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Model name to pull (required).
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// Allow insecure connections to the registry (optional).
        /// </summary>
        [JsonPropertyName("insecure")]
        public bool? Insecure { get; set; }

        /// <summary>
        /// Enable streaming of pull progress (optional).
        /// </summary>
        [JsonPropertyName("stream")]
        public bool? Stream { get; set; }

        /// <summary>
        /// Username for registry authentication (optional).
        /// </summary>
        [JsonPropertyName("username")]
        public string Username { get; set; }

        /// <summary>
        /// Password for registry authentication (optional).
        /// </summary>
        [JsonPropertyName("password")]
        public string Password { get; set; }

        /// <summary>
        /// Ollama pull model request.
        /// </summary>
        public OllamaPullModelRequest()
        {
        }
    }
}