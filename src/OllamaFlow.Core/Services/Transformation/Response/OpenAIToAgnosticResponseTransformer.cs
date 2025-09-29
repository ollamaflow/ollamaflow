namespace OllamaFlow.Core.Services.Transformation.Response
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models.Agnostic.Common;
    using OllamaFlow.Core.Models.Agnostic.Responses;
    using OllamaFlow.Core.Services.Transformation.Interfaces;

    /// <summary>
    /// Transforms OpenAI API responses to agnostic format.
    /// </summary>
    public class OpenAIToAgnosticResponseTransformer : IResponseTransformer
    {
        /// <summary>
        /// Determines if this transformer can handle the given API format and response type.
        /// </summary>
        /// <param name="apiFormat">The API format of the response.</param>
        /// <param name="responseType">The type of response object.</param>
        /// <returns>True if this transformer can handle the transformation.</returns>
        public bool CanTransform(ApiFormatEnum apiFormat, System.Type responseType)
        {
            return apiFormat == ApiFormatEnum.OpenAI;
        }

        /// <summary>
        /// Transform an OpenAI response to agnostic format.
        /// </summary>
        /// <param name="backendResponse">The backend response object (JSON string or bytes).</param>
        /// <param name="sourceFormat">The API format of the backend response.</param>
        /// <returns>Agnostic response object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public Task<AgnosticResponse> TransformToAgnosticAsync(object backendResponse, ApiFormatEnum sourceFormat)
        {
            try
            {
                string json = backendResponse switch
                {
                    string str => str,
                    byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
                    _ => backendResponse?.ToString() ?? ""
                };

                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;

                // Detect response type
                if (root.TryGetProperty("object", out JsonElement objType))
                {
                    string objectType = objType.GetString();
                    if (objectType == "chat.completion")
                    {
                        return Task.FromResult<AgnosticResponse>(ParseChatCompletionResponse(root));
                    }
                    else if (objectType == "text_completion")
                    {
                        return Task.FromResult<AgnosticResponse>(ParseCompletionResponse(root));
                    }
                    else if (objectType == "list" && root.TryGetProperty("data", out _))
                    {
                        // Could be embeddings or model list
                        if (root.TryGetProperty("model", out _))
                            return Task.FromResult<AgnosticResponse>(ParseEmbeddingResponse(root));
                        else
                            return Task.FromResult<AgnosticResponse>(ParseModelListResponse(root));
                    }
                }

                throw new TransformationException(
                    "Unable to determine OpenAI response type",
                    ApiFormatEnum.OpenAI,
                    ApiFormatEnum.Ollama,
                    "ResponseTypeDetection",
                    backendResponse);
            }
            catch (TransformationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new TransformationException(
                    $"Failed to transform OpenAI response: {ex.Message}",
                    ApiFormatEnum.OpenAI,
                    ApiFormatEnum.Ollama,
                    "ResponseTransformation",
                    backendResponse,
                    ex);
            }
        }

        /// <summary>
        /// Transform an agnostic response to OpenAI format.
        /// </summary>
        /// <param name="agnosticResponse">The agnostic response to transform.</param>
        /// <param name="targetFormat">The target API format.</param>
        /// <returns>OpenAI-format response object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public Task<object> TransformFromAgnosticAsync(AgnosticResponse agnosticResponse, ApiFormatEnum targetFormat)
        {
            // This class handles OpenAI → Agnostic, not Agnostic → OpenAI
            throw new NotSupportedException("Use AgnosticToOpenAIResponseTransformer for transforming to OpenAI format");
        }

        private AgnosticCompletionResponse ParseCompletionResponse(JsonElement root)
        {
            AgnosticCompletionResponse response = new AgnosticCompletionResponse
            {
                Id = root.TryGetProperty("id", out JsonElement id) ? id.GetString() : Guid.NewGuid().ToString(),
                Object = root.TryGetProperty("object", out JsonElement obj) ? obj.GetString() : "text_completion",
                Created = root.TryGetProperty("created", out JsonElement created) ? created.GetInt64() : DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = root.TryGetProperty("model", out JsonElement model) ? model.GetString() : "",
                SourceFormat = ApiFormatEnum.OpenAI
            };

            if (root.TryGetProperty("choices", out JsonElement choices) && choices.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement choice in choices.EnumerateArray())
                {
                    AgnosticChoice agnosticChoice = new AgnosticChoice
                    {
                        Index = choice.TryGetProperty("index", out JsonElement index) ? index.GetInt32() : 0,
                        Text = choice.TryGetProperty("text", out JsonElement text) ? text.GetString() : "",
                        FinishReason = choice.TryGetProperty("finish_reason", out JsonElement finishReason) ? finishReason.GetString() : null
                    };

                    response.Choices.Add(agnosticChoice);
                }
            }

            if (root.TryGetProperty("usage", out JsonElement usage))
            {
                response.Usage = new AgnosticUsage
                {
                    PromptTokens = usage.TryGetProperty("prompt_tokens", out JsonElement prompt) ? prompt.GetInt32() : 0,
                    CompletionTokens = usage.TryGetProperty("completion_tokens", out JsonElement completion) ? completion.GetInt32() : 0,
                    TotalTokens = usage.TryGetProperty("total_tokens", out JsonElement total) ? total.GetInt32() : 0
                };
            }

            // Set response field for Ollama compatibility
            if (response.Choices.Count > 0)
            {
                response.Response = response.Choices[0].Text;
            }

            return response;
        }

        private AgnosticChatResponse ParseChatCompletionResponse(JsonElement root)
        {
            AgnosticChatResponse response = new AgnosticChatResponse
            {
                Id = root.TryGetProperty("id", out JsonElement id) ? id.GetString() : Guid.NewGuid().ToString(),
                Object = root.TryGetProperty("object", out JsonElement obj) ? obj.GetString() : "chat.completion",
                Created = root.TryGetProperty("created", out JsonElement created) ? created.GetInt64() : DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = root.TryGetProperty("model", out JsonElement model) ? model.GetString() : "",
                SourceFormat = ApiFormatEnum.OpenAI
            };

            if (root.TryGetProperty("choices", out JsonElement choices) && choices.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement choice in choices.EnumerateArray())
                {
                    AgnosticChoice agnosticChoice = new AgnosticChoice
                    {
                        Index = choice.TryGetProperty("index", out JsonElement index) ? index.GetInt32() : 0,
                        FinishReason = choice.TryGetProperty("finish_reason", out JsonElement finishReason) ? finishReason.GetString() : null
                    };

                    if (choice.TryGetProperty("message", out JsonElement message))
                    {
                        agnosticChoice.Message = new AgnosticMessage
                        {
                            Role = message.TryGetProperty("role", out JsonElement role) ? role.GetString() : "assistant",
                            Content = message.TryGetProperty("content", out JsonElement content) ? content.GetString() : "",
                            Name = message.TryGetProperty("name", out JsonElement name) ? name.GetString() : null
                        };
                    }

                    response.Choices.Add(agnosticChoice);
                }
            }

            if (root.TryGetProperty("usage", out JsonElement usage))
            {
                response.Usage = new AgnosticUsage
                {
                    PromptTokens = usage.TryGetProperty("prompt_tokens", out JsonElement prompt) ? prompt.GetInt32() : 0,
                    CompletionTokens = usage.TryGetProperty("completion_tokens", out JsonElement completion) ? completion.GetInt32() : 0,
                    TotalTokens = usage.TryGetProperty("total_tokens", out JsonElement total) ? total.GetInt32() : 0
                };
            }

            return response;
        }

        private AgnosticEmbeddingResponse ParseEmbeddingResponse(JsonElement root)
        {
            AgnosticEmbeddingResponse response = new AgnosticEmbeddingResponse
            {
                Object = root.TryGetProperty("object", out JsonElement obj) ? obj.GetString() : "list",
                Model = root.TryGetProperty("model", out JsonElement model) ? model.GetString() : "",
                SourceFormat = ApiFormatEnum.OpenAI
            };

            if (root.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (JsonElement item in data.EnumerateArray())
                {
                    if (item.TryGetProperty("embedding", out JsonElement embedding) && embedding.ValueKind == JsonValueKind.Array)
                    {
                        List<double> values = new List<double>();
                        foreach (JsonElement val in embedding.EnumerateArray())
                        {
                            values.Add(val.GetDouble());
                        }

                        response.Data.Add(new AgnosticEmbeddingData
                        {
                            Index = index++,
                            Embedding = values
                        });
                    }
                }

                // Also set the first embedding as the single Embedding property for compatibility
                if (response.Data.Count > 0)
                {
                    response.Embedding = response.Data[0].Embedding;
                }
            }

            if (root.TryGetProperty("usage", out JsonElement usage))
            {
                response.Usage = new AgnosticUsage
                {
                    PromptTokens = usage.TryGetProperty("prompt_tokens", out JsonElement prompt) ? prompt.GetInt32() : 0,
                    TotalTokens = usage.TryGetProperty("total_tokens", out JsonElement total) ? total.GetInt32() : 0
                };
            }

            return response;
        }

        private AgnosticModelListResponse ParseModelListResponse(JsonElement root)
        {
            AgnosticModelListResponse response = new AgnosticModelListResponse
            {
                Object = root.TryGetProperty("object", out JsonElement obj) ? obj.GetString() : "list",
                SourceFormat = ApiFormatEnum.OpenAI
            };

            if (root.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in data.EnumerateArray())
                {
                    long createdTimestamp = item.TryGetProperty("created", out JsonElement created) ? created.GetInt64() : 0;

                    AgnosticModel agnosticModel = new AgnosticModel
                    {
                        Id = item.TryGetProperty("id", out JsonElement id) ? id.GetString() : "",
                        Name = item.TryGetProperty("id", out JsonElement nameId) ? nameId.GetString() : "",
                        CreatedAt = createdTimestamp > 0 ? DateTimeOffset.FromUnixTimeSeconds(createdTimestamp).DateTime : (DateTime?)null
                    };

                    // Add to both Data and Models for compatibility
                    response.Data.Add(agnosticModel);
                    response.Models.Add(agnosticModel);
                }
            }

            return response;
        }
    }
}