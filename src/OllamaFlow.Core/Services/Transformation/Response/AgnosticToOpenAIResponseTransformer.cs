namespace OllamaFlow.Core.Services.Transformation.Response
{
    using System;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models.Agnostic.Responses;
    using OllamaFlow.Core.Services.Transformation.Interfaces;

    /// <summary>
    /// Transforms agnostic responses to OpenAI API format.
    /// </summary>
    public class AgnosticToOpenAIResponseTransformer : IResponseTransformer
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
        /// Transform a backend response to agnostic format.
        /// </summary>
        /// <param name="backendResponse">The backend response object.</param>
        /// <param name="sourceFormat">The API format of the backend response.</param>
        /// <returns>Agnostic response object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public Task<AgnosticResponse> TransformToAgnosticAsync(object backendResponse, ApiFormatEnum sourceFormat)
        {
            throw new NotSupportedException("Use OpenAIToAgnosticResponseTransformer for transforming from OpenAI format");
        }

        /// <summary>
        /// Transform an agnostic response to OpenAI format.
        /// </summary>
        /// <param name="agnosticResponse">The agnostic response to transform.</param>
        /// <param name="targetFormat">The target API format for the client.</param>
        /// <returns>OpenAI-format response object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public Task<object> TransformFromAgnosticAsync(AgnosticResponse agnosticResponse, ApiFormatEnum targetFormat)
        {
            try
            {
                object result = agnosticResponse switch
                {
                    AgnosticCompletionResponse completion => TransformCompletionResponse(completion),
                    AgnosticChatResponse chat => TransformChatResponse(chat),
                    AgnosticEmbeddingResponse embedding => TransformEmbeddingResponse(embedding),
                    AgnosticModelListResponse modelList => TransformModelListResponse(modelList),
                    AgnosticModelInfoResponse modelInfo => TransformModelInfoResponse(modelInfo),
                    _ => throw new TransformationException(
                        $"Unsupported agnostic response type: {agnosticResponse.GetType().Name}",
                        ApiFormatEnum.OpenAI,
                        targetFormat,
                        "ResponseTypeValidation")
                };

                return Task.FromResult(result);
            }
            catch (TransformationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new TransformationException(
                    $"Failed to transform agnostic response to OpenAI format: {ex.Message}",
                    ApiFormatEnum.OpenAI,
                    targetFormat,
                    "ResponseTransformation",
                    agnosticResponse,
                    ex);
            }
        }

        private object TransformCompletionResponse(AgnosticCompletionResponse agnosticResponse)
        {
            OpenAICompletionResponse openAIResponse = new OpenAICompletionResponse
            {
                Id = agnosticResponse.Id ?? "cmpl-" + Guid.NewGuid().ToString(),
                Object = "text_completion",
                Created = agnosticResponse.Created,
                Model = agnosticResponse.Model,
                Choices = new System.Collections.Generic.List<OpenAICompletionChoice>()
            };

            if (agnosticResponse.Choices != null)
            {
                foreach (var choice in agnosticResponse.Choices)
                {
                    openAIResponse.Choices.Add(new OpenAICompletionChoice
                    {
                        Index = choice.Index,
                        Text = choice.Text ?? "",
                        FinishReason = choice.FinishReason
                    });
                }
            }

            if (agnosticResponse.Usage != null)
            {
                openAIResponse.Usage = new OpenAIUsage
                {
                    PromptTokens = agnosticResponse.Usage.PromptTokens ?? 0,
                    CompletionTokens = agnosticResponse.Usage.CompletionTokens ?? 0,
                    TotalTokens = agnosticResponse.Usage.TotalTokens ?? 0
                };
            }

            return openAIResponse;
        }

        private object TransformChatResponse(AgnosticChatResponse agnosticResponse)
        {
            OpenAIChatResponse openAIResponse = new OpenAIChatResponse
            {
                Id = agnosticResponse.Id ?? "chatcmpl-" + Guid.NewGuid().ToString(),
                Object = "chat.completion",
                Created = agnosticResponse.Created,
                Model = agnosticResponse.Model,
                Choices = new System.Collections.Generic.List<OpenAIChatChoice>()
            };

            if (agnosticResponse.Choices != null)
            {
                foreach (var choice in agnosticResponse.Choices)
                {
                    OpenAIChatMessage message = null;
                    if (choice.Message != null)
                    {
                        message = new OpenAIChatMessage
                        {
                            Role = choice.Message.Role ?? "assistant",
                            Content = choice.Message.Content ?? "",
                            Name = choice.Message.Name
                        };
                    }

                    openAIResponse.Choices.Add(new OpenAIChatChoice
                    {
                        Index = choice.Index,
                        Message = message,
                        FinishReason = choice.FinishReason
                    });
                }
            }

            if (agnosticResponse.Usage != null)
            {
                openAIResponse.Usage = new OpenAIUsage
                {
                    PromptTokens = agnosticResponse.Usage.PromptTokens ?? 0,
                    CompletionTokens = agnosticResponse.Usage.CompletionTokens ?? 0,
                    TotalTokens = agnosticResponse.Usage.TotalTokens ?? 0
                };
            }

            return openAIResponse;
        }

        private object TransformEmbeddingResponse(AgnosticEmbeddingResponse agnosticResponse)
        {
            OpenAIEmbeddingResponse openAIResponse = new OpenAIEmbeddingResponse
            {
                Object = "list",
                Model = agnosticResponse.Model,
                Data = new System.Collections.Generic.List<OpenAIEmbeddingData>()
            };

            if (agnosticResponse.Data != null)
            {
                foreach (var item in agnosticResponse.Data)
                {
                    openAIResponse.Data.Add(new OpenAIEmbeddingData
                    {
                        Index = item.Index,
                        Object = "embedding",
                        Embedding = item.Embedding.ToArray()
                    });
                }
            }

            if (agnosticResponse.Usage != null)
            {
                openAIResponse.Usage = new OpenAIUsage
                {
                    PromptTokens = agnosticResponse.Usage.PromptTokens ?? 0,
                    TotalTokens = agnosticResponse.Usage.TotalTokens ?? 0
                };
            }

            return openAIResponse;
        }

        private object TransformModelListResponse(AgnosticModelListResponse agnosticResponse)
        {
            OpenAIModelListResponse openAIResponse = new OpenAIModelListResponse
            {
                Object = "list",
                Data = new System.Collections.Generic.List<OpenAIModel>()
            };

            if (agnosticResponse.Models != null)
            {
                foreach (var model in agnosticResponse.Models)
                {
                    long created = 0;
                    if (model.CreatedAt.HasValue)
                        created = new DateTimeOffset(model.CreatedAt.Value).ToUnixTimeSeconds();
                    else if (model.ModifiedAt.HasValue)
                        created = new DateTimeOffset(model.ModifiedAt.Value).ToUnixTimeSeconds();

                    openAIResponse.Data.Add(new OpenAIModel
                    {
                        Id = model.Id,
                        Object = "model",
                        Created = created,
                        OwnedBy = "organization-owner"
                    });
                }
            }

            return openAIResponse;
        }

        private object TransformModelInfoResponse(AgnosticModelInfoResponse agnosticResponse)
        {
            throw new TransformationException(
                "Model info transformation to OpenAI format is not supported (OpenAI does not have equivalent endpoint)",
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.OpenAI,
                "UnsupportedOperation");
        }
    }

    internal class OpenAICompletionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("choices")]
        public System.Collections.Generic.List<OpenAICompletionChoice> Choices { get; set; }

        [JsonPropertyName("usage")]
        public OpenAIUsage Usage { get; set; }
    }

    internal class OpenAICompletionChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }

    internal class OpenAIChatResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("choices")]
        public System.Collections.Generic.List<OpenAIChatChoice> Choices { get; set; }

        [JsonPropertyName("usage")]
        public OpenAIUsage Usage { get; set; }
    }

    internal class OpenAIChatChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public OpenAIChatMessage Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }

    internal class OpenAIChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    internal class OpenAIUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    internal class OpenAIEmbeddingResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("data")]
        public System.Collections.Generic.List<OpenAIEmbeddingData> Data { get; set; }

        [JsonPropertyName("usage")]
        public OpenAIUsage Usage { get; set; }
    }

    internal class OpenAIEmbeddingData
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("embedding")]
        public double[] Embedding { get; set; }
    }

    internal class OpenAIModelListResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("data")]
        public System.Collections.Generic.List<OpenAIModel> Data { get; set; }
    }

    internal class OpenAIModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("owned_by")]
        public string OwnedBy { get; set; }
    }
}