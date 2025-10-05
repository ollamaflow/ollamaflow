namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI generate chat completion streaming result wrapper.
    /// This wrapper is returned immediately and contains an async enumerable stream of chunks.
    /// </summary>
    public class OpenAIGenerateChatCompletionStreamingResult
    {
        /// <summary>
        /// The async enumerable stream of chat completion chunks.
        /// Yields OpenAIChatCompletionChunk objects as they arrive from the server.
        /// </summary>
        public IAsyncEnumerable<OpenAIChatCompletionChunk> Chunks { get; set; }

        /// <summary>
        /// OpenAI generate chat completion streaming result.
        /// </summary>
        public OpenAIGenerateChatCompletionStreamingResult()
        {
        }
    }
}
