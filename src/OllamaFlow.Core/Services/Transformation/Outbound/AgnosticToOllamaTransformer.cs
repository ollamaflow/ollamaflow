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
    /// Transforms agnostic requests to Ollama API format.
    /// </summary>
    public class AgnosticToOllamaTransformer : IOutboundTransformer
    {
        /// <summary>
        /// Determines if this transformer can handle the given target format and agnostic request type.
        /// </summary>
        /// <param name="targetFormat">The target API format for the backend.</param>
        /// <param name="agnosticRequest">The agnostic request to be transformed.</param>
        /// <returns>True if this transformer can handle the transformation.</returns>
        public bool CanTransform(ApiFormatEnum targetFormat, AgnosticRequest agnosticRequest)
        {
            if (targetFormat != ApiFormatEnum.Ollama) return false;

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
                        ApiFormatEnum.Ollama,
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
                    $"Failed to transform agnostic request to Ollama format: {ex.Message}",
                    agnosticRequest.SourceFormat,
                    ApiFormatEnum.Ollama,
                    "RequestTransformation",
                    agnosticRequest,
                    ex);
            }
        }

        private object TransformChatRequest(AgnosticChatRequest agnosticRequest)
        {
            OllamaBackendChatRequest ollamaRequest = new OllamaBackendChatRequest
            {
                Model = agnosticRequest.Model,
                Messages = ConvertMessages(agnosticRequest.Messages),
                Stream = agnosticRequest.Stream,
                System = agnosticRequest.System,
                Template = agnosticRequest.Template,
                Options = BuildOptions(agnosticRequest)
            };

            return ollamaRequest;
        }

        private object TransformCompletionRequest(AgnosticCompletionRequest agnosticRequest)
        {
            OllamaBackendCompletionRequest ollamaRequest = new OllamaBackendCompletionRequest
            {
                Model = agnosticRequest.Model,
                Prompt = agnosticRequest.Prompt,
                Stream = agnosticRequest.Stream,
                System = agnosticRequest.System,
                Template = agnosticRequest.Template,
                Options = BuildOptions(agnosticRequest)
            };

            return ollamaRequest;
        }

        private object TransformEmbeddingRequest(AgnosticEmbeddingRequest agnosticRequest)
        {
            OllamaBackendEmbeddingRequest ollamaRequest = new OllamaBackendEmbeddingRequest
            {
                Model = agnosticRequest.Model,
                Prompt = agnosticRequest.Input?.ToString()
            };

            return ollamaRequest;
        }

        private object TransformModelListRequest(AgnosticModelListRequest agnosticRequest)
        {
            // Ollama model list requests don't typically have a body
            return new { };
        }

        private object TransformModelInfoRequest(AgnosticModelInfoRequest agnosticRequest)
        {
            OllamaBackendModelInfoRequest ollamaRequest = new OllamaBackendModelInfoRequest
            {
                Model = agnosticRequest.Model
            };

            return ollamaRequest;
        }

        private List<OllamaBackendMessage> ConvertMessages(List<Models.Agnostic.Common.AgnosticMessage> agnosticMessages)
        {
            List<OllamaBackendMessage> ollamaMessages = new List<OllamaBackendMessage>();

            if (agnosticMessages != null)
            {
                foreach (Models.Agnostic.Common.AgnosticMessage msg in agnosticMessages)
                {
                    ollamaMessages.Add(new OllamaBackendMessage
                    {
                        Role = msg.Role,
                        Content = msg.Content
                    });
                }
            }

            return ollamaMessages;
        }

        private Dictionary<string, object> BuildOptions(AgnosticChatRequest agnosticRequest)
        {
            Dictionary<string, object> options = new Dictionary<string, object>();

            // Start with any existing options
            if (agnosticRequest.Options != null)
            {
                foreach (System.Collections.Generic.KeyValuePair<string, object> kvp in agnosticRequest.Options)
                {
                    options[kvp.Key] = kvp.Value;
                }
            }

            // Add mapped parameters (these will override existing values if present)
            if (agnosticRequest.Temperature.HasValue)
                options["temperature"] = agnosticRequest.Temperature.Value;

            if (agnosticRequest.TopP.HasValue)
                options["top_p"] = agnosticRequest.TopP.Value;

            if (agnosticRequest.Seed.HasValue)
                options["seed"] = agnosticRequest.Seed.Value;

            if (agnosticRequest.MaxTokens.HasValue)
                options["num_predict"] = agnosticRequest.MaxTokens.Value;

            if (agnosticRequest.Stop != null && agnosticRequest.Stop.Length > 0)
                options["stop"] = agnosticRequest.Stop;

            return options.Count > 0 ? options : null;
        }

        private Dictionary<string, object> BuildOptions(AgnosticCompletionRequest agnosticRequest)
        {
            Dictionary<string, object> options = new Dictionary<string, object>();

            // Start with any existing options
            if (agnosticRequest.Options != null)
            {
                foreach (System.Collections.Generic.KeyValuePair<string, object> kvp in agnosticRequest.Options)
                {
                    options[kvp.Key] = kvp.Value;
                }
            }

            // Add mapped parameters (these will override existing values if present)
            if (agnosticRequest.Temperature.HasValue)
                options["temperature"] = agnosticRequest.Temperature.Value;

            if (agnosticRequest.TopP.HasValue)
                options["top_p"] = agnosticRequest.TopP.Value;

            if (agnosticRequest.Seed.HasValue)
                options["seed"] = agnosticRequest.Seed.Value;

            if (agnosticRequest.MaxTokens.HasValue)
                options["num_predict"] = agnosticRequest.MaxTokens.Value;

            if (agnosticRequest.Stop != null && agnosticRequest.Stop.Length > 0)
                options["stop"] = agnosticRequest.Stop;

            return options.Count > 0 ? options : null;
        }
    }

    // Ollama backend request models for serialization
    internal class OllamaBackendChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<OllamaBackendMessage> Messages { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("system")]
        public string System { get; set; }

        [JsonPropertyName("template")]
        public string Template { get; set; }

        [JsonPropertyName("options")]
        public Dictionary<string, object> Options { get; set; }
    }

    internal class OllamaBackendMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    internal class OllamaBackendCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("system")]
        public string System { get; set; }

        [JsonPropertyName("template")]
        public string Template { get; set; }

        [JsonPropertyName("options")]
        public Dictionary<string, object> Options { get; set; }
    }

    internal class OllamaBackendEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }
    }

    internal class OllamaBackendModelInfoRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
    }
}