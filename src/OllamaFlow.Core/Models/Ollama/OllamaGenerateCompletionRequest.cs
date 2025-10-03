namespace OllamaFlow.Core.Models.Ollama
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama generate completion request.
    /// </summary>
    public class OllamaGenerateCompletion
    {
        /// <summary>
        /// Model name to use for generation (required).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// The prompt to generate a response for (required).
        /// </summary>
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = null;

        /// <summary>
        /// Additional model parameters (optional).
        /// </summary>
        [JsonPropertyName("options")]
        public OllamaCompletionOptions Options { get; set; } = null;

        /// <summary>
        /// System message to use (overrides what is defined in the Modelfile) (optional).
        /// </summary>
        [JsonPropertyName("system")]
        public string System { get; set; } = null;

        /// <summary>
        /// The full prompt or prompt template (overrides what is defined in the Modelfile) (optional).
        /// </summary>
        [JsonPropertyName("template")]
        public string Template { get; set; } = null;

        /// <summary>
        /// The context from a previous request, used to keep a conversation going (optional).
        /// </summary>
        [JsonPropertyName("context")]
        public List<int> Context
        {
            get => _Context;
            set => _Context = (value != null ? value : new List<int>());
        }

        /// <summary>
        /// Enable streaming of generated text (optional, defaults to true).
        /// </summary>
        [JsonPropertyName("stream")]
        public bool? Stream { get; set; } = null;

        /// <summary>
        /// If false, the response will not include the prompt (optional).
        /// </summary>
        [JsonPropertyName("raw")]
        public bool? Raw { get; set; } = null;

        /// <summary>
        /// Format to return the response in. Currently only "json" is supported (optional).
        /// </summary>
        [JsonPropertyName("format")]
        public string Format { get; set; } = null;

        /// <summary>
        /// Base64-encoded images for multimodal models (optional).
        /// </summary>
        [JsonPropertyName("images")]
        public List<string> Images
        {
            get => _Images;
            set => _Images = (value != null ? value : new List<string>());
        }

        /// <summary>
        /// How long to keep the model loaded in memory (optional).
        /// Examples: "5m", "10m", "1h", "never"
        /// </summary>
        [JsonPropertyName("keep_alive")]
        public string KeepAlive { get; set; } = null;

        private List<int> _Context = new List<int>();
        private List<string> _Images = new List<string>();

        /// <summary>
        /// Ollama generate completion request.
        /// </summary>
        public OllamaGenerateCompletion()
        {
        }
    }
}