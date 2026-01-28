namespace OllamaFlow.Sdk.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core.Models.Ollama;

    /// <summary>
    /// Interface for Ollama API methods.
    /// </summary>
    public interface IOllamaMethods
    {
        /// <summary>
        /// Pulls a model from the Ollama registry.
        /// </summary>
        /// <param name="request">The pull model request containing model name and options.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An async enumerable that yields pull model result messages.</returns>
        IAsyncEnumerable<OllamaPullModelResultMessage> PullModel(OllamaPullModelRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a model from the local Ollama installation.
        /// </summary>
        /// <param name="request">The delete model request containing the model name.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the delete response.</returns>
        Task<object?> DeleteModel(OllamaDeleteModelRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all locally available models.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of local models.</returns>
        Task<List<OllamaLocalModel>?> ListLocalModels(CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all currently running models.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of running models.</returns>
        Task<List<OllamaRunningModel>?> ListRunningModels(CancellationToken cancellationToken = default);

        /// <summary>
        /// Shows detailed information about a specific model.
        /// </summary>
        /// <param name="request">The show model info request containing the model name.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the model information.</returns>
        Task<OllamaShowModelInfoResult?> ShowModelInfo(OllamaShowModelInfoRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates embeddings for the provided input text.
        /// </summary>
        /// <param name="request">The embeddings request containing the model and input text.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated embeddings.</returns>
        Task<OllamaGenerateEmbeddingsResult?> GenerateEmbeddings(OllamaGenerateEmbeddingsRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a text completion using the specified model.
        /// </summary>
        /// <param name="request">The completion request containing the model and prompt.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated completion.</returns>
        Task<OllamaGenerateCompletionResult?> GenerateCompletion(OllamaGenerateCompletion request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a chat completion using the specified model and messages.
        /// </summary>
        /// <param name="request">The chat completion request containing the model and messages.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated chat completion.</returns>
        Task<OllamaGenerateChatCompletionResult?> GenerateChatCompletion(OllamaGenerateChatCompletionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a streaming chat completion using the specified model and messages.
        /// </summary>
        /// <param name="request">The chat completion request containing the model and messages.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An async enumerable that yields chat completion chunks as they are generated.</returns>
        IAsyncEnumerable<OllamaGenerateChatCompletionChunk> GenerateChatCompletionStream(OllamaGenerateChatCompletionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a streaming text completion using the specified model.
        /// </summary>
        /// <param name="request">The completion request containing the model and prompt.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An async enumerable that yields completion chunks as they are generated.</returns>
        IAsyncEnumerable<OllamaStreamingCompletionResult> GenerateCompletionStream(OllamaGenerateCompletion request, CancellationToken cancellationToken = default);
    }
}


