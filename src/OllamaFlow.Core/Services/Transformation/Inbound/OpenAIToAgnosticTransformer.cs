namespace OllamaFlow.Core.Services.Transformation.Inbound
{
    using System;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models.Agnostic.Requests;
    using OllamaFlow.Core.Models.Agnostic.Common;
    using OllamaFlow.Core.Services.Transformation.Interfaces;
    using OllamaFlow.Core.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Transforms OpenAI API requests to agnostic format.
    /// </summary>
    public class OpenAIToAgnosticTransformer : IInboundTransformer
    {
        #region Private-Members

        private readonly Serializer _Serializer;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="serializer">Serializer.</param>
        public OpenAIToAgnosticTransformer(Serializer serializer = null)
        {
            _Serializer = serializer ?? new Serializer();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Determines if this transformer can handle the given source format and request type.
        /// </summary>
        /// <param name="sourceFormat">The API format of the source request.</param>
        /// <param name="requestType">The type of request being transformed.</param>
        /// <returns>True if this transformer can handle the transformation.</returns>
        public bool CanTransform(ApiFormatEnum sourceFormat, RequestTypeEnum requestType)
        {
            if (sourceFormat != ApiFormatEnum.OpenAI) return false;

            return requestType switch
            {
                RequestTypeEnum.GenerateChatCompletion => true,
                RequestTypeEnum.GenerateCompletion => true,
                RequestTypeEnum.GenerateEmbeddings => true,
                RequestTypeEnum.ListModels => true,
                RequestTypeEnum.ShowModelInformation => true,
                _ => false
            };
        }

        /// <summary>
        /// Transform the HTTP request context to an agnostic request.
        /// </summary>
        /// <param name="context">The HTTP context containing the request.</param>
        /// <param name="requestType">The type of request being transformed.</param>
        /// <returns>Agnostic request object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public async Task<AgnosticRequest> TransformAsync(HttpContextBase context, RequestTypeEnum requestType)
        {
            try
            {
                return requestType switch
                {
                    RequestTypeEnum.GenerateChatCompletion => await TransformChatCompletionAsync(context),
                    RequestTypeEnum.GenerateCompletion => await TransformCompletionAsync(context),
                    RequestTypeEnum.GenerateEmbeddings => await TransformEmbeddingAsync(context),
                    RequestTypeEnum.ListModels => await TransformModelListAsync(context),
                    RequestTypeEnum.ShowModelInformation => await TransformModelInfoAsync(context),
                    _ => throw new TransformationException(
                        $"Unsupported request type: {requestType}",
                        ApiFormatEnum.OpenAI,
                        ApiFormatEnum.OpenAI,
                        "RequestTypeValidation")
                };
            }
            catch (TransformationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new TransformationException(
                    $"Failed to transform OpenAI request: {ex.Message}",
                    ApiFormatEnum.OpenAI,
                    ApiFormatEnum.OpenAI,
                    "RequestTransformation",
                    context,
                    ex);
            }
        }

        #endregion

        #region Private-Methods

        private Task<AgnosticChatRequest> TransformChatCompletionAsync(HttpContextBase context)
        {
            string requestBody = context.Request.DataAsString;
            if (string.IsNullOrEmpty(requestBody))
            {
                throw new TransformationException(
                    "Request body is empty",
                    ApiFormatEnum.OpenAI,
                    ApiFormatEnum.OpenAI,
                    "RequestBodyValidation");
            }

            OpenAIChatRequest openAIChatRequest = _Serializer.DeserializeJson<OpenAIChatRequest>(requestBody);

            AgnosticChatRequest agnosticRequest = new AgnosticChatRequest
            {
                SourceFormat = ApiFormatEnum.OpenAI,
                Model = openAIChatRequest.Model,
                Messages = ConvertMessages(openAIChatRequest.Messages),
                Stream = openAIChatRequest.Stream,
                Temperature = openAIChatRequest.Temperature,
                MaxTokens = openAIChatRequest.MaxTokens,
                TopP = openAIChatRequest.TopP,
                N = openAIChatRequest.N,
                Stop = openAIChatRequest.Stop,
                FrequencyPenalty = openAIChatRequest.FrequencyPenalty,
                PresencePenalty = openAIChatRequest.PresencePenalty,
                Seed = openAIChatRequest.Seed,
                OriginalRequest = openAIChatRequest
            };

            return Task.FromResult(agnosticRequest);
        }

        private Task<AgnosticCompletionRequest> TransformCompletionAsync(HttpContextBase context)
        {
            string requestBody = context.Request.DataAsString;
            OpenAICompletionRequest openAICompletionRequest = _Serializer.DeserializeJson<OpenAICompletionRequest>(requestBody);

            AgnosticCompletionRequest agnosticRequest = new AgnosticCompletionRequest
            {
                SourceFormat = ApiFormatEnum.OpenAI,
                Model = openAICompletionRequest.Model,
                Prompt = openAICompletionRequest.Prompt,
                Stream = openAICompletionRequest.Stream,
                Temperature = openAICompletionRequest.Temperature,
                MaxTokens = openAICompletionRequest.MaxTokens,
                TopP = openAICompletionRequest.TopP,
                N = openAICompletionRequest.N,
                Stop = openAICompletionRequest.Stop,
                FrequencyPenalty = openAICompletionRequest.FrequencyPenalty,
                PresencePenalty = openAICompletionRequest.PresencePenalty,
                Seed = openAICompletionRequest.Seed,
                OriginalRequest = openAICompletionRequest
            };

            return Task.FromResult(agnosticRequest);
        }

        private Task<AgnosticEmbeddingRequest> TransformEmbeddingAsync(HttpContextBase context)
        {
            string requestBody = context.Request.DataAsString;
            OpenAIEmbeddingRequest openAIEmbeddingRequest = _Serializer.DeserializeJson<OpenAIEmbeddingRequest>(requestBody);

            AgnosticEmbeddingRequest result = new AgnosticEmbeddingRequest
            {
                SourceFormat = ApiFormatEnum.OpenAI,
                Model = openAIEmbeddingRequest.Model,
                Input = openAIEmbeddingRequest.Input,
                EncodingFormat = openAIEmbeddingRequest.EncodingFormat,
                OriginalRequest = openAIEmbeddingRequest
            };
            return Task.FromResult(result);
        }

        private Task<AgnosticModelListRequest> TransformModelListAsync(HttpContextBase context)
        {
            AgnosticModelListRequest result = new AgnosticModelListRequest
            {
                SourceFormat = ApiFormatEnum.OpenAI
            };
            return Task.FromResult(result);
        }

        private Task<AgnosticModelInfoRequest> TransformModelInfoAsync(HttpContextBase context)
        {
            // Use the existing RequestTypeHelper to extract model from URL
            string modelId = RequestTypeHelper.GetModelFromRequest(context.Request, RequestTypeEnum.OpenAIRetrieveModel);

            AgnosticModelInfoRequest result = new AgnosticModelInfoRequest
            {
                SourceFormat = ApiFormatEnum.OpenAI,
                Model = modelId
            };
            return Task.FromResult(result);
        }

        private List<AgnosticMessage> ConvertMessages(List<OpenAIMessage> openAIMessages)
        {
            List<AgnosticMessage> agnosticMessages = new List<AgnosticMessage>();

            if (openAIMessages != null)
            {
                foreach (OpenAIMessage msg in openAIMessages)
                {
                    agnosticMessages.Add(new AgnosticMessage
                    {
                        Role = msg.Role,
                        Content = msg.Content,
                        Name = msg.Name
                    });
                }
            }

            return agnosticMessages;
        }

        #endregion
    }

    // OpenAI request models for deserialization
    internal class OpenAIChatRequest
    {
        public string Model { get; set; }
        public List<OpenAIMessage> Messages { get; set; }
        public bool Stream { get; set; }
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }
        public double? TopP { get; set; }
        public int? N { get; set; } = 1;
        public string[] Stop { get; set; }
        public double? FrequencyPenalty { get; set; }
        public double? PresencePenalty { get; set; }
        public int? Seed { get; set; }
    }

    internal class OpenAIMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public string Name { get; set; }
    }

    internal class OpenAICompletionRequest
    {
        public string Model { get; set; }
        public string Prompt { get; set; }
        public bool Stream { get; set; }
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }
        public double? TopP { get; set; }
        public int? N { get; set; } = 1;
        public string[] Stop { get; set; }
        public double? FrequencyPenalty { get; set; }
        public double? PresencePenalty { get; set; }
        public int? Seed { get; set; }
    }

    internal class OpenAIEmbeddingRequest
    {
        public string Model { get; set; }
        public object Input { get; set; }
        public string EncodingFormat { get; set; } = "float";
    }
}