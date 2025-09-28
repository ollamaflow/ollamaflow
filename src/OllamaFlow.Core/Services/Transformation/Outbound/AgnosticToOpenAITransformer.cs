namespace OllamaFlow.Core.Services.Transformation.Outbound
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models.Agnostic.Requests;
    using OllamaFlow.Core.Services.Transformation.Interfaces;

    /// <summary>
    /// Transforms agnostic requests to OpenAI API format.
    /// </summary>
    public class AgnosticToOpenAITransformer : IOutboundTransformer
    {
        /// <summary>
        /// Determines if this transformer can handle the given target format and agnostic request type.
        /// </summary>
        /// <param name="targetFormat">The target API format for the backend.</param>
        /// <param name="agnosticRequest">The agnostic request to be transformed.</param>
        /// <returns>True if this transformer can handle the transformation.</returns>
        public bool CanTransform(ApiFormatEnum targetFormat, AgnosticRequest agnosticRequest)
        {
            if (targetFormat != ApiFormatEnum.OpenAI) return false;

            return agnosticRequest switch
            {
                AgnosticChatRequest => true,
                AgnosticCompletionRequest => true,
                AgnosticEmbeddingRequest => true,
                AgnosticModelListRequest => true,
                AgnosticModelInfoRequest => true,
                _ => false
            };
        }

        /// <summary>
        /// Transform the agnostic request to a backend-specific format.
        /// </summary>
        /// <param name="agnosticRequest">The agnostic request to transform.</param>
        /// <returns>Backend-specific request object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public Task<object> TransformAsync(AgnosticRequest agnosticRequest)
        {
            try
            {
                object result = agnosticRequest switch
                {
                    AgnosticChatRequest chatRequest => TransformChatRequest(chatRequest),
                    AgnosticCompletionRequest completionRequest => TransformCompletionRequest(completionRequest),
                    AgnosticEmbeddingRequest embeddingRequest => TransformEmbeddingRequest(embeddingRequest),
                    AgnosticModelListRequest modelListRequest => TransformModelListRequest(modelListRequest),
                    AgnosticModelInfoRequest modelInfoRequest => TransformModelInfoRequest(modelInfoRequest),
                    _ => throw new TransformationException(
                        $"Unsupported agnostic request type: {agnosticRequest.GetType().Name}",
                        agnosticRequest.SourceFormat,
                        ApiFormatEnum.OpenAI,
                        "RequestTypeValidation")
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
                    $"Failed to transform agnostic request to OpenAI format: {ex.Message}",
                    agnosticRequest.SourceFormat,
                    ApiFormatEnum.OpenAI,
                    "RequestTransformation",
                    agnosticRequest,
                    ex);
            }
        }

        private object TransformChatRequest(AgnosticChatRequest agnosticRequest)
        {
            OpenAIBackendChatRequest openAIRequest = new OpenAIBackendChatRequest
            {
                Model = agnosticRequest.Model,
                Messages = ConvertMessages(agnosticRequest.Messages),
                Stream = agnosticRequest.Stream,
                Temperature = agnosticRequest.Temperature,
                MaxTokens = agnosticRequest.MaxTokens,
                TopP = agnosticRequest.TopP,
                N = agnosticRequest.N ?? 1,
                Stop = agnosticRequest.Stop,
                FrequencyPenalty = agnosticRequest.FrequencyPenalty,
                PresencePenalty = agnosticRequest.PresencePenalty,
                Seed = agnosticRequest.Seed
            };

            return openAIRequest;
        }

        private object TransformCompletionRequest(AgnosticCompletionRequest agnosticRequest)
        {
            OpenAIBackendCompletionRequest openAIRequest = new OpenAIBackendCompletionRequest
            {
                Model = agnosticRequest.Model,
                Prompt = agnosticRequest.Prompt,
                Stream = agnosticRequest.Stream,
                Temperature = agnosticRequest.Temperature,
                MaxTokens = agnosticRequest.MaxTokens,
                TopP = agnosticRequest.TopP,
                N = agnosticRequest.N ?? 1,
                Stop = agnosticRequest.Stop,
                FrequencyPenalty = agnosticRequest.FrequencyPenalty,
                PresencePenalty = agnosticRequest.PresencePenalty,
                Seed = agnosticRequest.Seed
            };

            return openAIRequest;
        }

        private object TransformEmbeddingRequest(AgnosticEmbeddingRequest agnosticRequest)
        {
            OpenAIBackendEmbeddingRequest openAIRequest = new OpenAIBackendEmbeddingRequest
            {
                Model = agnosticRequest.Model,
                Input = agnosticRequest.Input,
                EncodingFormat = agnosticRequest.EncodingFormat
            };

            return openAIRequest;
        }

        private object TransformModelListRequest(AgnosticModelListRequest agnosticRequest)
        {
            // OpenAI model list requests don't have a body
            return new { };
        }

        private object TransformModelInfoRequest(AgnosticModelInfoRequest agnosticRequest)
        {
            // OpenAI model info is retrieved via GET /v1/models/{model}
            // The model ID is in the URL path, not the body
            return new { };
        }

        private List<OpenAIBackendMessage> ConvertMessages(List<Models.Agnostic.Common.AgnosticMessage> agnosticMessages)
        {
            List<OpenAIBackendMessage> openAIMessages = new List<OpenAIBackendMessage>();

            if (agnosticMessages != null)
            {
                foreach (Models.Agnostic.Common.AgnosticMessage msg in agnosticMessages)
                {
                    openAIMessages.Add(new OpenAIBackendMessage
                    {
                        Role = msg.Role,
                        Content = msg.Content,
                        Name = msg.Name
                    });
                }
            }

            return openAIMessages;
        }
    }

    // OpenAI backend request models for serialization
    internal class OpenAIBackendChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<OpenAIBackendMessage> Messages { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("top_p")]
        public double? TopP { get; set; }

        [JsonPropertyName("n")]
        public int N { get; set; } = 1;

        [JsonPropertyName("stop")]
        public string[] Stop { get; set; }

        [JsonPropertyName("frequency_penalty")]
        public double? FrequencyPenalty { get; set; }

        [JsonPropertyName("presence_penalty")]
        public double? PresencePenalty { get; set; }

        [JsonPropertyName("seed")]
        public int? Seed { get; set; }
    }

    internal class OpenAIBackendMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    internal class OpenAIBackendCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("top_p")]
        public double? TopP { get; set; }

        [JsonPropertyName("n")]
        public int N { get; set; } = 1;

        [JsonPropertyName("stop")]
        public string[] Stop { get; set; }

        [JsonPropertyName("frequency_penalty")]
        public double? FrequencyPenalty { get; set; }

        [JsonPropertyName("presence_penalty")]
        public double? PresencePenalty { get; set; }

        [JsonPropertyName("seed")]
        public int? Seed { get; set; }
    }

    internal class OpenAIBackendEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("input")]
        public object Input { get; set; }

        [JsonPropertyName("encoding_format")]
        public string EncodingFormat { get; set; } = "float";
    }
}