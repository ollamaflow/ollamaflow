namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama generate completion result.
    /// </summary>
    public class OllamaGenerateCompletionResult
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
        /// The generated text response.
        /// </summary>
        [JsonPropertyName("response")]
        public string Response { get; set; }

        /// <summary>
        /// Whether the response is complete (for streaming responses).
        /// </summary>
        [JsonPropertyName("done")]
        public bool? Done { get; set; }

        /// <summary>
        /// Context for maintaining conversation state.
        /// Can be passed in the next request to continue the conversation.
        /// </summary>
        [JsonPropertyName("context")]
        public List<int> Context { get; set; }

        /// <summary>
        /// Total duration in nanoseconds.
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long? TotalDuration { get; set; }

        /// <summary>
        /// Model load duration in nanoseconds.
        /// </summary>
        [JsonPropertyName("load_duration")]
        public long? LoadDuration { get; set; }

        /// <summary>
        /// Number of tokens in the prompt.
        /// </summary>
        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; }

        /// <summary>
        /// Prompt evaluation duration in nanoseconds.
        /// </summary>
        [JsonPropertyName("prompt_eval_duration")]
        public long? PromptEvalDuration { get; set; }

        /// <summary>
        /// Number of tokens generated in the response.
        /// </summary>
        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }

        /// <summary>
        /// Response generation duration in nanoseconds.
        /// </summary>
        [JsonPropertyName("eval_duration")]
        public long? EvalDuration { get; set; }

        /// <summary>
        /// Gets the prompt evaluation rate in tokens per second.
        /// </summary>
        /// <returns>Tokens per second for prompt evaluation, or null if data unavailable.</returns>
        public double? GetPromptTokensPerSecond()
        {
            if (PromptEvalCount.HasValue && PromptEvalDuration.HasValue && PromptEvalDuration.Value > 0)
            {
                return PromptEvalCount.Value / (PromptEvalDuration.Value / 1_000_000_000.0);
            }
            return null;
        }

        /// <summary>
        /// Gets the response generation rate in tokens per second.
        /// </summary>
        /// <returns>Tokens per second for generation, or null if data unavailable.</returns>
        public double? GetGenerationTokensPerSecond()
        {
            if (EvalCount.HasValue && EvalDuration.HasValue && EvalDuration.Value > 0)
            {
                return EvalCount.Value / (EvalDuration.Value / 1_000_000_000.0);
            }
            return null;
        }

        /// <summary>
        /// Gets the total response time in milliseconds.
        /// </summary>
        /// <returns>Total duration in milliseconds, or null if unavailable.</returns>
        public double? GetTotalDurationMilliseconds()
        {
            if (TotalDuration.HasValue)
            {
                return TotalDuration.Value / 1_000_000.0;
            }
            return null;
        }

        /// <summary>
        /// Gets the model load time in milliseconds.
        /// </summary>
        /// <returns>Load duration in milliseconds, or null if unavailable.</returns>
        public double? GetLoadDurationMilliseconds()
        {
            if (LoadDuration.HasValue)
            {
                return LoadDuration.Value / 1_000_000.0;
            }
            return null;
        }

        /// <summary>
        /// Gets the prompt evaluation time in milliseconds.
        /// </summary>
        /// <returns>Prompt evaluation duration in milliseconds, or null if unavailable.</returns>
        public double? GetPromptEvalDurationMilliseconds()
        {
            if (PromptEvalDuration.HasValue)
            {
                return PromptEvalDuration.Value / 1_000_000.0;
            }
            return null;
        }

        /// <summary>
        /// Gets the generation time in milliseconds.
        /// </summary>
        /// <returns>Generation duration in milliseconds, or null if unavailable.</returns>
        public double? GetEvalDurationMilliseconds()
        {
            if (EvalDuration.HasValue)
            {
                return EvalDuration.Value / 1_000_000.0;
            }
            return null;
        }

        /// <summary>
        /// Checks if this is a final response (for streaming).
        /// </summary>
        /// <returns>True if this is the final response in a stream.</returns>
        public bool IsFinalResponse()
        {
            return Done.HasValue && Done.Value;
        }

        /// <summary>
        /// Gets the total number of tokens (prompt + generated).
        /// </summary>
        /// <returns>Total token count, or null if data unavailable.</returns>
        public int? GetTotalTokenCount()
        {
            if (PromptEvalCount.HasValue && EvalCount.HasValue)
            {
                return PromptEvalCount.Value + EvalCount.Value;
            }
            return null;
        }

        /// <summary>
        /// Checks if context is available for continuing the conversation.
        /// </summary>
        /// <returns>True if context is available.</returns>
        public bool HasContext()
        {
            return Context != null && Context.Count > 0;
        }

        /// <summary>
        /// Gets the size of the context in tokens.
        /// </summary>
        /// <returns>Context size or 0 if no context.</returns>
        public int GetContextSize()
        {
            return Context?.Count ?? 0;
        }

        /// <summary>
        /// Ollama generate completion result.
        /// </summary>
        public OllamaGenerateCompletionResult()
        {
        }
    }
}