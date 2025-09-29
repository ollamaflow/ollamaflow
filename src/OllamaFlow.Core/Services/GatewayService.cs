namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Serialization;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Gateway service responsible for coordinating request processing and routing.
    /// </summary>
    public class GatewayService : IDisposable
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Private-Members

        private readonly string _Header = "[GatewayService] ";
        private OllamaFlowSettings _Settings = null;
        private OllamaFlowCallbacks _Callbacks = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private bool _IsDisposed = false;

        // Core services
        private FrontendService _FrontendService = null;
        private BackendService _BackendService = null;
        private HealthCheckService _HealthCheck = null;
        private ModelSynchronizationService _ModelSynchronization = null;
        private SessionStickinessService _SessionStickiness = null;

        // Extracted services
        private AdminApiService _AdminApiService = null;
        private StaticRouteHandler _StaticRouteHandler = null;
        private ProxyService _ProxyService = null;
        private RequestProcessorService _RequestProcessorService = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="callbacks">Callbacks.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="frontend">Frontend service.</param>
        /// <param name="backend">Backend service.</param>
        /// <param name="healthCheck">Healthcheck service.</param>
        /// <param name="modelSynchronization">Model synchronization service.</param>
        /// <param name="sessionStickiness">Session stickiness service.</param>
        /// <param name="adminApiService">Admin API service.</param>
        /// <param name="staticRouteHandler">Static route handler.</param>
        /// <param name="proxyService">Proxy service.</param>
        /// <param name="requestProcessorService">Request processor service.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public GatewayService(
            OllamaFlowSettings settings,
            OllamaFlowCallbacks callbacks,
            LoggingModule logging,
            Serializer serializer,
            FrontendService frontend,
            BackendService backend,
            HealthCheckService healthCheck,
            ModelSynchronizationService modelSynchronization,
            SessionStickinessService sessionStickiness,
            AdminApiService adminApiService,
            StaticRouteHandler staticRouteHandler,
            ProxyService proxyService,
            RequestProcessorService requestProcessorService,
            CancellationTokenSource tokenSource = default)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _TokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
            _FrontendService = frontend ?? throw new ArgumentNullException(nameof(frontend));
            _BackendService = backend ?? throw new ArgumentNullException(nameof(backend));
            _HealthCheck = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
            _ModelSynchronization = modelSynchronization ?? throw new ArgumentNullException(nameof(modelSynchronization));
            _SessionStickiness = sessionStickiness ?? throw new ArgumentNullException(nameof(sessionStickiness));
            _AdminApiService = adminApiService ?? throw new ArgumentNullException(nameof(adminApiService));
            _StaticRouteHandler = staticRouteHandler ?? throw new ArgumentNullException(nameof(staticRouteHandler));
            _ProxyService = proxyService ?? throw new ArgumentNullException(nameof(proxyService));
            _RequestProcessorService = requestProcessorService ?? throw new ArgumentNullException(nameof(requestProcessorService));

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose of the object.
        /// </summary>
        public void Dispose()
        {
            if (!_IsDisposed)
            {
                _IsDisposed = true;
                _Logging.Debug(_Header + "dispose");
            }
        }

        /// <summary>
        /// Default route handler - main entry point for AI API requests.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task DefaultRoute(HttpContextBase ctx)
        {
            Guid requestGuid = Guid.NewGuid();
            ctx.Response.Headers.Add(Constants.RequestIdHeader, requestGuid.ToString());
            ctx.Response.Headers.Add(Constants.ForwardedForHeader, ctx.Request.Source.IpAddress);
            ctx.Response.Headers.Add(Constants.ExposeHeadersHeader, "*");

            // Detect request type and API format once for efficiency
            RequestTypeEnum requestType = RequestTypeHelper.GetRequestTypeFromRequest(ctx.Request.Method, ctx.Request.Url.RawWithQuery);
            ApiFormatEnum apiFormat = RequestTypeHelper.GetApiFormatFromRequest(ctx.Request.Method, ctx.Request.Url.RawWithQuery);

            // Initialize telemetry with all known information upfront
            TelemetryMessage telemetry = new TelemetryMessage
            {
                ClientId = ctx.Request.Source.IpAddress,
                RequestBodySize = ctx.Request.ContentLength,
                RequestArrivalUtc = DateTime.UtcNow,
                RequestType = requestType,
                ApiFormat = apiFormat
            };

            try
            {
                // Handle admin API requests directly - don't try to transform them
                if (apiFormat == ApiFormatEnum.Admin)
                {
                    await HandleAdminApiRequest(ctx);
                    return;
                }

                Frontend frontend = await GetFrontend(ctx);
                if (frontend == null)
                {
                    _Logging.Warn(_Header + "no frontend found for " + ctx.Request.Method.ToString() + " " + ctx.Request.Url.Full);
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "No matching API endpoint found"), true));
                    return;
                }

                // Handle model pull requests - add to required models for synchronization
                if (requestType == RequestTypeEnum.PullModel)
                {
                    string model = RequestTypeHelper.GetModelFromRequest(ctx.Request, requestType);
                    if (!String.IsNullOrEmpty(model))
                    {
                        lock (frontend.Lock)
                        {
                            if (!frontend.RequiredModels.Contains(model))
                            {
                                frontend.RequiredModels.Add(model);
                            }
                        }
                    }
                }

                // Handle model delete requests - remove from required models
                if (requestType == RequestTypeEnum.DeleteModel)
                {
                    string model = RequestTypeHelper.GetModelFromRequest(ctx.Request, requestType);
                    if (!String.IsNullOrEmpty(model))
                    {
                        lock (frontend.Lock)
                        {
                            if (frontend.RequiredModels.Contains(model))
                            {
                                frontend.RequiredModels.Remove(model);
                            }
                        }
                    }
                }

                // Get client identifier and select backend
                string clientId = GetClientIdentifier(ctx);
                Backend backend = _HealthCheck.GetNextBackend(frontend, clientId);

                if (backend == null)
                {
                    _Logging.Warn(_Header + "no healthy backend found for frontend " + frontend.Identifier);
                    ctx.Response.StatusCode = 502;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadGateway, null, "No healthy backend servers available"), true));
                    return;
                }

                telemetry.BackendServerId = backend.Identifier;
                telemetry.BackendSelectedUtc = DateTime.UtcNow;

                // Set response headers for backend information
                ctx.Response.Headers.Add(Constants.StickyServerHeader, backend.IsSticky.ToString());

                // Check request body size limits
                if (frontend.MaxRequestBodySize > 0 && ctx.Request.ContentLength > frontend.MaxRequestBodySize)
                {
                    _Logging.Warn(_Header + "request too large from " + ctx.Request.Source.IpAddress + ": " + ctx.Request.ContentLength + " bytes");
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.TooLarge, null, "Your request was too large"), true));
                    return;
                }

                // Check rate limiting
                int totalRequests =
                    Volatile.Read(ref backend._ActiveRequests) +
                    Volatile.Read(ref backend._PendingRequests);

                if (totalRequests > backend.RateLimitRequestsThreshold)
                {
                    _Logging.Warn(_Header + "too many active requests for backend " + backend.Identifier + ", sending 429 response to request from " + ctx.Request.Source.IpAddress);
                    ctx.Response.StatusCode = 429;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.SlowDown)));
                    return;
                }

                Interlocked.Increment(ref backend._PendingRequests);

                // Process request using the RequestProcessorService
                bool responseReceived = await _RequestProcessorService.ProcessRequestWithTransformationAsync(
                    requestGuid,
                    ctx,
                    frontend,
                    backend,
                    clientId,
                    telemetry);

                if (!responseReceived)
                {
                    _Logging.Warn(_Header + "no response or exception from " + backend.Identifier + " for Ollama endpoint " + frontend.Identifier);
                    ctx.Response.StatusCode = 502;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadGateway), true));
                    return;
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception:" + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send();
            }
            finally
            {
                _Logging.Debug(_Header + _Serializer.SerializeJson(telemetry, false));
            }
        }

        /// <summary>
        /// OPTIONS route handler for CORS support.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task OptionsRoute(HttpContextBase ctx)
        {
            NameValueCollection responseHeaders = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            string[] requestedHeaders = null;
            string headers = "";

            if (ctx.Request.Headers != null)
            {
                for (int i = 0; i < ctx.Request.Headers.Count; i++)
                {
                    string key = ctx.Request.Headers.GetKey(i);
                    string value = ctx.Request.Headers.Get(i);
                    if (String.IsNullOrEmpty(key)) continue;
                    if (String.IsNullOrEmpty(value)) continue;
                    if (String.Compare(key.ToLower(), "access-control-request-headers") == 0)
                    {
                        requestedHeaders = value.Split(',');
                        break;
                    }
                }
            }

            if (requestedHeaders != null)
            {
                foreach (string curr in requestedHeaders)
                {
                    headers += ", " + curr;
                }
            }

            responseHeaders.Add("Access-Control-Allow-Methods", "OPTIONS, HEAD, GET, PUT, POST, DELETE");
            responseHeaders.Add("Access-Control-Allow-Headers", "*, Content-Type, X-Requested-With, " + headers);
            responseHeaders.Add("Access-Control-Expose-Headers", "Content-Type, X-Requested-With, " + headers);
            responseHeaders.Add("Access-Control-Allow-Origin", "*");
            responseHeaders.Add("Accept", "*/*");
            responseHeaders.Add("Accept-Language", "en-US, en");
            responseHeaders.Add("Accept-Charset", "ISO-8859-1, utf-8");
            responseHeaders.Add("Connection", "keep-alive");

            ctx.Response.StatusCode = 200;
            ctx.Response.Headers = responseHeaders;
            await ctx.Response.Send();
        }

        /// <summary>
        /// Authentication route handler.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task AuthenticationRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 401;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Unauthorized access"), true));
        }

        /// <summary>
        /// Pre-routing handler for request setup.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task PreRoutingHandler(HttpContextBase ctx)
        {
            ctx.Response.ContentType = Constants.JsonContentType;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Post-routing handler for cleanup.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task PostRoutingHandler(HttpContextBase ctx)
        {
            ctx.Timestamp.End = DateTime.UtcNow;

            _Logging.Debug(
                _Header +
                ctx.Request.Source.IpAddress + " " +
                ctx.Request.Method.ToString() + " " + ctx.Request.Url.RawWithQuery + ": " +
                ctx.Response.StatusCode +
                " (" + ctx.Timestamp.TotalMs.Value.ToString("F2") + "ms)");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Exception route handler for unhandled exceptions.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="e">Exception.</param>
        /// <returns>Task.</returns>
        public async Task ExceptionRoute(HttpContextBase ctx, Exception e)
        {
            _Logging.Warn(_Header + "exception of type " + e.GetType().Name + " encountered:" + Environment.NewLine + e.ToString());

            switch (e)
            {
                case ArgumentNullException:
                case ArgumentException:
                case InvalidOperationException:
                case JsonException:
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, e.Message), true));
                    return;
                case KeyNotFoundException:
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, e.Message), true));
                    return;
                case UnauthorizedAccessException:
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.AuthenticationFailed, null), true));
                    return;
                default:
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message), true));
                    return;
            }
        }

        // Static route delegates - delegated to StaticRouteHandler

        /// <summary>
        /// GET route for root endpoint (/).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetRootRoute(HttpContextBase ctx) => await _StaticRouteHandler.GetRootRoute(ctx);

        /// <summary>
        /// HEAD route for root endpoint (/).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task HeadRootRoute(HttpContextBase ctx) => await _StaticRouteHandler.HeadRootRoute(ctx);

        /// <summary>
        /// GET route for favicon.ico.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetFaviconRoute(HttpContextBase ctx) => await _StaticRouteHandler.GetFaviconRoute(ctx);

        /// <summary>
        /// HEAD route for favicon.ico.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task HeadFaviconRoute(HttpContextBase ctx) => await _StaticRouteHandler.HeadFaviconRoute(ctx);

        // Admin API route delegates - delegated to AdminApiService

        /// <summary>
        /// GET route for retrieving all frontends.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetFrontendsRoute(HttpContextBase ctx) => await _AdminApiService.GetFrontendsRoute(ctx);

        /// <summary>
        /// GET route for retrieving a specific frontend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetFrontendRoute(HttpContextBase ctx) => await _AdminApiService.GetFrontendRoute(ctx);

        /// <summary>
        /// DELETE route for removing a frontend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task DeleteFrontendRoute(HttpContextBase ctx) => await _AdminApiService.DeleteFrontendRoute(ctx);

        /// <summary>
        /// PUT route for creating a new frontend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task CreateFrontendRoute(HttpContextBase ctx) => await _AdminApiService.CreateFrontendRoute(ctx);

        /// <summary>
        /// PUT route for updating an existing frontend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task UpdateFrontendRoute(HttpContextBase ctx) => await _AdminApiService.UpdateFrontendRoute(ctx);

        /// <summary>
        /// GET route for retrieving all backends.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetBackendsRoute(HttpContextBase ctx) => await _AdminApiService.GetBackendsRoute(ctx);

        /// <summary>
        /// GET route for retrieving health status of all backends.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetBackendsHealthRoute(HttpContextBase ctx) => await _AdminApiService.GetBackendsHealthRoute(ctx);

        /// <summary>
        /// GET route for retrieving a specific backend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetBackendRoute(HttpContextBase ctx) => await _AdminApiService.GetBackendRoute(ctx);

        /// <summary>
        /// GET route for retrieving health status of a specific backend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetBackendHealthRoute(HttpContextBase ctx) => await _AdminApiService.GetBackendHealthRoute(ctx);

        /// <summary>
        /// DELETE route for removing a backend by identifier.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task DeleteBackendRoute(HttpContextBase ctx) => await _AdminApiService.DeleteBackendRoute(ctx);

        /// <summary>
        /// PUT route for creating a new backend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task CreateBackendRoute(HttpContextBase ctx) => await _AdminApiService.CreateBackendRoute(ctx);

        /// <summary>
        /// PUT route for updating an existing backend.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task UpdateBackendRoute(HttpContextBase ctx) => await _AdminApiService.UpdateBackendRoute(ctx);

        /// <summary>
        /// GET route for retrieving all active sessions.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetSessionsRoute(HttpContextBase ctx) => await _AdminApiService.GetSessionsRoute(ctx);

        /// <summary>
        /// GET route for retrieving sessions for a specific client.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetClientSessionsRoute(HttpContextBase ctx) => await _AdminApiService.GetClientSessionsRoute(ctx);

        /// <summary>
        /// DELETE route for removing sessions for a specific client.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task DeleteClientSessionsRoute(HttpContextBase ctx) => await _AdminApiService.DeleteClientSessionsRoute(ctx);

        /// <summary>
        /// DELETE route for removing all active sessions.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task DeleteAllSessionsRoute(HttpContextBase ctx) => await _AdminApiService.DeleteAllSessionsRoute(ctx);

        #endregion

        #region Private-Methods

        /// <summary>
        /// Handle admin API requests by routing to appropriate handlers.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        private async Task HandleAdminApiRequest(HttpContextBase ctx)
        {
            string path = ctx.Request.Url.RawWithoutQuery;
            string method = ctx.Request.Method.ToString().ToUpper();

            // Route to appropriate admin handler based on path and method
            try
            {
                switch (path)
                {
                    case "/v1.0/frontends":
                        if (method == "GET") await GetFrontendsRoute(ctx);
                        else if (method == "PUT") await CreateFrontendRoute(ctx);
                        else await SendMethodNotAllowed(ctx);
                        break;

                    case "/v1.0/backends":
                        if (method == "GET") await GetBackendsRoute(ctx);
                        else if (method == "PUT") await CreateBackendRoute(ctx);
                        else await SendMethodNotAllowed(ctx);
                        break;

                    case "/v1.0/backends/health":
                        if (method == "GET") await GetBackendsHealthRoute(ctx);
                        else await SendMethodNotAllowed(ctx);
                        break;

                    case "/v1.0/sessions":
                        if (method == "GET") await GetSessionsRoute(ctx);
                        else if (method == "DELETE") await DeleteAllSessionsRoute(ctx);
                        else await SendMethodNotAllowed(ctx);
                        break;

                    default:
                        // Handle dynamic paths like /v1.0/frontends/{id}, /v1.0/backends/{id}, etc.
                        await HandleDynamicAdminPath(ctx, path, method);
                        break;
                }
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"error handling admin API request: {ex.Message}");
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError), true));
            }
        }

        /// <summary>
        /// Handle dynamic admin API paths with parameters.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="path">Request path.</param>
        /// <param name="method">HTTP method.</param>
        /// <returns>Task.</returns>
        private async Task HandleDynamicAdminPath(HttpContextBase ctx, string path, string method)
        {
            if (path.StartsWith("/v1.0/frontends/"))
            {
                string identifier = ExtractIdentifierFromPath(path, "/v1.0/frontends/");
                if (string.IsNullOrEmpty(identifier))
                {
                    await SendNotFound(ctx);
                    return;
                }

                SetUrlParameter(ctx, "identifier", identifier);

                if (method == "GET") await GetFrontendRoute(ctx);
                else if (method == "PUT") await UpdateFrontendRoute(ctx);
                else if (method == "DELETE") await DeleteFrontendRoute(ctx);
                else await SendMethodNotAllowed(ctx);
            }
            else if (path.StartsWith("/v1.0/backends/"))
            {
                if (path.EndsWith("/health"))
                {
                    string identifier = ExtractIdentifierFromPath(path, "/v1.0/backends/", "/health");
                    if (string.IsNullOrEmpty(identifier))
                    {
                        await SendNotFound(ctx);
                        return;
                    }

                    SetUrlParameter(ctx, "identifier", identifier);

                    if (method == "GET") await GetBackendHealthRoute(ctx);
                    else await SendMethodNotAllowed(ctx);
                }
                else
                {
                    string identifier = ExtractIdentifierFromPath(path, "/v1.0/backends/");
                    if (string.IsNullOrEmpty(identifier))
                    {
                        await SendNotFound(ctx);
                        return;
                    }

                    SetUrlParameter(ctx, "identifier", identifier);

                    if (method == "GET") await GetBackendRoute(ctx);
                    else if (method == "PUT") await UpdateBackendRoute(ctx);
                    else if (method == "DELETE") await DeleteBackendRoute(ctx);
                    else await SendMethodNotAllowed(ctx);
                }
            }
            else if (path.StartsWith("/v1.0/sessions/"))
            {
                string identifier = ExtractIdentifierFromPath(path, "/v1.0/sessions/");
                if (string.IsNullOrEmpty(identifier))
                {
                    await SendNotFound(ctx);
                    return;
                }

                SetUrlParameter(ctx, "identifier", identifier);

                if (method == "GET") await GetClientSessionsRoute(ctx);
                else if (method == "DELETE") await DeleteClientSessionsRoute(ctx);
                else await SendMethodNotAllowed(ctx);
            }
            else
            {
                await SendNotFound(ctx);
            }
        }

        /// <summary>
        /// Extract identifier from URL path.
        /// </summary>
        /// <param name="path">The full URL path.</param>
        /// <param name="prefix">The prefix to remove.</param>
        /// <param name="suffix">Optional suffix to remove.</param>
        /// <returns>The extracted identifier.</returns>
        private string ExtractIdentifierFromPath(string path, string prefix, string suffix = null)
        {
            if (!path.StartsWith(prefix)) return null;

            string identifier = path.Substring(prefix.Length);

            if (!string.IsNullOrEmpty(suffix) && identifier.EndsWith(suffix))
            {
                identifier = identifier.Substring(0, identifier.Length - suffix.Length);
            }

            return identifier;
        }

        /// <summary>
        /// Set a URL parameter on the HTTP context for the admin API handlers.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="key">Parameter key.</param>
        /// <param name="value">Parameter value.</param>
        private void SetUrlParameter(HttpContextBase ctx, string key, string value)
        {
            if (ctx.Request.Url.Parameters == null) ctx.Request.Url.Parameters = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
            ctx.Request.Url.Parameters[key] = value;
        }

        /// <summary>
        /// Send a 405 Method Not Allowed response.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        private async Task SendMethodNotAllowed(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 405;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "Method not allowed"), true));
        }

        /// <summary>
        /// Send a 404 Not Found response.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        private async Task SendNotFound(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, "Endpoint not found"), true));
        }

        /// <summary>
        /// Get the frontend configuration based on the request context.
        /// Matches frontends by hostname, with "*" serving as a catch-all.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Frontend configuration or null if not found.</returns>
        private async Task<Frontend> GetFrontend(HttpContextBase ctx)
        {
            await Task.CompletedTask;

            if (String.IsNullOrEmpty(ctx.Request.Url.Full))
            {
                _Logging.Warn(_Header + "no URL found in request");
                return null;
            }

            // Extract hostname from the request
            string requestHostname = ctx.Request.Url.Host;
            if (String.IsNullOrEmpty(requestHostname))
            {
                _Logging.Warn(_Header + "no hostname found in request");
                return null;
            }

            List<Frontend> allFrontends = _FrontendService.GetAll().ToList();
            if (allFrontends.Count == 0)
            {
                _Logging.Warn(_Header + "no frontends configured");
                return null;
            }

            // First, try to find an exact hostname match
            Frontend exactMatch = allFrontends.FirstOrDefault(f =>
                !String.IsNullOrEmpty(f.Hostname) &&
                f.Hostname.Equals(requestHostname, StringComparison.InvariantCultureIgnoreCase));

            if (exactMatch != null)
            {
                _Logging.Debug(_Header + "found exact hostname match in frontend " + exactMatch.Identifier + " for hostname " + requestHostname);
                return exactMatch;
            }

            // If no exact match, look for a catch-all frontend with "*"
            if (allFrontends.Any(f => !String.IsNullOrEmpty(f.Hostname) && f.Hostname.Equals("*")))
            {
                Frontend catchAll = allFrontends.First(f =>
                    !String.IsNullOrEmpty(f.Hostname) &&
                    f.Hostname == "*");

                _Logging.Debug(_Header + "using catch-all frontend " + catchAll.Identifier + " for hostname " + requestHostname);
                return catchAll;
            }

            // No matching frontend found
            _Logging.Warn(_Header + "no frontend found for hostname: " + requestHostname);
            return null;
        }

        /// <summary>
        /// Get client identifier from the request context.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Client identifier.</returns>
        private string GetClientIdentifier(HttpContextBase ctx)
        {
            string ret = ctx.Request.Source.IpAddress;
            if (ctx.Request.Headers != null)
            {
                foreach (string stickyHeader in _Settings.StickyHeaders)
                {
                    string value = ctx.Request.Headers[stickyHeader]; // NameValueCollection lookups are case-insensitive
                    if (!String.IsNullOrEmpty(value)) return value;
                }
            }
            return ret;
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}