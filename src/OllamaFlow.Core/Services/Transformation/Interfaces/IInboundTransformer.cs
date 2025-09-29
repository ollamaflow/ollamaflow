namespace OllamaFlow.Core.Services.Transformation.Interfaces
{
    using System.Threading.Tasks;
    using WatsonWebserver.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models.Agnostic.Base;

    /// <summary>
    /// Interface for transforming inbound requests from specific API formats to agnostic format.
    /// </summary>
    public interface IInboundTransformer
    {
        /// <summary>
        /// Determines if this transformer can handle the given source format and request type.
        /// </summary>
        /// <param name="sourceFormat">The API format of the source request.</param>
        /// <param name="requestType">The type of request being transformed.</param>
        /// <returns>True if this transformer can handle the transformation.</returns>
        bool CanTransform(ApiFormatEnum sourceFormat, RequestTypeEnum requestType);

        /// <summary>
        /// Transform the HTTP request context to an agnostic request.
        /// </summary>
        /// <param name="context">The HTTP context containing the request.</param>
        /// <param name="requestType">The type of request being transformed.</param>
        /// <returns>Agnostic request object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        Task<AgnosticRequest> TransformAsync(HttpContextBase context, RequestTypeEnum requestType);
    }
}