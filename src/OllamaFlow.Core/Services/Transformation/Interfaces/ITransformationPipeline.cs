namespace OllamaFlow.Core.Services.Transformation.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models.Agnostic.Base;

    /// <summary>
    /// Main transformation pipeline interface for converting between API formats and agnostic format.
    /// </summary>
    public interface ITransformationPipeline
    {
        /// <summary>
        /// Transform an inbound request from a specific API format to agnostic format.
        /// </summary>
        /// <param name="context">HTTP context containing the request.</param>
        /// <param name="sourceFormat">The API format of the incoming request.</param>
        /// <returns>Agnostic request object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        Task<AgnosticRequest> TransformInboundAsync(HttpContextBase context, ApiFormatEnum sourceFormat);

        /// <summary>
        /// Transform an agnostic request to a specific backend API format.
        /// </summary>
        /// <param name="agnosticRequest">The agnostic request to transform.</param>
        /// <param name="targetFormat">The target API format for the backend.</param>
        /// <returns>Backend-specific request object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        Task<object> TransformOutboundAsync(AgnosticRequest agnosticRequest, ApiFormatEnum targetFormat);

        /// <summary>
        /// Transform a backend response from a specific API format to agnostic format.
        /// </summary>
        /// <param name="backendResponse">The backend response object.</param>
        /// <param name="sourceFormat">The API format of the backend response.</param>
        /// <returns>Agnostic response object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        Task<AgnosticResponse> TransformResponseInboundAsync(object backendResponse, ApiFormatEnum sourceFormat);

        /// <summary>
        /// Transform an agnostic response to a specific client API format.
        /// </summary>
        /// <param name="agnosticResponse">The agnostic response to transform.</param>
        /// <param name="targetFormat">The target API format for the client.</param>
        /// <returns>Client-specific response object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        Task<object> TransformResponseOutboundAsync(AgnosticResponse agnosticResponse, ApiFormatEnum targetFormat);

        /// <summary>
        /// Get the streaming transformer for handling streaming transformations.
        /// </summary>
        /// <returns>Streaming transformer instance.</returns>
        IStreamingTransformer GetStreamingTransformer();

        /// <summary>
        /// Determines if streaming transformation is supported between the specified formats.
        /// </summary>
        /// <param name="sourceFormat">Source API format.</param>
        /// <param name="targetFormat">Target API format.</param>
        /// <param name="requestType">Type of request being streamed.</param>
        /// <returns>True if streaming transformation is supported.</returns>
        bool SupportsStreamingTransformation(ApiFormatEnum sourceFormat, ApiFormatEnum targetFormat, RequestTypeEnum requestType);
    }
}