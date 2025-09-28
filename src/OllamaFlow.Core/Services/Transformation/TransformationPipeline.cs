namespace OllamaFlow.Core.Services.Transformation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Services.Transformation.Interfaces;
    using OllamaFlow.Core.Services.Transformation.Inbound;
    using OllamaFlow.Core.Services.Transformation.Outbound;
    using OllamaFlow.Core.Services.Transformation.Streaming;
    using OllamaFlow.Core.Serialization;

    /// <summary>
    /// Orchestrates the complete transformation pipeline for API requests and responses.
    /// </summary>
    public class TransformationPipeline : ITransformationPipeline
    {
        #region Private-Members

        private readonly List<IInboundTransformer> _InboundTransformers;
        private readonly List<IOutboundTransformer> _OutboundTransformers;
        private readonly IStreamingTransformer _StreamingTransformer;
        private readonly Serializer _Serializer;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initialize the transformation pipeline with default transformers.
        /// </summary>
        /// <param name="serializer">Serializer for JSON operations.</param>
        public TransformationPipeline(Serializer serializer = null)
        {
            _Serializer = serializer ?? new Serializer();

            _InboundTransformers = new List<IInboundTransformer>
            {
                new OllamaToAgnosticTransformer(_Serializer),
                new OpenAIToAgnosticTransformer(_Serializer)
            };

            _OutboundTransformers = new List<IOutboundTransformer>
            {
                new AgnosticToOllamaTransformer(),
                new AgnosticToOpenAITransformer()
            };

            _StreamingTransformer = new StreamingTransformer(_Serializer);
        }

        /// <summary>
        /// Initialize the transformation pipeline with custom transformers.
        /// </summary>
        /// <param name="inboundTransformers">Collection of inbound transformers.</param>
        /// <param name="outboundTransformers">Collection of outbound transformers.</param>
        /// <param name="serializer">Serializer for JSON operations.</param>
        public TransformationPipeline(
            IEnumerable<IInboundTransformer> inboundTransformers,
            IEnumerable<IOutboundTransformer> outboundTransformers,
            Serializer serializer = null)
        {
            _Serializer = serializer ?? new Serializer();
            _InboundTransformers = inboundTransformers?.ToList() ?? new List<IInboundTransformer>();
            _OutboundTransformers = outboundTransformers?.ToList() ?? new List<IOutboundTransformer>();
            _StreamingTransformer = new StreamingTransformer(_Serializer);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Transform an inbound request from a specific API format to agnostic format.
        /// </summary>
        /// <param name="context">HTTP context containing the request.</param>
        /// <param name="sourceFormat">The API format of the incoming request.</param>
        /// <returns>Agnostic request object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public async Task<AgnosticRequest> TransformInboundAsync(
            HttpContextBase context,
            ApiFormatEnum sourceFormat)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Detect request type from the context
            RequestTypeEnum requestType = DetectRequestType(context);

            IInboundTransformer transformer = _InboundTransformers.FirstOrDefault(t => t.CanTransform(sourceFormat, requestType));
            if (transformer == null)
            {
                throw new TransformationException(
                    $"No inbound transformer found for source format {sourceFormat} and request type {requestType}",
                    sourceFormat,
                    ApiFormatEnum.Ollama, // Default target for error reporting
                    "TransformerLookup");
            }

            try
            {
                return await transformer.TransformAsync(context, requestType).ConfigureAwait(false);
            }
            catch (TransformationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new TransformationException(
                    $"Inbound transformation failed: {ex.Message}",
                    sourceFormat,
                    ApiFormatEnum.Ollama,
                    "InboundTransformation",
                    context,
                    ex);
            }
        }

        /// <summary>
        /// Transform an agnostic request to backend-specific format.
        /// </summary>
        /// <param name="agnosticRequest">The agnostic request to transform.</param>
        /// <param name="targetFormat">The target API format for the backend.</param>
        /// <returns>Backend-specific request object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public async Task<object> TransformOutboundAsync(
            AgnosticRequest agnosticRequest,
            ApiFormatEnum targetFormat)
        {
            if (agnosticRequest == null) throw new ArgumentNullException(nameof(agnosticRequest));

            IOutboundTransformer transformer = _OutboundTransformers.FirstOrDefault(t => t.CanTransform(targetFormat, agnosticRequest));
            if (transformer == null)
            {
                throw new TransformationException(
                    $"No outbound transformer found for target format {targetFormat} and request type {agnosticRequest.GetType().Name}",
                    agnosticRequest.SourceFormat,
                    targetFormat,
                    "TransformerLookup",
                    agnosticRequest);
            }

            try
            {
                return await transformer.TransformAsync(agnosticRequest).ConfigureAwait(false);
            }
            catch (TransformationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new TransformationException(
                    $"Outbound transformation failed: {ex.Message}",
                    agnosticRequest.SourceFormat,
                    targetFormat,
                    "OutboundTransformation",
                    agnosticRequest,
                    ex);
            }
        }

        /// <summary>
        /// Transform a backend response from a specific API format to agnostic format.
        /// </summary>
        /// <param name="backendResponse">The backend response object.</param>
        /// <param name="sourceFormat">The API format of the backend response.</param>
        /// <returns>Agnostic response object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public Task<AgnosticResponse> TransformResponseInboundAsync(object backendResponse, ApiFormatEnum sourceFormat)
        {
            if (backendResponse == null) throw new ArgumentNullException(nameof(backendResponse));

            // For now, we'll implement basic response transformation
            // In a full implementation, you'd have response-specific transformers
            try
            {
                // This is a placeholder - in practice, you'd have dedicated response transformers
                // For now, assume the backend response is already in a compatible format
                AgnosticResponse result = new Models.Agnostic.Responses.AgnosticChatResponse
                {
                    SourceFormat = sourceFormat,
                    OriginalResponse = backendResponse
                };
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                throw new TransformationException(
                    $"Response inbound transformation failed: {ex.Message}",
                    sourceFormat,
                    ApiFormatEnum.Ollama,
                    "ResponseInboundTransformation",
                    backendResponse,
                    ex);
            }
        }

        /// <summary>
        /// Transform an agnostic response to a specific client API format.
        /// </summary>
        /// <param name="agnosticResponse">The agnostic response to transform.</param>
        /// <param name="targetFormat">The target API format for the client.</param>
        /// <returns>Client-specific response object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public Task<object> TransformResponseOutboundAsync(AgnosticResponse agnosticResponse, ApiFormatEnum targetFormat)
        {
            if (agnosticResponse == null) throw new ArgumentNullException(nameof(agnosticResponse));

            try
            {
                // This is a placeholder - in practice, you'd have dedicated response transformers
                // For now, return the original response if it exists, otherwise the agnostic response
                object result;
                if (agnosticResponse.OriginalResponse != null)
                {
                    result = agnosticResponse.OriginalResponse;
                }
                else
                {
                    result = agnosticResponse;
                }
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                throw new TransformationException(
                    $"Response outbound transformation failed: {ex.Message}",
                    agnosticResponse.SourceFormat,
                    targetFormat,
                    "ResponseOutboundTransformation",
                    agnosticResponse,
                    ex);
            }
        }

        /// <summary>
        /// Perform complete transformation pipeline: inbound → agnostic → outbound.
        /// </summary>
        /// <param name="context">The HTTP context containing the request.</param>
        /// <param name="sourceFormat">The API format of the incoming request.</param>
        /// <param name="targetFormat">The target API format for the backend.</param>
        /// <returns>Transformation pipeline result containing both agnostic and backend-specific requests.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        public async Task<TransformationPipelineResult> TransformCompleteAsync(
            HttpContextBase context,
            ApiFormatEnum sourceFormat,
            ApiFormatEnum targetFormat)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            try
            {
                // Step 1: Transform inbound request to agnostic format
                AgnosticRequest agnosticRequest = await TransformInboundAsync(context, sourceFormat).ConfigureAwait(false);

                // Step 2: Transform agnostic request to backend-specific format
                object backendRequest = await TransformOutboundAsync(agnosticRequest, targetFormat).ConfigureAwait(false);

                return new TransformationPipelineResult
                {
                    AgnosticRequest = agnosticRequest,
                    BackendRequest = backendRequest,
                    SourceFormat = sourceFormat,
                    TargetFormat = targetFormat,
                    RequestType = GetRequestTypeFromAgnosticRequest(agnosticRequest),
                    TransformationId = Guid.NewGuid().ToString(),
                    CompletedUtc = DateTime.UtcNow
                };
            }
            catch (TransformationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new TransformationException(
                    $"Complete transformation pipeline failed: {ex.Message}",
                    sourceFormat,
                    targetFormat,
                    "CompletePipeline",
                    context,
                    ex);
            }
        }

        /// <summary>
        /// Register a new inbound transformer.
        /// </summary>
        /// <param name="transformer">The transformer to register.</param>
        public void RegisterInboundTransformer(IInboundTransformer transformer)
        {
            if (transformer == null) throw new ArgumentNullException(nameof(transformer));
            if (!_InboundTransformers.Contains(transformer))
            {
                _InboundTransformers.Add(transformer);
            }
        }

        /// <summary>
        /// Register a new outbound transformer.
        /// </summary>
        /// <param name="transformer">The transformer to register.</param>
        public void RegisterOutboundTransformer(IOutboundTransformer transformer)
        {
            if (transformer == null) throw new ArgumentNullException(nameof(transformer));
            if (!_OutboundTransformers.Contains(transformer))
            {
                _OutboundTransformers.Add(transformer);
            }
        }

        /// <summary>
        /// Get information about available transformers.
        /// </summary>
        /// <returns>Transformation capabilities information.</returns>
        public TransformationCapabilities GetCapabilities()
        {
            List<TransformationCapability> inboundCapabilities = new List<TransformationCapability>();
            List<TransformationCapability> outboundCapabilities = new List<TransformationCapability>();

            // Collect inbound capabilities
            foreach (IInboundTransformer transformer in _InboundTransformers)
            {
                foreach (ApiFormatEnum sourceFormat in Enum.GetValues<ApiFormatEnum>())
                {
                    foreach (RequestTypeEnum requestType in Enum.GetValues<RequestTypeEnum>())
                    {
                        if (transformer.CanTransform(sourceFormat, requestType))
                        {
                            inboundCapabilities.Add(new TransformationCapability
                            {
                                TransformerType = transformer.GetType().Name,
                                SourceFormat = sourceFormat,
                                TargetFormat = null, // Inbound always targets agnostic
                                RequestType = requestType,
                                Direction = TransformationDirection.Inbound
                            });
                        }
                    }
                }
            }

            // Collect outbound capabilities
            foreach (IOutboundTransformer transformer in _OutboundTransformers)
            {
                foreach (ApiFormatEnum targetFormat in Enum.GetValues<ApiFormatEnum>())
                {
                    // Test with various agnostic request types
                    AgnosticRequest[] testRequests = new AgnosticRequest[]
                    {
                        new Models.Agnostic.Requests.AgnosticChatRequest { SourceFormat = ApiFormatEnum.Ollama },
                        new Models.Agnostic.Requests.AgnosticCompletionRequest { SourceFormat = ApiFormatEnum.Ollama },
                        new Models.Agnostic.Requests.AgnosticEmbeddingRequest { SourceFormat = ApiFormatEnum.Ollama },
                        new Models.Agnostic.Requests.AgnosticModelListRequest { SourceFormat = ApiFormatEnum.Ollama },
                        new Models.Agnostic.Requests.AgnosticModelInfoRequest { SourceFormat = ApiFormatEnum.Ollama }
                    };

                    foreach (AgnosticRequest testRequest in testRequests)
                    {
                        if (transformer.CanTransform(targetFormat, testRequest))
                        {
                            outboundCapabilities.Add(new TransformationCapability
                            {
                                TransformerType = transformer.GetType().Name,
                                SourceFormat = null, // Outbound always sources from agnostic
                                TargetFormat = targetFormat,
                                RequestType = GetRequestTypeFromAgnosticRequest(testRequest),
                                Direction = TransformationDirection.Outbound
                            });
                        }
                    }
                }
            }

            return new TransformationCapabilities
            {
                InboundCapabilities = inboundCapabilities,
                OutboundCapabilities = outboundCapabilities,
                SupportedSourceFormats = inboundCapabilities.Select(c => c.SourceFormat.Value).Distinct().ToList(),
                SupportedTargetFormats = outboundCapabilities.Select(c => c.TargetFormat.Value).Distinct().ToList()
            };
        }

        /// <summary>
        /// Get the streaming transformer for handling streaming transformations.
        /// </summary>
        /// <returns>Streaming transformer instance.</returns>
        public IStreamingTransformer GetStreamingTransformer()
        {
            return _StreamingTransformer;
        }

        /// <summary>
        /// Determines if streaming transformation is supported between the specified formats.
        /// </summary>
        /// <param name="sourceFormat">Source API format.</param>
        /// <param name="targetFormat">Target API format.</param>
        /// <param name="requestType">Type of request being streamed.</param>
        /// <returns>True if streaming transformation is supported.</returns>
        public bool SupportsStreamingTransformation(ApiFormatEnum sourceFormat, ApiFormatEnum targetFormat, RequestTypeEnum requestType)
        {
            return _StreamingTransformer.CanTransformStream(sourceFormat, targetFormat, requestType);
        }

        #endregion

        #region Private-Methods

        private RequestTypeEnum DetectRequestType(HttpContextBase context)
        {
            // Use the existing RequestTypeHelper which already handles both Ollama and OpenAI URL patterns
            return RequestTypeHelper.DetermineRequestType(context.Request.Method, context.Request.Url?.RawWithoutQuery);
        }

        private RequestTypeEnum GetRequestTypeFromAgnosticRequest(AgnosticRequest request)
        {
            return request switch
            {
                Models.Agnostic.Requests.AgnosticChatRequest => RequestTypeEnum.GenerateChatCompletion,
                Models.Agnostic.Requests.AgnosticCompletionRequest => RequestTypeEnum.GenerateCompletion,
                Models.Agnostic.Requests.AgnosticEmbeddingRequest => RequestTypeEnum.GenerateEmbeddings,
                Models.Agnostic.Requests.AgnosticModelListRequest => RequestTypeEnum.ListModels,
                Models.Agnostic.Requests.AgnosticModelInfoRequest => RequestTypeEnum.ShowModelInformation,
                _ => RequestTypeEnum.GenerateChatCompletion // Default fallback
            };
        }

        #endregion
    }

    /// <summary>
    /// Result of a complete transformation pipeline operation.
    /// </summary>
    public class TransformationPipelineResult
    {
        /// <summary>
        /// The agnostic request created from the inbound transformation.
        /// </summary>
        public AgnosticRequest AgnosticRequest { get; set; }

        /// <summary>
        /// The backend-specific request created from the outbound transformation.
        /// </summary>
        public object BackendRequest { get; set; }

        /// <summary>
        /// The source API format of the original request.
        /// </summary>
        public ApiFormatEnum SourceFormat { get; set; }

        /// <summary>
        /// The target API format for the backend request.
        /// </summary>
        public ApiFormatEnum TargetFormat { get; set; }

        /// <summary>
        /// The type of request that was transformed.
        /// </summary>
        public RequestTypeEnum RequestType { get; set; }

        /// <summary>
        /// Unique identifier for this transformation operation.
        /// </summary>
        public string TransformationId { get; set; }

        /// <summary>
        /// Timestamp when the transformation was completed.
        /// </summary>
        public DateTime CompletedUtc { get; set; }
    }

    /// <summary>
    /// Represents the transformation capabilities of the pipeline.
    /// </summary>
    public class TransformationCapabilities
    {
        /// <summary>
        /// Available inbound transformation capabilities.
        /// </summary>
        public List<TransformationCapability> InboundCapabilities { get; set; } = new List<TransformationCapability>();

        /// <summary>
        /// Available outbound transformation capabilities.
        /// </summary>
        public List<TransformationCapability> OutboundCapabilities { get; set; } = new List<TransformationCapability>();

        /// <summary>
        /// All supported source API formats.
        /// </summary>
        public List<ApiFormatEnum> SupportedSourceFormats { get; set; } = new List<ApiFormatEnum>();

        /// <summary>
        /// All supported target API formats.
        /// </summary>
        public List<ApiFormatEnum> SupportedTargetFormats { get; set; } = new List<ApiFormatEnum>();
    }

    /// <summary>
    /// Represents a specific transformation capability.
    /// </summary>
    public class TransformationCapability
    {
        /// <summary>
        /// The name of the transformer class.
        /// </summary>
        public string TransformerType { get; set; }

        /// <summary>
        /// The source API format (null for outbound transformers).
        /// </summary>
        public ApiFormatEnum? SourceFormat { get; set; }

        /// <summary>
        /// The target API format (null for inbound transformers).
        /// </summary>
        public ApiFormatEnum? TargetFormat { get; set; }

        /// <summary>
        /// The request type supported.
        /// </summary>
        public RequestTypeEnum RequestType { get; set; }

        /// <summary>
        /// The direction of transformation.
        /// </summary>
        public TransformationDirection Direction { get; set; }
    }

    /// <summary>
    /// Direction of transformation.
    /// </summary>
    public enum TransformationDirection
    {
        /// <summary>
        /// Inbound transformation (client request → agnostic).
        /// </summary>
        Inbound,

        /// <summary>
        /// Outbound transformation (agnostic → backend request).
        /// </summary>
        Outbound
    }
}