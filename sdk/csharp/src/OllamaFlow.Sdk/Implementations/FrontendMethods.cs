namespace OllamaFlow.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using IFrontendMethods = Interfaces.IFrontendMethods;

    /// <summary>
    /// Implementation of Frontend management methods.
    /// </summary>
    public class FrontendMethods : IFrontendMethods
    {
        private readonly OllamaFlowSdk _Sdk;

        /// <summary>
        /// Initialize the FrontendMethods.
        /// </summary>
        /// <param name="sdk">OllamaFlowSdk instance.</param>
        public FrontendMethods(OllamaFlowSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        /// <inheritdoc/>
        public async Task<List<Frontend>?> RetrieveMany(CancellationToken cancellationToken = default)
        {
            string url = _Sdk.Endpoint + "/v1.0/frontends";
            return await _Sdk.GetAllAsync<Frontend>(url, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Frontend?> Retrieve(string identifier, CancellationToken cancellationToken = default)
        {
             if (string.IsNullOrEmpty(identifier))
                throw new ArgumentNullException(nameof(identifier));

            string url = _Sdk.Endpoint + $"/v1.0/frontends/{Uri.EscapeDataString(identifier)}";
            return await _Sdk.GetAsync<Frontend>(url, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<bool> Exists(string identifier, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentNullException(nameof(identifier));

            string url = _Sdk.Endpoint + $"/v1.0/frontends/{Uri.EscapeDataString(identifier)}";
            return await _Sdk.HeadAsync(url, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<bool> Delete(string identifier, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentNullException(nameof(identifier));

            string url = _Sdk.Endpoint + $"/v1.0/frontends/{Uri.EscapeDataString(identifier)}";
            return await _Sdk.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Frontend?> Create(Frontend frontend, CancellationToken cancellationToken = default)
        {
            if (frontend == null)
                throw new ArgumentNullException(nameof(frontend));

            string url = _Sdk.Endpoint + "/v1.0/frontends";
            return await _Sdk.PutAsync<Frontend>(url, frontend, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Frontend?> Update(string identifier, Frontend frontend, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentNullException(nameof(identifier));
            if (frontend == null)
                throw new ArgumentNullException(nameof(frontend));

            string url = _Sdk.Endpoint + $"/v1.0/frontends/{Uri.EscapeDataString(identifier)}";
            return await _Sdk.PutAsync<Frontend>(url, frontend, cancellationToken).ConfigureAwait(false);
        }
    }
}
