namespace OllamaFlow.Core.Models.Agnostic.Responses
{
    using System.Collections.Generic;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models.Agnostic.Common;

    /// <summary>
    /// Agnostic chat completion response.
    /// </summary>
    public class AgnosticChatResponse : AgnosticResponse
    {
        /// <summary>
        /// Unique identifier for the completion.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Object type, typically "chat.completion".
        /// </summary>
        public string Object { get; set; } = "chat.completion";

        /// <summary>
        /// Unix timestamp when the completion was created.
        /// </summary>
        public long Created { get; set; }

        /// <summary>
        /// The model used for the completion.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// The completion choices.
        /// </summary>
        public List<AgnosticChoice> Choices { get; set; } = new List<AgnosticChoice>();

        /// <summary>
        /// Usage statistics for the completion.
        /// </summary>
        public AgnosticUsage Usage { get; set; }

        /// <summary>
        /// System fingerprint for the completion.
        /// </summary>
        public string SystemFingerprint { get; set; }

        /// <summary>
        /// Whether the response is complete.
        /// </summary>
        public bool Done { get; set; } = true;
    }
}