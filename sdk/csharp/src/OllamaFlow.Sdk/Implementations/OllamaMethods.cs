namespace OllamaFlow.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using RestWrapper;
    using OllamaFlow.Core.Models.Ollama;
    using OllamaFlow.Sdk.Interfaces;

    /// <summary>
    /// Implementation of Ollama API methods.
    /// </summary>
    public class OllamaMethods : IOllamaMethods
    {
        private readonly OllamaFlowSdk _Sdk;

        /// <summary>
        /// Initializes a new instance of the OllamaMethods class.
        /// </summary>
        /// <param name="sdk">The OllamaFlowSdk instance to use for API calls.</param>
        /// <exception cref="ArgumentNullException">Thrown when sdk is null.</exception>
        public OllamaMethods(OllamaFlowSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<OllamaPullModelResultMessage> PullModel(OllamaPullModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string url = _Sdk.Endpoint + "/api/pull";

            string json = JsonSerializer.Serialize(request);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            using (RestRequest req = new RestRequest(url, HttpMethod.Post))
            {
                req.TimeoutMilliseconds = _Sdk.TimeoutMs;
                req.ContentType = "application/json";

                if (_Sdk.LogRequests)
                    _Sdk.Log("DEBUG", $"POST request to {url} with {jsonBytes.Length} bytes");

                using (RestResponse resp = await req.SendAsync(jsonBytes, cancellationToken).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            if (resp.ChunkedTransferEncoding)
                            {
                                ChunkData? chunk = null;
                                while ((chunk = await resp.ReadChunkAsync(cancellationToken).ConfigureAwait(false)) != null)
                                {
                                    if (chunk.Data != null && chunk.Data.Length > 0)
                                    {
                                        string chunkText = Encoding.UTF8.GetString(chunk.Data);
                                        var lines = chunkText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                                        foreach (var line in lines)
                                        {
                                            if (string.IsNullOrWhiteSpace(line)) continue;
                                            OllamaPullModelResultMessage? pullResult = null;
                                            try { pullResult = JsonSerializer.Deserialize<OllamaPullModelResultMessage>(line); }
                                            catch { continue; }
                                            if (pullResult != null)
                                            {
                                                yield return pullResult;
                                                if (pullResult.Status == "success")
                                                    yield break;
                                            }
                                        }
                                    }
                                    if (chunk.IsFinal) break;
                                }
                            }
                            else
                            {
                                string responseData = resp.DataAsString;
                                if (!string.IsNullOrEmpty(responseData))
                                {
                                    OllamaPullModelResultMessage? result = null;
                                    try { result = JsonSerializer.Deserialize<OllamaPullModelResultMessage>(responseData); }
                                    catch { }
                                    if (result != null)
                                        yield return result;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<object?> DeleteModel(OllamaDeleteModelRequest request, CancellationToken cancellationToken = default)
        {
            string url = _Sdk.Endpoint + "/api/delete";
            return await _Sdk.DeleteAsync<object>(url, request, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<List<OllamaLocalModel>?> ListLocalModels(CancellationToken cancellationToken = default)
        {
            string url = _Sdk.Endpoint + "/api/tags";
            string? jsonResponse = await _Sdk.GetRawResponse(url, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(jsonResponse)) return new List<OllamaLocalModel>();
            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<List<OllamaLocalModel>>(jsonResponse);
                else if (doc.RootElement.TryGetProperty("models", out JsonElement modelsElement))
                    return JsonSerializer.Deserialize<List<OllamaLocalModel>>(modelsElement.GetRawText());
            }
            return new List<OllamaLocalModel>();
        }

        /// <inheritdoc/>
        public async Task<List<OllamaRunningModel>?> ListRunningModels(CancellationToken cancellationToken = default)
        {
            string url = _Sdk.Endpoint + "/api/ps";
            string? jsonResponse = await _Sdk.GetRawResponse(url, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(jsonResponse)) return new List<OllamaRunningModel>();
            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<List<OllamaRunningModel>>(jsonResponse);
                else if (doc.RootElement.TryGetProperty("models", out JsonElement modelsElement))
                    return JsonSerializer.Deserialize<List<OllamaRunningModel>>(modelsElement.GetRawText());
            }
            return new List<OllamaRunningModel>();
        }

        /// <inheritdoc/>
        public async Task<OllamaShowModelInfoResult?> ShowModelInfo(OllamaShowModelInfoRequest request, CancellationToken cancellationToken = default)
        {
            string url = _Sdk.Endpoint + "/api/show";
            return await _Sdk.PostAsync<OllamaShowModelInfoResult>(url, request, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<OllamaGenerateEmbeddingsResult?> GenerateEmbeddings(OllamaGenerateEmbeddingsRequest request, CancellationToken cancellationToken = default)
        {
            string url = _Sdk.Endpoint + "/api/embed";
            return await _Sdk.PostAsync<OllamaGenerateEmbeddingsResult>(url, request, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<OllamaGenerateCompletionResult?> GenerateCompletion(OllamaGenerateCompletion request, CancellationToken cancellationToken = default)
        {
            request.Stream = false;
            string url = _Sdk.Endpoint + "/api/generate";
            return await _Sdk.PostAsync<OllamaGenerateCompletionResult>(url, request, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<OllamaGenerateChatCompletionResult?> GenerateChatCompletion(OllamaGenerateChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            request.Stream = false;
            string url = _Sdk.Endpoint + "/api/chat";
            return await _Sdk.PostAsync<OllamaGenerateChatCompletionResult>(url, request, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<OllamaGenerateChatCompletionChunk> GenerateChatCompletionStream(OllamaGenerateChatCompletionRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            request.Stream = true;
            string url = _Sdk.Endpoint + "/api/chat";
            await foreach (var streamingResult in _Sdk.PostStreamAsync<OllamaStreamingChatCompletionResult>(url, request, cancellationToken))
            {
                var result = new OllamaGenerateChatCompletionChunk
                {
                    Model = streamingResult.Model,
                    CreatedAt = streamingResult.CreatedAt,
                    Message = streamingResult.Message,
                    Done = streamingResult.Done
                };
                yield return result;
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<OllamaStreamingCompletionResult> GenerateCompletionStream(OllamaGenerateCompletion request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            request.Stream = true;
            string url = _Sdk.Endpoint + "/api/generate";
            await foreach (var result in _Sdk.PostStreamAsync<OllamaStreamingCompletionResult>(url, request, cancellationToken))
            {
                yield return result;
            }
        }
    }
}


