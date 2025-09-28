namespace OllamaFlow.Core.Models.Agnostic.Requests
{
    using System.Collections.Generic;
    using OllamaFlow.Core.Models.Agnostic.Base;

    /// <summary>
    /// Agnostic completion request.
    /// </summary>
    public class AgnosticCompletionRequest : AgnosticRequest
    {
        /// <summary>
        /// The model to use for the completion.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// The prompt to generate a completion for.
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// Sampling temperature between 0 and 2.
        /// </summary>
        public double? Temperature { get; set; }

        /// <summary>
        /// Maximum number of tokens to generate.
        /// </summary>
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Whether to stream the response.
        /// </summary>
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Sequences where the API will stop generating further tokens.
        /// </summary>
        public string[] Stop { get; set; }

        /// <summary>
        /// Nucleus sampling parameter between 0 and 1.
        /// </summary>
        public double? TopP { get; set; }

        /// <summary>
        /// Number of completions to generate.
        /// </summary>
        public int? N { get; set; }

        /// <summary>
        /// Frequency penalty between -2.0 and 2.0.
        /// </summary>
        public double? FrequencyPenalty { get; set; }

        /// <summary>
        /// Presence penalty between -2.0 and 2.0.
        /// </summary>
        public double? PresencePenalty { get; set; }

        /// <summary>
        /// Seed for deterministic generation.
        /// </summary>
        public int? Seed { get; set; }

        /// <summary>
        /// System prompt or context.
        /// </summary>
        public string System { get; set; }

        /// <summary>
        /// Template to use for formatting the prompt.
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Additional options for model-specific parameters.
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();
    }
}