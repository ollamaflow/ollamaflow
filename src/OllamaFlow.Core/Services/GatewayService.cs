namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using RestWrapper;
    using SerializationHelper;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Helpers;
    using SyslogLogging;
    using Timestamps;
    using UrlMatcher;
    using WatsonWebserver;
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
        private bool _IsDisposed = false;

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
        public GatewayService(
            OllamaFlowSettings settings,
            OllamaFlowCallbacks callbacks,
            LoggingModule logging,
            Serializer serializer)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
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
            webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/", GetRootRoute);
            webserver.Routes.PreAuthentication.Static.Add(HttpMethod.HEAD, "/", HeadRootRoute);
            webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/favicon.ico", GetFaviconRoute);
            webserver.Routes.PreAuthentication.Static.Add(HttpMethod.HEAD, "/favicon.ico", HeadFaviconRoute);
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
        }

        /// <summary>
        /// Post-routing handler.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task PostRoutingHandler(HttpContextBase ctx)
        {
        }

        /// <summary>
        /// Authenticate request.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task AuthenticateRequest(HttpContextBase ctx)
        {
        }

        /// <summary>
        /// Default request handler.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task DefaultRoute(HttpContextBase ctx)
        {
            Guid requestGuid = Guid.NewGuid();

            try
            {
                OllamaFrontend frontend = GetFrontend(ctx);
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
                                File.WriteAllText(Constants.SettingsFile, _Serializer.SerializeJson(_Settings, true));
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
                                File.WriteAllText(Constants.SettingsFile, _Serializer.SerializeJson(_Settings, true));
                            }
                        }
                    }
                }

                OllamaBackend backend = GetBackend(frontend);
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
                    Volatile.Read(ref backend.ActiveRequests) +
                    Volatile.Read(ref backend.PendingRequests);

                if (totalRequests > backend.RateLimitRequestsThreshold)
                {
                    _Logging.Warn(_Header + "too many active requests for backend " + backend.Identifier + ", sending 429 response to request from " + ctx.Request.Source.IpAddress);
                    ctx.Response.StatusCode = 429;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.SlowDown)));
                    return;
                }

                Interlocked.Increment(ref backend.PendingRequests);

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

        private OllamaFrontend GetFrontend(HttpContextBase ctx)
        {
            try
            {
                Uri uri = new Uri(ctx.Request.Url.Full);
                // _Logging.Debug(_Header + "locating frontend for host " + uri.Host);

                foreach (OllamaFrontend ep in _Settings.Frontends)
                {
                    if (ep.Hostname.Equals("*"))
                    {
                        // _Logging.Debug(_Header + "catch-all host found in Ollama frontend " + ep.Identifier);
                        return ep;
                    }

                    if (ep.Hostname.Equals(uri.Host)) return ep;
                }

                _Logging.Warn(_Header + "no frontend found for host " + uri.Host);
                return null;
            }
            catch (Exception e)
            {
                _Logging.Warn(
                    _Header + 
                    "exception attempting to find frontend for " + ctx.Request.Method.ToString() + " " + ctx.Request.Url.Full + 
                    Environment.NewLine + 
                    e.ToString());

                return null;
            }
        }

        private OllamaBackend GetBackend(OllamaFrontend frontend)
        {
            if (frontend == null) return null;
            if (frontend.Backends == null || frontend.Backends.Count < 1) return null;

            OllamaBackend origin = null;

            lock (frontend.Lock)
            {
                List<OllamaBackend> healthyBackends = _Settings.Backends
                    .Where(b => frontend.Backends.Contains(b.Identifier))
                    .Where(b =>
                    {
                        lock (b.Lock)
                        {
                            return b.Healthy;
                        }
                    })
                    .ToList();

                if (healthyBackends.Count < 1)
                {
                    _Logging.Warn(_Header + "no healthy backends found for frontend " + frontend.Identifier);
                    return null;
                }
                else
                {
                    if (frontend.LoadBalancing == LoadBalancingMode.Random)
                    {
                        int index = _Random.Next(0, healthyBackends.Count);
                        frontend.LastIndex = index;
                        origin = healthyBackends[index];
                        if (origin != default(OllamaBackend)) return origin;
                        return null;
                    }
                    else if (frontend.LoadBalancing == LoadBalancingMode.RoundRobin)
                    {
                        if (frontend.LastIndex >= healthyBackends.Count) frontend.LastIndex = _Random.Next(0, healthyBackends.Count);
                        origin = healthyBackends[frontend.LastIndex];

                        if ((frontend.LastIndex + 1) > (frontend.Backends.Count - 1)) frontend.LastIndex = 0;
                        else frontend.LastIndex = frontend.LastIndex + 1;

                        if (origin != default(OllamaBackend)) return origin;
                        return null;
                    }
                    else
                    {
                        throw new ArgumentException("Unknown load balancing scheme '" + frontend.LoadBalancing.ToString() + "'.");
                    }
                }
            }
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
                    #region Enter-Semaphore

                    await backend.Semaphore.WaitAsync().ConfigureAwait(false);
                    Interlocked.Increment(ref backend.ActiveRequests);
                    Interlocked.Decrement(ref backend.PendingRequests);

                    #endregion

                    #region Build-Request-and-Send

                    using (RestRequest req = new RestRequest(url, ConvertHttpMethod(ctx.Request.Method)))
                    {
                        if (frontend.TimeoutMs > 0)
                            req.TimeoutMilliseconds = frontend.TimeoutMs;

                        req.Headers.Add(Constants.ForwardedForHeader, ctx.Request.Source.IpAddress);
                        req.Headers.Add(Constants.RequestIdHeader, requestGuid.ToString());

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
                            _Logging.Debug(
                                _Header
                                + "request body (" + ctx.Request.DataAsBytes.Length + " bytes): "
                                + Environment.NewLine
                                + Encoding.UTF8.GetString(ctx.Request.DataAsBytes));

                            _Logging.Debug(_Header + "using content-type: " + req.ContentType);
                        }

                        #endregion

                        #region Send-Request

                        if (ctx.Request.DataAsBytes != null && ctx.Request.DataAsBytes.Length > 0)
                        {
                            #region With-Data

                            if (!String.IsNullOrEmpty(ctx.Request.ContentType))
                                req.ContentType = ctx.Request.ContentType;
                            else
                                req.ContentType = Constants.BinaryContentType;

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
                            ctx.Response.Headers.Add(Constants.RequestIdHeader, requestGuid.ToString());
                            ctx.Response.ChunkedTransfer = resp.ChunkedTransferEncoding;

                            #endregion

                            #region Send-Response

                            if (!resp.ServerSentEvents)
                            {
                                if (!ctx.Response.ChunkedTransfer)
                                {
                                    ctx.Response.ChunkedTransfer = false;

                                    await ctx.Response.Send(resp.DataAsBytes);
                                }
                                else
                                {
                                    ctx.Response.ChunkedTransfer = true;

                                    if (resp.DataAsBytes.Length > 0)
                                    {
                                        for (int i = 0; i < resp.DataAsBytes.Length; i += BUFFER_SIZE)
                                        {
                                            int currentChunkSize = Math.Min(BUFFER_SIZE, resp.DataAsBytes.Length - i);

                                            byte[] chunk = new byte[currentChunkSize];
                                            Array.Copy(resp.DataAsBytes, i, chunk, 0, currentChunkSize);

                                            if (chunk.Length == BUFFER_SIZE) await ctx.Response.SendChunk(chunk, false).ConfigureAwait(false);
                                            else await ctx.Response.SendChunk(chunk, true).ConfigureAwait(false);
                                        }
                                    }
                                    else
                                    {
                                        await ctx.Response.SendChunk(Array.Empty<byte>(), true).ConfigureAwait(false);
                                    }
                                }
                            }
                            else
                            {
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
                            }

                            #endregion

                            return true;
                        }
                        else
                        {
                            _Logging.Warn(_Header + "no response from origin " + url);
                            return false;
                        }

                        #endregion
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
                    Interlocked.Decrement(ref backend.ActiveRequests);
                }

                #endregion
            }
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
