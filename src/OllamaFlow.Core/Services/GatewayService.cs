namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using RestWrapper;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Core.Services.Transformation.Interfaces;
    using OllamaFlow.Core.Services.Transformation;
    using SyslogLogging;
    using Timestamps;
    using WatsonWebserver.Core;

    /// <summary>
    /// Gateway service.
    /// </summary>
    public class GatewayService : IDisposable
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[GatewayService] ";
        private OllamaFlowSettings _Settings = null;
        private OllamaFlowCallbacks _Callbacks = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private Random _Random = new Random(Guid.NewGuid().GetHashCode());
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private bool _IsDisposed = false;

        private FrontendService _FrontendService = null;
        private BackendService _BackendService = null;
        private HealthCheckService _HealthCheck = null;
        private ModelSynchronizationService _ModelSynchronization = null;
        private SessionStickinessService _SessionStickiness = null;
        private ITransformationPipeline _TransformationPipeline = null;

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
        /// <param name="transformationPipeline">Transformation pipeline for API format conversion.</param>
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
            ITransformationPipeline transformationPipeline,
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
            _TransformationPipeline = transformationPipeline ?? throw new ArgumentNullException(nameof(transformationPipeline));

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    _Random = null;
                    _Serializer = null;
                    _Logging = null;
                    _Settings = null;
                }

                _IsDisposed = true;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initialize routes.
        /// </summary>
        /// <param name="webserver">Webserver.</param>
        public void InitializeRoutes(WebserverBase webserver)
        {
            webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/", GetRootRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Static.Add(HttpMethod.HEAD, "/", HeadRootRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/favicon.ico", GetFaviconRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Static.Add(HttpMethod.HEAD, "/favicon.ico", HeadFaviconRoute, ExceptionRoute);

            webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/frontends", GetFrontendsRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/frontends/{identifier}", GetFrontendRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/frontends/{identifier}", DeleteFrontendRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/frontends", CreateFrontendRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/frontends/{identifier}", UpdateFrontendRoute, ExceptionRoute);

            webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/backends", GetBackendsRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/backends/health", GetBackendsHealthRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/backends/{identifier}", GetBackendRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/backends/{identifier}/health", GetBackendHealthRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/backends/{identifier}", DeleteBackendRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/backends", CreateBackendRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/backends/{identifier}", UpdateBackendRoute, ExceptionRoute);

            webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/sessions", GetSessionsRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/sessions/{clientId}", GetClientSessionsRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/sessions/{clientId}", DeleteClientSessionsRoute, ExceptionRoute);
            webserver.Routes.PreAuthentication.Static.Add(HttpMethod.DELETE, "/v1.0/sessions", DeleteAllSessionsRoute, ExceptionRoute);
        }

        /// <summary>
        /// Authentication route.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task AuthenticationRoute(HttpContextBase ctx)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Route for handling OPTIONS requests.
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
            return;
        }

        /// <summary>
        /// Pre-routing handler.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task PreRoutingHandler(HttpContextBase ctx)
        {
            ctx.Response.ContentType = Constants.JsonContentType;
        }

        /// <summary>
        /// Post-routing handler.
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
        }

        /// <summary>
        /// Exception route.
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

        /// <summary>
        /// Default request handler.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task DefaultRoute(HttpContextBase ctx)
        {
            Guid requestGuid = Guid.NewGuid();
            ctx.Response.Headers.Add(Constants.RequestIdHeader, requestGuid.ToString());
            ctx.Response.Headers.Add(Constants.ForwardedForHeader, ctx.Request.Source.IpAddress);
            ctx.Response.Headers.Add(Constants.ExposeHeadersHeader, "*");

            TelemetryMessage telemetry = new TelemetryMessage
            {
                ClientId = ctx.Request.Source.IpAddress,
                RequestBodySize = ctx.Request.ContentLength,
                RequestArrivalUtc = DateTime.UtcNow
            };

            try
            {
                Frontend frontend = await GetFrontend(ctx);
                if (frontend == null)
                {
                    _Logging.Warn(_Header + "no frontend found for " + ctx.Request.Method.ToString() + " " + ctx.Request.Url.Full);
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "No matching API endpoint found"), true));
                    return;
                }

                RequestTypeEnum requestType = RequestTypeHelper.DetermineRequestType(ctx.Request.Method, ctx.Request.Url.RawWithQuery);
                telemetry.RequestType = requestType;

                if (requestType == RequestTypeEnum.PullModel)
                {
                    string model = Helpers.RequestTypeHelper.GetModelFromRequest(ctx.Request, requestType);

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

                if (requestType == RequestTypeEnum.DeleteModel)
                {
                    string model = Helpers.RequestTypeHelper.GetModelFromRequest(ctx.Request, requestType);

                    if (!String.IsNullOrEmpty(model))
                    {
                        lock (frontend.Lock)
                        {
                            if (!frontend.RequiredModels.Contains(model))
                            {
                                frontend.RequiredModels.Remove(model);
                            }
                        }
                    }
                }

                string clientId = GetClientIdentifier(ctx);
                telemetry.ClientId = clientId;

                Backend backend = _HealthCheck.GetNextBackend(frontend, clientId);
                if (backend == null)
                {
                    _Logging.Warn(_Header + "no backend found for " + ctx.Request.Method.ToString() + " " + ctx.Request.Url.RawWithoutQuery);
                    ctx.Response.StatusCode = 502;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadGateway, null, "No backend servers are available to service your request"), true));
                    return;
                }
                else
                {
                    telemetry.BackendServerId = backend.Identifier;
                    telemetry.BackendSelectedUtc = DateTime.UtcNow;
                }

                ctx.Response.Headers.Add(Constants.StickyServerHeader, backend.IsSticky.ToString());

                if (frontend.MaxRequestBodySize > 0 && ctx.Request.ContentLength > frontend.MaxRequestBodySize)
                {
                    _Logging.Warn(_Header + "request too large from " + ctx.Request.Source.IpAddress + ": " + ctx.Request.ContentLength + " bytes");
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.TooLarge, null, "Your request was too large"), true));
                    return;
                }

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

                bool responseReceived = await ProcessRequestWithTransformation(
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

        #endregion

        #region Private-Methods

        /// <summary>
        /// Detect the source API format based on the request URL and headers.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Detected API format.</returns>
        private ApiFormatEnum DetectSourceApiFormat(HttpContextBase ctx)
        {
            if (ctx?.Request?.Url?.RawWithoutQuery == null)
                return ApiFormatEnum.Ollama; // Default fallback

            string path = ctx.Request.Url.RawWithoutQuery.ToLowerInvariant();

            // OpenAI API paths start with /v1/
            if (path.StartsWith("/v1/"))
                return ApiFormatEnum.OpenAI;

            // Ollama API paths start with /api/ or root endpoints
            if (path.StartsWith("/api/") || path == "/" || path == "")
                return ApiFormatEnum.Ollama;

            // Check User-Agent header for additional hints
            string userAgent = ctx.Request.Headers?["User-Agent"];
            if (!string.IsNullOrEmpty(userAgent))
            {
                if (userAgent.ToLowerInvariant().Contains("openai"))
                    return ApiFormatEnum.OpenAI;
            }

            // Default to Ollama for backward compatibility
            return ApiFormatEnum.Ollama;
        }

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

        private async Task GetRootRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.HtmlContentType;
            await ctx.Response.Send(Constants.HtmlHomepage);
        }

        private async Task HeadRootRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.TextContentType;
            await ctx.Response.Send();
        }

        private async Task GetFaviconRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.FaviconContentType;
            await ctx.Response.Send(File.ReadAllBytes(Constants.FaviconFilename));
        }

        private async Task HeadFaviconRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.FaviconContentType;
            await ctx.Response.Send();
        }

        private async Task<Frontend> GetFrontend(HttpContextBase ctx)
        {
            Uri uri = new Uri(ctx.Request.Url.Full);

            List<Frontend> frontends = _HealthCheck.Frontends;

            foreach (Frontend ep in frontends)
            {
                if (ep.Hostname.Equals("*")) return ep;
                if (ep.Hostname.Equals(uri.Host)) return ep;
            }

            _Logging.Warn(_Header + "no frontend found for host " + uri.Host);
            return null;
        }

        private System.Net.Http.HttpMethod ConvertHttpMethod(WatsonWebserver.Core.HttpMethod method)
        {
            switch (method)
            {
                case HttpMethod.CONNECT:
                    return System.Net.Http.HttpMethod.Connect;
                case HttpMethod.DELETE:
                    return System.Net.Http.HttpMethod.Delete;
                case HttpMethod.GET:
                    return System.Net.Http.HttpMethod.Get;
                case HttpMethod.HEAD:
                    return System.Net.Http.HttpMethod.Head;
                case HttpMethod.OPTIONS:
                    return System.Net.Http.HttpMethod.Options;
                case HttpMethod.PATCH:
                    return System.Net.Http.HttpMethod.Patch;
                case HttpMethod.POST:
                    return System.Net.Http.HttpMethod.Post;
                case HttpMethod.PUT:
                    return System.Net.Http.HttpMethod.Put;
                case HttpMethod.TRACE:
                    return System.Net.Http.HttpMethod.Trace;
                default:
                    throw new ArgumentException("Unknown HTTP method " + method.ToString());
            }
        }

        /// <summary>
        /// Result of a proxy request operation.
        /// </summary>
        internal class ProxyResult
        {
            /// <summary>
            /// Whether a response was received from the backend.
            /// </summary>
            public bool ResponseReceived { get; set; }

            /// <summary>
            /// The HTTP status code returned by the backend.
            /// </summary>
            public int StatusCode { get; set; }

            /// <summary>
            /// The response body as bytes.
            /// </summary>
            public byte[] ResponseBody { get; set; }

            /// <summary>
            /// The response content type.
            /// </summary>
            public string ContentType { get; set; }

            /// <summary>
            /// The response headers.
            /// </summary>
            public System.Collections.Specialized.NameValueCollection Headers { get; set; }

            /// <summary>
            /// Whether the response was sent to the client (for non-transformed responses).
            /// </summary>
            public bool AlreadySent { get; set; }
        }

        /// <summary>
        /// Process a request with retry logic for 50x errors.
        /// </summary>
        /// <param name="requestGuid">Request unique identifier.</param>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="frontend">Frontend configuration.</param>
        /// <param name="initialBackend">Initially selected backend.</param>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="telemetry">Telemetry object.</param>
        /// <returns>True if response was successfully sent, false otherwise.</returns>
        private async Task<bool> ProcessRequestWithRetry(
            Guid requestGuid,
            HttpContextBase ctx,
            Frontend frontend,
            Backend initialBackend,
            string clientId,
            TelemetryMessage telemetry)
        {
            Backend currentBackend = initialBackend;

            ProxyResult result = await ProxyRequestInternal(requestGuid, ctx, frontend, currentBackend, telemetry);

            // Check if we should retry on 50x response
            if (frontend.AllowRetries &&
                result.ResponseReceived &&
                result.StatusCode >= 500 &&
                result.StatusCode < 600)
            {
                _Logging.Info(_Header + "received " + result.StatusCode + " response from backend " + currentBackend.Identifier + ", attempting retry");

                // Remove sticky sessions for the failed backend
                if (frontend.UseStickySessions)
                {
                    _SessionStickiness.RemoveSession(clientId, frontend.Identifier);
                    _Logging.Debug(_Header + "removed sticky session for client " + clientId + " due to backend failure");
                }

                // Get another backend for retry (exclude the failed backend)
                Backend retryBackend = GetAlternativeBackend(frontend, initialBackend.Identifier);

                if (retryBackend != null)
                {
                    _Logging.Info(_Header + "retrying request with backend " + retryBackend.Identifier);
                    telemetry.BackendServerId = retryBackend.Identifier;
                    telemetry.BackendSelectedUtc = DateTime.UtcNow;

                    // Update the response headers for the new backend
                    ctx.Response.Headers.Remove(Constants.BackendServerHeader);
                    ctx.Response.Headers.Remove(Constants.StickyServerHeader);
                    ctx.Response.Headers.Add(Constants.StickyServerHeader, retryBackend.IsSticky.ToString());

                    ProxyResult retryResult = await ProxyRequestInternal(requestGuid, ctx, frontend, retryBackend, telemetry);

                    if (retryResult.ResponseReceived)
                    {
                        // If retry succeeded and sticky sessions are enabled, create new sticky session
                        if (frontend.UseStickySessions && retryResult.StatusCode < 500)
                        {
                            _SessionStickiness.SetStickyBackend(clientId, frontend.Identifier, retryBackend.Identifier, frontend.StickySessionExpirationMs);
                            _Logging.Debug(_Header + "created new sticky session for client " + clientId + " with backend " + retryBackend.Identifier);
                        }

                        return true;
                    }
                    else
                    {
                        _Logging.Warn(_Header + "retry to backend " + retryBackend.Identifier + " also failed");
                        return false;
                    }
                }
                else
                {
                    _Logging.Warn(_Header + "no alternative backend available for retry");
                    // Return 502 Bad Gateway when no backends are available
                    ctx.Response.StatusCode = 502;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadGateway, null, "No backend servers are available to retry your request"), true));
                    return false;
                }
            }

            return result.ResponseReceived;
        }

        /// <summary>
        /// Process request using the transformation pipeline for API format compatibility.
        /// </summary>
        /// <param name="requestGuid">Request identifier.</param>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="frontend">Frontend configuration.</param>
        /// <param name="backend">Selected backend.</param>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="telemetry">Telemetry object.</param>
        /// <returns>True if response was successfully sent, false otherwise.</returns>
        private async Task<bool> ProcessRequestWithTransformation(
            Guid requestGuid,
            HttpContextBase ctx,
            Frontend frontend,
            Backend backend,
            string clientId,
            TelemetryMessage telemetry)
        {
            try
            {
                // Step 1: Detect source and target API formats
                ApiFormatEnum sourceFormat = DetectSourceApiFormat(ctx);
                ApiFormatEnum targetFormat = backend.ApiFormat;

                _Logging.Debug(_Header + $"transforming request from {sourceFormat} to {targetFormat} format for backend {backend.Identifier}");

                // Step 2: If formats match, use direct proxy (legacy behavior)
                if (sourceFormat == targetFormat)
                {
                    _Logging.Debug(_Header + "API formats match, using direct proxy");
                    return await ProcessRequestWithRetry(requestGuid, ctx, frontend, backend, clientId, telemetry);
                }

                // Step 3: Detect if request is for streaming and determine the request type
                Models.Agnostic.Base.AgnosticRequest agnosticRequest = await _TransformationPipeline.TransformInboundAsync(ctx, sourceFormat).ConfigureAwait(false);
                RequestTypeEnum requestType = GetRequestTypeFromAgnosticRequest(agnosticRequest);
                bool isStreamingRequest = IsStreamingRequest(ctx, agnosticRequest, requestType);

                // Step 4: Choose appropriate processing method based on streaming support
                if (isStreamingRequest && _TransformationPipeline.SupportsStreamingTransformation(sourceFormat, targetFormat, requestType))
                {
                    _Logging.Debug(_Header + "using streaming transformation for request");
                    return await ProcessRequestWithStreamingTransformation(
                        requestGuid, ctx, frontend, backend, clientId, telemetry, sourceFormat, targetFormat, requestType);
                }

                // Step 5: Fall back to non-streaming transformation
                object backendRequest = await _TransformationPipeline.TransformOutboundAsync(agnosticRequest, targetFormat).ConfigureAwait(false);

                // Create transformation result
                Services.Transformation.TransformationPipelineResult transformationResult = new Services.Transformation.TransformationPipelineResult
                {
                    AgnosticRequest = agnosticRequest,
                    BackendRequest = backendRequest,
                    SourceFormat = sourceFormat,
                    TargetFormat = targetFormat,
                    RequestType = requestType,
                    TransformationId = Guid.NewGuid().ToString(),
                    CompletedUtc = DateTime.UtcNow
                };

                telemetry.TransformationId = transformationResult.TransformationId;

                // Step 4: Serialize transformed request
                string transformedRequestBody = _Serializer.SerializeJson(backendRequest, false);
                byte[] transformedBytes = Encoding.UTF8.GetBytes(transformedRequestBody);

                // Step 5: Determine target URL path
                string targetPath = GetTargetUrlPath(ctx.Request.Url.RawWithoutQuery, sourceFormat, targetFormat);

                // Step 6: Process the transformed request
                ProxyResult proxyResult = await ProxyTransformedRequest(
                    requestGuid, ctx, frontend, backend, telemetry, transformationResult, transformedBytes, targetPath);

                // Step 6: Transform response back to source format if needed
                if (proxyResult.ResponseReceived && sourceFormat != targetFormat)
                {
                    await TransformAndSendResponse(ctx, proxyResult, sourceFormat, targetFormat, backend);
                }

                return proxyResult.ResponseReceived;
            }
            catch (TransformationException tex)
            {
                _Logging.Error(_Header + $"transformation failed:{Environment.NewLine}{tex.ToString()}");

                // Send error response in the expected format
                object errorResponse = tex.GenerateErrorResponse();
                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(errorResponse, true));

                return false;
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"request processing failed:{Environment.NewLine}{ex.ToString()}");

                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(
                    new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Internal server error"), true));

                return false;
            }
        }

        /// <summary>
        /// Get the target URL path based on API format conversion.
        /// </summary>
        /// <param name="sourcePath">Source URL path.</param>
        /// <param name="sourceFormat">Source API format.</param>
        /// <param name="targetFormat">Target API format.</param>
        /// <returns>Target URL path.</returns>
        private string GetTargetUrlPath(string sourcePath, ApiFormatEnum sourceFormat, ApiFormatEnum targetFormat)
        {
            // If formats match, no transformation needed
            if (sourceFormat == targetFormat)
                return sourcePath;

            // Convert between OpenAI and Ollama path conventions
            if (sourceFormat == ApiFormatEnum.OpenAI && targetFormat == ApiFormatEnum.Ollama)
            {
                return sourcePath switch
                {
                    "/v1/chat/completions" => "/api/chat",
                    "/v1/completions" => "/api/generate",
                    "/v1/embeddings" => "/api/embeddings",
                    "/v1/models" => "/api/tags",
                    string path when path.StartsWith("/v1/models/") => "/api/show",
                    _ => sourcePath
                };
            }
            else if (sourceFormat == ApiFormatEnum.Ollama && targetFormat == ApiFormatEnum.OpenAI)
            {
                return sourcePath switch
                {
                    "/api/chat" => "/v1/chat/completions",
                    "/api/generate" => "/v1/completions",
                    "/api/embeddings" => "/v1/embeddings",
                    "/api/tags" => "/v1/models",
                    "/api/show" => "/v1/models",
                    _ => sourcePath
                };
            }

            return sourcePath;
        }


        /// <summary>
        /// Proxy the transformed request to the backend server.
        /// </summary>
        /// <param name="requestGuid">Request identifier.</param>
        /// <param name="originalContext">Original HTTP context.</param>
        /// <param name="frontend">Frontend configuration.</param>
        /// <param name="backend">Backend server.</param>
        /// <param name="telemetry">Telemetry object.</param>
        /// <param name="transformationResult">Transformation result for logging.</param>
        /// <param name="transformedRequestBody">Transformed request body bytes.</param>
        /// <param name="targetUrlPath">Target URL path.</param>
        /// <returns>Proxy result.</returns>
        private async Task<ProxyResult> ProxyTransformedRequest(
            Guid requestGuid,
            HttpContextBase originalContext,
            Frontend frontend,
            Backend backend,
            TelemetryMessage telemetry,
            TransformationPipelineResult transformationResult,
            byte[] transformedRequestBody,
            string targetUrlPath)
        {
            _Logging.Debug(_Header + $"proxying transformed request (ID: {transformationResult.TransformationId}) to backend {backend.Identifier}");

            // Use the existing ProxyRequestInternal method with transformed data
            // captureResponseForTransformation=true so we can transform the response before sending to client
            return await ProxyRequestInternal(requestGuid, originalContext, frontend, backend, telemetry, transformedRequestBody, targetUrlPath, captureResponseForTransformation: true);
        }

        /// <summary>
        /// Transform the backend response and send it to the client.
        /// </summary>
        /// <param name="originalContext">Original HTTP context for the client response.</param>
        /// <param name="proxyResult">Result from the backend proxy operation.</param>
        /// <param name="sourceFormat">Source API format (client).</param>
        /// <param name="targetFormat">Target API format (backend).</param>
        /// <param name="backend">Backend server information.</param>
        private async Task TransformAndSendResponse(
            HttpContextBase originalContext,
            ProxyResult proxyResult,
            ApiFormatEnum sourceFormat,
            ApiFormatEnum targetFormat,
            Backend backend)
        {
            try
            {
                _Logging.Debug(_Header + $"transforming response from {targetFormat} back to {sourceFormat} format");

                // If response was already sent (non-transformed path), nothing to do
                if (proxyResult.AlreadySent)
                {
                    _Logging.Debug(_Header + "response already sent, skipping transformation");
                    return;
                }

                // Step 1: Transform backend response (targetFormat) to agnostic format
                Services.Transformation.Response.OpenAIToAgnosticResponseTransformer backendTransformer =
                    new Services.Transformation.Response.OpenAIToAgnosticResponseTransformer();

                Models.Agnostic.Base.AgnosticResponse agnosticResponse = await backendTransformer.TransformToAgnosticAsync(
                    proxyResult.ResponseBody,
                    targetFormat).ConfigureAwait(false);

                // Step 2: Transform agnostic format to client format (sourceFormat)
                Services.Transformation.Response.AgnosticToOllamaResponseTransformer clientTransformer =
                    new Services.Transformation.Response.AgnosticToOllamaResponseTransformer();

                object clientResponse = await clientTransformer.TransformFromAgnosticAsync(
                    agnosticResponse,
                    sourceFormat).ConfigureAwait(false);

                // Step 3: Serialize and send response to client
                string responseJson = _Serializer.SerializeJson(clientResponse, false);
                byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);

                // Set response headers
                if (proxyResult.Headers != null && proxyResult.Headers.Count > 0)
                {
                    foreach (string headerName in proxyResult.Headers.AllKeys)
                    {
                        if (headerName != null && originalContext.Response.Headers[headerName] == null &&
                            !headerName.Equals("content-length", StringComparison.OrdinalIgnoreCase) &&
                            !headerName.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                        {
                            originalContext.Response.Headers.Add(headerName, proxyResult.Headers[headerName]);
                        }
                    }
                }

                originalContext.Response.StatusCode = proxyResult.StatusCode;
                originalContext.Response.ContentType = Constants.JsonContentType;
                originalContext.Response.Headers.Add(Constants.BackendServerHeader, backend.Identifier);

                await originalContext.Response.Send(responseBytes);

                _Logging.Debug(_Header + $"response transformation completed (status: {proxyResult.StatusCode})");
            }
            catch (TransformationException tex)
            {
                _Logging.Error(_Header + $"response transformation failed: {tex.Message}");

                // Send transformation error to client
                object errorResponse = tex.GenerateErrorResponse();
                originalContext.Response.StatusCode = 500;
                originalContext.Response.ContentType = Constants.JsonContentType;
                await originalContext.Response.Send(_Serializer.SerializeJson(errorResponse, true));
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"response transformation failed: {ex.Message}");

                // Send generic error to client
                originalContext.Response.StatusCode = 500;
                originalContext.Response.ContentType = Constants.JsonContentType;
                await originalContext.Response.Send(_Serializer.SerializeJson(new { error = "Response transformation failed" }, true));
            }
        }

        /// <summary>
        /// Process a request with streaming transformation support.
        /// This method handles streaming responses and transforms them between API formats in real-time.
        /// </summary>
        /// <param name="requestGuid">Request identifier.</param>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="frontend">Frontend configuration.</param>
        /// <param name="backend">Backend server.</param>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="telemetry">Telemetry message.</param>
        /// <param name="sourceFormat">Source API format (client).</param>
        /// <param name="targetFormat">Target API format (backend).</param>
        /// <param name="requestType">Type of request being processed.</param>
        /// <returns>True if request was processed successfully.</returns>
        private async Task<bool> ProcessRequestWithStreamingTransformation(
            Guid requestGuid,
            HttpContextBase ctx,
            Frontend frontend,
            Backend backend,
            string clientId,
            TelemetryMessage telemetry,
            ApiFormatEnum sourceFormat,
            ApiFormatEnum targetFormat,
            RequestTypeEnum requestType)
        {
            try
            {
                _Logging.Debug(_Header + $"processing request with streaming transformation (source: {sourceFormat}, target: {targetFormat})");

                // Step 1: Check if streaming transformation is supported
                if (!_TransformationPipeline.SupportsStreamingTransformation(sourceFormat, targetFormat, requestType))
                {
                    _Logging.Warn(_Header + $"streaming transformation not supported for {sourceFormat} -> {targetFormat} ({requestType})");
                    // Fall back to non-streaming transformation
                    return await ProcessRequestWithTransformation(requestGuid, ctx, frontend, backend, clientId, telemetry);
                }

                // Step 2: Transform the request (same as non-streaming)
                Models.Agnostic.Base.AgnosticRequest agnosticRequest = await _TransformationPipeline.TransformInboundAsync(ctx, sourceFormat).ConfigureAwait(false);
                object backendRequest = await _TransformationPipeline.TransformOutboundAsync(agnosticRequest, targetFormat).ConfigureAwait(false);

                // Create transformation result
                Services.Transformation.TransformationPipelineResult transformationResult = new Services.Transformation.TransformationPipelineResult
                {
                    AgnosticRequest = agnosticRequest,
                    BackendRequest = backendRequest,
                    SourceFormat = sourceFormat,
                    TargetFormat = targetFormat,
                    RequestType = GetRequestTypeFromAgnosticRequest(agnosticRequest),
                    TransformationId = Guid.NewGuid().ToString(),
                    CompletedUtc = DateTime.UtcNow
                };

                telemetry.TransformationId = transformationResult.TransformationId;

                // Step 3: Serialize transformed request and determine target path
                string transformedRequestBody = _Serializer.SerializeJson(backendRequest, false);
                byte[] transformedBytes = Encoding.UTF8.GetBytes(transformedRequestBody);
                string targetPath = GetTargetUrlPath(ctx.Request.Url.RawWithoutQuery, sourceFormat, targetFormat);

                // Step 4: Proxy request with streaming transformation
                return await ProxyRequestWithStreamingTransformation(
                    requestGuid, ctx, frontend, backend, telemetry,
                    transformationResult, sourceFormat, targetFormat, requestType, transformedBytes, targetPath);
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"streaming transformation request failed: {ex.Message}");

                try
                {
                    ctx.Response.StatusCode = 500;
                    ctx.Response.ContentType = Constants.JsonContentType;

                    byte[] errorResponseBytes = Encoding.UTF8.GetBytes(_Serializer.SerializeJson(
                        new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Streaming transformation failed"), true));

                    // Check if response is configured for chunked transfer
                    if (ctx.Response.ChunkedTransfer)
                    {
                        await ctx.Response.SendChunk(errorResponseBytes, true);
                    }
                    else
                    {
                        await ctx.Response.Send(errorResponseBytes);
                    }
                }
                catch (Exception sendEx)
                {
                    _Logging.Error(_Header + $"error sending streaming transformation failure response: {sendEx.Message}");
                }

                return false;
            }
        }

        /// <summary>
        /// Proxy request to backend with real-time streaming transformation support.
        /// </summary>
        /// <param name="requestGuid">Request identifier.</param>
        /// <param name="originalContext">Original HTTP context from client.</param>
        /// <param name="frontend">Frontend configuration.</param>
        /// <param name="backend">Backend server.</param>
        /// <param name="telemetry">Telemetry message.</param>
        /// <param name="transformationResult">Transformation pipeline result.</param>
        /// <param name="sourceFormat">Source API format (client).</param>
        /// <param name="targetFormat">Target API format (backend).</param>
        /// <param name="requestType">Type of request being processed.</param>
        /// <param name="transformedRequestBody">Transformed request body bytes.</param>
        /// <param name="targetUrlPath">Target URL path for backend.</param>
        /// <returns>True if request was processed successfully.</returns>
        private async Task<bool> ProxyRequestWithStreamingTransformation(
            Guid requestGuid,
            HttpContextBase originalContext,
            Frontend frontend,
            Backend backend,
            TelemetryMessage telemetry,
            Services.Transformation.TransformationPipelineResult transformationResult,
            ApiFormatEnum sourceFormat,
            ApiFormatEnum targetFormat,
            RequestTypeEnum requestType,
            byte[] transformedRequestBody,
            string targetUrlPath)
        {
            _Logging.Debug(_Header + $"proxying request with streaming transformation to {backend.Identifier}");

            RestResponse resp = null;

            using (Timestamp ts = new Timestamp())
            {
                string url = backend.UrlPrefix + targetUrlPath;

                try
                {
                    await backend.Semaphore.WaitAsync().ConfigureAwait(false);
                    Interlocked.Increment(ref backend._ActiveRequests);
                    Interlocked.Decrement(ref backend._PendingRequests);

                    using (RestRequest req = new RestRequest(url, ConvertHttpMethod(originalContext.Request.Method)))
                    {
                        if (frontend.TimeoutMs > 0) req.TimeoutMilliseconds = frontend.TimeoutMs;

                        req.Headers.Add(Constants.ForwardedForHeader, originalContext.Request.Source.IpAddress);

                        // Copy headers
                        if (originalContext.Request.Headers != null && originalContext.Request.Headers.Count > 0)
                        {
                            foreach (string key in originalContext.Request.Headers.Keys)
                            {
                                if (!req.Headers.AllKeys.Contains(key))
                                {
                                    string val = originalContext.Request.Headers.Get(key);
                                    req.Headers.Add(key, val);
                                }
                            }
                        }

                        // Set correct host header for backend
                        foreach (string key in req.Headers.AllKeys)
                        {
                            if (key.ToLower().Equals("host"))
                            {
                                req.Headers.Remove(key);
                                req.Headers.Add("Host", backend.Hostname + ":" + backend.Port.ToString());
                            }
                        }

                        telemetry.BackendRequestSentUtc = DateTime.UtcNow;

                        if (transformedRequestBody != null && transformedRequestBody.Length > 0)
                        {
                            req.ContentType = Constants.JsonContentType;

                            resp = await req.SendAsync(transformedRequestBody);
                        }
                        else
                        {
                            resp = await req.SendAsync();
                        }

                        if (resp != null)
                        {
                            // Setup client response headers
                            if (resp.Headers != null && resp.Headers.Count > 0)
                            {
                                foreach (string headerName in resp.Headers.AllKeys)
                                {
                                    if (headerName != null && originalContext.Response.Headers[headerName] == null)
                                    {
                                        originalContext.Response.Headers.Add(headerName, resp.Headers[headerName]);
                                    }
                                }
                            }

                            originalContext.Response.StatusCode = resp.StatusCode;
                            originalContext.Response.Headers.Add(Constants.BackendServerHeader, backend.Identifier);

                            // Handle streaming response with transformation
                            return await ProcessStreamingResponseWithTransformation(
                                resp, originalContext, telemetry, sourceFormat, targetFormat, requestType);
                        }
                        else
                        {
                            _Logging.Warn(_Header + "no response from backend " + url);
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _Logging.Error(_Header + $"error proxying streaming request: {ex.Message}");
                    return false;
                }
                finally
                {
                    ts.End = DateTime.UtcNow;
                    _Logging.Debug(_Header + $"completed streaming request {requestGuid} to {backend.Identifier} ({ts.TotalMs}ms)");

                    if (resp != null) resp.Dispose();
                    backend.Semaphore.Release();
                    Interlocked.Decrement(ref backend._ActiveRequests);
                }
            }
        }

        /// <summary>
        /// Process streaming response with real-time transformation between API formats.
        /// </summary>
        /// <param name="backendResponse">Response from backend server.</param>
        /// <param name="clientContext">HTTP context for client response.</param>
        /// <param name="telemetry">Telemetry message.</param>
        /// <param name="sourceFormat">Source API format (client).</param>
        /// <param name="targetFormat">Target API format (backend).</param>
        /// <param name="requestType">Type of request being processed.</param>
        /// <returns>True if streaming was processed successfully.</returns>
        private async Task<bool> ProcessStreamingResponseWithTransformation(
            RestResponse backendResponse,
            HttpContextBase clientContext,
            TelemetryMessage telemetry,
            ApiFormatEnum sourceFormat,
            ApiFormatEnum targetFormat,
            RequestTypeEnum requestType)
        {
            try
            {
                IStreamingTransformer streamingTransformer = _TransformationPipeline.GetStreamingTransformer();

                // Setup client response based on source format
                if (sourceFormat == ApiFormatEnum.OpenAI)
                {
                    clientContext.Response.ContentType = "text/plain";
                    clientContext.Response.ServerSentEvents = true;
                    clientContext.Response.ProtocolVersion = "HTTP/1.1";
                }
                else
                {
                    clientContext.Response.ContentType = "application/x-ndjson";
                    clientContext.Response.ChunkedTransfer = true;
                }

                if (!backendResponse.ServerSentEvents)
                {
                    #region Chunked-Transfer-Encoding

                    if (!backendResponse.ChunkedTransferEncoding)
                    {
                        // Non-streaming response - handle as single chunk
                        if (backendResponse.DataAsBytes != null && backendResponse.DataAsBytes.Length > 0)
                        {
                            StreamingChunkResult chunkResult = await streamingTransformer.TransformChunkAsync(
                                backendResponse.DataAsBytes, targetFormat, sourceFormat, requestType);

                            if (!string.IsNullOrEmpty(chunkResult.Error))
                            {
                                _Logging.Error(_Header + $"transformation error: {chunkResult.Error}");
                                return false;
                            }

                            if (chunkResult.IsServerSentEvent && sourceFormat == ApiFormatEnum.OpenAI)
                            {
                                await clientContext.Response.SendEvent(Encoding.UTF8.GetString(chunkResult.ChunkData), true);
                            }
                            else
                            {
                                await clientContext.Response.Send(chunkResult.ChunkData);
                            }
                        }

                        telemetry.LastTokenTimeUtc = DateTime.UtcNow;
                        return true;
                    }
                    else
                    {
                        // Chunked streaming response
                        while (true)
                        {
                            ChunkData chunk = await backendResponse.ReadChunkAsync().ConfigureAwait(false);
                            if (telemetry.FirstTokenTimeUtc == null) telemetry.FirstTokenTimeUtc = DateTime.UtcNow;

                            if (chunk == null || chunk.IsFinal)
                            {
                                // Handle final chunk
                                if (chunk?.Data != null && chunk.Data.Length > 0)
                                {
                                    StreamingChunkResult finalChunkResult = await streamingTransformer.TransformChunkAsync(
                                        chunk.Data, targetFormat, sourceFormat, requestType);

                                    if (!string.IsNullOrEmpty(finalChunkResult.Error))
                                    {
                                        _Logging.Error(_Header + $"final chunk transformation error: {finalChunkResult.Error}");
                                    }
                                    else
                                    {
                                        await SendTransformedChunk(clientContext, finalChunkResult, sourceFormat, true);
                                    }
                                }

                                // Send final chunk indicating end of stream
                                StreamingChunkResult endChunkResult = await streamingTransformer.CreateFinalChunkAsync(sourceFormat, requestType);
                                await SendTransformedChunk(clientContext, endChunkResult, sourceFormat, true);
                                break;
                            }
                            else if (chunk.Data != null && chunk.Data.Length > 0)
                            {
                                StreamingChunkResult chunkResult = await streamingTransformer.TransformChunkAsync(
                                    chunk.Data, targetFormat, sourceFormat, requestType);

                                if (!string.IsNullOrEmpty(chunkResult.Error))
                                {
                                    _Logging.Warn(_Header + $"chunk transformation error: {chunkResult.Error}");
                                    continue; // Skip malformed chunks
                                }

                                await SendTransformedChunk(clientContext, chunkResult, sourceFormat, false);
                            }
                        }
                    }

                    #endregion
                }
                else
                {
                    #region Server-Sent-Events

                    while (true)
                    {
                        ServerSentEvent sse = await backendResponse.ReadEventAsync();
                        if (telemetry.FirstTokenTimeUtc == null) telemetry.FirstTokenTimeUtc = DateTime.UtcNow;

                        if (sse == null)
                        {
                            // End of stream - send final chunk
                            StreamingChunkResult endChunkResult = await streamingTransformer.CreateFinalChunkAsync(sourceFormat, requestType);
                            await SendTransformedChunk(clientContext, endChunkResult, sourceFormat, true);
                            break;
                        }
                        else if (!String.IsNullOrEmpty(sse.Data))
                        {
                            StreamingChunkResult chunkResult = await streamingTransformer.TransformChunkAsync(
                                sse.Data, targetFormat, sourceFormat, requestType);

                            if (!string.IsNullOrEmpty(chunkResult.Error))
                            {
                                _Logging.Warn(_Header + $"SSE transformation error: {chunkResult.Error}");
                                continue; // Skip malformed events
                            }

                            await SendTransformedChunk(clientContext, chunkResult, sourceFormat, false);
                        }
                    }

                    #endregion
                }

                telemetry.LastTokenTimeUtc = DateTime.UtcNow;
                return true;
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"streaming transformation failed: {ex.Message}");

                try
                {
                    // Send error response using appropriate method based on transfer encoding
                    byte[] errorResponseBytes = Encoding.UTF8.GetBytes(_Serializer.SerializeJson(
                        new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Streaming response transformation failed"), true));

                    if (clientContext.Response.ChunkedTransfer)
                    {
                        await clientContext.Response.SendChunk(errorResponseBytes, true);
                    }
                    else if (clientContext.Response.ServerSentEvents)
                    {
                        await clientContext.Response.SendEvent(Encoding.UTF8.GetString(errorResponseBytes), true);
                    }
                    else
                    {
                        await clientContext.Response.Send(errorResponseBytes);
                    }
                }
                catch (Exception sendEx)
                {
                    _Logging.Error(_Header + $"error sending streaming transformation error response: {sendEx.Message}");
                }

                return false;
            }
        }

        /// <summary>
        /// Send a transformed chunk to the client in the appropriate format.
        /// </summary>
        /// <param name="context">Client HTTP context.</param>
        /// <param name="chunkResult">Transformed chunk result.</param>
        /// <param name="targetFormat">Target format for the client.</param>
        /// <param name="isFinal">Whether this is the final chunk.</param>
        private async Task SendTransformedChunk(
            HttpContextBase context,
            Services.Transformation.Interfaces.StreamingChunkResult chunkResult,
            ApiFormatEnum targetFormat,
            bool isFinal)
        {
            if (chunkResult.ChunkData == null || chunkResult.ChunkData.Length == 0)
                return;

            try
            {
                if (targetFormat == ApiFormatEnum.OpenAI && chunkResult.IsServerSentEvent)
                {
                    // Send as Server-Sent Event
                    string eventData = Encoding.UTF8.GetString(chunkResult.ChunkData);
                    await context.Response.SendEvent(eventData, isFinal);
                }
                else
                {
                    // Send as chunked data
                    await context.Response.SendChunk(chunkResult.ChunkData, isFinal);
                }
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"error sending transformed chunk: {ex.Message}");
            }
        }

        /// <summary>
        /// Get an alternative backend excluding a specific backend.
        /// </summary>
        /// <param name="frontend">Frontend configuration.</param>
        /// <param name="excludeBackendId">Backend to exclude from selection.</param>
        /// <returns>Alternative backend or null if none available.</returns>
        private Backend GetAlternativeBackend(Frontend frontend, string excludeBackendId)
        {
            List<Backend> healthyBackends = _HealthCheck.Backends.Where(b =>
                frontend.Backends.Contains(b.Identifier) &&
                b.Healthy &&
                b.Identifier != excludeBackendId).ToList();

            if (!healthyBackends.Any())
            {
                _Logging.Debug(_Header + "no alternative healthy backends available (excluding " + excludeBackendId + ")");
                return null;
            }

            // Use load balancing to select from available backends
            Backend selected = null;

            switch (frontend.LoadBalancing)
            {
                case LoadBalancingMode.RoundRobin:
                    lock (frontend.Lock)
                    {
                        if (frontend.LastBackendIndex >= healthyBackends.Count)
                        {
                            frontend.LastBackendIndex = 0;
                        }
                        selected = healthyBackends[frontend.LastBackendIndex];
                        frontend.LastBackendIndex++;
                    }
                    break;

                case LoadBalancingMode.Random:
                    int index = _Random.Next(0, healthyBackends.Count);
                    selected = healthyBackends[index];
                    break;

                default:
                    selected = healthyBackends.First();
                    break;
            }

            _Logging.Debug(_Header + "selected alternative backend " + selected.Identifier + " using " + frontend.LoadBalancing + " load balancing");
            return selected;
        }

        private async Task<ProxyResult> ProxyRequestInternal(
            Guid requestGuid,
            HttpContextBase ctx,
            Frontend frontend,
            Backend backend,
            TelemetryMessage telemetry,
            byte[] overrideRequestBody = null,
            string overrideUrlPath = null,
            bool captureResponseForTransformation = false)
        {
            _Logging.Debug(_Header + "proxying request to " + backend.Identifier + " for Ollama endpoint " + frontend.Identifier + " for request " + requestGuid.ToString());

            RestResponse resp = null;

            using (Timestamp ts = new Timestamp())
            {
                string urlPath = overrideUrlPath ?? ctx.Request.Url.RawWithQuery;
                string url = backend.UrlPrefix + urlPath;

                try
                {
                    await backend.Semaphore.WaitAsync().ConfigureAwait(false);
                    Interlocked.Increment(ref backend._ActiveRequests);
                    Interlocked.Decrement(ref backend._PendingRequests);

                    using (RestRequest req = new RestRequest(url, ConvertHttpMethod(ctx.Request.Method)))
                    {
                        if (frontend.TimeoutMs > 0) req.TimeoutMilliseconds = frontend.TimeoutMs;

                        req.Headers.Add(Constants.ForwardedForHeader, ctx.Request.Source.IpAddress);

                        if (ctx.Request.Headers != null && ctx.Request.Headers.Count > 0)
                        {
                            foreach (string key in ctx.Request.Headers.Keys)
                            {
                                if (!req.Headers.AllKeys.Contains(key))
                                {
                                    string val = ctx.Request.Headers.Get(key);
                                    req.Headers.Add(key, val);
                                }
                            }
                        }

                        foreach (string key in req.Headers.AllKeys)
                        {
                            if (key.ToLower().Equals("host"))
                            {
                                req.Headers.Remove(key);
                                req.Headers.Add("Host", backend.Hostname + ":" + backend.Port.ToString());
                            }
                        }

                        if (frontend.LogRequestBody || backend.LogRequestBody)
                        {
                            byte[] dataBytes = overrideRequestBody ?? ctx.Request.DataAsBytes ?? Array.Empty<byte>();
                            int length = dataBytes.Length;

                            _Logging.Debug(
                                _Header
                                + "request body (" + length + " bytes): "
                                + Environment.NewLine
                                + Encoding.UTF8.GetString(dataBytes));

                            _Logging.Debug(_Header + "using content-type: " + req.ContentType);
                        }

                        telemetry.BackendRequestSentUtc = DateTime.UtcNow;
                        byte[] requestBody = overrideRequestBody ?? ctx.Request.DataAsBytes;

                        if (requestBody != null && requestBody.Length > 0)
                        {
                            if (overrideRequestBody != null)
                            {
                                // Transformed request, always JSON
                                req.ContentType = Constants.JsonContentType;
                            }
                            else if (!String.IsNullOrEmpty(ctx.Request.ContentType))
                            {
                                req.ContentType = ctx.Request.ContentType;
                            }
                            else
                            {
                                req.ContentType = Constants.BinaryContentType;
                            }

                            resp = await req.SendAsync(requestBody);
                        }
                        else
                        {
                            resp = await req.SendAsync();
                        }

                        if (resp != null)
                        {
                            #region Log-Response-Body

                            if (frontend.LogResponseBody || backend.LogResponseBody)
                            {
                                if (resp.DataAsBytes != null && resp.DataAsBytes.Length > 0)
                                {
                                    _Logging.Debug(
                                        _Header
                                        + "response body (" + resp.DataAsBytes.Length + " bytes) status " + resp.StatusCode + ": "
                                        + Environment.NewLine
                                        + Encoding.UTF8.GetString(resp.DataAsBytes));
                                }
                                else
                                {
                                    _Logging.Debug(
                                        _Header
                                        + "response body (0 bytes) status " + resp.StatusCode);
                                }
                            }

                            #endregion

                            #region Capture-Response-For-Transformation

                            // If capturing for transformation, store response and return without sending
                            if (captureResponseForTransformation && !resp.ServerSentEvents && !resp.ChunkedTransferEncoding)
                            {
                                return new ProxyResult
                                {
                                    ResponseReceived = true,
                                    StatusCode = resp.StatusCode,
                                    ResponseBody = resp.DataAsBytes,
                                    ContentType = resp.ContentType,
                                    Headers = resp.Headers,
                                    AlreadySent = false
                                };
                            }

                            #endregion

                            #region Set-Headers

                            if (resp.Headers != null && resp.Headers.Count > 0)
                            {
                                // copy header into ctx.Response.Headers without disturbing values
                                // that already exist in ctx.Response.Headers
                                // but ONLY if the value doesn't already exist
                                foreach (string headerName in resp.Headers.AllKeys)
                                {
                                    if (headerName != null && ctx.Response.Headers[headerName] == null)
                                    {
                                        ctx.Response.Headers.Add(headerName, resp.Headers[headerName]);
                                    }
                                }
                            }

                            ctx.Response.StatusCode = resp.StatusCode;
                            ctx.Response.ContentType = resp.ContentType;
                            ctx.Response.Headers.Add(Constants.BackendServerHeader, backend.Identifier);
                            ctx.Response.ChunkedTransfer = resp.ChunkedTransferEncoding;

                            #endregion

                            #region Send-Response

                            if (!resp.ServerSentEvents)
                            {
                                #region Not-Server-Sent-Events

                                if (!ctx.Response.ChunkedTransfer)
                                {
                                    await ctx.Response.Send(resp.DataAsBytes);
                                }
                                else
                                {
                                    while (true)
                                    {
                                        ChunkData chunk = await resp.ReadChunkAsync().ConfigureAwait(false);
                                        if (telemetry.FirstTokenTimeUtc == null) telemetry.FirstTokenTimeUtc = DateTime.UtcNow;

                                        if (chunk == null || chunk.IsFinal)
                                        {
                                            if (chunk?.Data != null && chunk.Data.Length > 0)
                                            {
                                                // For NDJSON format, append Environment.NewLine to final chunk
                                                byte[] newlineBytes = System.Text.Encoding.UTF8.GetBytes(Environment.NewLine);
                                                byte[] finalData = new byte[chunk.Data.Length + newlineBytes.Length];
                                                Array.Copy(chunk.Data, finalData, chunk.Data.Length);
                                                Array.Copy(newlineBytes, 0, finalData, chunk.Data.Length, newlineBytes.Length);
                                                await ctx.Response.SendChunk(finalData, true).ConfigureAwait(false);
                                            }
                                            else
                                            {
                                                // Send empty final chunk
                                                await ctx.Response.SendChunk(Array.Empty<byte>(), true).ConfigureAwait(false);
                                            }
                                            break;
                                        }
                                        else if (chunk.Data != null && chunk.Data.Length > 0)
                                        {
                                            // For NDJSON format, append Environment.NewLine to each chunk
                                            byte[] newlineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);
                                            byte[] chunkWithNewline = new byte[chunk.Data.Length + newlineBytes.Length];
                                            Array.Copy(chunk.Data, chunkWithNewline, chunk.Data.Length);
                                            Array.Copy(newlineBytes, 0, chunkWithNewline, chunk.Data.Length, newlineBytes.Length);
                                            await ctx.Response.SendChunk(chunkWithNewline, false).ConfigureAwait(false);
                                        }
                                    }
                                }

                                telemetry.LastTokenTimeUtc = DateTime.UtcNow;

                                #endregion
                            }
                            else
                            {
                                #region Server-Sent-Events

                                ctx.Response.ProtocolVersion = "HTTP/1.1";
                                ctx.Response.ServerSentEvents = true;

                                while (true)
                                {
                                    ServerSentEvent sse = await resp.ReadEventAsync();
                                    if (telemetry.FirstTokenTimeUtc == null) telemetry.FirstTokenTimeUtc = DateTime.UtcNow;

                                    if (sse == null)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        if (!String.IsNullOrEmpty(sse.Data))
                                        {
                                            await ctx.Response.SendEvent(sse.Data, false);
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }

                                telemetry.LastTokenTimeUtc = DateTime.UtcNow;

                                await ctx.Response.SendEvent(null, true);

                                #endregion
                            }

                            #endregion

                            return new ProxyResult { ResponseReceived = true, StatusCode = resp.StatusCode, AlreadySent = true };
                        }
                        else
                        {
                            _Logging.Warn(_Header + "no response from origin " + url);
                            return new ProxyResult { ResponseReceived = false, StatusCode = 0 };
                        }
                    }
                }
                catch (System.Net.Http.HttpRequestException hre)
                {
                    _Logging.Warn(
                        _Header
                        + "exception proxying request to backend " + backend.Identifier
                        + " for endpoint " + frontend.Identifier
                        + " for request " + requestGuid.ToString()
                        + ": " + hre.Message);

                    return new ProxyResult { ResponseReceived = false, StatusCode = 0 };
                }
                catch (SocketException se)
                {
                    _Logging.Warn(
                        _Header
                        + "exception proxying request to backend " + backend.Identifier
                        + " for endpoint " + frontend.Identifier
                        + " for request " + requestGuid.ToString()
                        + ": " + se.Message);

                    return new ProxyResult { ResponseReceived = false, StatusCode = 0 };
                }
                catch (Exception e)
                {
                    _Logging.Warn(
                        _Header
                        + "exception proxying request to backend " + backend.Identifier
                        + " for endpoint " + frontend.Identifier
                        + " for request " + requestGuid.ToString()
                        + Environment.NewLine
                        + e.ToString());

                    return new ProxyResult { ResponseReceived = false, StatusCode = 0 };
                }
                finally
                {
                    ts.End = DateTime.UtcNow;
                    _Logging.Debug(
                        _Header
                        + "completed request " + requestGuid.ToString() + " "
                        + "backend " + backend.Identifier + " "
                        + "frontend " + frontend.Identifier + " "
                        + (resp != null ? resp.StatusCode : "0") + " "
                        + "(" + ts.TotalMs + "ms)");

                    if (resp != null) resp.Dispose();

                    backend.Semaphore.Release();
                    Interlocked.Decrement(ref backend._ActiveRequests);
                }
            }
        }

        private async Task<bool> ProxyRequest(
            Guid requestGuid,
            HttpContextBase ctx,
            Frontend frontend,
            Backend backend,
            TelemetryMessage telemetry)
        {
            _Logging.Debug(_Header + "proxying request to " + backend.Identifier + " for Ollama endpoint " + frontend.Identifier + " for request " + requestGuid.ToString());

            RestResponse resp = null;

            using (Timestamp ts = new Timestamp())
            {
                string url = backend.UrlPrefix + ctx.Request.Url.RawWithQuery;

                try
                {
                    await backend.Semaphore.WaitAsync().ConfigureAwait(false);
                    Interlocked.Increment(ref backend._ActiveRequests);
                    Interlocked.Decrement(ref backend._PendingRequests);

                    using (RestRequest req = new RestRequest(url, ConvertHttpMethod(ctx.Request.Method)))
                    {
                        if (frontend.TimeoutMs > 0) req.TimeoutMilliseconds = frontend.TimeoutMs;

                        req.Headers.Add(Constants.ForwardedForHeader, ctx.Request.Source.IpAddress);

                        if (ctx.Request.Headers != null && ctx.Request.Headers.Count > 0)
                        {
                            foreach (string key in ctx.Request.Headers.Keys)
                            {
                                if (!req.Headers.AllKeys.Contains(key))
                                {
                                    string val = ctx.Request.Headers.Get(key);
                                    req.Headers.Add(key, val);
                                }
                            }
                        }

                        foreach (string key in req.Headers.AllKeys)
                        {
                            if (key.ToLower().Equals("host"))
                            {
                                req.Headers.Remove(key);
                                req.Headers.Add("Host", backend.Hostname + ":" + backend.Port.ToString());
                            }
                        }

                        if (frontend.LogRequestBody || backend.LogRequestBody)
                        {
                            int length = ctx.Request.DataAsBytes?.Length ?? 0;
                            byte[] dataBytes =  ctx.Request.DataAsBytes ?? Array.Empty<byte>();

                            _Logging.Debug(
                                _Header
                                + "request body (" + length + " bytes): "
                                + Environment.NewLine
                                + Encoding.UTF8.GetString(dataBytes));

                            _Logging.Debug(_Header + "using content-type: " + req.ContentType);
                        }

                        telemetry.BackendRequestSentUtc = DateTime.UtcNow;
                        if (ctx.Request.DataAsBytes != null && ctx.Request.DataAsBytes.Length > 0)
                        {
                            if (!String.IsNullOrEmpty(ctx.Request.ContentType)) req.ContentType = ctx.Request.ContentType;
                            else req.ContentType = Constants.BinaryContentType;

                            resp = await req.SendAsync(ctx.Request.DataAsBytes);
                        }
                        else
                        {
                            resp = await req.SendAsync();
                        }

                        if (resp != null)
                        {
                            #region Log-Response-Body

                            if (frontend.LogResponseBody || backend.LogResponseBody)
                            {
                                if (resp.DataAsBytes != null && resp.DataAsBytes.Length > 0)
                                {
                                    _Logging.Debug(
                                        _Header
                                        + "response body (" + resp.DataAsBytes.Length + " bytes) status " + resp.StatusCode + ": "
                                        + Environment.NewLine
                                        + Encoding.UTF8.GetString(resp.DataAsBytes));
                                }
                                else
                                {
                                    _Logging.Debug(
                                        _Header
                                        + "response body (0 bytes) status " + resp.StatusCode);
                                }
                            }

                            #endregion

                            #region Set-Headers

                            if (resp.Headers != null && resp.Headers.Count > 0)
                            {
                                // copy header into ctx.Response.Headers without disturbing values
                                // that already exist in ctx.Response.Headers
                                // but ONLY if the value doesn't already exist
                                foreach (string headerName in resp.Headers.AllKeys)
                                {
                                    if (headerName != null && ctx.Response.Headers[headerName] == null)
                                    {
                                        ctx.Response.Headers.Add(headerName, resp.Headers[headerName]);
                                    }
                                }
                            }

                            ctx.Response.StatusCode = resp.StatusCode;
                            ctx.Response.ContentType = resp.ContentType;
                            ctx.Response.Headers.Add(Constants.BackendServerHeader, backend.Identifier);
                            ctx.Response.ChunkedTransfer = resp.ChunkedTransferEncoding;

                            #endregion

                            #region Send-Response

                            if (!resp.ServerSentEvents)
                            {
                                #region Not-Server-Sent-Events

                                if (!ctx.Response.ChunkedTransfer)
                                {
                                    await ctx.Response.Send(resp.DataAsBytes);
                                }
                                else
                                {
                                    while (true)
                                    {
                                        ChunkData chunk = await resp.ReadChunkAsync().ConfigureAwait(false);
                                        if (telemetry.FirstTokenTimeUtc == null) telemetry.FirstTokenTimeUtc = DateTime.UtcNow;

                                        if (chunk == null || chunk.IsFinal)
                                        {
                                            if (chunk?.Data != null && chunk.Data.Length > 0)
                                            {
                                                // For NDJSON format, append Environment.NewLine to final chunk
                                                byte[] newlineBytes = System.Text.Encoding.UTF8.GetBytes(Environment.NewLine);
                                                byte[] finalData = new byte[chunk.Data.Length + newlineBytes.Length];
                                                Array.Copy(chunk.Data, finalData, chunk.Data.Length);
                                                Array.Copy(newlineBytes, 0, finalData, chunk.Data.Length, newlineBytes.Length);
                                                await ctx.Response.SendChunk(finalData, true).ConfigureAwait(false);
                                            }
                                            else
                                            {
                                                // Send empty final chunk
                                                await ctx.Response.SendChunk(Array.Empty<byte>(), true).ConfigureAwait(false);
                                            }
                                            break;
                                        }
                                        else if (chunk.Data != null && chunk.Data.Length > 0)
                                        {
                                            // For NDJSON format, append Environment.NewLine to each chunk
                                            byte[] newlineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);
                                            byte[] chunkWithNewline = new byte[chunk.Data.Length + newlineBytes.Length];
                                            Array.Copy(chunk.Data, chunkWithNewline, chunk.Data.Length);
                                            Array.Copy(newlineBytes, 0, chunkWithNewline, chunk.Data.Length, newlineBytes.Length);
                                            await ctx.Response.SendChunk(chunkWithNewline, false).ConfigureAwait(false);
                                        }
                                    }
                                }

                                telemetry.LastTokenTimeUtc = DateTime.UtcNow;

                                #endregion
                            }
                            else
                            {
                                #region Server-Sent-Events

                                ctx.Response.ProtocolVersion = "HTTP/1.1";
                                ctx.Response.ServerSentEvents = true;

                                while (true)
                                {
                                    ServerSentEvent sse = await resp.ReadEventAsync();
                                    if (telemetry.FirstTokenTimeUtc == null) telemetry.FirstTokenTimeUtc = DateTime.UtcNow;

                                    if (sse == null)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        if (!String.IsNullOrEmpty(sse.Data))
                                        {
                                            await ctx.Response.SendEvent(sse.Data, false);
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }

                                telemetry.LastTokenTimeUtc = DateTime.UtcNow;

                                await ctx.Response.SendEvent(null, true);

                                #endregion
                            }

                            #endregion

                            return true;
                        }
                        else
                        {
                            _Logging.Warn(_Header + "no response from origin " + url);
                            return false;
                        }
                    }
                }
                catch (System.Net.Http.HttpRequestException hre)
                {
                    _Logging.Warn(
                        _Header
                        + "exception proxying request to backend " + backend.Identifier
                        + " for endpoint " + frontend.Identifier
                        + " for request " + requestGuid.ToString()
                        + ": " + hre.Message);

                    return false;
                }
                catch (SocketException se)
                {
                    _Logging.Warn(
                        _Header
                        + "exception proxying request to backend " + backend.Identifier
                        + " for endpoint " + frontend.Identifier
                        + " for request " + requestGuid.ToString()
                        + ": " + se.Message);

                    return false;
                }
                catch (Exception e)
                {
                    _Logging.Warn(
                        _Header
                        + "exception proxying request to backend " + backend.Identifier
                        + " for endpoint " + frontend.Identifier
                        + " for request " + requestGuid.ToString()
                        + Environment.NewLine
                        + e.ToString());

                    return false;
                }
                finally
                {
                    ts.End = DateTime.UtcNow;
                    _Logging.Debug(
                        _Header
                        + "completed request " + requestGuid.ToString() + " "
                        + "backend " + backend.Identifier + " "
                        + "frontend " + frontend.Identifier + " "
                        + (resp != null ? resp.StatusCode : "0") + " "
                        + "(" + ts.TotalMs + "ms)");

                    if (resp != null) resp.Dispose();

                    backend.Semaphore.Release();
                    Interlocked.Decrement(ref backend._ActiveRequests);
                }

                #endregion
            }
        }

        private bool IsAuthenticated(HttpContextBase ctx)
        {
            if (ctx.Request.Authorization != null  && !String.IsNullOrEmpty(ctx.Request.Authorization.BearerToken))
            {
                if (_Settings.AdminBearerTokens != null)
                {
                    if (_Settings.AdminBearerTokens.Contains(ctx.Request.Authorization.BearerToken)) 
                        return true;
                }
            }

            return false;
        }

        private async Task GetFrontendsRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            List<Frontend> objs = _FrontendService.GetAll().ToList();
            await ctx.Response.Send(_Serializer.SerializeJson(objs, true));
        }

        private async Task GetFrontendRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = ctx.Request.Url.Parameters["identifier"];
            Frontend obj = _FrontendService.GetByIdentifier(identifier);
            if (obj == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");
            await ctx.Response.Send(_Serializer.SerializeJson(obj, true));
        }

        private async Task DeleteFrontendRoute(HttpContextBase ctx)
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

        private async Task CreateFrontendRoute(HttpContextBase ctx)
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

        private async Task UpdateFrontendRoute(HttpContextBase ctx)
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

        private async Task GetBackendsRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            List<Backend> objs = _BackendService.GetAll().ToList();
            await ctx.Response.Send(_Serializer.SerializeJson(objs, true));
        }

        private async Task GetBackendsHealthRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            List<Backend> objs = new List<Backend>(_HealthCheck.Backends);
            await ctx.Response.Send(_Serializer.SerializeJson(objs, true));
        }

        private async Task GetBackendRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = ctx.Request.Url.Parameters["identifier"];
            Backend obj = _BackendService.GetByIdentifier(identifier);
            if (obj == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");
            await ctx.Response.Send(_Serializer.SerializeJson(obj, true));
        }

        private async Task GetBackendHealthRoute(HttpContextBase ctx)
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

        private async Task DeleteBackendRoute(HttpContextBase ctx)
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

        private async Task CreateBackendRoute(HttpContextBase ctx)
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

        private async Task UpdateBackendRoute(HttpContextBase ctx)
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

        private async Task GetSessionsRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            List<Models.StickySession> sessions = _SessionStickiness.GetAllSessions();
            await ctx.Response.Send(_Serializer.SerializeJson(sessions, true));
        }

        private async Task GetClientSessionsRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string clientId = ctx.Request.Url.Parameters["clientId"];
            List<Models.StickySession> sessions = _SessionStickiness.GetClientSessions(clientId);
            await ctx.Response.Send(_Serializer.SerializeJson(sessions, true));
        }

        private async Task DeleteClientSessionsRoute(HttpContextBase ctx)
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

        private async Task DeleteAllSessionsRoute(HttpContextBase ctx)
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

        private RequestTypeEnum GetRequestTypeFromAgnosticRequest(Models.Agnostic.Base.AgnosticRequest request)
        {
            return request switch
            {
                Models.Agnostic.Requests.AgnosticChatRequest => RequestTypeEnum.GenerateChatCompletion,
                Models.Agnostic.Requests.AgnosticCompletionRequest => RequestTypeEnum.GenerateCompletion,
                Models.Agnostic.Requests.AgnosticEmbeddingRequest => RequestTypeEnum.GenerateEmbeddings,
                Models.Agnostic.Requests.AgnosticModelListRequest => RequestTypeEnum.ListModels,
                Models.Agnostic.Requests.AgnosticModelInfoRequest => RequestTypeEnum.ShowModelInformation,
                _ => RequestTypeEnum.GenerateChatCompletion // Default fallback
            };
        }

        /// <summary>
        /// Determines if the request is asking for streaming responses.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="agnosticRequest">Agnostic request.</param>
        /// <param name="requestType">Request type.</param>
        /// <returns>True if streaming is requested.</returns>
        private bool IsStreamingRequest(HttpContextBase ctx, Models.Agnostic.Base.AgnosticRequest agnosticRequest, RequestTypeEnum requestType)
        {
            // Only chat completions and completions support streaming
            if (requestType != RequestTypeEnum.GenerateChatCompletion && requestType != RequestTypeEnum.GenerateCompletion)
                return false;

            // Check stream parameter in agnostic request
            if (agnosticRequest is Models.Agnostic.Requests.AgnosticChatRequest chatRequest)
            {
                return chatRequest.Stream == true;
            }
            else if (agnosticRequest is Models.Agnostic.Requests.AgnosticCompletionRequest completionRequest)
            {
                return completionRequest.Stream == true;
            }

            // Fall back to checking headers for streaming indicators
            string acceptHeader = ctx.Request.Headers?["Accept"];
            if (!string.IsNullOrEmpty(acceptHeader))
            {
                if (acceptHeader.Contains("text/event-stream") || acceptHeader.Contains("application/x-ndjson"))
                    return true;
            }

            return false;
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
