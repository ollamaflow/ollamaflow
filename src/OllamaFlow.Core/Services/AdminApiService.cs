namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Serialization;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Administrative API service for managing frontends, backends, and sessions.
    /// </summary>
    public class AdminApiService
    {
        #region Private-Members

        private readonly string _Header = "[AdminApiService] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private FrontendService _FrontendService = null;
        private BackendService _BackendService = null;
        private HealthCheckService _HealthCheck = null;
        private ModelSynchronizationService _ModelSynchronization = null;
        private SessionStickinessService _SessionStickiness = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="frontendService">Frontend service.</param>
        /// <param name="backendService">Backend service.</param>
        /// <param name="healthCheck">Health check service.</param>
        /// <param name="modelSynchronization">Model synchronization service.</param>
        /// <param name="sessionStickiness">Session stickiness service.</param>
        public AdminApiService(
            OllamaFlowSettings settings,
            LoggingModule logging,
            Serializer serializer,
            FrontendService frontendService,
            BackendService backendService,
            HealthCheckService healthCheck,
            ModelSynchronizationService modelSynchronization,
            SessionStickinessService sessionStickiness)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _FrontendService = frontendService ?? throw new ArgumentNullException(nameof(frontendService));
            _BackendService = backendService ?? throw new ArgumentNullException(nameof(backendService));
            _HealthCheck = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
            _ModelSynchronization = modelSynchronization ?? throw new ArgumentNullException(nameof(modelSynchronization));
            _SessionStickiness = sessionStickiness ?? throw new ArgumentNullException(nameof(sessionStickiness));

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Check if the request is authenticated with a valid bearer token.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>True if authenticated, false otherwise.</returns>
        public bool IsAuthenticated(HttpContextBase ctx)
        {
            if (ctx.Request.Authorization != null && !String.IsNullOrEmpty(ctx.Request.Authorization.BearerToken))
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
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        public async Task GetFrontendsRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            List<Frontend> objs = _FrontendService.GetAll().ToList();
            await ctx.Response.Send(_Serializer.SerializeJson(objs, true));
        }

        /// <summary>
        /// Get a specific frontend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when frontend with specified identifier is not found.</exception>
        public async Task GetFrontendRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = ctx.Request.Url.Parameters["identifier"];
            Frontend obj = _FrontendService.GetByIdentifier(identifier);
            if (obj == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");
            await ctx.Response.Send(_Serializer.SerializeJson(obj, true));
        }

        /// <summary>
        /// Delete a frontend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when frontend with specified identifier is not found.</exception>
        public async Task DeleteFrontendRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = ctx.Request.Url.Parameters["identifier"];
            if (!_FrontendService.Exists(identifier)) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");

            _FrontendService.Delete(identifier);

            // Notify all services of frontend removal
            _HealthCheck.RemoveFrontend(identifier);
            _ModelSynchronization.RemoveFrontend(identifier);

            ctx.Response.StatusCode = 204;
            await ctx.Response.Send();
        }

        /// <summary>
        /// Create a new frontend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="ArgumentException">Thrown when frontend with specified identifier already exists.</exception>
        public async Task CreateFrontendRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            Frontend obj = _Serializer.DeserializeJson<Frontend>(ctx.Request.DataAsString);
            Frontend existing = _FrontendService.GetByIdentifier(obj.Identifier);
            if (existing != null) throw new ArgumentException("An object with identifier " + obj.Identifier + " already exists.");

            Frontend created = _FrontendService.Create(obj);

            // Notify all services of new frontend
            _HealthCheck.AddFrontend(created);
            _ModelSynchronization.AddFrontend(created);

            ctx.Response.StatusCode = 201;
            await ctx.Response.Send(_Serializer.SerializeJson(created, true));
        }

        /// <summary>
        /// Update an existing frontend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when frontend with specified identifier is not found.</exception>
        public async Task UpdateFrontendRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = ctx.Request.Url.Parameters["identifier"];
            Frontend original = _FrontendService.GetByIdentifier(identifier);
            if (original == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");

            Frontend updated = _Serializer.DeserializeJson<Frontend>(ctx.Request.DataAsString);
            updated.Identifier = identifier;
            updated = _FrontendService.Update(updated);

            // Notify all services of frontend update
            _HealthCheck.UpdateFrontend(updated);
            _ModelSynchronization.UpdateFrontend(updated);

            await ctx.Response.Send(_Serializer.SerializeJson(updated, true));
        }

        /// <summary>
        /// Get all backends.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        public async Task GetBackendsRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            List<Backend> objs = _BackendService.GetAll().ToList();
            await ctx.Response.Send(_Serializer.SerializeJson(objs, true));
        }

        /// <summary>
        /// Get health status of all backends.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        public async Task GetBackendsHealthRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            List<Backend> objs = new List<Backend>(_HealthCheck.Backends);
            await ctx.Response.Send(_Serializer.SerializeJson(objs, true));
        }

        /// <summary>
        /// Get a specific backend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when backend with specified identifier is not found.</exception>
        public async Task GetBackendRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = ctx.Request.Url.Parameters["identifier"];
            Backend obj = _BackendService.GetByIdentifier(identifier);
            if (obj == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");
            await ctx.Response.Send(_Serializer.SerializeJson(obj, true));
        }

        /// <summary>
        /// Get health status of a specific backend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when backend with specified identifier is not found.</exception>
        public async Task GetBackendHealthRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = ctx.Request.Url.Parameters["identifier"];
            List<Backend> backends = new List<Backend>(_HealthCheck.Backends);
            if (backends.Any(b => b.Identifier.Equals(identifier)))
            {
                Backend backend = backends.First(b => b.Identifier.Equals(identifier));
                await ctx.Response.Send(_Serializer.SerializeJson(backend, true));
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
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when backend with specified identifier is not found.</exception>
        public async Task DeleteBackendRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = ctx.Request.Url.Parameters["identifier"];
            if (!_BackendService.Exists(identifier)) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");

            _BackendService.Delete(identifier);

            // Notify all services of backend removal
            _HealthCheck.RemoveBackend(identifier);
            _ModelSynchronization.RemoveBackend(identifier);

            ctx.Response.StatusCode = 204;
            await ctx.Response.Send();
        }

        /// <summary>
        /// Create a new backend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="ArgumentException">Thrown when backend with specified identifier already exists.</exception>
        public async Task CreateBackendRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            Backend obj = _Serializer.DeserializeJson<Backend>(ctx.Request.DataAsString);
            Backend existing = _BackendService.GetByIdentifier(obj.Identifier);
            if (existing != null) throw new ArgumentException("An object with identifier " + obj.Identifier + " already exists.");

            Backend created = _BackendService.Create(obj);

            // Notify all services of new backend
            _HealthCheck.AddBackend(created);
            _ModelSynchronization.AddBackend(created);

            ctx.Response.StatusCode = 201;
            await ctx.Response.Send(_Serializer.SerializeJson(created, true));
        }

        /// <summary>
        /// Update an existing backend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when backend with specified identifier is not found.</exception>
        public async Task UpdateBackendRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = ctx.Request.Url.Parameters["identifier"];
            Backend original = _BackendService.GetByIdentifier(identifier);
            if (original == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");

            Backend updated = _Serializer.DeserializeJson<Backend>(ctx.Request.DataAsString);
            updated.Identifier = identifier;
            updated = _BackendService.Update(updated);

            // Notify all services of backend update
            _HealthCheck.UpdateBackend(updated);
            _ModelSynchronization.UpdateBackend(updated);

            await ctx.Response.Send(_Serializer.SerializeJson(updated, true));
        }

        /// <summary>
        /// Get all sticky sessions.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        public async Task GetSessionsRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            List<Models.StickySession> sessions = _SessionStickiness.GetAllSessions();
            await ctx.Response.Send(_Serializer.SerializeJson(sessions, true));
        }

        /// <summary>
        /// Get sticky sessions for a specific client.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        public async Task GetClientSessionsRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string clientId = ctx.Request.Url.Parameters["clientId"];
            List<Models.StickySession> sessions = _SessionStickiness.GetClientSessions(clientId);
            await ctx.Response.Send(_Serializer.SerializeJson(sessions, true));
        }

        /// <summary>
        /// Delete all sticky sessions for a specific client.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        public async Task DeleteClientSessionsRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string clientId = ctx.Request.Url.Parameters["clientId"];
            int removedCount = _SessionStickiness.RemoveClientSessions(clientId);

            var result = new
            {
                clientId = clientId,
                removedSessionCount = removedCount,
                message = $"Removed {removedCount} sessions for client {clientId}"
            };

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(result, true));
        }

        /// <summary>
        /// Delete all sticky sessions.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when request is not authenticated.</exception>
        public async Task DeleteAllSessionsRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            int removedCount = _SessionStickiness.ClearAllSessions();

            var result = new
            {
                removedSessionCount = removedCount,
                message = $"Removed all {removedCount} sessions"
            };

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(result, true));
        }

        #endregion
    }
}