namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama generate completion chunk (single chunk from streaming response).
    /// </summary>
    public class OllamaGenerateCompletionChunk
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
        /// The partial generated text response.
        /// </summary>
        [JsonPropertyName("response")]
        public string Response { get; set; }

        /// <summary>
        /// Whether the response is complete.
        /// False for intermediate chunks, true for the final chunk.
        /// </summary>
        [JsonPropertyName("done")]
        public bool Done { get; set; }

        /// <summary>
        /// Context for maintaining conversation state.
        /// Only present in the final chunk (when done=true).
        /// </summary>
        [JsonPropertyName("context")]
        public List<int> Context { get; set; }

        /// <summary>
        /// Total duration in nanoseconds.
        /// Only present in the final chunk (when done=true).
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long? TotalDuration { get; set; }

        /// <summary>
        /// Model load duration in nanoseconds.
        /// Only present in the final chunk (when done=true).
        /// </summary>
        [JsonPropertyName("load_duration")]
        public long? LoadDuration { get; set; }

        /// <summary>
        /// Number of tokens in the prompt.
        /// Only present in the final chunk (when done=true).
        /// </summary>
        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; }

        /// <summary>
        /// Prompt evaluation duration in nanoseconds.
        /// Only present in the final chunk (when done=true).
        /// </summary>
        [JsonPropertyName("prompt_eval_duration")]
        public long? PromptEvalDuration { get; set; }

        /// <summary>
        /// Number of tokens generated in the response.
        /// Only present in the final chunk (when done=true).
        /// </summary>
        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }

        /// <summary>
        /// Response generation duration in nanoseconds.
        /// Only present in the final chunk (when done=true).
        /// </summary>
        [JsonPropertyName("eval_duration")]
        public long? EvalDuration { get; set; }

        /// <summary>
        /// Ollama generate completion chunk.
        /// </summary>
        public OllamaGenerateCompletionChunk()
        {
        }
    }
}
