namespace OllamaFlow.Sdk.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;

    /// <summary>
    /// Interface for Backend API methods.
    /// </summary>
    public interface IBackendMethods
    {
        /// <summary>
        /// Retrieves all backends.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of backends.</returns>
        Task<List<Backend>?> RetrieveMany(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves health status for all backends.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of backends with health status.</returns>
        Task<List<Backend>?> RetrieveAllHealth(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a specific backend by identifier.
        /// </summary>
        /// <param name="identifier">The backend identifier.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the backend.</returns>
        Task<Backend?> Retrieve(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves health status for a specific backend.
        /// </summary>
        /// <param name="identifier">The backend identifier.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the backend with health status.</returns>
        Task<Backend?> RetrieveHealth(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a backend exists.
        /// </summary>
        /// <param name="identifier">The backend identifier.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the backend exists.</returns>
        Task<bool> Exists(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a backend.
        /// </summary>
        /// <param name="identifier">The backend identifier.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the deletion was successful.</returns>
        Task<bool> Delete(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new backend.
        /// </summary>
        /// <param name="backend">The backend configuration.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created backend with server-assigned properties.</returns>
        Task<Backend?> Create(Backend backend, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing backend.
        /// </summary>
        /// <param name="identifier">The backend identifier.</param>
        /// <param name="backend">The updated backend configuration.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated backend with server-assigned properties.</returns>
        Task<Backend?> Update(string identifier, Backend backend, CancellationToken cancellationToken = default);
    }
}
