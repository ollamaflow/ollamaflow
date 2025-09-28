namespace OllamaFlow.Core.Models.Agnostic.Common
{
    /// <summary>
    /// Token usage information for a completion.
    /// </summary>
    public class AgnosticUsage
    {
        /// <summary>
        /// Number of tokens in the prompt.
        /// </summary>
        public int? PromptTokens { get; set; }

        /// <summary>
        /// Number of tokens in the completion.
        /// </summary>
        public int? CompletionTokens { get; set; }

        /// <summary>
        /// Total number of tokens used.
        /// </summary>
        public int? TotalTokens { get; set; }

        /// <summary>
        /// Time taken to evaluate the prompt (milliseconds).
        /// </summary>
        public double? PromptEvalDuration { get; set; }

        /// <summary>
        /// Time taken to generate the completion (milliseconds).
        /// </summary>
        public double? CompletionDuration { get; set; }

        /// <summary>
        /// Total time taken for the request (milliseconds).
        /// </summary>
        public double? TotalDuration { get; set; }

        /// <summary>
        /// Number of evaluations per second for prompt.
        /// </summary>
        public double? PromptEvalRate { get; set; }

        /// <summary>
        /// Number of evaluations per second for completion.
        /// </summary>
        public double? CompletionRate { get; set; }
    }
}