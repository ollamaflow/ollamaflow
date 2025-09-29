namespace OllamaFlow.Core.Models.Agnostic.Responses
{
    using System;
    using System.Collections.Generic;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models.Agnostic.Common;

    /// <summary>
    /// Agnostic streaming chat completion response.
    /// </summary>
    public class AgnosticStreamingChatResponse : AgnosticResponse
    {
        /// <summary>
        /// Unique identifier for this streaming chunk.
        /// </summary>
        public string ChunkId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The model that generated this response chunk.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Choices for the streaming response chunk.
        /// </summary>
        public List<AgnosticStreamingChoice> Choices { get; set; } = new List<AgnosticStreamingChoice>();

        /// <summary>
        /// Usage information for the complete response (only in final chunk).
        /// </summary>
        public AgnosticUsage Usage { get; set; }

        /// <summary>
        /// Indicates if this is the final chunk in the stream.
        /// </summary>
        public bool IsFinal { get; set; } = false;

        /// <summary>
        /// Indicates if the response generation is complete.
        /// </summary>
        public bool Done { get; set; } = false;

        /// <summary>
        /// Error information if the streaming failed.
        /// </summary>
        public string Error { get; set; }
    }
}