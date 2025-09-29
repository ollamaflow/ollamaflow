namespace OllamaFlow.Core.Models.Agnostic.Common
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a completion choice in a response.
    /// </summary>
    public class AgnosticChoice
    {
        /// <summary>
        /// Index of this choice in the list of choices.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The message content for this choice.
        /// </summary>
        public AgnosticMessage Message { get; set; }

        /// <summary>
        /// The text content for this choice (for non-chat completions).
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The reason the completion finished.
        /// Valid values: "stop", "length", "content_filter", null
        /// </summary>
        public string FinishReason { get; set; }

        /// <summary>
        /// Log probabilities for the tokens in this choice.
        /// </summary>
        public object LogProbs { get; set; }

        /// <summary>
        /// Extension data for API-specific choice features.
        /// </summary>
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
    }
}