namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Performance metrics for chat completion.
    /// </summary>
    public class OllamaChatCompletionMetrics
    {
        /// <summary>
        /// Number of tokens in the prompt.
        /// </summary>
        public int PromptTokens { get; set; }

        /// <summary>
        /// Number of tokens generated.
        /// </summary>
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Total number of tokens.
        /// </summary>
        public int TotalTokens => PromptTokens + CompletionTokens;

        /// <summary>
        /// Prompt evaluation rate (tokens/second).
        /// </summary>
        public double PromptTokensPerSecond { get; set; }

        /// <summary>
        /// Generation rate (tokens/second).
        /// </summary>
        public double GenerationTokensPerSecond { get; set; }

        /// <summary>
        /// Total response time in milliseconds.
        /// </summary>
        public double TotalDurationMs { get; set; }

        /// <summary>
        /// Model load time in milliseconds.
        /// </summary>
        public double LoadDurationMs { get; set; }

        /// <summary>
        /// Prompt evaluation time in milliseconds.
        /// </summary>
        public double PromptEvalDurationMs { get; set; }

        /// <summary>
        /// Generation time in milliseconds.
        /// </summary>
        public double GenerationDurationMs { get; set; }

        /// <summary>
        /// Creates metrics from a chat completion result.
        /// </summary>
        /// <param name="result">The chat completion result.</param>
        /// <returns>Metrics object or null if insufficient data.</returns>
        public static OllamaChatCompletionMetrics FromResult(OllamaGenerateChatCompletionResult result)
        {
            if (result == null)
                return null;

            var metrics = new OllamaChatCompletionMetrics();

            if (result.PromptEvalCount.HasValue)
                metrics.PromptTokens = result.PromptEvalCount.Value;

            if (result.EvalCount.HasValue)
                metrics.CompletionTokens = result.EvalCount.Value;

            var promptTps = result.GetPromptTokensPerSecond();
            if (promptTps.HasValue)
                metrics.PromptTokensPerSecond = promptTps.Value;

            var genTps = result.GetGenerationTokensPerSecond();
            if (genTps.HasValue)
                metrics.GenerationTokensPerSecond = genTps.Value;

            var totalMs = result.GetTotalDurationMilliseconds();
            if (totalMs.HasValue)
                metrics.TotalDurationMs = totalMs.Value;

            var loadMs = result.GetLoadDurationMilliseconds();
            if (loadMs.HasValue)
                metrics.LoadDurationMs = loadMs.Value;

            if (result.PromptEvalDuration.HasValue)
                metrics.PromptEvalDurationMs = result.PromptEvalDuration.Value / 1_000_000.0;

            if (result.EvalDuration.HasValue)
                metrics.GenerationDurationMs = result.EvalDuration.Value / 1_000_000.0;

            return metrics;
        }

        /// <summary>
        /// Ollama chat completion metrics.
        /// </summary>
        public OllamaChatCompletionMetrics()
        {
        }
    }
}