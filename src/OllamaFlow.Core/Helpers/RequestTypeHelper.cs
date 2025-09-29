namespace OllamaFlow.Core.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;
    using OllamaFlow.Core.Enums;

    using HttpMethod = WatsonWebserver.Core.HttpMethod;

    internal static class RequestTypeHelper
    {
        /// <summary>
        /// Determines the API format from a request based on the URL path.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="url">Request URL path.</param>
        /// <returns>The detected API format.</returns>
        internal static ApiFormatEnum GetApiFormatFromRequest(HttpMethod method, string url)
        {
            if (String.IsNullOrEmpty(url)) url = "/";

            // Normalize URL - ensure it starts with / and remove trailing /
            if (!url.StartsWith("/")) url = "/" + url;
            if (url.Length > 1 && url.EndsWith("/")) url = url.TrimEnd('/');

            // Admin API paths start with /v1.0/
            if (url.StartsWith("/v1.0/"))
                return ApiFormatEnum.Admin;

            // OpenAI API paths start with /v1/
            if (url.StartsWith("/v1/"))
                return ApiFormatEnum.OpenAI;

            // Ollama API paths start with /api/ or are root endpoints
            if (url.StartsWith("/api/") || url == "/" || url == "")
                return ApiFormatEnum.Ollama;

            // Default to Ollama for backward compatibility
            return ApiFormatEnum.Ollama;
        }

        /// <summary>
        /// Determines the generic request type from a request, regardless of API format.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="url">Request URL path.</param>
        /// <returns>The generic request type.</returns>
        internal static RequestTypeEnum GetRequestTypeFromRequest(HttpMethod method, string url)
        {
            if (String.IsNullOrEmpty(url)) url = "/";

            // Normalize URL - ensure it starts with / and remove trailing /
            if (!url.StartsWith("/")) url = "/" + url;
            if (url.Length > 1 && url.EndsWith("/")) url = url.TrimEnd('/');

            // Admin API endpoints (check first since they have precedence)
            if (url.StartsWith("/v1.0/"))
            {
                return GetAdminRequestType(method, url);
            }

            // Root endpoints
            if (url.Equals("/"))
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.Root;
                else if (method == HttpMethod.HEAD) return RequestTypeEnum.ValidateConnectivity;
            }

            // Ollama API endpoints
            if (url.Equals("/api/generate"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.GenerateCompletion;
            }
            else if (url.Equals("/api/chat"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.GenerateChatCompletion;
            }
            else if (url.Equals("/api/embeddings") || url.Equals("/api/embed"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.GenerateEmbeddings;
            }
            else if (url.Equals("/api/pull"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.PullModel;
            }
            else if (url.Equals("/api/push"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.PushModel;
            }
            else if (url.Equals("/api/create"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.CreateModel;
            }
            else if (url.Equals("/api/copy"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.CopyModel;
            }
            else if (url.Equals("/api/delete"))
            {
                if (method == HttpMethod.DELETE) return RequestTypeEnum.DeleteModel;
            }
            else if (url.Equals("/api/tags"))
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.ListModels;
            }
            else if (url.Equals("/api/ps"))
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.ListRunningModels;
            }
            else if (url.Equals("/api/show"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.ShowModelInformation;
            }
            else if (url.StartsWith("/api/blobs/"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.CreateBlob;
                else if (method == HttpMethod.HEAD) return RequestTypeEnum.CheckBlob;
            }

            // OpenAI-compatible endpoints - map to generic request types
            else if (url.Equals("/v1/chat/completions"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.GenerateChatCompletion;
            }
            else if (url.Equals("/v1/completions"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.GenerateCompletion;
            }
            else if (url.Equals("/v1/embeddings"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.GenerateEmbeddings;
            }
            else if (url.Equals("/v1/models"))
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.ListModels;
            }
            else if (url.StartsWith("/v1/models/") && !url.Equals("/v1/models"))
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.ShowModelInformation;
            }

            return RequestTypeEnum.Unknown;
        }

        internal static string GetModelFromBody(HttpRequestBase req)
        {
            string data = req.DataAsString;
            if (String.IsNullOrEmpty(data)) return null;

            try
            {
                using (JsonDocument document = JsonDocument.Parse(data))
                {
                    if (document.RootElement.TryGetProperty("model", out JsonElement modelElement))
                    {
                        return modelElement.GetString();
                    }
                }
            }
            catch
            {
                // Silently fail and return null
            }

            return null;
        }

        /// <summary>
        /// Determines the request type from a request (legacy method, use GetRequestTypeFromRequest instead).
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="url">Request URL path.</param>
        /// <returns>The generic request type.</returns>
        [Obsolete("Use GetRequestTypeFromRequest instead. This method will be removed in a future version.")]
        internal static RequestTypeEnum DetermineRequestType(HttpMethod method, string url)
        {
            return GetRequestTypeFromRequest(method, url);
        }

        internal static string GetModelFromRequest(HttpRequestBase req, RequestTypeEnum requestType)
        {
            // For ShowModelInformation from OpenAI API format, try to get model from URL path first
            if (requestType == RequestTypeEnum.ShowModelInformation &&
                req.Url != null && req.Url.RawWithQuery.StartsWith("/v1/models/"))
            {
                // Extract model from /v1/models/{model}
                string modelPath = req.Url.RawWithQuery.Substring("/v1/models/".Length);
                // Remove any query parameters
                int queryIndex = modelPath.IndexOf('?');
                if (queryIndex >= 0)
                {
                    modelPath = modelPath.Substring(0, queryIndex);
                }
                return Uri.UnescapeDataString(modelPath);
            }

            // For all other request types that include model in body
            switch (requestType)
            {
                case RequestTypeEnum.GenerateCompletion:
                case RequestTypeEnum.GenerateChatCompletion:
                case RequestTypeEnum.GenerateEmbeddings:
                case RequestTypeEnum.PullModel:
                case RequestTypeEnum.PushModel:
                case RequestTypeEnum.CreateModel:
                case RequestTypeEnum.CopyModel:
                case RequestTypeEnum.DeleteModel:
                case RequestTypeEnum.ShowModelInformation:
                    return GetModelFromBody(req);
            }

            return null;
        }

        /// <summary>
        /// Determine the admin request type from the method and URL.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="url">Request URL path.</param>
        /// <returns>The admin request type.</returns>
        private static RequestTypeEnum GetAdminRequestType(HttpMethod method, string url)
        {
            // Strip /v1.0/ prefix and normalize
            string path = url.Substring(5); // Remove "/v1.0"
            if (path.StartsWith("/")) path = path.Substring(1);

            // Frontend endpoints
            if (path == "frontends")
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.AdminListFrontends;
                if (method == HttpMethod.PUT) return RequestTypeEnum.AdminCreateFrontend;
            }
            else if (path.StartsWith("frontends/"))
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.AdminGetFrontend;
                if (method == HttpMethod.PUT) return RequestTypeEnum.AdminUpdateFrontend;
                if (method == HttpMethod.DELETE) return RequestTypeEnum.AdminDeleteFrontend;
            }

            // Backend endpoints
            else if (path == "backends")
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.AdminListBackends;
                if (method == HttpMethod.PUT) return RequestTypeEnum.AdminCreateBackend;
            }
            else if (path == "backends/health")
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.AdminGetBackendsHealth;
            }
            else if (path.StartsWith("backends/") && path.EndsWith("/health"))
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.AdminGetBackendHealth;
            }
            else if (path.StartsWith("backends/"))
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.AdminGetBackend;
                if (method == HttpMethod.PUT) return RequestTypeEnum.AdminUpdateBackend;
                if (method == HttpMethod.DELETE) return RequestTypeEnum.AdminDeleteBackend;
            }

            // Session endpoints
            else if (path == "sessions")
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.AdminListSessions;
                if (method == HttpMethod.DELETE) return RequestTypeEnum.AdminDeleteAllSessions;
            }
            else if (path.StartsWith("sessions/"))
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.AdminGetClientSessions;
                if (method == HttpMethod.DELETE) return RequestTypeEnum.AdminDeleteClientSessions;
            }

            // Default to unknown for unrecognized admin paths
            return RequestTypeEnum.Unknown;
        }
    }
}