namespace OllamaFlow.Core.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Core.Services;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Administrative API handler for managing frontends, backends, and sessions.
    /// </summary>
    public class AdminApiHandler : IDisposable
    {
        private readonly string _Header = "[AdminApiHandler] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private ServiceContext _Services = null;
        private CancellationTokenSource _TokenSource = null;
        private bool _Disposed = false;

        /// <summary>
        /// Administrative API handler for managing frontends, backends, and sessions.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="services">Service context.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public AdminApiHandler(
            OllamaFlowSettings settings,
            LoggingModule logging,
            Serializer serializer,
            ServiceContext services,
            CancellationTokenSource tokenSource)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Services = services ?? throw new ArgumentNullException(nameof(services));
            _TokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        /// <summary>
        /// Initialize.
        /// </summary>
        public void Initialize()
        {
            _Logging.Debug(_Header + "initialized");
        }

        /// <summary>
        /// Check if the request is authenticated with a valid bearer token.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>True if authenticated, false otherwise.</returns>
        public bool IsAuthenticated(HttpContextBase ctx)
        {
            if (ctx.Request.Authorization != null && !string.IsNullOrEmpty(ctx.Request.Authorization.BearerToken))
            {
                if (_Settings.AdminBearerTokens != null)
                {
                    if (_Settings.AdminBearerTokens.Contains(ctx.Request.Authorization.BearerToken))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get all frontends.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        public async Task GetFrontendsRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            List<Frontend> objs = _Services.Frontend.GetAll().ToList();
            await ctx.Response.Send(_Serializer.SerializeJson(objs, true), token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a specific frontend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when frontend with specified identifier is not found.</exception>
        public async Task GetFrontendRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = GetParameter(ctx, "identifier");
            Frontend obj = _Services.Frontend.GetByIdentifier(identifier);
            if (obj == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");
            await ctx.Response.Send(_Serializer.SerializeJson(obj, true), token).ConfigureAwait(false);
        }

        /// <summary>
        /// Check if a frontend exists by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        public async Task ExistsFrontendRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = GetParameter(ctx, "identifier");
            bool exists = _Services.Frontend.Exists(identifier);
            
            if (exists)
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.Send("true", token).ConfigureAwait(false);
            }
            else
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send("false", token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Delete a frontend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when frontend with specified identifier is not found.</exception>
        public async Task DeleteFrontendRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = GetParameter(ctx, "identifier");
            if (!_Services.Frontend.Exists(identifier)) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");

            _Services.Frontend.Delete(identifier);
            ctx.Response.StatusCode = 204;
            await ctx.Response.Send(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new frontend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="ArgumentException">Thrown when frontend with specified identifier already exists.</exception>
        public async Task CreateFrontendRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            Frontend obj = _Serializer.DeserializeJson<Frontend>(ctx.Request.DataAsString);
            Frontend existing = _Services.Frontend.GetByIdentifier(obj.Identifier);
            if (existing != null) throw new ArgumentException("An object with identifier " + obj.Identifier + " already exists.");

            Frontend created = _Services.Frontend.Create(obj);
            ctx.Response.StatusCode = 201;
            await ctx.Response.Send(_Serializer.SerializeJson(created, true), token).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing frontend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when frontend with specified identifier is not found.</exception>
        public async Task UpdateFrontendRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = GetParameter(ctx, "identifier");
            Frontend original = _Services.Frontend.GetByIdentifier(identifier);
            if (original == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");

            Frontend updated = _Serializer.DeserializeJson<Frontend>(ctx.Request.DataAsString);
            updated.Identifier = identifier;
            updated = _Services.Frontend.Update(updated);

            await ctx.Response.Send(_Serializer.SerializeJson(updated, true), token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get all backends.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        public async Task GetBackendsRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            List<Backend> objs = _Services.Backend.GetAll().ToList();
            await ctx.Response.Send(_Serializer.SerializeJson(objs, true), token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get health status of all backends.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        public async Task GetBackendsHealthRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            List<Backend> objs = new List<Backend>(_Services.HealthCheck.Backends);
            await ctx.Response.Send(_Serializer.SerializeJson(objs, true), token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a specific backend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when backend with specified identifier is not found.</exception>
        public async Task GetBackendRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = GetParameter(ctx, "identifier");
            Backend obj = _Services.Backend.GetByIdentifier(identifier);
            if (obj == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");
            await ctx.Response.Send(_Serializer.SerializeJson(obj, true), token).ConfigureAwait(false);
        }

        /// <summary>
        /// Check if a backend exists by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        public async Task ExistsBackendRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = GetParameter(ctx, "identifier");
            bool exists = _Services.Backend.Exists(identifier);
            
            if (exists)
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.Send("true", token).ConfigureAwait(false);
            }
            else
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send("false", token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get health status of a specific backend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when backend with specified identifier is not found.</exception>
        public async Task GetBackendHealthRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = GetParameter(ctx, "identifier");
            List<Backend> backends = new List<Backend>(_Services.HealthCheck.Backends);
            if (backends.Any(b => b.Identifier.Equals(identifier)))
            {
                Backend backend = backends.First(b => b.Identifier.Equals(identifier));
                await ctx.Response.Send(_Serializer.SerializeJson(backend, true), token).ConfigureAwait(false);
            }
            else
            {
                throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");
            }
        }

        /// <summary>
        /// Delete a backend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when backend with specified identifier is not found.</exception>
        public async Task DeleteBackendRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = GetParameter(ctx, "identifier");
            if (!_Services.Backend.Exists(identifier)) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");
            if (_Services.Backend.Delete(identifier, false))
            {
                ctx.Response.StatusCode = 204;
                await ctx.Response.Send(token).ConfigureAwait(false);
            }
            else
                throw new InvalidOperationException("The specified backend is linked and cannot be deleted.");
        }

        /// <summary>
        /// Create a new backend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="ArgumentException">Thrown when backend with specified identifier already exists.</exception>
        public async Task CreateBackendRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            Backend obj = _Serializer.DeserializeJson<Backend>(ctx.Request.DataAsString);
            Backend existing = _Services.Backend.GetByIdentifier(obj.Identifier);
            if (existing != null) throw new ArgumentException("An object with identifier " + obj.Identifier + " already exists.");

            Backend created = _Services.Backend.Create(obj);
            ctx.Response.StatusCode = 201;
            await ctx.Response.Send(_Serializer.SerializeJson(created, true), token).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing backend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when backend with specified identifier is not found.</exception>
        public async Task UpdateBackendRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = GetParameter(ctx, "identifier");
            Backend original = _Services.Backend.GetByIdentifier(identifier);
            if (original == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");

            Backend updated = _Serializer.DeserializeJson<Backend>(ctx.Request.DataAsString);
            updated.Identifier = identifier;
            updated = _Services.Backend.Update(updated);

            await ctx.Response.Send(_Serializer.SerializeJson(updated, true), token).ConfigureAwait(false);
        }

        private string GetParameter(HttpContextBase ctx, string parameter)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            return req.UrlContext.GetParameter(parameter);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _Disposed = true;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}