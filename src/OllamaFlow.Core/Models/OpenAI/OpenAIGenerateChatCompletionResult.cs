namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI generate chat completion result.
    /// </summary>
    public class OpenAIGenerateChatCompletionResult
    {
        /// <summary>
        /// Unique identifier for the chat completion.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Object type (always "chat.completion").
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; }

        /// <summary>
        /// Unix timestamp when the completion was created.
        /// </summary>
        [JsonPropertyName("created")]
        public long? Created { get; set; }

        /// <summary>
        /// Model used for the completion.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// System fingerprint.
        /// </summary>
        [JsonPropertyName("system_fingerprint")]
        public string SystemFingerprint { get; set; }

        /// <summary>
        /// List of chat completion choices.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<OpenAIChatChoice> Choices { get; set; }

        /// <summary>
        /// Usage statistics for the completion.
        /// </summary>
        [JsonPropertyName("usage")]
        public OpenAIUsage Usage { get; set; }

        /// <summary>
        /// Gets the created timestamp as DateTime.
        /// </summary>
        public DateTime? GetCreatedDateTime()
        {
            if (!Created.HasValue)
                return null;
            return DateTimeOffset.FromUnixTimeSeconds(Created.Value).DateTime;
        }

        /// <summary>
        /// Gets the primary message from the assistant.
        /// </summary>
        public OpenAIChatMessage GetAssistantMessage()
        {
            return Choices?.FirstOrDefault()?.Message;
        }

        /// <summary>
        /// Checks if any choice contains tool calls.
        /// </summary>
        public bool HasToolCalls()
        {
            return Choices?.Any(c => c.Message?.ToolCalls != null && c.Message.ToolCalls.Count > 0) ?? false;
        }

        /// <summary>
        /// Gets all tool calls from all choices.
        /// </summary>
        public List<OpenAIToolCall> GetAllToolCalls()
        {
            return Choices?
                .Where(c => c.Message?.ToolCalls != null)
                .SelectMany(c => c.Message.ToolCalls)
                .ToList() ?? new List<OpenAIToolCall>();
        }

        /// <summary>
        /// OpenAI generate chat completion result.
        /// </summary>
        public OpenAIGenerateChatCompletionResult()
        {
        }
    }
}