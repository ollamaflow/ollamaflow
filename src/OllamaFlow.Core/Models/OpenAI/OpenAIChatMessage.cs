namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI chat message.
    /// </summary>
    public class OpenAIChatMessage
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
        /// Content of the message.
        /// Can be a string or an array of content parts for multimodal input.
        /// </summary>
        [JsonPropertyName("content")]
        public object Content { get; set; } = null;

        /// <summary>
        /// Name of the author of this message.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// Tool calls made by the assistant.
        /// </summary>
        [JsonPropertyName("tool_calls")]
        public List<OpenAIToolCall> ToolCalls { get; set; } = null;

        /// <summary>
        /// Tool call ID this message is responding to.
        /// </summary>
        [JsonPropertyName("tool_call_id")]
        public string ToolCallId { get; set; } = null;

        private string _Role;

        /// <summary>
        /// OpenAI chat message.
        /// </summary>
        public OpenAIChatMessage()
        {
        }
    }
}