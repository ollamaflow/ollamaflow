namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Services.Transformation;
    using RestWrapper;
    using SyslogLogging;
    using Timestamps;
    using WatsonWebserver.Core;

    /// <summary>
    /// Service responsible for proxying requests to backend servers.
    /// </summary>
    public class ProxyService
    {
        #region Private-Members

        private readonly string _Header = "[ProxyService] ";
        private LoggingModule _Logging = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="logging">Logging.</param>
        public ProxyService(LoggingModule logging)
        {
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Proxy a request to a backend server with full functionality including transformation support.
        /// </summary>
        /// <param name="requestGuid">Request unique identifier.</param>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="frontend">Frontend configuration.</param>
        /// <param name="backend">Backend server.</param>
        /// <param name="telemetry">Telemetry message.</param>
        /// <param name="overrideRequestBody">Optional override request body.</param>
        /// <param name="overrideUrlPath">Optional override URL path.</param>
        /// <param name="captureResponseForTransformation">Whether to capture response for transformation.</param>
        /// <returns>Proxy result.</returns>
        public async Task<ProxyResult> ProxyRequestAsync(
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
                        // Configure request using helper method
                        ConfigureProxyRequest(req, ctx, frontend, backend, overrideRequestBody);

                        byte[] requestBody = overrideRequestBody ?? ctx.Request.DataAsBytes;
                        LogRequestBodyIfEnabled(frontend, backend, requestBody, req.ContentType);

                        telemetry.BackendRequestSentUtc = DateTime.UtcNow;

                        if (requestBody != null && requestBody.Length > 0)
                        {
                            resp = await req.SendAsync(requestBody);
                        }
                        else
                        {
                            resp = await req.SendAsync();
                        }

                        if (resp != null)
                        {
                            LogResponseBodyIfEnabled(frontend, backend, resp);

                            // If capturing for transformation, store response and return without sending
                            if (captureResponseForTransformation)
                            {
                                if (!resp.ServerSentEvents && !resp.ChunkedTransferEncoding)
                                {
                                    // Non-streaming response - safe to capture
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
                                else
                                {
                                    // Streaming response - cannot capture for transformation
                                    _Logging.Warn(_Header + "cannot capture streaming response for transformation, backend " + backend.Identifier);
                                    return new ProxyResult
                                    {
                                        ResponseReceived = false,
                                        StatusCode = resp.StatusCode
                                    };
                                }
                            }

                            // Copy headers and set response properties
                            CopyResponseHeaders(ctx.Response, resp, backend);

                            // Handle different response types
                            if (!resp.ServerSentEvents)
                            {
                                if (!ctx.Response.ChunkedTransfer)
                                {
                                    try
                                    {
                                        await ctx.Response.Send(resp.DataAsBytes);
                                    }
                                    catch (InvalidOperationException ex) when (ex.Message.Contains("server-sent events"))
                                    {
                                        _Logging.Error(_Header + "unexpected SSE response detected in non-SSE path: " + ex.Message);
                                        // Fall back to SSE handling
                                        await HandleServerSentEventResponse(resp, ctx.Response, telemetry);
                                    }
                                }
                                else
                                {
                                    await HandleChunkedResponse(resp, ctx.Response, telemetry);
                                }
                            }
                            else
                            {
                                await HandleServerSentEventResponse(resp, ctx.Response, telemetry);
                            }

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
        public async Task<bool> ProxyRequestWithStreamingTransformationAsync(
            Guid requestGuid,
            HttpContextBase originalContext,
            Frontend frontend,
            Backend backend,
            TelemetryMessage telemetry,
            TransformationPipelineResult transformationResult,
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
                        // Configure request using helper method
                        ConfigureProxyRequest(req, originalContext, frontend, backend, transformedRequestBody);

                        LogRequestBodyIfEnabled(frontend, backend, transformedRequestBody, req.ContentType);

                        telemetry.BackendRequestSentUtc = DateTime.UtcNow;

                        if (transformedRequestBody != null && transformedRequestBody.Length > 0)
                        {
                            resp = await req.SendAsync(transformedRequestBody);
                        }
                        else
                        {
                            resp = await req.SendAsync();
                        }

                        if (resp != null)
                        {
                            // Log response body if enabled
                            LogResponseBodyIfEnabled(frontend, backend, resp);

                            // Setup client response headers
                            SetupStreamingResponseHeaders(originalContext.Response, resp, backend, sourceFormat);

                            // Handle streaming transformation based on response type
                            if (resp.ServerSentEvents)
                            {
                                return await HandleStreamingTransformationWithSSE(
                                    resp, originalContext.Response, telemetry, transformationResult,
                                    sourceFormat, targetFormat, requestType);
                            }
                            else if (resp.ChunkedTransferEncoding)
                            {
                                return await HandleStreamingTransformationWithChunks(
                                    resp, originalContext.Response, telemetry, transformationResult,
                                    sourceFormat, targetFormat, requestType);
                            }
                            else
                            {
                                // Non-streaming response - handle as regular transformation
                                _Logging.Warn(_Header + "expected streaming response but got non-streaming response");
                                await originalContext.Response.Send(resp.DataAsBytes);
                                return true;
                            }
                        }
                        else
                        {
                            _Logging.Warn(_Header + "no response from backend " + url);
                            return false;
                        }
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("server-sent events"))
                {
                    _Logging.Debug(_Header + $"SendAsync failed for SSE response as expected, handling directly: {ex.Message}");

                    // For SSE responses, we can't use SendAsync(), but we can access the response directly
                    // The RestRequest creates the response even when SendAsync() fails
                    resp = null; // We'll handle this differently

                    // Since we can't get the response object directly, we need to handle this error
                    // by implementing proper SSE handling in the transformation pipeline
                    _Logging.Error(_Header + "streaming transformation with SSE not fully implemented yet");
                    return false;
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
        /// Configure a RestRequest with common headers and settings.
        /// </summary>
        /// <param name="request">The RestRequest to configure.</param>
        /// <param name="ctx">HTTP context for source request.</param>
        /// <param name="frontend">Frontend configuration.</param>
        /// <param name="backend">Backend server.</param>
        /// <param name="overrideRequestBody">Optional override request body.</param>
        public void ConfigureProxyRequest(RestRequest request, HttpContextBase ctx, Frontend frontend, Backend backend, byte[] overrideRequestBody = null)
        {
            if (frontend.TimeoutMs > 0)
                request.TimeoutMilliseconds = frontend.TimeoutMs;

            request.Headers.Add(Constants.ForwardedForHeader, ctx.Request.Source.IpAddress);

            List<string> excludedHeaders = new List<string>();
            
            // Copy headers from original request, excluding problematic headers
            if (ctx.Request.Headers != null && ctx.Request.Headers.Count > 0)
            {
                foreach (string key in ctx.Request.Headers.Keys)
                {
                    if (!request.Headers.AllKeys.Contains(key))
                    {
                        if (ShouldExcludeHeader(key))
                        {
                            excludedHeaders.Add(key);
                        }
                        else
                        {
                            string val = ctx.Request.Headers.Get(key);
                            request.Headers.Add(key, val);
                        }
                    }
                }
            }

            if (excludedHeaders.Count > 0)
            {
                _Logging.Debug(_Header + "excluded " + excludedHeaders.Count + " header(s): " +
                               string.Join(", ", excludedHeaders));
            }

            // Set correct host header for backend
            foreach (string key in request.Headers.AllKeys)
            {
                if (key.ToLower().Equals("host"))
                {
                    request.Headers.Remove(key);
                    request.Headers.Add("Host", backend.Hostname + ":" + backend.Port.ToString());
                    break;
                }
            }

            // Set content type based on request body type
            byte[] requestBody = overrideRequestBody ?? ctx.Request.DataAsBytes;
            if (requestBody != null && requestBody.Length > 0)
            {
                if (overrideRequestBody != null)
                {
                    // Transformed request, always JSON
                    request.ContentType = Constants.JsonContentType;
                }
                else if (!String.IsNullOrEmpty(ctx.Request.ContentType))
                {
                    request.ContentType = ctx.Request.ContentType;
                }
                else
                {
                    request.ContentType = Constants.BinaryContentType;
                }
            }
        }

        /// <summary>
        /// Log request body if logging is enabled.
        /// </summary>
        /// <param name="frontend">Frontend configuration.</param>
        /// <param name="backend">Backend configuration.</param>
        /// <param name="requestBody">Request body bytes.</param>
        /// <param name="contentType">Content type of request.</param>
        public void LogRequestBodyIfEnabled(Frontend frontend, Backend backend, byte[] requestBody, string contentType)
        {
            if (frontend.LogRequestBody || backend.LogRequestBody)
            {
                byte[] dataBytes = requestBody ?? Array.Empty<byte>();
                int length = dataBytes.Length;

                _Logging.Debug(
                    _Header
                    + "request body (" + length + " bytes): "
                    + Environment.NewLine
                    + Encoding.UTF8.GetString(dataBytes));
            }
        }

        /// <summary>
        /// Log response body if logging is enabled.
        /// </summary>
        /// <param name="frontend">Frontend configuration.</param>
        /// <param name="backend">Backend configuration.</param>
        /// <param name="response">Response from backend.</param>
        public void LogResponseBodyIfEnabled(Frontend frontend, Backend backend, RestResponse response)
        {
            if (frontend.LogResponseBody || backend.LogResponseBody)
            {
                // Check if this is a streaming response (SSE or chunked)
                if (response.ServerSentEvents)
                {
                    _Logging.Debug(
                        _Header
                        + "response is server-sent events (streaming), status " + response.StatusCode);
                }
                else if (response.ChunkedTransferEncoding)
                {
                    _Logging.Debug(
                        _Header
                        + "response is chunked transfer encoding (streaming), status " + response.StatusCode);
                }
                else
                {
                    // Safe to read DataAsBytes for non-streaming responses
                    try
                    {
                        if (response.DataAsBytes != null && response.DataAsBytes.Length > 0)
                        {
                            _Logging.Debug(
                                _Header
                                + "response body (" + response.DataAsBytes.Length + " bytes) status " + response.StatusCode + ": "
                                + Environment.NewLine
                                + Encoding.UTF8.GetString(response.DataAsBytes));
                        }
                        else
                        {
                            _Logging.Debug(
                                _Header
                                + "response body (0 bytes) status " + response.StatusCode);
                        }
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("server-sent events") || ex.Message.Contains("chunked"))
                    {
                        _Logging.Debug(
                            _Header
                            + "response is streaming (detected via exception), status " + response.StatusCode);
                    }
                }
            }
        }

        /// <summary>
        /// Copy response headers from backend response to client response, avoiding duplicates.
        /// </summary>
        /// <param name="clientResponse">Client HTTP response.</param>
        /// <param name="backendResponse">Backend response.</param>
        /// <param name="backend">Backend server information.</param>
        public void CopyResponseHeaders(HttpResponseBase clientResponse, RestResponse backendResponse, Backend backend)
        {
            // Copy headers from backend response
            if (backendResponse.Headers != null && backendResponse.Headers.Count > 0)
            {
                foreach (string headerName in backendResponse.Headers.AllKeys)
                {
                    if (headerName != null && clientResponse.Headers[headerName] == null)
                    {
                        clientResponse.Headers.Add(headerName, backendResponse.Headers[headerName]);
                    }
                }
            }

            // Set response properties
            clientResponse.StatusCode = backendResponse.StatusCode;
            clientResponse.ContentType = backendResponse.ContentType;
            clientResponse.Headers.Add(Constants.BackendServerHeader, backend.Identifier);
            clientResponse.ChunkedTransfer = backendResponse.ChunkedTransferEncoding;
        }

        /// <summary>
        /// Handle streaming response with chunked transfer encoding.
        /// </summary>
        /// <param name="backendResponse">Response from backend.</param>
        /// <param name="clientResponse">Client HTTP response.</param>
        /// <param name="telemetry">Telemetry message.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task HandleChunkedResponse(RestResponse backendResponse, HttpResponseBase clientResponse, TelemetryMessage telemetry)
        {
            while (true)
            {
                ChunkData chunk = await backendResponse.ReadChunkAsync().ConfigureAwait(false);
                if (telemetry.FirstTokenTimeUtc == null)
                    telemetry.FirstTokenTimeUtc = DateTime.UtcNow;

                if (chunk == null || chunk.IsFinal)
                {
                    if (chunk?.Data != null && chunk.Data.Length > 0)
                    {
                        // For NDJSON format, append Environment.NewLine to final chunk
                        byte[] newlineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);
                        byte[] finalData = new byte[chunk.Data.Length + newlineBytes.Length];
                        Array.Copy(chunk.Data, finalData, chunk.Data.Length);
                        Array.Copy(newlineBytes, 0, finalData, chunk.Data.Length, newlineBytes.Length);
                        await clientResponse.SendChunk(finalData, true).ConfigureAwait(false);
                    }
                    else
                    {
                        // Send empty final chunk
                        await clientResponse.SendChunk(Array.Empty<byte>(), true).ConfigureAwait(false);
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
                    await clientResponse.SendChunk(chunkWithNewline, false).ConfigureAwait(false);
                }
            }

            telemetry.LastTokenTimeUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Handle streaming response with server-sent events.
        /// </summary>
        /// <param name="backendResponse">Response from backend.</param>
        /// <param name="clientResponse">Client HTTP response.</param>
        /// <param name="telemetry">Telemetry message.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task HandleServerSentEventResponse(RestResponse backendResponse, HttpResponseBase clientResponse, TelemetryMessage telemetry)
        {
            clientResponse.ProtocolVersion = "HTTP/1.1";
            clientResponse.ServerSentEvents = true;

            while (true)
            {
                ServerSentEvent sse = await backendResponse.ReadEventAsync();
                if (telemetry.FirstTokenTimeUtc == null)
                    telemetry.FirstTokenTimeUtc = DateTime.UtcNow;

                if (sse == null)
                {
                    break;
                }
                else
                {
                    if (!String.IsNullOrEmpty(sse.Data))
                    {
                        await clientResponse.SendEvent(sse.Data, false);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            telemetry.LastTokenTimeUtc = DateTime.UtcNow;
            await clientResponse.SendEvent(null, true);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Determine if a header should be excluded from being forwarded to the backend.
        /// </summary>
        /// <param name="headerName">The header name to check.</param>
        /// <returns>True if the header should be excluded.</returns>
        private bool ShouldExcludeHeader(string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
                return true;

            string lowerHeaderName = headerName.ToLowerInvariant();

            // Exclude authentication and authorization headers
            if (lowerHeaderName.Equals("authorization") ||
                lowerHeaderName.Equals("x-api-key") ||
                lowerHeaderName.Equals("x-auth-token") ||
                lowerHeaderName.StartsWith("x-ollamaflow-") ||
                lowerHeaderName.Equals("cookie") ||
                lowerHeaderName.Equals("set-cookie"))
            {
                return true;
            }

            // Exclude CORS-related headers that could interfere
            if (lowerHeaderName.StartsWith("access-control-") ||
                lowerHeaderName.Equals("origin") ||
                lowerHeaderName.Equals("referer"))
            {
                return true;
            }

            // Exclude connection-related headers that could cause issues
            if (lowerHeaderName.Equals("connection") ||
                lowerHeaderName.Equals("upgrade") ||
                lowerHeaderName.Equals("proxy-authorization") ||
                lowerHeaderName.Equals("proxy-authenticate"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Convert Watson HTTP method to System.Net.Http method.
        /// </summary>
        /// <param name="method">Watson HTTP method.</param>
        /// <returns>System.Net.Http method.</returns>
        /// <exception cref="ArgumentException">Thrown when HTTP method is unknown.</exception>
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
        /// Setup response headers for streaming transformation.
        /// </summary>
        /// <param name="clientResponse">Client HTTP response.</param>
        /// <param name="backendResponse">Backend response.</param>
        /// <param name="backend">Backend server information.</param>
        /// <param name="sourceFormat">Source API format (client).</param>
        private void SetupStreamingResponseHeaders(HttpResponseBase clientResponse, RestResponse backendResponse, Backend backend, ApiFormatEnum sourceFormat)
        {
            // Copy headers from backend response (excluding content-specific headers)
            if (backendResponse.Headers != null && backendResponse.Headers.Count > 0)
            {
                foreach (string headerName in backendResponse.Headers.AllKeys)
                {
                    if (headerName != null && clientResponse.Headers[headerName] == null &&
                        !headerName.Equals("content-length", StringComparison.OrdinalIgnoreCase) &&
                        !headerName.Equals("content-type", StringComparison.OrdinalIgnoreCase) &&
                        !headerName.Equals("transfer-encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        clientResponse.Headers.Add(headerName, backendResponse.Headers[headerName]);
                    }
                }
            }

            // Set response properties based on source format
            clientResponse.StatusCode = backendResponse.StatusCode;
            clientResponse.Headers.Add(Constants.BackendServerHeader, backend.Identifier);

            // Set content type based on source format
            if (sourceFormat == ApiFormatEnum.OpenAI)
            {
                clientResponse.ContentType = "text/plain; charset=utf-8";
                clientResponse.ServerSentEvents = true;
            }
            else
            {
                clientResponse.ContentType = "application/x-ndjson";
                clientResponse.ChunkedTransfer = true;
            }
        }

        /// <summary>
        /// Handle streaming transformation with Server-Sent Events.
        /// </summary>
        /// <param name="backendResponse">Response from backend.</param>
        /// <param name="clientResponse">Client HTTP response.</param>
        /// <param name="telemetry">Telemetry message.</param>
        /// <param name="transformationResult">Transformation pipeline result.</param>
        /// <param name="sourceFormat">Source API format (client).</param>
        /// <param name="targetFormat">Target API format (backend).</param>
        /// <param name="requestType">Type of request being processed.</param>
        /// <returns>True if successful.</returns>
        private async Task<bool> HandleStreamingTransformationWithSSE(
            RestResponse backendResponse,
            HttpResponseBase clientResponse,
            TelemetryMessage telemetry,
            TransformationPipelineResult transformationResult,
            ApiFormatEnum sourceFormat,
            ApiFormatEnum targetFormat,
            RequestTypeEnum requestType)
        {
            try
            {
                Services.Transformation.Streaming.StreamingTransformer streamingTransformer =
                    new Services.Transformation.Streaming.StreamingTransformer();

                while (true)
                {
                    ServerSentEvent sse = await backendResponse.ReadEventAsync();
                    if (telemetry.FirstTokenTimeUtc == null)
                        telemetry.FirstTokenTimeUtc = DateTime.UtcNow;

                    if (sse == null)
                    {
                        break;
                    }

                    if (!String.IsNullOrEmpty(sse.Data))
                    {
                        // Transform the SSE data from target format back to source format
                        Services.Transformation.Interfaces.StreamingChunkResult transformedChunk =
                            await streamingTransformer.TransformChunkAsync(sse.Data, targetFormat, sourceFormat, requestType);

                        if (!string.IsNullOrEmpty(transformedChunk.Error))
                        {
                            _Logging.Error(_Header + $"streaming transformation error: {transformedChunk.Error}");
                            break;
                        }

                        if (transformedChunk.ChunkData?.Length > 0)
                        {
                            if (sourceFormat == ApiFormatEnum.OpenAI)
                            {
                                // Send as SSE
                                string eventData = Encoding.UTF8.GetString(transformedChunk.ChunkData);
                                await clientResponse.SendEvent(eventData, transformedChunk.IsFinal);
                            }
                            else
                            {
                                // Send as chunk
                                await clientResponse.SendChunk(transformedChunk.ChunkData, transformedChunk.IsFinal);
                            }
                        }

                        if (transformedChunk.IsFinal)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                telemetry.LastTokenTimeUtc = DateTime.UtcNow;

                // Send final chunk/event if needed
                if (sourceFormat == ApiFormatEnum.OpenAI)
                {
                    await clientResponse.SendEvent(null, true);
                }
                else
                {
                    await clientResponse.SendChunk(Array.Empty<byte>(), true);
                }

                return true;
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"error in SSE streaming transformation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handle streaming transformation with chunked transfer encoding.
        /// </summary>
        /// <param name="backendResponse">Response from backend.</param>
        /// <param name="clientResponse">Client HTTP response.</param>
        /// <param name="telemetry">Telemetry message.</param>
        /// <param name="transformationResult">Transformation pipeline result.</param>
        /// <param name="sourceFormat">Source API format (client).</param>
        /// <param name="targetFormat">Target API format (backend).</param>
        /// <param name="requestType">Type of request being processed.</param>
        /// <returns>True if successful.</returns>
        private async Task<bool> HandleStreamingTransformationWithChunks(
            RestResponse backendResponse,
            HttpResponseBase clientResponse,
            TelemetryMessage telemetry,
            TransformationPipelineResult transformationResult,
            ApiFormatEnum sourceFormat,
            ApiFormatEnum targetFormat,
            RequestTypeEnum requestType)
        {
            try
            {
                Services.Transformation.Streaming.StreamingTransformer streamingTransformer =
                    new Services.Transformation.Streaming.StreamingTransformer();

                while (true)
                {
                    ChunkData chunk = await backendResponse.ReadChunkAsync().ConfigureAwait(false);
                    if (telemetry.FirstTokenTimeUtc == null)
                        telemetry.FirstTokenTimeUtc = DateTime.UtcNow;

                    if (chunk == null || chunk.IsFinal)
                    {
                        if (chunk?.Data != null && chunk.Data.Length > 0)
                        {
                            // Transform final chunk
                            Services.Transformation.Interfaces.StreamingChunkResult transformedChunk =
                                await streamingTransformer.TransformChunkAsync(chunk.Data, targetFormat, sourceFormat, requestType);

                            if (!string.IsNullOrEmpty(transformedChunk.Error))
                            {
                                _Logging.Error(_Header + $"streaming transformation error: {transformedChunk.Error}");
                            }
                            else if (transformedChunk.ChunkData?.Length > 0)
                            {
                                if (sourceFormat == ApiFormatEnum.OpenAI)
                                {
                                    string eventData = Encoding.UTF8.GetString(transformedChunk.ChunkData);
                                    await clientResponse.SendEvent(eventData, true);
                                }
                                else
                                {
                                    await clientResponse.SendChunk(transformedChunk.ChunkData, true);
                                }
                            }
                        }

                        // Send final chunk/event
                        if (sourceFormat == ApiFormatEnum.OpenAI)
                        {
                            await clientResponse.SendEvent(null, true);
                        }
                        else
                        {
                            await clientResponse.SendChunk(Array.Empty<byte>(), true);
                        }
                        break;
                    }
                    else if (chunk.Data != null && chunk.Data.Length > 0)
                    {
                        // Transform chunk
                        Services.Transformation.Interfaces.StreamingChunkResult transformedChunk =
                            await streamingTransformer.TransformChunkAsync(chunk.Data, targetFormat, sourceFormat, requestType);

                        if (!string.IsNullOrEmpty(transformedChunk.Error))
                        {
                            _Logging.Error(_Header + $"streaming transformation error: {transformedChunk.Error}");
                            break;
                        }

                        if (transformedChunk.ChunkData?.Length > 0)
                        {
                            if (sourceFormat == ApiFormatEnum.OpenAI)
                            {
                                string eventData = Encoding.UTF8.GetString(transformedChunk.ChunkData);
                                await clientResponse.SendEvent(eventData, false);
                            }
                            else
                            {
                                await clientResponse.SendChunk(transformedChunk.ChunkData, false);
                            }
                        }

                        if (transformedChunk.IsFinal)
                        {
                            break;
                        }
                    }
                }

                telemetry.LastTokenTimeUtc = DateTime.UtcNow;
                return true;
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"error in chunked streaming transformation: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}