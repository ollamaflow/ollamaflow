namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama streaming completion result.
    /// Used for intermediate streaming responses.
    /// </summary>
    public class OllamaStreamingCompletionResult
    {
        /// <summary>
        /// The model that generated the response.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// The timestamp of when the response was created.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// The partial response text.
        /// </summary>
        [JsonPropertyName("response")]
        public string Response { get; set; }

        /// <summary>
        /// Whether this is the final chunk in the stream.
        /// </summary>
        [JsonPropertyName("done")]
        public bool Done { get; set; }

        /// <summary>
        /// Ollama streaming completion result.
        /// </summary>
        public OllamaStreamingCompletionResult()
        {
        }
    }
}