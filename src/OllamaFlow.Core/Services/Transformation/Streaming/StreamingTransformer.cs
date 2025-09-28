namespace OllamaFlow.Core.Services.Transformation.Streaming
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Core.Services.Transformation.Interfaces;
    using OllamaFlow.Core.Models.Agnostic.Responses;
    using OllamaFlow.Core.Models.Agnostic.Common;

    /// <summary>
    /// Universal streaming transformer that handles bidirectional streaming transformations
    /// between OpenAI and Ollama API formats via agnostic format.
    /// </summary>
    public class StreamingTransformer : IStreamingTransformer
    {
        #region Private-Members

        private readonly Serializer _Serializer;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="serializer">Serializer.</param>
        public StreamingTransformer(Serializer serializer = null)
        {
            _Serializer = serializer ?? new Serializer();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Determines if this transformer can handle streaming transformations between the specified formats.
        /// </summary>
        /// <param name="sourceFormat">Source API format.</param>
        /// <param name="targetFormat">Target API format.</param>
        /// <param name="requestType">Type of request being streamed.</param>
        /// <returns>True if transformation is supported.</returns>
        public bool CanTransformStream(ApiFormatEnum sourceFormat, ApiFormatEnum targetFormat, RequestTypeEnum requestType)
        {
            // Currently support chat completion streaming for both formats
            if (requestType != RequestTypeEnum.GenerateChatCompletion && requestType != RequestTypeEnum.GenerateCompletion)
                return false;

            return (sourceFormat == ApiFormatEnum.OpenAI && targetFormat == ApiFormatEnum.Ollama) ||
                   (sourceFormat == ApiFormatEnum.Ollama && targetFormat == ApiFormatEnum.OpenAI) ||
                   (sourceFormat == targetFormat); // Pass-through
        }

        /// <summary>
        /// Transform a streaming response chunk from source format to target format via agnostic format.
        /// </summary>
        /// <param name="sourceChunk">Source format streaming chunk (as string or bytes).</param>
        /// <param name="sourceFormat">Source API format.</param>
        /// <param name="targetFormat">Target API format.</param>
        /// <param name="requestType">Type of request being streamed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Transformed chunk ready for target format output.</returns>
        public async Task<StreamingChunkResult> TransformChunkAsync(
            object sourceChunk,
            ApiFormatEnum sourceFormat,
            ApiFormatEnum targetFormat,
            RequestTypeEnum requestType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // If formats match, pass through
                if (sourceFormat == targetFormat)
                {
                    return CreatePassThroughChunk(sourceChunk, targetFormat);
                }

                // Convert source chunk to string
                string chunkText = sourceChunk switch
                {
                    string str => str,
                    byte[] bytes => Encoding.UTF8.GetString(bytes),
                    _ => sourceChunk?.ToString() ?? ""
                };

                if (string.IsNullOrWhiteSpace(chunkText))
                {
                    return CreateEmptyChunk(targetFormat);
                }

                // Step 1: Parse source format chunk to agnostic format
                AgnosticStreamingChatResponse agnosticChunk = await ParseToAgnosticAsync(chunkText, sourceFormat, requestType);

                // Step 2: Transform agnostic format to target format
                return await TransformFromAgnosticAsync(agnosticChunk, targetFormat, requestType);
            }
            catch (Exception ex)
            {
                return new StreamingChunkResult
                {
                    Error = $"Streaming transformation failed: {ex.Message}",
                    ChunkData = Encoding.UTF8.GetBytes(""),
                    IsFinal = true
                };
            }
        }

        /// <summary>
        /// Create a final chunk to signal end of stream in the target format.
        /// </summary>
        /// <param name="targetFormat">Target API format.</param>
        /// <param name="requestType">Type of request being streamed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Final chunk for target format.</returns>
        public async Task<StreamingChunkResult> CreateFinalChunkAsync(
            ApiFormatEnum targetFormat,
            RequestTypeEnum requestType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                AgnosticStreamingChatResponse finalChunk = new AgnosticStreamingChatResponse
                {
                    IsFinal = true,
                    Done = true,
                    Choices = new List<AgnosticStreamingChoice>
                    {
                        new AgnosticStreamingChoice
                        {
                            Index = 0,
                            Delta = new AgnosticMessageDelta(),
                            FinishReason = "stop"
                        }
                    }
                };

                return await TransformFromAgnosticAsync(finalChunk, targetFormat, requestType);
            }
            catch (Exception ex)
            {
                return new StreamingChunkResult
                {
                    Error = $"Failed to create final chunk: {ex.Message}",
                    ChunkData = Encoding.UTF8.GetBytes(""),
                    IsFinal = true
                };
            }
        }

        #endregion

        #region Private-Methods

        private async Task<AgnosticStreamingChatResponse> ParseToAgnosticAsync(
            string chunkText,
            ApiFormatEnum sourceFormat,
            RequestTypeEnum requestType)
        {
            return sourceFormat switch
            {
                ApiFormatEnum.OpenAI => await ParseOpenAIChunkAsync(chunkText, requestType),
                ApiFormatEnum.Ollama => await ParseOllamaChunkAsync(chunkText, requestType),
                _ => throw new NotSupportedException($"Source format {sourceFormat} not supported for streaming")
            };
        }

        private async Task<StreamingChunkResult> TransformFromAgnosticAsync(
            AgnosticStreamingChatResponse agnosticChunk,
            ApiFormatEnum targetFormat,
            RequestTypeEnum requestType)
        {
            return targetFormat switch
            {
                ApiFormatEnum.OpenAI => await TransformToOpenAIAsync(agnosticChunk, requestType),
                ApiFormatEnum.Ollama => await TransformToOllamaAsync(agnosticChunk, requestType),
                _ => throw new NotSupportedException($"Target format {targetFormat} not supported for streaming")
            };
        }

        private Task<AgnosticStreamingChatResponse> ParseOpenAIChunkAsync(string chunkText, RequestTypeEnum requestType)
        {
            // OpenAI streaming format: "data: {json}\n\n" or "data: [DONE]\n\n"
            if (chunkText.Contains("[DONE]"))
            {
                AgnosticStreamingChatResponse result = new AgnosticStreamingChatResponse
                {
                    IsFinal = true,
                    Done = true
                };
                return Task.FromResult(result);
            }

            // Remove "data: " prefix and parse JSON
            string jsonData = chunkText.Trim();
            if (jsonData.StartsWith("data: "))
                jsonData = jsonData.Substring(6).Trim();

            try
            {
                using JsonDocument document = JsonDocument.Parse(jsonData);
                JsonElement root = document.RootElement;

                AgnosticStreamingChatResponse agnosticResponse = new AgnosticStreamingChatResponse
                {
                    ChunkId = root.TryGetProperty("id", out var id) ? id.GetString() : Guid.NewGuid().ToString(),
                    Model = root.TryGetProperty("model", out var model) ? model.GetString() : "",
                    SourceFormat = ApiFormatEnum.OpenAI
                };

                if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement choice in choices.EnumerateArray())
                    {
                        AgnosticStreamingChoice agnosticChoice = new AgnosticStreamingChoice
                        {
                            Index = choice.TryGetProperty("index", out var index) ? index.GetInt32() : 0
                        };

                        // Handle both chat completions (delta) and regular completions (text)
                        if (choice.TryGetProperty("delta", out var delta))
                        {
                            // Chat completion format
                            string content = "";
                            if (delta.TryGetProperty("content", out var contentEl) && contentEl.ValueKind != JsonValueKind.Null)
                            {
                                content = contentEl.GetString() ?? "";
                            }

                            agnosticChoice.Delta = new AgnosticMessageDelta
                            {
                                Role = delta.TryGetProperty("role", out var role) && role.ValueKind != JsonValueKind.Null ? role.GetString() : null,
                                Content = content,
                                Name = delta.TryGetProperty("name", out var name) && name.ValueKind != JsonValueKind.Null ? name.GetString() : null
                            };
                        }
                        else if (choice.TryGetProperty("text", out var text))
                        {
                            // Regular completion format (OpenAI /v1/completions)
                            agnosticChoice.Delta = new AgnosticMessageDelta
                            {
                                Role = "assistant",
                                Content = text.ValueKind != JsonValueKind.Null ? (text.GetString() ?? "") : ""
                            };
                        }

                        if (choice.TryGetProperty("finish_reason", out var finishReason) && finishReason.ValueKind != JsonValueKind.Null)
                        {
                            agnosticChoice.FinishReason = finishReason.GetString();
                            agnosticResponse.Done = !string.IsNullOrEmpty(agnosticChoice.FinishReason);
                        }

                        agnosticResponse.Choices.Add(agnosticChoice);
                    }
                }

                return Task.FromResult(agnosticResponse);
            }
            catch (JsonException)
            {
                // Handle malformed JSON by creating empty response
                AgnosticStreamingChatResponse errorResult = new AgnosticStreamingChatResponse
                {
                    Error = "Invalid JSON in streaming chunk"
                };
                return Task.FromResult(errorResult);
            }
        }

        private Task<AgnosticStreamingChatResponse> ParseOllamaChunkAsync(string chunkText, RequestTypeEnum requestType)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(chunkText);
                JsonElement root = document.RootElement;

                AgnosticStreamingChatResponse agnosticResponse = new AgnosticStreamingChatResponse
                {
                    Model = root.TryGetProperty("model", out var model) ? model.GetString() : "",
                    Done = root.TryGetProperty("done", out var done) ? done.GetBoolean() : false,
                    SourceFormat = ApiFormatEnum.Ollama
                };

                // Handle Ollama's message structure
                if (root.TryGetProperty("message", out JsonElement message))
                {
                    string content = message.TryGetProperty("content", out JsonElement messageContent) ? messageContent.GetString() : "";
                    string role = message.TryGetProperty("role", out JsonElement messageRole) ? messageRole.GetString() : "assistant";

                    agnosticResponse.Choices.Add(new AgnosticStreamingChoice
                    {
                        Index = 0,
                        Delta = new AgnosticMessageDelta
                        {
                            Role = role,
                            Content = content
                        },
                        FinishReason = agnosticResponse.Done ? "stop" : null
                    });
                }

                agnosticResponse.IsFinal = agnosticResponse.Done;
                return Task.FromResult(agnosticResponse);
            }
            catch (JsonException)
            {
                AgnosticStreamingChatResponse errorResult = new AgnosticStreamingChatResponse
                {
                    Error = "Invalid JSON in Ollama streaming chunk"
                };
                return Task.FromResult(errorResult);
            }
        }

        private Task<StreamingChunkResult> TransformToOpenAIAsync(AgnosticStreamingChatResponse agnosticChunk, RequestTypeEnum requestType)
        {
            if (!string.IsNullOrEmpty(agnosticChunk.Error))
            {
                StreamingChunkResult errorResult = new StreamingChunkResult
                {
                    Error = agnosticChunk.Error,
                    ChunkData = Encoding.UTF8.GetBytes(""),
                    IsFinal = true
                };
                return Task.FromResult(errorResult);
            }

            if (agnosticChunk.IsFinal || agnosticChunk.Done)
            {
                string doneData = "data: [DONE]\n\n";
                StreamingChunkResult doneResult = new StreamingChunkResult
                {
                    ChunkData = Encoding.UTF8.GetBytes(doneData),
                    ContentType = "text/plain",
                    IsFinal = true,
                    AgnosticResponse = agnosticChunk
                };
                return Task.FromResult(doneResult);
            }

            var openAIChunk = new
            {
                id = agnosticChunk.ChunkId,
                @object = "chat.completion.chunk",
                created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                model = agnosticChunk.Model,
                choices = agnosticChunk.Choices.Select(choice => new
                {
                    index = choice.Index,
                    delta = new
                    {
                        role = choice.Delta?.Role,
                        content = choice.Delta?.Content
                    },
                    finish_reason = choice.FinishReason
                }).ToArray()
            };

            string jsonString = _Serializer.SerializeJson(openAIChunk, false);
            string sseData = $"data: {jsonString}\n\n";

            StreamingChunkResult result = new StreamingChunkResult
            {
                ChunkData = Encoding.UTF8.GetBytes(sseData),
                ContentType = "text/plain",
                IsServerSentEvent = true,
                IsFinal = false,
                AgnosticResponse = agnosticChunk
            };
            return Task.FromResult(result);
        }

        private Task<StreamingChunkResult> TransformToOllamaAsync(AgnosticStreamingChatResponse agnosticChunk, RequestTypeEnum requestType)
        {
            if (!string.IsNullOrEmpty(agnosticChunk.Error))
            {
                StreamingChunkResult errorResult = new StreamingChunkResult
                {
                    Error = agnosticChunk.Error,
                    ChunkData = Encoding.UTF8.GetBytes(""),
                    IsFinal = true
                };
                return Task.FromResult(errorResult);
            }

            string content = agnosticChunk.Choices.FirstOrDefault()?.Delta?.Content ?? "";
            bool isDone = agnosticChunk.Done || agnosticChunk.IsFinal;

            // Skip chunks with no content and not done (heartbeat/metadata chunks)
            if (string.IsNullOrEmpty(content) && !isDone)
            {
                return Task.FromResult(new StreamingChunkResult
                {
                    ChunkData = Array.Empty<byte>(),
                    ContentType = "application/x-ndjson",
                    IsFinal = false,
                    AgnosticResponse = agnosticChunk
                });
            }

            object ollamaChunk;

            if (requestType == RequestTypeEnum.GenerateCompletion)
            {
                // Ollama /api/generate format - uses "response" field
                ollamaChunk = new
                {
                    model = agnosticChunk.Model,
                    created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    response = content,
                    done = isDone
                };
            }
            else
            {
                // Ollama /api/chat format - uses "message" object
                ollamaChunk = new
                {
                    model = agnosticChunk.Model,
                    created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    message = agnosticChunk.Choices.FirstOrDefault()?.Delta != null ? new
                    {
                        role = agnosticChunk.Choices.First().Delta.Role ?? "assistant",
                        content = content
                    } : null,
                    done = isDone
                };
            }

            string jsonString = _Serializer.SerializeJson(ollamaChunk, false);

            StreamingChunkResult result = new StreamingChunkResult
            {
                ChunkData = Encoding.UTF8.GetBytes(jsonString + "\n"),
                ContentType = "application/x-ndjson",
                IsFinal = isDone,
                AgnosticResponse = agnosticChunk
            };
            return Task.FromResult(result);
        }

        private StreamingChunkResult CreatePassThroughChunk(object sourceChunk, ApiFormatEnum format)
        {
            byte[] chunkBytes = sourceChunk switch
            {
                byte[] bytes => bytes,
                string str => Encoding.UTF8.GetBytes(str),
                _ => Encoding.UTF8.GetBytes(sourceChunk?.ToString() ?? "")
            };

            return new StreamingChunkResult
            {
                ChunkData = chunkBytes,
                ContentType = format == ApiFormatEnum.OpenAI ? "text/plain" : "application/x-ndjson",
                IsServerSentEvent = format == ApiFormatEnum.OpenAI
            };
        }

        private StreamingChunkResult CreateEmptyChunk(ApiFormatEnum format)
        {
            return new StreamingChunkResult
            {
                ChunkData = Array.Empty<byte>(),
                ContentType = format == ApiFormatEnum.OpenAI ? "text/plain" : "application/x-ndjson",
                IsServerSentEvent = format == ApiFormatEnum.OpenAI
            };
        }

        #endregion
    }
}