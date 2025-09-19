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
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Serialization;
    using SyslogLogging;
    using Timestamps;
    using UrlMatcher;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using System.Data.Common;

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

        private const int BUFFER_SIZE = 65536;

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

            try
            {
                OllamaFrontend frontend = await GetFrontend(ctx);
                if (frontend == null)
                {
                    _Logging.Warn(_Header + "no frontend found for " + ctx.Request.Method.ToString() + " " + ctx.Request.Url.Full);
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "No matching API endpoint found"), true));
                    return;
                }

                RequestTypeEnum requestType = RequestTypeHelper.DetermineRequestType(ctx.Request.Method, ctx.Request.Url.RawWithQuery);

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
                OllamaBackend backend = _HealthCheck.GetNextBackend(frontend, clientId);
                if (backend == null)
                {
                    _Logging.Warn(_Header + "no backend found for " + ctx.Request.Method.ToString() + " " + ctx.Request.Url.RawWithoutQuery);
                    ctx.Response.StatusCode = 502;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadGateway, null, "No backend servers are available to service your request"), true));
                    return;
                }

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

                bool responseReceived = await ProxyRequest(
                    requestGuid,
                    ctx,
                    frontend,
                    backend);

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
        }

        #endregion

        #region Private-Methods

        private string GetClientIdentifier(HttpContextBase ctx)
        {
            return ctx.Request.Source.IpAddress;
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

        private async Task<OllamaFrontend> GetFrontend(HttpContextBase ctx)
        {
            Uri uri = new Uri(ctx.Request.Url.Full);

            List<OllamaFrontend> frontends = _HealthCheck.Frontends;

            foreach (OllamaFrontend ep in frontends)
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

        private async Task<bool> ProxyRequest(
            Guid requestGuid,
            HttpContextBase ctx,
            OllamaFrontend frontend,
            OllamaBackend backend)
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

                        #region Log-Request-Body

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

                        #endregion

                        #region Send-Request

                        if (ctx.Request.DataAsBytes != null && ctx.Request.DataAsBytes.Length > 0)
                        {
                            #region With-Data

                            if (!String.IsNullOrEmpty(ctx.Request.ContentType)) req.ContentType = ctx.Request.ContentType;
                            else req.ContentType = Constants.BinaryContentType;

                            resp = await req.SendAsync(ctx.Request.DataAsBytes);

                            #endregion
                        }
                        else
                        {
                            #region Without-Data

                            resp = await req.SendAsync();

                            #endregion
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

                            ctx.Response.StatusCode = resp.StatusCode;
                            ctx.Response.ContentType = resp.ContentType;
                            ctx.Response.Headers = resp.Headers;
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
            List<OllamaFrontend> objs = _FrontendService.GetAll().ToList();
            await ctx.Response.Send(_Serializer.SerializeJson(objs, true));
        }

        private async Task GetFrontendRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = ctx.Request.Url.Parameters["identifier"];
            OllamaFrontend obj = _FrontendService.GetByIdentifier(identifier);
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
            OllamaFrontend obj = _Serializer.DeserializeJson<OllamaFrontend>(ctx.Request.DataAsString);
            OllamaFrontend existing = _FrontendService.GetByIdentifier(obj.Identifier);
            if (existing != null) throw new ArgumentException("An object with identifier " + obj.Identifier + " already exists.");

            OllamaFrontend created = _FrontendService.Create(obj);

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
            OllamaFrontend original = _FrontendService.GetByIdentifier(identifier);
            if (original == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");

            OllamaFrontend updated = _Serializer.DeserializeJson<OllamaFrontend>(ctx.Request.DataAsString);
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
            List<OllamaBackend> objs = _BackendService.GetAll().ToList();
            await ctx.Response.Send(_Serializer.SerializeJson(objs, true));
        }

        private async Task GetBackendsHealthRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            List<OllamaBackend> objs = new List<OllamaBackend>(_HealthCheck.Backends);
            await ctx.Response.Send(_Serializer.SerializeJson(objs, true));
        }

        private async Task GetBackendRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = ctx.Request.Url.Parameters["identifier"];
            OllamaBackend obj = _BackendService.GetByIdentifier(identifier);
            if (obj == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");
            await ctx.Response.Send(_Serializer.SerializeJson(obj, true));
        }

        private async Task GetBackendHealthRoute(HttpContextBase ctx)
        {
            if (!IsAuthenticated(ctx)) throw new UnauthorizedAccessException();
            string identifier = ctx.Request.Url.Parameters["identifier"];
            List<OllamaBackend> backends = new List<OllamaBackend>(_HealthCheck.Backends);
            if (backends.Any(b => b.Identifier.Equals(identifier)))
            {
                OllamaBackend backend = backends.First(b => b.Identifier.Equals(identifier));
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
            OllamaBackend obj = _Serializer.DeserializeJson<OllamaBackend>(ctx.Request.DataAsString);
            OllamaBackend existing = _BackendService.GetByIdentifier(obj.Identifier);
            if (existing != null) throw new ArgumentException("An object with identifier " + obj.Identifier + " already exists.");

            OllamaBackend created = _BackendService.Create(obj);

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
            OllamaBackend original = _BackendService.GetByIdentifier(identifier);
            if (original == null) throw new KeyNotFoundException("Unable to find object with identifier " + identifier + ".");

            OllamaBackend updated = _Serializer.DeserializeJson<OllamaBackend>(ctx.Request.DataAsString);
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


        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
