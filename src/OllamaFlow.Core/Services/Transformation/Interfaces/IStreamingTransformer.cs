namespace OllamaFlow.Core.Services.Transformation.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models.Agnostic.Base;

    /// <summary>
    /// Interface for transforming streaming responses between API formats.
    /// </summary>
    public interface IStreamingTransformer
    {
        /// <summary>
        /// Determines if this transformer can handle streaming transformations between the specified formats.
        /// </summary>
        /// <param name="sourceFormat">Source API format.</param>
        /// <param name="targetFormat">Target API format.</param>
        /// <param name="requestType">Type of request being streamed.</param>
        /// <returns>True if transformation is supported.</returns>
        bool CanTransformStream(ApiFormatEnum sourceFormat, ApiFormatEnum targetFormat, RequestTypeEnum requestType);

        /// <summary>
        /// Transform a streaming response chunk from source format to target format via agnostic format.
        /// </summary>
        /// <param name="sourceChunk">Source format streaming chunk (as string or bytes).</param>
        /// <param name="sourceFormat">Source API format.</param>
        /// <param name="targetFormat">Target API format.</param>
        /// <param name="requestType">Type of request being streamed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Transformed chunk ready for target format output.</returns>
        Task<StreamingChunkResult> TransformChunkAsync(
            object sourceChunk,
            ApiFormatEnum sourceFormat,
            ApiFormatEnum targetFormat,
            RequestTypeEnum requestType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a final chunk to signal end of stream in the target format.
        /// </summary>
        /// <param name="targetFormat">Target API format.</param>
        /// <param name="requestType">Type of request being streamed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Final chunk for target format.</returns>
        Task<StreamingChunkResult> CreateFinalChunkAsync(
            ApiFormatEnum targetFormat,
            RequestTypeEnum requestType,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of a streaming chunk transformation.
    /// </summary>
    public class StreamingChunkResult
    {
        /// <summary>
        /// The transformed chunk data.
        /// </summary>
        public byte[] ChunkData { get; set; }

        /// <summary>
        /// Content type for the chunk.
        /// </summary>
        public string ContentType { get; set; } = "application/json";

        /// <summary>
        /// Indicates if this chunk should be sent using Server-Sent Events format.
        /// </summary>
        public bool IsServerSentEvent { get; set; } = false;

        /// <summary>
        /// SSE event name (only relevant if IsServerSentEvent is true).
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Indicates if this is the final chunk in the stream.
        /// </summary>
        public bool IsFinal { get; set; } = false;

        /// <summary>
        /// Error information if transformation failed.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Agnostic response object for logging/debugging.
        /// </summary>
        public AgnosticResponse AgnosticResponse { get; set; }
    }
}