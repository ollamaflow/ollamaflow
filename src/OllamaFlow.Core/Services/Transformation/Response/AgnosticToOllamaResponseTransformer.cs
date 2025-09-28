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
    /// Transforms agnostic responses to Ollama API format.
    /// </summary>
    public class AgnosticToOllamaResponseTransformer : IResponseTransformer
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
        /// Transform a backend response to agnostic format.
        /// </summary>
        /// <param name="backendResponse">The backend response object.</param>
        /// <param name="sourceFormat">The API format of the backend response.</param>
        /// <returns>Agnostic response object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public Task<AgnosticResponse> TransformToAgnosticAsync(object backendResponse, ApiFormatEnum sourceFormat)
        {
            // This class handles Agnostic → Ollama, not Ollama → Agnostic
            throw new NotSupportedException("Use OllamaToAgnosticResponseTransformer for transforming from Ollama format");
        }

        /// <summary>
        /// Transform an agnostic response to Ollama format.
        /// </summary>
        /// <param name="agnosticResponse">The agnostic response to transform.</param>
        /// <param name="targetFormat">The target API format for the client.</param>
        /// <returns>Ollama-format response object.</returns>
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
                        ApiFormatEnum.Ollama,
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
                    $"Failed to transform agnostic response to Ollama format: {ex.Message}",
                    ApiFormatEnum.Ollama,
                    targetFormat,
                    "ResponseTransformation",
                    agnosticResponse,
                    ex);
            }
        }

        private object TransformCompletionResponse(AgnosticCompletionResponse agnosticResponse)
        {
            string responseText = "";
            if (agnosticResponse.Choices != null && agnosticResponse.Choices.Count > 0)
            {
                responseText = agnosticResponse.Choices[0].Text ?? "";
            }

            OllamaCompletionResponse ollamaResponse = new OllamaCompletionResponse
            {
                Model = agnosticResponse.Model,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(agnosticResponse.Created).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Response = responseText,
                Done = agnosticResponse.Done
            };

            if (agnosticResponse.Usage != null)
            {
                ollamaResponse.PromptEvalCount = agnosticResponse.Usage.PromptTokens ?? 0;
                ollamaResponse.EvalCount = agnosticResponse.Usage.CompletionTokens ?? 0;
            }

            return ollamaResponse;
        }

        private object TransformChatResponse(AgnosticChatResponse agnosticResponse)
        {
            OllamaChatMessage message = null;
            if (agnosticResponse.Choices != null && agnosticResponse.Choices.Count > 0 && agnosticResponse.Choices[0].Message != null)
            {
                message = new OllamaChatMessage
                {
                    Role = agnosticResponse.Choices[0].Message.Role ?? "assistant",
                    Content = agnosticResponse.Choices[0].Message.Content ?? ""
                };
            }

            OllamaChatResponse ollamaResponse = new OllamaChatResponse
            {
                Model = agnosticResponse.Model,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(agnosticResponse.Created).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Message = message,
                Done = agnosticResponse.Done
            };

            if (agnosticResponse.Usage != null)
            {
                ollamaResponse.PromptEvalCount = agnosticResponse.Usage.PromptTokens ?? 0;
                ollamaResponse.EvalCount = agnosticResponse.Usage.CompletionTokens ?? 0;
            }

            return ollamaResponse;
        }

        private object TransformEmbeddingResponse(AgnosticEmbeddingResponse agnosticResponse)
        {
            double[] embedding = Array.Empty<double>();

            if (agnosticResponse.Embedding != null && agnosticResponse.Embedding.Count > 0)
            {
                embedding = agnosticResponse.Embedding.ToArray();
            }
            else if (agnosticResponse.Data != null && agnosticResponse.Data.Count > 0)
            {
                embedding = agnosticResponse.Data[0].Embedding.ToArray();
            }

            OllamaEmbeddingResponse ollamaResponse = new OllamaEmbeddingResponse
            {
                Embedding = embedding
            };

            return ollamaResponse;
        }

        private object TransformModelListResponse(AgnosticModelListResponse agnosticResponse)
        {
            OllamaModelListResponse ollamaResponse = new OllamaModelListResponse
            {
                Models = new System.Collections.Generic.List<OllamaModelInfo>()
            };

            if (agnosticResponse.Models != null)
            {
                foreach (var model in agnosticResponse.Models)
                {
                    string modifiedAt = model.ModifiedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                        ?? model.CreatedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                        ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                    ollamaResponse.Models.Add(new OllamaModelInfo
                    {
                        Name = model.Id,
                        ModifiedAt = modifiedAt
                    });
                }
            }

            return ollamaResponse;
        }

        private object TransformModelInfoResponse(AgnosticModelInfoResponse agnosticResponse)
        {
            string parametersStr = "";
            if (agnosticResponse.Parameters != null)
            {
                if (agnosticResponse.Parameters is string str)
                    parametersStr = str;
                else
                    parametersStr = System.Text.Json.JsonSerializer.Serialize(agnosticResponse.Parameters);
            }

            OllamaModelDetailsResponse ollamaResponse = new OllamaModelDetailsResponse
            {
                Modelfile = "", // Ollama-specific, not in agnostic model
                Parameters = parametersStr,
                Template = agnosticResponse.Template ?? ""
            };

            return ollamaResponse;
        }
    }

    // Ollama response models for serialization
    internal class OllamaCompletionResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("response")]
        public string Response { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }
    }

    internal class OllamaChatResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("message")]
        public OllamaChatMessage Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }
    }

    internal class OllamaChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    internal class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public double[] Embedding { get; set; }
    }

    internal class OllamaModelListResponse
    {
        [JsonPropertyName("models")]
        public System.Collections.Generic.List<OllamaModelInfo> Models { get; set; }
    }

    internal class OllamaModelInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("modified_at")]
        public string ModifiedAt { get; set; }
    }

    internal class OllamaModelDetailsResponse
    {
        [JsonPropertyName("modelfile")]
        public string Modelfile { get; set; }

        [JsonPropertyName("parameters")]
        public string Parameters { get; set; }

        [JsonPropertyName("template")]
        public string Template { get; set; }
    }
}