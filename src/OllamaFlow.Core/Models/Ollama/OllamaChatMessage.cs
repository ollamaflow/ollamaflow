namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama chat message.
    /// </summary>
    public class OllamaChatMessage
    {
        /// <summary>
        /// Role of the message sender (required).
        /// Valid values: "system", "user", "assistant", "tool"
        /// </summary>
        [JsonPropertyName("role")]
        public string Role
        {
            get => _Role;
            set
            {
                if (value != null && value != "system" && value != "user" && value != "assistant" && value != "tool")
                    throw new ArgumentException("Role must be 'system', 'user', 'assistant', or 'tool'", nameof(Role));
                _Role = value;
            }
        }

        /// <summary>
        /// Content of the message (required).
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = null;

        /// <summary>
        /// Base64-encoded images for multimodal models (optional).
        /// Only valid for user messages.
        /// </summary>
        [JsonPropertyName("images")]
        public List<string> Images
        {
            get => _Images;
            set
            {
                if (value != null && _Role != null && _Role != "user")
                    throw new ArgumentException("Images can only be included in 'user' messages", nameof(Images));
                _Images = value;
            }
        }

        /// <summary>
        /// Tool calls made by the assistant (optional).
        /// Only valid for assistant messages.
        /// </summary>
        [JsonPropertyName("tool_calls")]
        public List<OllamaToolCall> ToolCalls
        {
            get => _ToolCalls;
            set
            {
                if (value != null && _Role != null && _Role != "assistant")
                    throw new ArgumentException("ToolCalls can only be included in 'assistant' messages", nameof(ToolCalls));
                _ToolCalls = value;
            }
        }

        // Private backing fields
        private string _Role;
        private List<string> _Images;
        private List<OllamaToolCall> _ToolCalls;

        /// <summary>
        /// Ollama chat message.
        /// </summary>
        public OllamaChatMessage()
        {
        }
    }
}