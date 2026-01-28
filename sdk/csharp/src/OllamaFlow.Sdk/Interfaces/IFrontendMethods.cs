namespace OllamaFlow.Sdk.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for Frontend management methods.
    /// </summary>
    public interface IFrontendMethods
    {
        /// <summary>
        /// Retrieve all frontends.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of frontends.</returns>
        Task<List<Frontend>?> RetrieveMany(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieve a specific frontend by identifier.
        /// </summary>
        /// <param name="identifier">Frontend identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Frontend object if found, null otherwise.</returns>
        Task<Frontend?> Retrieve(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a frontend exists by identifier.
        /// </summary>
        /// <param name="identifier">Frontend identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if frontend exists, false otherwise.</returns>
        Task<bool> Exists(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a frontend by identifier.
        /// </summary>
        /// <param name="identifier">Frontend identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if deleted successfully, false otherwise.</returns>
        Task<bool> Delete(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a new frontend.
        /// </summary>
        /// <param name="frontend">Frontend object to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Created frontend object if successful, null otherwise.</returns>
        Task<Frontend?> Create(Frontend frontend, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an existing frontend.
        /// </summary>
        /// <param name="identifier">Frontend identifier.</param>
        /// <param name="frontend">Frontend object with updated data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated frontend object if successful, null otherwise.</returns>
        Task<Frontend?> Update(string identifier, Frontend frontend, CancellationToken cancellationToken = default);
    }
}
