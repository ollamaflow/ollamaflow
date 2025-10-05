namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama generate chat completion streaming result wrapper.
    /// This wrapper is returned immediately and contains an async enumerable stream of chunks.
    /// </summary>
    public class OllamaGenerateChatCompletionStreamingResult
    {
        /// <summary>
        /// The async enumerable stream of chat completion chunks.
        /// Yields OllamaGenerateChatCompletionChunk objects as they arrive from the server.
        /// </summary>
        public IAsyncEnumerable<OllamaGenerateChatCompletionChunk> Chunks { get; set; }

        /// <summary>
        /// Ollama generate chat completion streaming result.
        /// </summary>
        public OllamaGenerateChatCompletionStreamingResult()
        {
        }
    }
}
