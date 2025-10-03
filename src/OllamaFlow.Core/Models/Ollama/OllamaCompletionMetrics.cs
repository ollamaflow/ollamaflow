namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Performance metrics for completion generation.
    /// </summary>
    public class OllamaCompletionMetrics
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
        /// Size of the context maintained.
        /// </summary>
        public int ContextSize { get; set; }

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
        /// Creates metrics from a completion result.
        /// </summary>
        /// <param name="result">The completion result.</param>
        /// <returns>Metrics object or null if insufficient data.</returns>
        public static OllamaCompletionMetrics FromResult(OllamaGenerateCompletionResult result)
        {
            if (result == null)
                return null;

            var metrics = new OllamaCompletionMetrics();

            if (result.PromptEvalCount.HasValue)
                metrics.PromptTokens = result.PromptEvalCount.Value;

            if (result.EvalCount.HasValue)
                metrics.CompletionTokens = result.EvalCount.Value;

            metrics.ContextSize = result.GetContextSize();

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

            var promptMs = result.GetPromptEvalDurationMilliseconds();
            if (promptMs.HasValue)
                metrics.PromptEvalDurationMs = promptMs.Value;

            var evalMs = result.GetEvalDurationMilliseconds();
            if (evalMs.HasValue)
                metrics.GenerationDurationMs = evalMs.Value;

            return metrics;
        }

        /// <summary>
        /// Ollama completion metrics.
        /// </summary>
        public OllamaCompletionMetrics()
        {
        }
    }
}