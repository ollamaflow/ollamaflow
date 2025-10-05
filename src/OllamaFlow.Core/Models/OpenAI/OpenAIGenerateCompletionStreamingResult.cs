namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI generate completion streaming result wrapper.
    /// This wrapper is returned immediately and contains an async enumerable stream of chunks.
    /// </summary>
    public class OpenAIGenerateCompletionStreamingResult
    {
        /// <summary>
        /// The async enumerable stream of completion chunks.
        /// Yields OpenAICompletionChunk objects as they arrive from the server.
        /// </summary>
        public IAsyncEnumerable<OpenAICompletionChunk> Chunks { get; set; }

        /// <summary>
        /// OpenAI generate completion streaming result.
        /// </summary>
        public OpenAIGenerateCompletionStreamingResult()
        {
        }
    }
}
