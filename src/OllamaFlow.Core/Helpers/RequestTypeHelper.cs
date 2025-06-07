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

    using HttpMethod = WatsonWebserver.Core.HttpMethod;

    internal static class RequestTypeHelper
    {
        internal static RequestTypeEnum DetermineRequestType(HttpMethod method, string url)
        {
            if (String.IsNullOrEmpty(url)) url = "/";

            // Normalize URL - ensure it starts with / and remove trailing /
            if (!url.StartsWith("/")) url = "/" + url;
            if (url.Length > 1 && url.EndsWith("/")) url = url.TrimEnd('/');

            // Root endpoints
            if (url.Equals("/"))
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.Root;
                else if (method == HttpMethod.HEAD) return RequestTypeEnum.ValidateConnectivity;
            }

            // Native Ollama API endpoints
            if (url.Equals("/api/generate"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.GenerateCompletion;
            }
            else if (url.Equals("/api/chat"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.GenerateChatCompletion;
            }
            else if (url.Equals("/api/embeddings"))
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

            // OpenAI-compatible endpoints
            else if (url.Equals("/v1/chat/completions"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.OpenAIChatCompletions;
            }
            else if (url.Equals("/v1/completions"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.OpenAICompletions;
            }
            else if (url.Equals("/v1/embeddings"))
            {
                if (method == HttpMethod.POST) return RequestTypeEnum.OpenAIEmbeddings;
            }
            else if (url.Equals("/v1/models"))
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.OpenAIListModels;
            }
            else if (url.StartsWith("/v1/models/") && !url.Equals("/v1/models"))
            {
                if (method == HttpMethod.GET) return RequestTypeEnum.OpenAIRetrieveModel;
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

        internal static string GetModelFromRequest(HttpRequestBase req, RequestTypeEnum requestType)
        {
            // For certain request types, try to get model from URL path first
            switch (requestType)
            {
                case RequestTypeEnum.OpenAIRetrieveModel:
                    // Extract model from /v1/models/{model}
                    if (req.Url != null && req.Url.RawWithQuery.StartsWith("/v1/models/"))
                    {
                        string modelPath = req.Url.RawWithQuery.Substring("/v1/models/".Length);
                        // Remove any query parameters
                        int queryIndex = modelPath.IndexOf('?');
                        if (queryIndex >= 0)
                        {
                            modelPath = modelPath.Substring(0, queryIndex);
                        }
                        return Uri.UnescapeDataString(modelPath);
                    }
                    break;
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
                case RequestTypeEnum.OpenAIChatCompletions:
                case RequestTypeEnum.OpenAICompletions:
                case RequestTypeEnum.OpenAIEmbeddings:
                    return GetModelFromBody(req);
            }

            return null;
        }
    }
}