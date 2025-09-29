namespace OllamaFlow.Core.Models.Agnostic.Requests
{
    using System.Collections.Generic;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models.Agnostic.Common;

    /// <summary>
    /// Agnostic chat completion request.
    /// </summary>
    public class AgnosticChatRequest : AgnosticRequest
    {
        /// <summary>
        /// The model to use for the completion.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// The messages to generate a completion for.
        /// </summary>
        public List<AgnosticMessage> Messages { get; set; } = new List<AgnosticMessage>();

        /// <summary>
        /// Sampling temperature between 0 and 2.
        /// Higher values make output more random, lower values more focused.
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