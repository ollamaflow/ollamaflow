namespace OllamaFlow.Core.Models.Agnostic.Common
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a message in a conversation.
    /// </summary>
    public class AgnosticMessage
    {
        /// <summary>
        /// The role of the message author.
        /// Valid values: "system", "user", "assistant"
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// The content of the message.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Optional name for the message author.
        /// Used for multi-participant conversations.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Extension data for API-specific message features.
        /// </summary>
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
    }
}