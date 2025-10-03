namespace OllamaFlow.Core.Models.Ollama
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama show model information request.
    /// </summary>
    public class OllamaShowModelInfoRequest
    {
        /// <summary>
        /// Name of the model to show (required).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Include verbose information about the model (optional).
        /// </summary>
        [JsonPropertyName("verbose")]
        public bool? Verbose { get; set; }

        /// <summary>
        /// Ollama show model information request.
        /// </summary>
        public OllamaShowModelInfoRequest()
        {
        }
    }
}