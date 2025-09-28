namespace OllamaFlow.Core.Services.Transformation.Inbound
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models.Agnostic.Requests;
    using OllamaFlow.Core.Models.Agnostic.Common;
    using OllamaFlow.Core.Services.Transformation.Interfaces;
    using OllamaFlow.Core.Serialization;

    /// <summary>
    /// Transforms Ollama API requests to agnostic format.
    /// </summary>
    public class OllamaToAgnosticTransformer : IInboundTransformer
    {
        private readonly Serializer _Serializer;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="serializer">Serializer.</param>
        public OllamaToAgnosticTransformer(Serializer serializer = null)
        {
            _Serializer = serializer ?? new Serializer();
        }

        /// <summary>
        /// Determines if this transformer can handle the given source format and request type.
        /// </summary>
        /// <param name="sourceFormat">The API format of the source request.</param>
        /// <param name="requestType">The type of request being transformed.</param>
        /// <returns>True if this transformer can handle the transformation.</returns>
        public bool CanTransform(ApiFormatEnum sourceFormat, RequestTypeEnum requestType)
        {
            if (sourceFormat != ApiFormatEnum.Ollama) return false;

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
                        ApiFormatEnum.Ollama,
                        ApiFormatEnum.Ollama,
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
                    $"Failed to transform Ollama request: {ex.Message}",
                    ApiFormatEnum.Ollama,
                    ApiFormatEnum.Ollama,
                    "RequestTransformation",
                    context,
                    ex);
            }
        }

        private Task<AgnosticChatRequest> TransformChatCompletionAsync(HttpContextBase context)
        {
            string requestBody = context.Request.DataAsString;
            if (string.IsNullOrEmpty(requestBody))
            {
                throw new TransformationException(
                    "Request body is empty",
                    ApiFormatEnum.Ollama,
                    ApiFormatEnum.Ollama,
                    "RequestBodyValidation");
            }

            OllamaChatRequest ollamaChatRequest = _Serializer.DeserializeJson<OllamaChatRequest>(requestBody);

            AgnosticChatRequest agnosticRequest = new AgnosticChatRequest
            {
                SourceFormat = ApiFormatEnum.Ollama,
                Model = ollamaChatRequest.Model,
                Messages = ConvertMessages(ollamaChatRequest.Messages),
                Stream = ollamaChatRequest.Stream,
                System = ollamaChatRequest.System,
                Template = ollamaChatRequest.Template,
                OriginalRequest = ollamaChatRequest
            };

            // Map Ollama options to agnostic parameters
            if (ollamaChatRequest.Options != null)
            {
                agnosticRequest.Options = new System.Collections.Generic.Dictionary<string, object>(ollamaChatRequest.Options);

                // Extract common parameters and remove them from options to avoid duplication
                if (agnosticRequest.Options.TryGetValue("temperature", out object temp))
                {
                    agnosticRequest.Temperature = ConvertToDouble(temp);
                    agnosticRequest.Options.Remove("temperature");
                }

                if (agnosticRequest.Options.TryGetValue("top_p", out object topP))
                {
                    agnosticRequest.TopP = ConvertToDouble(topP);
                    agnosticRequest.Options.Remove("top_p");
                }

                if (agnosticRequest.Options.TryGetValue("seed", out object seed))
                {
                    agnosticRequest.Seed = ConvertToInt32(seed);
                    agnosticRequest.Options.Remove("seed");
                }

                if (agnosticRequest.Options.TryGetValue("num_predict", out object maxTokens))
                {
                    agnosticRequest.MaxTokens = ConvertToInt32(maxTokens);
                    agnosticRequest.Options.Remove("num_predict");
                }

                if (agnosticRequest.Options.TryGetValue("frequency_penalty", out object freqPenalty))
                {
                    agnosticRequest.FrequencyPenalty = ConvertToDouble(freqPenalty);
                    agnosticRequest.Options.Remove("frequency_penalty");
                }

                if (agnosticRequest.Options.TryGetValue("presence_penalty", out object presPenalty))
                {
                    agnosticRequest.PresencePenalty = ConvertToDouble(presPenalty);
                    agnosticRequest.Options.Remove("presence_penalty");
                }

                if (agnosticRequest.Options.TryGetValue("stop", out object stopSeqs))
                {
                    if (stopSeqs is JsonElement stopElement && stopElement.ValueKind == JsonValueKind.Array)
                    {
                        System.Collections.Generic.List<string> stops = new System.Collections.Generic.List<string>();
                        foreach (JsonElement item in stopElement.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                                stops.Add(item.GetString());
                        }
                        if (stops.Count > 0)
                            agnosticRequest.Stop = stops.ToArray();
                    }
                    agnosticRequest.Options.Remove("stop");
                }
            }

            return Task.FromResult(agnosticRequest);
        }

        private Task<AgnosticCompletionRequest> TransformCompletionAsync(HttpContextBase context)
        {
            string requestBody = context.Request.DataAsString;
            OllamaCompletionRequest ollamaCompletionRequest = _Serializer.DeserializeJson<OllamaCompletionRequest>(requestBody);

            AgnosticCompletionRequest agnosticRequest = new AgnosticCompletionRequest
            {
                SourceFormat = ApiFormatEnum.Ollama,
                Model = ollamaCompletionRequest.Model,
                Prompt = ollamaCompletionRequest.Prompt,
                Stream = ollamaCompletionRequest.Stream,
                System = ollamaCompletionRequest.System,
                Template = ollamaCompletionRequest.Template,
                OriginalRequest = ollamaCompletionRequest
            };

            // Map options
            if (ollamaCompletionRequest.Options != null)
            {
                agnosticRequest.Options = new System.Collections.Generic.Dictionary<string, object>(ollamaCompletionRequest.Options);

                if (agnosticRequest.Options.TryGetValue("temperature", out object temp))
                {
                    agnosticRequest.Temperature = ConvertToDouble(temp);
                    agnosticRequest.Options.Remove("temperature");
                }

                if (agnosticRequest.Options.TryGetValue("top_p", out object topP))
                {
                    agnosticRequest.TopP = ConvertToDouble(topP);
                    agnosticRequest.Options.Remove("top_p");
                }

                if (agnosticRequest.Options.TryGetValue("seed", out object seed))
                {
                    agnosticRequest.Seed = ConvertToInt32(seed);
                    agnosticRequest.Options.Remove("seed");
                }

                if (agnosticRequest.Options.TryGetValue("num_predict", out object maxTokens))
                {
                    agnosticRequest.MaxTokens = ConvertToInt32(maxTokens);
                    agnosticRequest.Options.Remove("num_predict");
                }

                if (agnosticRequest.Options.TryGetValue("frequency_penalty", out object freqPenalty))
                {
                    agnosticRequest.FrequencyPenalty = ConvertToDouble(freqPenalty);
                    agnosticRequest.Options.Remove("frequency_penalty");
                }

                if (agnosticRequest.Options.TryGetValue("presence_penalty", out object presPenalty))
                {
                    agnosticRequest.PresencePenalty = ConvertToDouble(presPenalty);
                    agnosticRequest.Options.Remove("presence_penalty");
                }

                if (agnosticRequest.Options.TryGetValue("stop", out object stopSeqs))
                {
                    if (stopSeqs is JsonElement stopElement && stopElement.ValueKind == JsonValueKind.Array)
                    {
                        System.Collections.Generic.List<string> stops = new System.Collections.Generic.List<string>();
                        foreach (JsonElement item in stopElement.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                                stops.Add(item.GetString());
                        }
                        if (stops.Count > 0)
                            agnosticRequest.Stop = stops.ToArray();
                    }
                    agnosticRequest.Options.Remove("stop");
                }
            }

            return Task.FromResult(agnosticRequest);
        }

        private Task<AgnosticEmbeddingRequest> TransformEmbeddingAsync(HttpContextBase context)
        {
            string requestBody = context.Request.DataAsString;
            OllamaEmbeddingRequest ollamaEmbeddingRequest = _Serializer.DeserializeJson<OllamaEmbeddingRequest>(requestBody);

            AgnosticEmbeddingRequest result = new AgnosticEmbeddingRequest
            {
                SourceFormat = ApiFormatEnum.Ollama,
                Model = ollamaEmbeddingRequest.Model,
                Input = ollamaEmbeddingRequest.Prompt,
                OriginalRequest = ollamaEmbeddingRequest
            };
            return Task.FromResult(result);
        }

        private Task<AgnosticModelListRequest> TransformModelListAsync(HttpContextBase context)
        {
            AgnosticModelListRequest result = new AgnosticModelListRequest
            {
                SourceFormat = ApiFormatEnum.Ollama
            };
            return Task.FromResult(result);
        }

        private Task<AgnosticModelInfoRequest> TransformModelInfoAsync(HttpContextBase context)
        {
            string requestBody = context.Request.DataAsString;
            OllamaModelInfoRequest ollamaModelInfoRequest = _Serializer.DeserializeJson<OllamaModelInfoRequest>(requestBody);

            AgnosticModelInfoRequest result = new AgnosticModelInfoRequest
            {
                SourceFormat = ApiFormatEnum.Ollama,
                Model = ollamaModelInfoRequest.Model,
                OriginalRequest = ollamaModelInfoRequest
            };
            return Task.FromResult(result);
        }

        private System.Collections.Generic.List<AgnosticMessage> ConvertMessages(System.Collections.Generic.List<OllamaMessage> ollamaMessages)
        {
            System.Collections.Generic.List<AgnosticMessage> agnosticMessages = new System.Collections.Generic.List<AgnosticMessage>();

            if (ollamaMessages != null)
            {
                foreach (OllamaMessage msg in ollamaMessages)
                {
                    agnosticMessages.Add(new AgnosticMessage
                    {
                        Role = msg.Role,
                        Content = msg.Content
                    });
                }
            }

            return agnosticMessages;
        }

        /// <summary>
        /// Safely convert an object (potentially a JsonElement) to double.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <returns>Double value.</returns>
        private double ConvertToDouble(object value)
        {
            if (value == null)
                return 0.0;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number)
                    return jsonElement.GetDouble();
                if (jsonElement.ValueKind == JsonValueKind.String && double.TryParse(jsonElement.GetString(), out double result))
                    return result;
                return 0.0;
            }

            return Convert.ToDouble(value);
        }

        /// <summary>
        /// Safely convert an object (potentially a JsonElement) to int32.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <returns>Integer value.</returns>
        private int ConvertToInt32(object value)
        {
            if (value == null)
                return 0;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number)
                    return jsonElement.GetInt32();
                if (jsonElement.ValueKind == JsonValueKind.String && int.TryParse(jsonElement.GetString(), out int result))
                    return result;
                return 0;
            }

            return Convert.ToInt32(value);
        }
    }

    // Ollama request models for deserialization
    internal class OllamaChatRequest
    {
        public string Model { get; set; }
        public System.Collections.Generic.List<OllamaMessage> Messages { get; set; }
        public bool Stream { get; set; }
        public string System { get; set; }
        public string Template { get; set; }
        public System.Collections.Generic.Dictionary<string, object> Options { get; set; }
    }

    internal class OllamaMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    internal class OllamaCompletionRequest
    {
        public string Model { get; set; }
        public string Prompt { get; set; }
        public bool Stream { get; set; }
        public string System { get; set; }
        public string Template { get; set; }
        public System.Collections.Generic.Dictionary<string, object> Options { get; set; }
    }

    internal class OllamaEmbeddingRequest
    {
        public string Model { get; set; }
        public string Prompt { get; set; }
    }

    internal class OllamaModelInfoRequest
    {
        public string Model { get; set; }
    }
}