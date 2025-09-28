namespace OllamaFlow.Core.Services.Transformation.Interfaces
{
    using System.Threading.Tasks;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Models.Agnostic.Base;

    /// <summary>
    /// Interface for transforming agnostic requests to specific backend API formats.
    /// </summary>
    public interface IOutboundTransformer
    {
        /// <summary>
        /// Determines if this transformer can handle the given target format and agnostic request type.
        /// </summary>
        /// <param name="targetFormat">The target API format for the backend.</param>
        /// <param name="agnosticRequest">The agnostic request to be transformed.</param>
        /// <returns>True if this transformer can handle the transformation.</returns>
        bool CanTransform(ApiFormatEnum targetFormat, AgnosticRequest agnosticRequest);

        /// <summary>
        /// Transform the agnostic request to a backend-specific format.
        /// </summary>
        /// <param name="agnosticRequest">The agnostic request to transform.</param>
        /// <returns>Backend-specific request object.</returns>
        /// <exception cref="TransformationException">Thrown when transformation fails.</exception>
        Task<object> TransformAsync(AgnosticRequest agnosticRequest);
    }
}