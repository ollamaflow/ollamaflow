namespace OllamaFlow.Core.Services.Transformation.Interfaces
{
    using System.Threading.Tasks;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Models.Agnostic.Base;

    /// <summary>
    /// Interface for transforming responses between API formats and agnostic format.
    /// </summary>
    public interface IResponseTransformer
    {
        /// <summary>
        /// Determines if this transformer can handle the given API format and response type.
        /// </summary>
        /// <param name="apiFormat">The API format of the response.</param>
        /// <param name="responseType">The type of response object.</param>
        /// <returns>True if this transformer can handle the transformation.</returns>
        bool CanTransform(ApiFormatEnum apiFormat, System.Type responseType);

        /// <summary>
        /// Transform a backend response to agnostic format.
        /// </summary>
        /// <param name="backendResponse">The backend response object.</param>
        /// <param name="sourceFormat">The API format of the backend response.</param>
        /// <returns>Agnostic response object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        Task<AgnosticResponse> TransformToAgnosticAsync(object backendResponse, ApiFormatEnum sourceFormat);

        /// <summary>
        /// Transform an agnostic response to a specific client API format.
        /// </summary>
        /// <param name="agnosticResponse">The agnostic response to transform.</param>
        /// <param name="targetFormat">The target API format for the client.</param>
        /// <returns>Client-specific response object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        Task<object> TransformFromAgnosticAsync(AgnosticResponse agnosticResponse, ApiFormatEnum targetFormat);
    }
}