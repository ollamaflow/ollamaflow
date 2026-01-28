namespace OllamaFlow.Sdk.Implementations
{
    using OllamaFlow.Core.Models.OpenAI;
    using OllamaFlow.Sdk.Interfaces;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of OpenAI-compatible API methods.
    /// </summary>
    public class OpenAIMethods : IOpenAIMethods
    {
        private readonly OllamaFlowSdk _Sdk;

        /// <summary>
        /// Initialize the OpenAIMethods.
        /// </summary>
        /// <param name="sdk">OllamaFlowSdk instance.</param>
        public OpenAIMethods(OllamaFlowSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        /// <inheritdoc/>
        public async Task<OpenAIGenerateCompletionResult?> GenerateCompletion(OpenAIGenerateCompletionRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.Stream = false;
            string url = _Sdk.Endpoint + "/v1/completions";
            return await _Sdk.PostAsync<OpenAIGenerateCompletionResult>(url, request, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<OpenAIGenerateChatCompletionResult?> GenerateChatCompletion(OpenAIGenerateChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            request.Stream = false;
            string url = _Sdk.Endpoint + "/v1/chat/completions";
            return await _Sdk.PostAsync<OpenAIGenerateChatCompletionResult>(url, request, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<OpenAIGenerateEmbeddingsResult?> GenerateEmbeddings(OpenAIGenerateEmbeddingsRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            string url = _Sdk.Endpoint + "/v1/embeddings";
            return await _Sdk.PostAsync<OpenAIGenerateEmbeddingsResult>(url, request, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<OpenAIStreamingCompletionResult> GenerateCompletionStream(OpenAIGenerateCompletionRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.Stream = true;

            string url = _Sdk.Endpoint + "/v1/completions";
            await foreach (var chunk in _Sdk.PostStreamAsync<OpenAIStreamingCompletionResult>(url, request, cancellationToken))
            {
                if (chunk != null)
                    yield return chunk;
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<OpenAIStreamingChatCompletionResult> GenerateChatCompletionStream(OpenAIGenerateChatCompletionRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.Stream = true;

            string url = _Sdk.Endpoint + "/v1/chat/completions";
            await foreach (var chunk in _Sdk.PostStreamAsync<OpenAIStreamingChatCompletionResult>(url, request, cancellationToken))
            {
                if (chunk != null)
                    yield return chunk;
            }
        }
    }
}
