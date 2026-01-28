namespace OllamaFlow.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using RestWrapper;
    using OllamaFlow.Sdk.Interfaces;
    using OllamaFlow.Sdk.Implementations;

    /// <summary>
    /// OllamaFlow SDK for interacting with OllamaFlow server.
    /// </summary>
    public class OllamaFlowSdk : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Enable or disable logging of request bodies.
        /// </summary>
        public bool LogRequests { get; set; } = false;

        /// <summary>
        /// Enable or disable logging of response bodies.
        /// </summary>
        public bool LogResponses { get; set; } = false;

        /// <summary>
        /// Method to invoke to send log messages.
        /// </summary>
        public Action<string, string>? Logger { get; set; }

        /// <summary>
        /// Endpoint URL for the OllamaFlow server.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Timeout in milliseconds for HTTP requests.
        /// </summary>
        public int TimeoutMs { get; set; } = 300000;

        /// <summary>
        /// Bearer token for authentication with admin API endpoints.
        /// </summary>
        public string? BearerToken { get; set; } = null;

        /// <summary>
        /// Ollama API methods.
        /// </summary>
        public IOllamaMethods Ollama { get; private set; }

        /// <summary>
        /// Backend API methods.
        /// </summary>
        public IBackendMethods Backend { get; private set; }

        /// <summary>
        /// Frontend API methods.
        /// </summary>
        public IFrontendMethods Frontend { get; private set; }

        /// <summary>
        /// OpenAI-compatible API methods.
        /// </summary>
        public IOpenAIMethods OpenAI { get; private set; }

        /// <summary>
        /// JSON serializer options for API requests and responses.
        /// </summary>
        public readonly JsonSerializerOptions _JsonOptions;

        #endregion

        #region Private-Members

        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initialize the OllamaFlow SDK.
        /// </summary>
        /// <param name="endpoint">OllamaFlow server endpoint URL.</param>
        public OllamaFlowSdk(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                throw new ArgumentNullException(nameof(endpoint));

            Endpoint = endpoint.TrimEnd('/');

            _JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            _JsonOptions.Converters.Add(new JsonStringEnumConverter());

            Ollama = new OllamaMethods(this);
            Backend = new BackendMethods(this);
            Frontend = new FrontendMethods(this);
            OpenAI = new OpenAIMethods(this);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the OllamaFlowSdk and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
                _Disposed = true;
        }

        /// <summary>
        /// Logs a message with the specified level.
        /// </summary>
        /// <param name="level">The log level (e.g., DEBUG, INFO, WARN, ERROR).</param>
        /// <param name="message">The message to log.</param>
        public void Log(string level, string message)
        {
            if (!string.IsNullOrEmpty(message))
                Logger?.Invoke(level, message);
        }

        /// <summary>
        /// Sets the authorization header on a RestRequest if a BearerToken is configured.
        /// </summary>
        /// <param name="request">The RestRequest to set the authorization header on.</param>
        private void SetAuthorizationHeader(RestRequest request)
        {
            if (!string.IsNullOrEmpty(BearerToken))
                request.Headers.Add("Authorization", $"Bearer {BearerToken}");
        }

        /// <summary>
        /// Sends a POST request to the specified URL with the provided data.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="url">The URL to send the POST request to.</param>
        /// <param name="data">The data to send in the request body.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized response object.</returns>
        public async Task<T?> PostAsync<T>(string url, object data, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (data == null) throw new ArgumentNullException(nameof(data));

            string json = JsonSerializer.Serialize(data, _JsonOptions);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            using (RestRequest req = new RestRequest(url, HttpMethod.Post))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.ContentType = "application/json";
                SetAuthorizationHeader(req);

                if (LogRequests)
                    Log("DEBUG", $"POST request to {url} with {jsonBytes.Length} bytes");

                using (RestResponse resp = await req.SendAsync(jsonBytes, cancellationToken).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        string? responseData = await ReadResponse(resp, cancellationToken).ConfigureAwait(false);

                        if (LogResponses)
                            Log("DEBUG", $"Response from {url} (status {resp.StatusCode}): {responseData}");

                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            Log("DEBUG", $"Success from {url}: {resp.StatusCode}, {resp.ContentLength} bytes");

                            if (!string.IsNullOrEmpty(responseData))
                                return JsonSerializer.Deserialize<T>(responseData, _JsonOptions);
                            return default(T);
                        }
                        else
                        {
                            Log("WARN", $"Non-success from {url}: {resp.StatusCode}, {resp.ContentLength} bytes");
                            return default(T);
                        }
                    }
                    else
                    {
                        Log("WARN", $"No response from {url}");
                        return default(T);
                    }
                }
            }
        }

        /// <summary>
        /// Sends a GET request to the specified URL and returns a list of objects.
        /// </summary>
        /// <typeparam name="T">The type of objects in the list.</typeparam>
        /// <param name="url">The URL to send the GET request to.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of deserialized objects.</returns>
        public async Task<List<T>?> GetAllAsync<T>(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (RestRequest req = new RestRequest(url, HttpMethod.Get))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                SetAuthorizationHeader(req);

                if (LogRequests)
                    Log("DEBUG", $"GET request to {url}");

                using (RestResponse resp = await req.SendAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        string? responseData = await ReadResponse(resp, cancellationToken).ConfigureAwait(false);

                        if (LogResponses)
                            Log("DEBUG", $"Response from {url} (status {resp.StatusCode}): {responseData}");

                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            if (!string.IsNullOrEmpty(responseData))
                            {
                                using (JsonDocument doc = JsonDocument.Parse(responseData))
                                {
                                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                                        return JsonSerializer.Deserialize<List<T>>(responseData, _JsonOptions);
                                    else if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement))
                                        return JsonSerializer.Deserialize<List<T>>(dataElement.GetRawText(), _JsonOptions);
                                    else if (doc.RootElement.TryGetProperty("items", out JsonElement itemsElement))
                                        return JsonSerializer.Deserialize<List<T>>(itemsElement.GetRawText(), _JsonOptions);
                                }
                            }
                            return new List<T>();
                        }
                        else
                        {
                            Log("WARN", $"Non-success from {url}: {resp.StatusCode}, {resp.ContentLength} bytes");
                            return new List<T>();
                        }
                    }
                    else
                    {
                        Log("WARN", $"No response from {url}");
                        return new List<T>();
                    }
                }
            }
        }

        /// <summary>
        /// Sends a GET request to the specified URL.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="url">The URL to send the GET request to.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized response object.</returns>
        public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (RestRequest req = new RestRequest(url, HttpMethod.Get))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                SetAuthorizationHeader(req);

                if (LogRequests)
                    Log("DEBUG", $"GET request to {url}");

                using (RestResponse resp = await req.SendAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        string? responseData = await ReadResponse(resp, cancellationToken).ConfigureAwait(false);

                        if (LogResponses)
                            Log("DEBUG", $"Response from {url} (status {resp.StatusCode}): {responseData}");

                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            if (!string.IsNullOrEmpty(responseData))
                                return JsonSerializer.Deserialize<T>(responseData, _JsonOptions);
                            return default(T);
                        }
                        else
                        {
                            Log("WARN", $"Non-success from {url}: {resp.StatusCode}, {resp.ContentLength} bytes");
                            return default(T);
                        }
                    }
                    else
                    {
                        Log("WARN", $"No response from {url}");
                        return default(T);
                    }
                }
            }
        }

        /// <summary>
        /// Sends a DELETE request to the specified URL with the provided data.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="url">The URL to send the DELETE request to.</param>
        /// <param name="data">The data to send in the request body.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized response object.</returns>
        public async Task<T?> DeleteAsync<T>(string url, object data, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (data == null) throw new ArgumentNullException(nameof(data));

            string json = JsonSerializer.Serialize(data, _JsonOptions);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            using (RestRequest req = new RestRequest(url, HttpMethod.Delete))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.ContentType = "application/json";

                if (LogRequests)
                    Log("DEBUG", $"DELETE request to {url} with {jsonBytes.Length} bytes");

                using (RestResponse resp = await req.SendAsync(jsonBytes, cancellationToken).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        string? responseData = await ReadResponse(resp, cancellationToken).ConfigureAwait(false);

                        if (LogResponses)
                            Log("DEBUG", $"Response from {url} (status {resp.StatusCode}): {responseData}");

                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            if (!string.IsNullOrEmpty(responseData))
                                return JsonSerializer.Deserialize<T>(responseData, _JsonOptions);
                            return default(T);
                        }
                        else
                        {
                            Log("WARN", $"Non-success from {url}: {resp.StatusCode}, {resp.ContentLength} bytes");
                            return default(T);
                        }
                    }
                    else
                    {
                        Log("WARN", $"No response from {url}");
                        return default(T);
                    }
                }
            }
        }

        /// <summary>
        /// Sends a GET request to the specified URL and returns the raw response as a string.
        /// </summary>
        /// <param name="url">The URL to send the GET request to.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the raw response string.</returns>
        public async Task<string?> GetRawResponse(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (RestRequest req = new RestRequest(url, HttpMethod.Get))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                SetAuthorizationHeader(req);

                if (LogRequests)
                    Log("DEBUG", $"GET request to {url}");

                using (RestResponse resp = await req.SendAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        string? responseData = await ReadResponse(resp, cancellationToken).ConfigureAwait(false);
                        if (LogResponses)
                            Log("DEBUG", $"Response from {url} (status {resp.StatusCode}): {responseData}");
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                            return responseData;
                        Log("WARN", $"Non-success from {url}: {resp.StatusCode}, {resp.ContentLength} bytes");
                        return null;
                    }
                    else
                    {
                        Log("WARN", $"No response from {url}");
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Sends a DELETE request to the specified URL.
        /// </summary>
        /// <param name="url">The URL to send the DELETE request to.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the deletion was successful.</returns>
        public async Task<bool> DeleteAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (RestRequest req = new RestRequest(url, HttpMethod.Delete))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                SetAuthorizationHeader(req);

                if (LogRequests)
                    Log("DEBUG", $"DELETE request to {url}");

                using (RestResponse resp = await req.SendAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (LogResponses)
                            Log("DEBUG", $"Response from {url} (status {resp.StatusCode})");

                        return resp.StatusCode >= 200 && resp.StatusCode <= 299;
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// Sends a HEAD request to the specified URL.
        /// </summary>
        /// <param name="url">The URL to send the HEAD request to.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the resource exists.</returns>
        public async Task<bool> HeadAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (RestRequest req = new RestRequest(url, HttpMethod.Head))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                SetAuthorizationHeader(req);

                if (LogRequests)
                    Log("DEBUG", $"HEAD request to {url}");

                using (RestResponse resp = await req.SendAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (LogResponses)
                            Log("DEBUG", $"Response from {url} (status {resp.StatusCode})");

                        return resp.StatusCode >= 200 && resp.StatusCode <= 299;
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// Sends a PUT request to the specified URL with the provided data.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="url">The URL to send the PUT request to.</param>
        /// <param name="data">The data to send in the request body.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized response object.</returns>
        public async Task<T?> PutAsync<T>(string url, object data, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (data == null) throw new ArgumentNullException(nameof(data));

            string json = JsonSerializer.Serialize(data, _JsonOptions);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            using (RestRequest req = new RestRequest(url, HttpMethod.Put))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.ContentType = "application/json";
                SetAuthorizationHeader(req);

                if (LogRequests)
                    Log("DEBUG", $"PUT request to {url} with {jsonBytes.Length} bytes");

                using (RestResponse resp = await req.SendAsync(jsonBytes, cancellationToken).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        string? responseData = await ReadResponse(resp, cancellationToken).ConfigureAwait(false);

                        if (LogResponses)
                            Log("DEBUG", $"Response from {url} (status {resp.StatusCode}): {responseData}");

                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            Log("DEBUG", $"Success from {url}: {resp.StatusCode}, {resp.ContentLength} bytes");

                            if (!string.IsNullOrEmpty(responseData))
                                return JsonSerializer.Deserialize<T>(responseData, _JsonOptions);
                            return default(T);
                        }
                        else
                        {
                            Log("WARN", $"Non-success from {url}: {resp.StatusCode}, {resp.ContentLength} bytes");
                            return default(T);
                        }
                    }
                    else
                    {
                        Log("WARN", $"No response from {url}");
                        return default(T);
                    }
                }
            }
        }

        /// <summary>
        /// Sends a POST request to the specified URL with the provided data and returns the raw response as a string.
        /// </summary>
        /// <param name="url">The URL to send the POST request to.</param>
        /// <param name="data">The data to send in the request body.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the raw response string.</returns>
        public async Task<string?> GetRawPostResponse(string url, object data, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (data == null) throw new ArgumentNullException(nameof(data));

            string json = JsonSerializer.Serialize(data, _JsonOptions);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            using (RestRequest req = new RestRequest(url, HttpMethod.Post))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.ContentType = "application/json";
                SetAuthorizationHeader(req);

                if (LogRequests)
                    Log("DEBUG", $"POST request to {url} with {jsonBytes.Length} bytes");

                using (RestResponse resp = await req.SendAsync(jsonBytes, cancellationToken).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        string? responseData = await ReadResponse(resp, cancellationToken).ConfigureAwait(false);
                        if (LogResponses)
                            Log("DEBUG", $"Response from {url} (status {resp.StatusCode}): {responseData}");
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                            return responseData;
                        Log("WARN", $"Non-success from {url}: {resp.StatusCode}, {resp.ContentLength} bytes");
                        return null;
                    }
                    else
                    {
                        Log("WARN", $"No response from {url}");
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Reads the response from a RestResponse object, handling both chunked and non-chunked responses.
        /// </summary>
        /// <param name="resp">The RestResponse object to read from.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response content as a string.</returns>
        public async Task<string?> ReadResponse(RestResponse resp, CancellationToken token = default)
        {
            if (resp == null) return null;

            string str = string.Empty;

            if (resp.ChunkedTransferEncoding)
            {
                List<string> chunks = new List<string>();
                ChunkData? chunk = null;
                while ((chunk = await resp.ReadChunkAsync(token).ConfigureAwait(false)) != null)
                {
                    if (chunk.Data != null && chunk.Data.Length > 0)
                        chunks.Add(System.Text.Encoding.UTF8.GetString(chunk.Data));
                    if (chunk.IsFinal) break;
                }
                str = string.Join("", chunks);
            }
            else
            {
                str = resp.DataAsString;
            }

            return str;
        }

        /// <summary>
        /// Sends a POST request to the specified URL and returns a stream of deserialized objects.
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize from the stream.</typeparam>
        /// <param name="url">The URL to send the POST request to.</param>
        /// <param name="data">The data to send in the request body.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <param name="processLine">Optional function to process each line before deserialization.</param>
        /// <param name="shouldStop">Optional function to determine if streaming should stop.</param>
        /// <returns>An async enumerable that yields deserialized objects from the stream.</returns>
        public async IAsyncEnumerable<T> PostStreamAsync<T>(
            string url,
            object data,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default,
            Func<string, string>? processLine = null,
            Func<T, bool>? shouldStop = null)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (data == null) throw new ArgumentNullException(nameof(data));

            string json = JsonSerializer.Serialize(data, _JsonOptions);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            using (RestRequest req = new RestRequest(url, HttpMethod.Post))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.ContentType = "application/json";
                SetAuthorizationHeader(req);

                if (LogRequests)
                    Log("DEBUG", $"POST request to {url} with {jsonBytes.Length} bytes");

                using (RestResponse resp = await req.SendAsync(jsonBytes, cancellationToken).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            if (resp.ChunkedTransferEncoding)
                            {
                                ChunkData? chunk = null;
                                while ((chunk = await resp.ReadChunkAsync(cancellationToken).ConfigureAwait(false)) != null)
                                {
                                    if (chunk.Data != null && chunk.Data.Length > 0)
                                    {
                                        string chunkText = System.Text.Encoding.UTF8.GetString(chunk.Data);
                                        var lines = chunkText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                                        foreach (var line in lines)
                                        {
                                            if (string.IsNullOrWhiteSpace(line)) continue;
                                            string processedLine = processLine?.Invoke(line) ?? line;
                                            T? result = default(T);
                                            try
                                            {
                                                result = JsonSerializer.Deserialize<T>(processedLine, _JsonOptions);
                                            }
                                            catch
                                            {
                                                continue;
                                            }
                                            if (result != null)
                                            {
                                                yield return result;
                                                if (shouldStop?.Invoke(result) == true)
                                                    yield break;
                                            }
                                        }
                                    }
                                    if (chunk.IsFinal) break;
                                }
                            }
                            else
                            {
                                string responseData = resp.DataAsString;
                                if (!string.IsNullOrEmpty(responseData))
                                {
                                    string processedData = processLine?.Invoke(responseData) ?? responseData;
                                    T? result = default(T);
                                    try
                                    {
                                        result = JsonSerializer.Deserialize<T>(processedData, _JsonOptions);
                                    }
                                    catch { }
                                    if (result != null)
                                        yield return result;
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}