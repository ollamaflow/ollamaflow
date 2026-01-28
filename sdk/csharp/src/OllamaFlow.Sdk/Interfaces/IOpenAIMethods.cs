namespace OllamaFlow.Sdk.Interfaces
{
    using OllamaFlow.Core.Models.OpenAI;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for OpenAI-compatible API methods.
    /// </summary>
    public interface IOpenAIMethods
    {
        /// <summary>
        /// Generate text completion using OpenAI-compatible API.
        /// </summary>
        /// <param name="request">Completion request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion result.</returns>
        Task<OpenAIGenerateCompletionResult?> GenerateCompletion(OpenAIGenerateCompletionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate chat completion using OpenAI-compatible API.
        /// </summary>
        /// <param name="request">Chat completion request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Chat completion result.</returns>
        Task<OpenAIGenerateChatCompletionResult?> GenerateChatCompletion(OpenAIGenerateChatCompletionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate embeddings using OpenAI-compatible API.
        /// </summary>
        /// <param name="request">Embeddings request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Embeddings result.</returns>
        Task<OpenAIGenerateEmbeddingsResult?> GenerateEmbeddings(OpenAIGenerateEmbeddingsRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate streaming text completion using OpenAI-compatible API.
        /// </summary>
        /// <param name="request">Completion request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async enumerable of streaming completion chunks.</returns>
        IAsyncEnumerable<OpenAIStreamingCompletionResult> GenerateCompletionStream(OpenAIGenerateCompletionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate streaming chat completion using OpenAI-compatible API.
        /// </summary>
        /// <param name="request">Chat completion request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async enumerable of streaming chat completion chunks.</returns>
        IAsyncEnumerable<OpenAIStreamingChatCompletionResult> GenerateChatCompletionStream(OpenAIGenerateChatCompletionRequest request, CancellationToken cancellationToken = default);
    }
}
