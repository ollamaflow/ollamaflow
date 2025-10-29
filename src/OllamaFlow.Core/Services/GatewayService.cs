namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using JsonMerge;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Handlers;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Models.Ollama;
    using OllamaFlow.Core.Serialization;
    using RestWrapper;
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
        private ServiceContext _Services = null;
        private HandlerContext _Handlers = null;
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="callbacks">Callbacks.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="services">Service context.</param>
        /// <param name="handlers">Handler context.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public GatewayService(
            OllamaFlowSettings settings,
            OllamaFlowCallbacks callbacks,
            LoggingModule logging,
            Serializer serializer,
            ServiceContext services,
            HandlerContext handlers,
            CancellationTokenSource tokenSource = default)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Services = services ?? throw new ArgumentNullException(nameof(services));
            _Handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
            _TokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        #endregion

        #region Public-Methods

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

        #endregion

        #region Internal-Methods

        /// <summary>
        /// Initialize.
        /// </summary>
        internal void Initialize()
        {
            _Logging.Debug(_Header + "initialized");
        }

        /// <summary>
        /// OPTIONS route handler for CORS support.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        internal async Task OptionsRoute(HttpContextBase ctx)
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
            await ctx.Response.Send(_TokenSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Pre-routing handler for request setup.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        internal async Task PreRoutingHandler(HttpContextBase ctx)
        {
            ctx.Metadata = new RequestContext(_Settings, ctx);
            ctx.Response.ContentType = Constants.JsonContentType;
        }

        /// <summary>
        /// Post-routing handler for cleanup.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        internal async Task PostRoutingHandler(HttpContextBase ctx)
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
        /// Exception route handler for unhandled exceptions.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="e">Exception.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        internal async Task ExceptionRoute(
            HttpContextBase ctx, 
            Exception e, 
            CancellationToken token = default)
        {
            _Logging.Warn(_Header + "exception of type " + e.GetType().Name + " encountered:" + Environment.NewLine + e.ToString());

            switch (e)
            {
                case ArgumentNullException:
                case ArgumentException:
                case InvalidOperationException:
                case JsonException:
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, e.Message), true), token).ConfigureAwait(false);
                    return;
                case KeyNotFoundException:
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, e.Message), true), token).ConfigureAwait(false);
                    return;
                case UnauthorizedAccessException:
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.AuthenticationFailed, null), true), token).ConfigureAwait(false);
                    return;
                case DuplicateNameException:
                    ctx.Response.StatusCode = 409;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Conflict, null, e.Message), true), token).ConfigureAwait(false);
                    return;
                default:
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message), true), token).ConfigureAwait(false);
                    return;
            }
        }

        /// <summary>
        /// Default route handler - main entry point for AI API requests.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        internal async Task DefaultRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            TelemetryMessage telemetry = null;

            try
            {
                telemetry = new TelemetryMessage
                {
                    ClientId = ctx.Request.Source.IpAddress,
                    RequestBodySize = ctx.Request.ContentLength,
                    RequestArrivalUtc = DateTime.UtcNow,
                    RequestType = req.RequestType,
                    ApiFormat = req.ApiFormat
                };

                ctx.Response.Headers.Add(Constants.RequestIdHeader, req.GUID.ToString());
                ctx.Response.Headers.Add(Constants.ForwardedForHeader, ctx.Request.Source.IpAddress);
                ctx.Response.Headers.Add(Constants.ExposeHeadersHeader, "*");

                #endregion

                #region Unauthenticated-APIs

                switch (req.RequestType)
                {
                    case RequestTypeEnum.Root:
                        await _Handlers.StaticRoute.GetRootRoute(ctx, _TokenSource.Token).ConfigureAwait(false);
                        return;
                    case RequestTypeEnum.ValidateConnectivity:
                        await _Handlers.StaticRoute.HeadRootRoute(ctx, _TokenSource.Token).ConfigureAwait(false);
                        return;
                    case RequestTypeEnum.GetFavicon:
                        await _Handlers.StaticRoute.GetFaviconRoute(ctx, _TokenSource.Token).ConfigureAwait(false);
                        return;
                    case RequestTypeEnum.ExistsFavicon:
                        await _Handlers.StaticRoute.HeadFaviconRoute(ctx, _TokenSource.Token).ConfigureAwait(false);
                        return;
                }

                #endregion

                #region Admin-APIs

                if (await HandleAdminApiRequest(ctx, req, _TokenSource.Token).ConfigureAwait(false))
                {
                    return;
                }

                #endregion

                #region Unknown-Requests

                if (req.RequestType == RequestTypeEnum.Unknown
                    || (req.ApiFormat != ApiFormatEnum.Ollama && req.ApiFormat != ApiFormatEnum.OpenAI))
                {
                    _Logging.Warn($"{_Header}unknown HTTP method or URL {ctx.Request.Method} {ctx.Request.Url.RawWithQuery} from {ctx.Request.Source.IpAddress}");
                    await SendBadRequest(ctx, "Unknown HTTP method or URL.", _TokenSource.Token).ConfigureAwait(false);
                    return;
                }

                #endregion

                #region Get-Frontend

                Frontend frontend = await GetMatchingFrontend(ctx, _TokenSource.Token).ConfigureAwait(false);
                if (frontend == null)
                {
                    _Logging.Warn($"{_Header}no frontend found for {ctx.Request.Method.ToString()} {ctx.Request.Url.Full}");
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "No matching API endpoint found"), true), _TokenSource.Token).ConfigureAwait(false);
                    return;
                }

                ctx.Response.Headers.Add(Constants.StickyServerHeader, frontend.UseStickySessions.ToString());

                #endregion

                #region Check-Request-Size

                if (frontend.MaxRequestBodySize > 0 && req.ContentLength > frontend.MaxRequestBodySize)
                {
                    _Logging.Warn($"{_Header}request too large for frontend {frontend.Identifier} from {ctx.Request.Source.IpAddress}: {req.ContentLength} bytes");
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.TooLarge, null, "Your request was too large"), true), _TokenSource.Token).ConfigureAwait(false);
                    return;
                }

                #endregion

                #region Validate-Request-Against-Frontend

                if (req.IsEmbeddingsRequest && !frontend.AllowEmbeddings)
                {
                    _Logging.Warn($"{_Header}embeddings request explicitly denied on frontend {frontend.Identifier}");
                    await SendNotAuthorized(ctx, "Embeddings request are not permitted on this frontend", _TokenSource.Token).ConfigureAwait(false);
                    return;
                }

                if (req.IsCompletionsRequest && !frontend.AllowCompletions)
                {
                    _Logging.Warn($"{_Header}completions request explicitly denied on frontend {frontend.Identifier}");
                    await SendNotAuthorized(ctx, "Completions requests are not permitted on this frontend", _TokenSource.Token).ConfigureAwait(false);
                    return;
                }

                #endregion

                #region Model-Pull-and-Delete-Requests

                if (req.RequestType == RequestTypeEnum.OllamaPullModel)
                {
                    OllamaPullModelRequest opmr = _Serializer.DeserializeJson<OllamaPullModelRequest>(ctx.Request.DataAsString);
                    if (opmr == null)
                    {
                        _Logging.Warn($"{_Header}no model supplied in pull model request");
                        await SendBadRequest(ctx, "No model property supplied in pull model request.", _TokenSource.Token).ConfigureAwait(false);
                        return;
                    }

                    string modelName = null;
                    if (!String.IsNullOrEmpty(opmr.Name)) modelName = opmr.Name;
                    if (!String.IsNullOrEmpty(opmr.Model)) modelName = opmr.Model;

                    if (String.IsNullOrEmpty(modelName))
                    {
                        _Logging.Warn($"{_Header}no model supplied in pull model request");
                        await SendBadRequest(ctx, "No model property supplied in pull model request.", _TokenSource.Token).ConfigureAwait(false);
                        return;
                    }

                    lock (frontend.Lock)
                    {
                        if (!frontend.RequiredModels.Contains(modelName))
                        {
                            frontend.RequiredModels.Add(modelName);
                            Frontend updated = _Services.Frontend.Update(frontend);
                            _Logging.Debug($"{_Header}added model {modelName} to required models for frontend {frontend.Identifier}");
                        }
                        else
                        {
                            _Logging.Debug($"{_Header}model {modelName} already required by frontend {frontend.Identifier}");
                        }
                    }
                }

                if (req.RequestType == RequestTypeEnum.OllamaDeleteModel)
                {
                    OllamaDeleteModelRequest odmr = _Serializer.DeserializeJson<OllamaDeleteModelRequest>(ctx.Request.DataAsString);
                    if (odmr == null || String.IsNullOrEmpty(odmr.Model))
                    {
                        _Logging.Warn($"{_Header}no model supplied in delete model request");
                        await SendBadRequest(ctx, "No model property supplied in delete model request.", _TokenSource.Token).ConfigureAwait(false);
                        return;
                    }

                    lock (frontend.Lock)
                    {
                        if (frontend.RequiredModels.Contains(odmr.Model))
                        {
                            frontend.RequiredModels.Remove(odmr.Model);
                            Frontend updated = _Services.Frontend.Update(frontend);
                            _Logging.Debug($"{_Header}removed model {odmr.Model} from required models for frontend {frontend.Identifier}");
                        }
                        else
                        {
                            _Logging.Debug($"{_Header}model {odmr.Model} not listed as required by frontend {frontend.Identifier}");
                        }
                    }
                }

                #endregion

                #region Process-Request

                int retries = 1;
                if (frontend.AllowRetries)
                {
                    for (int i = 0; i < retries; i++)
                    {
                        _Logging.Debug($"{_Header}attempt {i} of {retries} for {req.RequestType} from {req.ClientIdentifier}");
                        bool success = await ProcessRequest(ctx, req, frontend, telemetry, _TokenSource.Token).ConfigureAwait(false);
                        if (success) return;
                    }
                }

                _Logging.Warn($"{_Header}unable to satisfy request {req.RequestType} for frontend {frontend.Identifier} for {req.ClientIdentifier}");

                ctx.Response.StatusCode = 502;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadGateway, null, "No healthy backend servers available"), true), _TokenSource.Token).ConfigureAwait(false);
                return;

                #endregion
            }
            catch (Exception e)
            {
                await ExceptionRoute(ctx, e, _TokenSource.Token).ConfigureAwait(false);
                return;
            }
            finally
            {
                if (telemetry != null)
                    _Logging.Debug(_Header + _Serializer.SerializeJson(telemetry, false));
            }
        }

        #region Private-Methods

        /// <summary>
        /// Handler for administrative API requests.
        /// Return true if the request is handled, otherwise, return false.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="req">Request context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the request is handled and complete, false otherwise.</returns>
        private async Task<bool> HandleAdminApiRequest(
            HttpContextBase ctx, 
            RequestContext req, 
            CancellationToken token = default)
        {
            switch (req.RequestType)
            {
                case RequestTypeEnum.AdminGetFrontends:
                    await _Handlers.AdminApi.GetFrontendsRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminGetFrontend:
                    await _Handlers.AdminApi.GetFrontendRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminExistsFrontend:
                    await _Handlers.AdminApi.ExistsFrontendRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminCreateFrontend:
                    await _Handlers.AdminApi.CreateFrontendRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminUpdateFrontend:
                    await _Handlers.AdminApi.UpdateFrontendRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminDeleteFrontend:
                    await _Handlers.AdminApi.DeleteFrontendRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminGetBackends:
                    await _Handlers.AdminApi.GetBackendsRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminGetBackend:
                    await _Handlers.AdminApi.GetBackendRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminExistsBackend:
                    await _Handlers.AdminApi.ExistsBackendRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminCreateBackend:
                    await _Handlers.AdminApi.CreateBackendRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminUpdateBackend:
                    await _Handlers.AdminApi.UpdateBackendRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminDeleteBackend:
                    await _Handlers.AdminApi.DeleteBackendRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminGetBackendsHealth:
                    await _Handlers.AdminApi.GetBackendsHealthRoute(ctx, token).ConfigureAwait(false);
                    return true;
                case RequestTypeEnum.AdminGetBackendHealth:
                    await _Handlers.AdminApi.GetBackendHealthRoute(ctx, token).ConfigureAwait(false);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Process a request.  This method will retrieve the backend, perform transformations as needed, and marshal the request to the backend.
        /// The return value indicates whether or not the request has been fully serviced to its completion.
        /// If the result from the backend should be retried, return false.  Examples include 500-series errors or no connectivity to a backend.
        /// If the result from the backend is complete, return true.  Examples include successful responses and 400-series errors.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="req">Request context.</param>
        /// <param name="frontend">Frontend.</param>
        /// <param name="telemetry">Telemetry message.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the request has been serviced to completion.</returns>
        private async Task<bool> ProcessRequest(
            HttpContextBase ctx,
            RequestContext req,
            Frontend frontend,
            TelemetryMessage telemetry, 
            CancellationToken token = default)
        {
            #region Get-Backend

            Backend backend = _Services.HealthCheck.GetNextBackend(req, frontend); // considers stickiness, labels
            if (backend == null)
            {
                _Logging.Warn($"{_Header}no healthy backend found for frontend {frontend.Identifier}");
                return false; // retry
            }
            else
            {
                _Logging.Debug($"{_Header}using backend {backend.Identifier} for frontend {frontend.Identifier}");
            }

            telemetry.BackendServerId = backend.Identifier;
            telemetry.BackendSelectedUtc = DateTime.UtcNow;
            telemetry.IsSticky = backend.IsSticky;

            #endregion

            #region Get-Request-Body

            string requestBody = "";
            if (!String.IsNullOrEmpty(ctx.Request.DataAsString)) requestBody = ctx.Request.DataAsString;

            #endregion

            #region Merge-Frontend-Pinned-Request-Parameters

            if (req.IsEmbeddingsRequest
                && !String.IsNullOrEmpty(requestBody)
                && frontend.PinnedEmbeddingsProperties.Any())
            {
                _Logging.Debug(_Header + "merging frontend pinned embeddings properties: " + frontend.PinnedEmbeddingsPropertiesString);
                requestBody = JsonMerger.MergeJson(requestBody, frontend.PinnedEmbeddingsPropertiesString);
                _Logging.Debug(_Header + "merged request: " + requestBody);
            }
            else if (req.IsCompletionsRequest
                && !String.IsNullOrEmpty(requestBody)
                && frontend.PinnedCompletionsProperties.Any())
            {
                _Logging.Debug(_Header + "merging frontend pinned completions properties: " + frontend.PinnedCompletionsPropertiesString);
                requestBody = JsonMerger.MergeJson(requestBody, frontend.PinnedCompletionsPropertiesString);
                _Logging.Debug(_Header + "merged request: " + requestBody);
            }

            #endregion

            #region Process-Request

            string url = UrlBuilder.BuildUrl(backend, req.RequestType);
            System.Net.Http.HttpMethod method = UrlBuilder.GetMethod(backend, req.RequestType);

            RestResponse restResponse = null;

            try
            {
                using (RestRequest restRequest = new RestRequest(url, method))
                {
                    #region Set-Content-Type-and-Length

                    if (method == System.Net.Http.HttpMethod.Put
                        || method == System.Net.Http.HttpMethod.Post)
                    {
                        restRequest.ContentType = Constants.JsonContentType;
                    }

                    if (requestBody.Length > 0)
                    {
                        restRequest.ContentLength = requestBody.Length;
                    }

                    #endregion

                    #region Send

                    // _Logging.Debug($"{_Header}sending request to backend {backend.Identifier} for frontend {frontend.Identifier} using {method} {url}{Environment.NewLine}{requestBody}");
                    _Logging.Debug($"{_Header}sending request to backend {backend.Identifier} for frontend {frontend.Identifier} using {method} {url}");

                    if (!String.IsNullOrEmpty(requestBody))
                    {
                        restResponse = await restRequest.SendAsync(requestBody, token).ConfigureAwait(false);
                    }
                    else
                    {
                        restResponse = await restRequest.SendAsync(token).ConfigureAwait(false);
                    }

                    _Logging.Debug($"{_Header}request sent to backend {backend.Identifier} for frontend {frontend.Identifier} using {method} {url}");

                    if (restResponse != null)
                    {
                        _Logging.Debug($"{_Header}response with status {restResponse.StatusCode} received from {method.ToString()} {url} for backend {backend.Identifier}");
                    }
                    else
                    {
                        _Logging.Warn($"{_Header}no response received from {method.ToString()} {url} for backend {backend.Identifier}");
                        return false; // retry
                    }

                    if (restResponse.StatusCode >= 500)
                    {
                        _Logging.Warn($"{_Header}non-success server error status {restResponse.StatusCode} received from {method.ToString()} {url} for backend {backend.Identifier}");
                        return false; // retry
                    }

                    ctx.Response.StatusCode = restResponse.StatusCode;
                    ctx.Response.ContentType = restResponse.ContentType;
                    ctx.Response.ContentLength = restResponse.ContentLength != null ? restResponse.ContentLength.Value : 0;
                    ctx.Response.ServerSentEvents = restResponse.ServerSentEvents;
                    ctx.Response.ChunkedTransfer = restResponse.ChunkedTransferEncoding;
                    ctx.Response.Headers.Add(Constants.BackendServerHeader, backend.Identifier);

                    if (restResponse.Headers != null && restResponse.Headers.Count > 0)
                    {
                        foreach (string key in restResponse.Headers.AllKeys)
                        {
                            string val = restResponse.Headers[key];

                            // Skip excluded headers
                            if (string.Equals(key, "Host", StringComparison.InvariantCultureIgnoreCase) ||
                                string.Equals(key, "Date", StringComparison.InvariantCultureIgnoreCase) ||
                                string.Equals(key, "Connection", StringComparison.InvariantCultureIgnoreCase))
                            {
                                continue;
                            }

                            ctx.Response.Headers[key] = val;
                        }
                    }

                    if (restResponse.ServerSentEvents)
                    {
                        _Logging.Debug($"{_Header}backend {backend.Identifier} responded using sse for {req.RequestType.ToString()}");

                        while (true)
                        {
                            ServerSentEvent sse = await restResponse.ReadEventAsync(token).ConfigureAwait(false);
                            if (sse == null)
                            {
                                await ctx.Response.SendEvent("", true, token).ConfigureAwait(false);
                                break;
                            }
                            else
                            {
                                await ctx.Response.SendEvent(sse.Event, false, token).ConfigureAwait(false);
                            }
                        }
                    }
                    else if (restResponse.ChunkedTransferEncoding)
                    {
                        _Logging.Debug($"{_Header}backend {backend.Identifier} responded using chunked transfer encoding for {req.RequestType.ToString()}");

                        while (true)
                        {
                            ChunkData chunk = await restResponse.ReadChunkAsync(token).ConfigureAwait(false);
                            if (chunk == null)
                            {
                                await ctx.Response.SendChunk(Array.Empty<byte>(), true, token).ConfigureAwait(false);
                                break;
                            }
                            else
                            {
                                byte[] chunkBytes = new byte[chunk.Data.Length + 1];
                                Buffer.BlockCopy(chunk.Data, 0, chunkBytes, 0, chunk.Data.Length);
                                Buffer.BlockCopy(Encoding.UTF8.GetBytes("\n"), 0, chunkBytes, (chunkBytes.Length - 1), 1);
                                await ctx.Response.SendChunk(chunkBytes, chunk.IsFinal, token).ConfigureAwait(false);
                                if (chunk.IsFinal) break;
                            }
                        }
                    }
                    else
                    {
                        if (restResponse.ContentLength > 0)
                        {
                            await ctx.Response.Send(restResponse.DataAsBytes, token).ConfigureAwait(false);
                        }
                        else
                        {
                            await ctx.Response.Send(token).ConfigureAwait(false);
                        }
                    }

                    return true; // no retry

                    #endregion
                }
            }
            finally
            {
                if (restResponse != null)
                    restResponse.Dispose();
            }

            #endregion
        }

        private async Task SendNotAuthorized(HttpContextBase ctx, string msg, CancellationToken token = default)
        {
            ctx.Response.StatusCode = 401;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.AuthorizationFailed, null, msg), true), token).ConfigureAwait(false);
        }

        private async Task SendBadRequest(HttpContextBase ctx, string msg, CancellationToken token = default)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, msg), true), token).ConfigureAwait(false);
        }

        private async Task<Frontend> GetMatchingFrontend(HttpContextBase ctx, CancellationToken token = default)
        {
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

            List<Frontend> allFrontends = _Services.Frontend.GetAll().ToList();
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

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}