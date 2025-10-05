namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama generate completion streaming result wrapper.
    /// This wrapper is returned immediately and contains an async enumerable stream of chunks.
    /// </summary>
    public class OllamaGenerateCompletionStreamingResult
    {
        /// <summary>
        /// The async enumerable stream of completion chunks.
        /// Yields OllamaGenerateCompletionChunk objects as they arrive from the server.
        /// </summary>
        public IAsyncEnumerable<OllamaGenerateCompletionChunk> Chunks { get; set; }

        /// <summary>
        /// Ollama generate completion streaming result.
        /// </summary>
        public OllamaGenerateCompletionStreamingResult()
        {
        }
    }
}
