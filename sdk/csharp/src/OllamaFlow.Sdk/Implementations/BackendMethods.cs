namespace OllamaFlow.Sdk.Implementations
{
    using OllamaFlow.Core;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using IBackendMethods = Interfaces.IBackendMethods;

    /// <summary>
    /// Implementation of Backend API methods.
    /// </summary>
    public class BackendMethods : IBackendMethods
    {
        private readonly OllamaFlowSdk _Sdk;

        /// <summary>
        /// Initializes a new instance of the BackendMethods class.
        /// </summary>
        /// <param name="sdk">The OllamaFlowSdk instance to use for API calls.</param>
        /// <exception cref="ArgumentNullException">Thrown when sdk is null.</exception>
        public BackendMethods(OllamaFlowSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        /// <inheritdoc/>
        public async Task<List<Backend>?> RetrieveMany(CancellationToken cancellationToken = default)
        {
            string url = _Sdk.Endpoint + "/v1.0/backends";
            return await _Sdk.GetAllAsync<Backend>(url, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<List<Backend>?> RetrieveAllHealth(CancellationToken cancellationToken = default)
        {
            string url = _Sdk.Endpoint + "/v1.0/backends/health";
            return await _Sdk.GetAllAsync<Backend>(url, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Backend?> Retrieve(string identifier, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentNullException(nameof(identifier));

            string url = _Sdk.Endpoint + $"/v1.0/backends/{Uri.EscapeDataString(identifier)}";
            return await _Sdk.GetAsync<Backend>(url, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Backend?> RetrieveHealth(string identifier, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentNullException(nameof(identifier));

            string url = _Sdk.Endpoint + $"/v1.0/backends/{Uri.EscapeDataString(identifier)}/health";
            return await _Sdk.GetAsync<Backend>(url, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<bool> Exists(string identifier, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentNullException(nameof(identifier));

            string url = _Sdk.Endpoint + $"/v1.0/backends/{Uri.EscapeDataString(identifier)}";
            return await _Sdk.HeadAsync(url, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<bool> Delete(string identifier, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentNullException(nameof(identifier));

            string url = _Sdk.Endpoint + $"/v1.0/backends/{Uri.EscapeDataString(identifier)}";
            return await _Sdk.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Backend?> Create(Backend backend, CancellationToken cancellationToken = default)
        {
            if (backend == null)
                throw new ArgumentNullException(nameof(backend));

            string url = _Sdk.Endpoint + "/v1.0/backends";
            return await _Sdk.PutAsync<Backend>(url, backend, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Backend?> Update(string identifier, Backend backend, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentNullException(nameof(identifier));
            if (backend == null)
                throw new ArgumentNullException(nameof(backend));

            string url = _Sdk.Endpoint + $"/v1.0/backends/{Uri.EscapeDataString(identifier)}";
            return await _Sdk.PutAsync<Backend>(url, backend, cancellationToken).ConfigureAwait(false);
        }
    }
}
