namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama generate chat completion request.
    /// </summary>
    public class OllamaGenerateChatCompletionRequest
    {
        /// <summary>
        /// Model name to use for chat completion (required).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Messages for the chat (required).
        /// </summary>
        [JsonPropertyName("messages")]
        public List<OllamaChatMessage> Messages { get; set; } = null;

        /// <summary>
        /// Additional model parameters (optional).
        /// </summary>
        [JsonPropertyName("options")]
        public OllamaCompletionOptions Options { get; set; } = null;

        /// <summary>
        /// Format to return the response in. Currently only "json" is supported (optional).
        /// </summary>
        [JsonPropertyName("format")]
        public string Format { get; set; } = null;

        /// <summary>
        /// The full prompt template (overrides what is defined in the Modelfile) (optional).
        /// </summary>
        [JsonPropertyName("template")]
        public string Template { get; set; } = null;

        /// <summary>
        /// Enable streaming of generated text (optional, defaults to true).
        /// </summary>
        [JsonPropertyName("stream")]
        public bool? Stream { get; set; } = null;

        /// <summary>
        /// How long to keep the model loaded in memory (optional).
        /// Examples: "5m", "10m", "1h", "never"
        /// </summary>
        [JsonPropertyName("keep_alive")]
        public string KeepAlive { get; set; } = null;

        /// <summary>
        /// Tools/functions available for the model to use (optional).
        /// </summary>
        [JsonPropertyName("tools")]
        public List<OllamaTool> Tools { get; set; } = null;

        /// <summary>
        /// Ollama generate chat completion request.
        /// </summary>
        public OllamaGenerateChatCompletionRequest()
        {
        }
    }
}