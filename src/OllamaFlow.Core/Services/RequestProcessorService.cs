namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Core.Services.Transformation;
    using OllamaFlow.Core.Services.Transformation.Interfaces;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Service responsible for processing requests with transformation, retry logic, and streaming support.
    /// </summary>
    public class RequestProcessorService
    {
        #region Private-Members

        private readonly string _Header = "[RequestProcessorService] ";
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private HealthCheckService _HealthCheck = null;
        private SessionStickinessService _SessionStickiness = null;
        private ITransformationPipeline _TransformationPipeline = null;
        private ProxyService _ProxyService = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="healthCheck">Health check service.</param>
        /// <param name="sessionStickiness">Session stickiness service.</param>
        /// <param name="transformationPipeline">Transformation pipeline for API format conversion.</param>
        /// <param name="proxyService">Proxy service.</param>
        public RequestProcessorService(
            LoggingModule logging,
            Serializer serializer,
            HealthCheckService healthCheck,
            SessionStickinessService sessionStickiness,
            ITransformationPipeline transformationPipeline,
            ProxyService proxyService)
        {
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _HealthCheck = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
            _SessionStickiness = sessionStickiness ?? throw new ArgumentNullException(nameof(sessionStickiness));
            _TransformationPipeline = transformationPipeline ?? throw new ArgumentNullException(nameof(transformationPipeline));
            _ProxyService = proxyService ?? throw new ArgumentNullException(nameof(proxyService));

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

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
        public async Task<bool> ProcessRequestWithTransformationAsync(
            Guid requestGuid,
            HttpContextBase ctx,
            Frontend frontend,
            Backend backend,
            string clientId,
            TelemetryMessage telemetry)
        {
            try
            {
                // Step 1: Use already detected source format from telemetry for efficiency
                ApiFormatEnum sourceFormat = telemetry.ApiFormat;
                ApiFormatEnum targetFormat = backend.ApiFormat;

                // Step 2: If formats match, use direct proxy (legacy behavior)
                if (sourceFormat == targetFormat)
                {
                    _Logging.Debug(_Header + $"no transformation required from API format {sourceFormat} for backend {backend.Identifier}");
                    return await ProcessRequestWithRetryAsync(requestGuid, ctx, frontend, backend, clientId, telemetry);
                }

                // Step 3: Transformation is required
                _Logging.Debug(_Header + $"transforming request from {sourceFormat} to {targetFormat} format for backend {backend.Identifier}");

                // Step 4: Detect if request is for streaming and determine the request type
                Models.Agnostic.Base.AgnosticRequest agnosticRequest = await _TransformationPipeline.TransformInboundAsync(ctx, sourceFormat).ConfigureAwait(false);
                RequestTypeEnum requestType = GetRequestTypeFromAgnosticRequest(agnosticRequest);
                bool isStreamingRequest = IsStreamingRequest(ctx, agnosticRequest, requestType);

                // Step 4: Choose appropriate processing method based on streaming support
                if (isStreamingRequest && _TransformationPipeline.SupportsStreamingTransformation(sourceFormat, targetFormat, requestType))
                {
                    _Logging.Debug(_Header + "using streaming transformation for request");

                    // Log original request body for debugging
                    if (frontend.LogRequestBody || backend.LogRequestBody)
                    {
                        byte[] originalRequestBody = ctx.Request.DataAsBytes ?? Array.Empty<byte>();
                        _Logging.Debug(_Header + $"original {sourceFormat} request body ({originalRequestBody.Length} bytes):" + Environment.NewLine +
                                     System.Text.Encoding.UTF8.GetString(originalRequestBody));
                    }

                    return await ProcessRequestWithStreamingTransformationAsync(
                        requestGuid, ctx, frontend, backend, clientId, telemetry, sourceFormat, targetFormat, requestType);
                }

                // Step 5: Fall back to non-streaming transformation
                object backendRequest = await _TransformationPipeline.TransformOutboundAsync(agnosticRequest, targetFormat).ConfigureAwait(false);

                // Create transformation result
                TransformationPipelineResult transformationResult = new TransformationPipelineResult
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
                ProxyResult proxyResult = await _ProxyService.ProxyRequestAsync(
                    requestGuid, ctx, frontend, backend, telemetry, transformedBytes, targetPath, captureResponseForTransformation: true);

                // Step 6: Transform response back to source format if needed
                if (proxyResult.ResponseReceived && sourceFormat != targetFormat)
                {
                    await TransformAndSendResponseAsync(ctx, proxyResult, sourceFormat, targetFormat, backend);
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
        /// Process a request with retry logic for 50x errors.
        /// </summary>
        /// <param name="requestGuid">Request unique identifier.</param>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="frontend">Frontend configuration.</param>
        /// <param name="initialBackend">Initially selected backend.</param>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="telemetry">Telemetry object.</param>
        /// <returns>True if response was successfully sent, false otherwise.</returns>
        public async Task<bool> ProcessRequestWithRetryAsync(
            Guid requestGuid,
            HttpContextBase ctx,
            Frontend frontend,
            Backend initialBackend,
            string clientId,
            TelemetryMessage telemetry)
        {
            Backend currentBackend = initialBackend;

            ProxyResult result = await _ProxyService.ProxyRequestAsync(requestGuid, ctx, frontend, currentBackend, telemetry);

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

                    ProxyResult retryResult = await _ProxyService.ProxyRequestAsync(requestGuid, ctx, frontend, retryBackend, telemetry);

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
        public async Task<bool> ProcessRequestWithStreamingTransformationAsync(
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
                    return await ProcessRequestWithTransformationAsync(requestGuid, ctx, frontend, backend, clientId, telemetry);
                }

                // Step 2: Transform the request (same as non-streaming)
                Models.Agnostic.Base.AgnosticRequest agnosticRequest = await _TransformationPipeline.TransformInboundAsync(ctx, sourceFormat).ConfigureAwait(false);
                object backendRequest = await _TransformationPipeline.TransformOutboundAsync(agnosticRequest, targetFormat).ConfigureAwait(false);

                // Create transformation result
                TransformationPipelineResult transformationResult = new TransformationPipelineResult
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
                bool streamingSuccess = await _ProxyService.ProxyRequestWithStreamingTransformationAsync(
                    requestGuid, ctx, frontend, backend, telemetry,
                    transformationResult, sourceFormat, targetFormat, requestType, transformedBytes, targetPath);

                // If streaming transformation failed, fall back to non-streaming backend request with streaming client response
                if (!streamingSuccess)
                {
                    _Logging.Info(_Header + "streaming transformation failed, modifying request to non-streaming for backend");

                    // Modify the transformed request to be non-streaming for the backend
                    string nonStreamingRequestBody = ModifyRequestToNonStreaming(transformedRequestBody);
                    byte[] nonStreamingBytes = Encoding.UTF8.GetBytes(nonStreamingRequestBody);

                    // Use the regular proxy method with transformation (non-streaming backend request)
                    ProxyResult proxyResult = await _ProxyService.ProxyRequestAsync(
                        requestGuid, ctx, frontend, backend, telemetry, nonStreamingBytes, targetPath, captureResponseForTransformation: true);

                    // Transform response back to source format and send as streaming to client
                    if (proxyResult.ResponseReceived && sourceFormat != targetFormat)
                    {
                        await TransformAndSendStreamingResponseAsync(ctx, proxyResult, sourceFormat, targetFormat, backend, true);
                    }

                    return proxyResult.ResponseReceived;
                }

                return streamingSuccess;
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

        #endregion

        #region Private-Methods

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
        /// Transform the backend response and send it to the client.
        /// </summary>
        /// <param name="originalContext">Original HTTP context for the client response.</param>
        /// <param name="proxyResult">Result from the backend proxy operation.</param>
        /// <param name="sourceFormat">Source API format (client).</param>
        /// <param name="targetFormat">Target API format (backend).</param>
        /// <param name="backend">Backend server information.</param>
        private async Task TransformAndSendResponseAsync(
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
        /// Get request type from an agnostic request object.
        /// </summary>
        /// <param name="request">Agnostic request.</param>
        /// <returns>Request type enum.</returns>
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
                    Random random = new Random();
                    int index = random.Next(0, healthyBackends.Count);
                    selected = healthyBackends[index];
                    break;

                default:
                    selected = healthyBackends.First();
                    break;
            }

            _Logging.Debug(_Header + "selected alternative backend " + selected.Identifier + " using " + frontend.LoadBalancing + " load balancing");
            return selected;
        }

        /// <summary>
        /// Modify a request body to set streaming to false.
        /// </summary>
        /// <param name="requestBody">Original request body JSON.</param>
        /// <returns>Modified request body with stream set to false.</returns>
        private string ModifyRequestToNonStreaming(string requestBody)
        {
            try
            {
                // Parse the JSON request
                using JsonDocument doc = JsonDocument.Parse(requestBody);
                JsonElement root = doc.RootElement;

                // Create a new object with stream set to false
                var modifiedRequest = new Dictionary<string, object>();

                foreach (JsonProperty property in root.EnumerateObject())
                {
                    if (property.Name.Equals("stream", StringComparison.OrdinalIgnoreCase))
                    {
                        modifiedRequest[property.Name] = false;
                    }
                    else
                    {
                        modifiedRequest[property.Name] = JsonElementToObject(property.Value);
                    }
                }

                return _Serializer.SerializeJson(modifiedRequest, false);
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"failed to modify request to non-streaming: {ex.Message}");
                return requestBody; // Return original if modification fails
            }
        }

        /// <summary>
        /// Transform response and send as streaming response to client.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="proxyResult">Proxy result from backend.</param>
        /// <param name="sourceFormat">Source API format (client).</param>
        /// <param name="targetFormat">Target API format (backend).</param>
        /// <param name="backend">Backend server information.</param>
        /// <param name="isStreamingRequest">Whether the original client request was streaming.</param>
        private async Task TransformAndSendStreamingResponseAsync(
            HttpContextBase ctx,
            ProxyResult proxyResult,
            ApiFormatEnum sourceFormat,
            ApiFormatEnum targetFormat,
            Backend backend,
            bool isStreamingRequest)
        {
            try
            {
                _Logging.Debug(_Header + $"transforming non-streaming response to streaming format from {targetFormat} back to {sourceFormat}");

                // Transform the non-streaming response to agnostic format
                Services.Transformation.Response.OpenAIToAgnosticResponseTransformer backendTransformer =
                    new Services.Transformation.Response.OpenAIToAgnosticResponseTransformer();

                Models.Agnostic.Base.AgnosticResponse agnosticResponse = await backendTransformer.TransformToAgnosticAsync(
                    proxyResult.ResponseBody,
                    targetFormat).ConfigureAwait(false);

                // Set up response headers for streaming
                ctx.Response.StatusCode = proxyResult.StatusCode;
                ctx.Response.Headers.Add(Constants.BackendServerHeader, backend.Identifier);

                if (sourceFormat == ApiFormatEnum.OpenAI)
                {
                    ctx.Response.ContentType = "text/plain; charset=utf-8";
                    ctx.Response.ServerSentEvents = true;
                }
                else
                {
                    ctx.Response.ContentType = "application/x-ndjson";
                    ctx.Response.ChunkedTransfer = true;
                }

                // Convert the non-streaming response to streaming format using the streaming transformer
                Services.Transformation.Streaming.StreamingTransformer streamingTransformer =
                    new Services.Transformation.Streaming.StreamingTransformer();

                // Create streaming chunks from the response
                if (agnosticResponse is Models.Agnostic.Responses.AgnosticChatResponse chatResponse)
                {
                    // Send the response as a single streaming chunk
                    Services.Transformation.Interfaces.StreamingChunkResult chunk = await streamingTransformer.TransformChunkAsync(
                        _Serializer.SerializeJson(chatResponse, false), targetFormat, sourceFormat, RequestTypeEnum.GenerateChatCompletion);

                    if (!string.IsNullOrEmpty(chunk.Error))
                    {
                        _Logging.Error(_Header + $"streaming chunk transformation error: {chunk.Error}");
                        throw new Exception($"Streaming transformation failed: {chunk.Error}");
                    }

                    if (chunk.ChunkData?.Length > 0)
                    {
                        if (sourceFormat == ApiFormatEnum.OpenAI)
                        {
                            string eventData = Encoding.UTF8.GetString(chunk.ChunkData);
                            await ctx.Response.SendEvent(eventData, false);
                        }
                        else
                        {
                            await ctx.Response.SendChunk(chunk.ChunkData, false);
                        }
                    }

                    // Send final chunk
                    Services.Transformation.Interfaces.StreamingChunkResult finalChunk = await streamingTransformer.CreateFinalChunkAsync(
                        sourceFormat, RequestTypeEnum.GenerateChatCompletion);

                    if (finalChunk.ChunkData?.Length > 0)
                    {
                        if (sourceFormat == ApiFormatEnum.OpenAI)
                        {
                            string finalEventData = Encoding.UTF8.GetString(finalChunk.ChunkData);
                            await ctx.Response.SendEvent(finalEventData, true);
                        }
                        else
                        {
                            await ctx.Response.SendChunk(finalChunk.ChunkData, true);
                        }
                    }
                    else
                    {
                        // Send empty final chunk
                        if (sourceFormat == ApiFormatEnum.OpenAI)
                        {
                            await ctx.Response.SendEvent(null, true);
                        }
                        else
                        {
                            await ctx.Response.SendChunk(Array.Empty<byte>(), true);
                        }
                    }
                }

                _Logging.Debug(_Header + $"streaming response transformation completed (status: {proxyResult.StatusCode})");
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"streaming response transformation failed: {ex.Message}");

                // Send error response
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(new { error = "Streaming response transformation failed" }, true));
            }
        }

        /// <summary>
        /// Convert JsonElement to appropriate object type.
        /// </summary>
        /// <param name="element">JSON element.</param>
        /// <returns>Converted object.</returns>
        private object JsonElementToObject(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (JsonProperty prop in element.EnumerateObject())
                    {
                        obj[prop.Name] = JsonElementToObject(prop.Value);
                    }
                    return obj;
                case JsonValueKind.Array:
                    var array = new List<object>();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        array.Add(JsonElementToObject(item));
                    }
                    return array;
                default:
                    return element.ToString();
            }
        }

        #endregion
    }
}