namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Extension of OllamaChatMessage for response-specific properties.
    /// </summary>
    public class OllamaChatResponseMessage : OllamaChatMessage
    {
        /// <summary>
        /// Tool calls made by the assistant (if any).
        /// Inherited from OllamaChatMessage.
        /// </summary>

        /// <summary>
        /// Checks if this is a tool call response.
        /// </summary>
        /// <returns>True if the message contains tool calls.</returns>
        public bool IsToolCallResponse()
        {
            return ToolCalls != null && ToolCalls.Count > 0;
        }

        /// <summary>
        /// Gets the first tool call if available.
        /// </summary>
        /// <returns>First tool call or null if none.</returns>
        public OllamaToolCall GetFirstToolCall()
        {
            if (ToolCalls != null && ToolCalls.Count > 0)
            {
                return ToolCalls[0];
            }
            return null;
        }

        /// <summary>
        /// Ollama chat response message.
        /// </summary>
        public OllamaChatResponseMessage()
        {
        }
    }
}