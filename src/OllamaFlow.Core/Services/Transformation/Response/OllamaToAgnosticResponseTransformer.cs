namespace OllamaFlow.Core.Services.Transformation.Response
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models.Agnostic.Common;
    using OllamaFlow.Core.Models.Agnostic.Responses;
    using OllamaFlow.Core.Services.Transformation.Interfaces;

    /// <summary>
    /// Transforms Ollama API responses to agnostic format.
    /// </summary>
    public class OllamaToAgnosticResponseTransformer : IResponseTransformer
    {
        /// <summary>
        /// Determines if this transformer can handle the given API format and response type.
        /// </summary>
        /// <param name="apiFormat">The API format of the response.</param>
        /// <param name="responseType">The type of response object.</param>
        /// <returns>True if this transformer can handle the transformation.</returns>
        public bool CanTransform(ApiFormatEnum apiFormat, System.Type responseType)
        {
            return apiFormat == ApiFormatEnum.Ollama;
        }

        /// <summary>
        /// Transform an Ollama response to agnostic format.
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

                if (root.TryGetProperty("message", out _))
                {
                    return Task.FromResult<AgnosticResponse>(ParseChatResponse(root));
                }
                else if (root.TryGetProperty("response", out _))
                {
                    return Task.FromResult<AgnosticResponse>(ParseCompletionResponse(root));
                }
                else if (root.TryGetProperty("embedding", out _))
                {
                    return Task.FromResult<AgnosticResponse>(ParseEmbeddingResponse(root));
                }
                else if (root.TryGetProperty("models", out _))
                {
                    return Task.FromResult<AgnosticResponse>(ParseModelListResponse(root));
                }
                else if (root.TryGetProperty("modelfile", out _) || root.TryGetProperty("parameters", out _) || root.TryGetProperty("template", out _))
                {
                    return Task.FromResult<AgnosticResponse>(ParseModelInfoResponse(root));
                }

                throw new TransformationException(
                    "Unable to determine Ollama response type",
                    ApiFormatEnum.Ollama,
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
                    $"Failed to transform Ollama response: {ex.Message}",
                    ApiFormatEnum.Ollama,
                    ApiFormatEnum.Ollama,
                    "ResponseTransformation",
                    backendResponse,
                    ex);
            }
        }

        /// <summary>
        /// Transform an agnostic response to Ollama format.
        /// </summary>
        /// <param name="agnosticResponse">The agnostic response to transform.</param>
        /// <param name="targetFormat">The target API format.</param>
        /// <returns>Ollama-format response object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public Task<object> TransformFromAgnosticAsync(AgnosticResponse agnosticResponse, ApiFormatEnum targetFormat)
        {
            throw new NotSupportedException("Use AgnosticToOllamaResponseTransformer for transforming to Ollama format");
        }

        private AgnosticChatResponse ParseChatResponse(JsonElement root)
        {
            string model = root.TryGetProperty("model", out JsonElement modelEl) ? modelEl.GetString() : "";
            string createdAt = root.TryGetProperty("created_at", out JsonElement createdAtEl) ? createdAtEl.GetString() : "";
            bool done = root.TryGetProperty("done", out JsonElement doneEl) ? doneEl.GetBoolean() : true;

            AgnosticChatResponse response = new AgnosticChatResponse
            {
                Id = Guid.NewGuid().ToString(),
                Object = "chat.completion",
                Created = ParseOllamaTimestamp(createdAt),
                Model = model,
                Done = done,
                SourceFormat = ApiFormatEnum.Ollama
            };

            if (root.TryGetProperty("message", out JsonElement message))
            {
                AgnosticChoice choice = new AgnosticChoice
                {
                    Index = 0,
                    Message = new AgnosticMessage
                    {
                        Role = message.TryGetProperty("role", out JsonElement role) ? role.GetString() : "assistant",
                        Content = message.TryGetProperty("content", out JsonElement content) ? content.GetString() : ""
                    },
                    FinishReason = done ? "stop" : null
                };
                response.Choices.Add(choice);
            }

            if (root.TryGetProperty("prompt_eval_count", out JsonElement promptEvalCount) ||
                root.TryGetProperty("eval_count", out JsonElement evalCount))
            {
                response.Usage = new AgnosticUsage
                {
                    PromptTokens = root.TryGetProperty("prompt_eval_count", out JsonElement prompt) ? prompt.GetInt32() : 0,
                    CompletionTokens = root.TryGetProperty("eval_count", out JsonElement completion) ? completion.GetInt32() : 0,
                    TotalTokens = (root.TryGetProperty("prompt_eval_count", out JsonElement p) ? p.GetInt32() : 0) +
                                  (root.TryGetProperty("eval_count", out JsonElement c) ? c.GetInt32() : 0)
                };
            }

            return response;
        }

        private AgnosticCompletionResponse ParseCompletionResponse(JsonElement root)
        {
            string model = root.TryGetProperty("model", out JsonElement modelEl) ? modelEl.GetString() : "";
            string createdAt = root.TryGetProperty("created_at", out JsonElement createdAtEl) ? createdAtEl.GetString() : "";
            string responseText = root.TryGetProperty("response", out JsonElement responseEl) ? responseEl.GetString() : "";
            bool done = root.TryGetProperty("done", out JsonElement doneEl) ? doneEl.GetBoolean() : true;

            AgnosticCompletionResponse response = new AgnosticCompletionResponse
            {
                Id = Guid.NewGuid().ToString(),
                Object = "text_completion",
                Created = ParseOllamaTimestamp(createdAt),
                Model = model,
                Done = done,
                SourceFormat = ApiFormatEnum.Ollama
            };

            AgnosticChoice choice = new AgnosticChoice
            {
                Index = 0,
                Text = responseText,
                FinishReason = done ? "stop" : null
            };
            response.Choices.Add(choice);

            if (root.TryGetProperty("prompt_eval_count", out JsonElement promptEvalCount) ||
                root.TryGetProperty("eval_count", out JsonElement evalCount))
            {
                response.Usage = new AgnosticUsage
                {
                    PromptTokens = root.TryGetProperty("prompt_eval_count", out JsonElement prompt) ? prompt.GetInt32() : 0,
                    CompletionTokens = root.TryGetProperty("eval_count", out JsonElement completion) ? completion.GetInt32() : 0,
                    TotalTokens = (root.TryGetProperty("prompt_eval_count", out JsonElement p) ? p.GetInt32() : 0) +
                                  (root.TryGetProperty("eval_count", out JsonElement c) ? c.GetInt32() : 0)
                };
            }

            return response;
        }

        private AgnosticEmbeddingResponse ParseEmbeddingResponse(JsonElement root)
        {
            AgnosticEmbeddingResponse response = new AgnosticEmbeddingResponse
            {
                Object = "list",
                Model = "",
                SourceFormat = ApiFormatEnum.Ollama
            };

            if (root.TryGetProperty("embedding", out JsonElement embedding) && embedding.ValueKind == JsonValueKind.Array)
            {
                List<double> values = new List<double>();
                foreach (JsonElement val in embedding.EnumerateArray())
                {
                    values.Add(val.GetDouble());
                }

                response.Embedding = values;
                response.Data.Add(new AgnosticEmbeddingData
                {
                    Index = 0,
                    Embedding = values
                });
            }

            return response;
        }

        private AgnosticModelListResponse ParseModelListResponse(JsonElement root)
        {
            AgnosticModelListResponse response = new AgnosticModelListResponse
            {
                Object = "list",
                SourceFormat = ApiFormatEnum.Ollama
            };

            if (root.TryGetProperty("models", out JsonElement models) && models.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in models.EnumerateArray())
                {
                    string name = item.TryGetProperty("name", out JsonElement nameEl) ? nameEl.GetString() : "";
                    string modifiedAt = item.TryGetProperty("modified_at", out JsonElement modifiedAtEl) ? modifiedAtEl.GetString() : "";

                    AgnosticModel agnosticModel = new AgnosticModel
                    {
                        Id = name,
                        Name = name,
                        ModifiedAt = ParseOllamaTimestampToDateTime(modifiedAt)
                    };

                    response.Data.Add(agnosticModel);
                    response.Models.Add(agnosticModel);
                }
            }

            return response;
        }

        private AgnosticModelInfoResponse ParseModelInfoResponse(JsonElement root)
        {
            AgnosticModelInfoResponse response = new AgnosticModelInfoResponse
            {
                SourceFormat = ApiFormatEnum.Ollama,
                Parameters = root.TryGetProperty("parameters", out JsonElement parameters) ? parameters.GetString() : "",
                Template = root.TryGetProperty("template", out JsonElement template) ? template.GetString() : ""
            };

            return response;
        }

        private long ParseOllamaTimestamp(string timestamp)
        {
            if (string.IsNullOrEmpty(timestamp))
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (DateTimeOffset.TryParse(timestamp, out DateTimeOffset dto))
                return dto.ToUnixTimeSeconds();

            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private DateTime? ParseOllamaTimestampToDateTime(string timestamp)
        {
            if (string.IsNullOrEmpty(timestamp))
                return null;

            if (DateTimeOffset.TryParse(timestamp, out DateTimeOffset dto))
                return dto.DateTime;

            return null;
        }
    }
}