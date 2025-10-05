namespace OllamaFlow.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using OllamaFlow.Core.Enums;

    /// <summary>
    /// URL builder.
    /// </summary>
    public static class UrlBuilder
    {
        /// <summary>
        /// Build the URL for a given request.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="frontend">Frontend.</param>
        /// <param name="requestType">Request type.</param>
        /// <returns>URL.</returns>
        public static string BuildUrl(OllamaFlowSettings settings, Frontend frontend, RequestTypeEnum requestType)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));

            string hostname = "localhost";
            if (settings.Webserver.Hostname != "*") hostname = settings.Webserver.Hostname;
            string prefix = (settings.Webserver.Ssl.Enable ? "https://" : "http://") + hostname + ":" + settings.Webserver.Port;

            switch (requestType)
            {
                case RequestTypeEnum.OllamaPullModel:
                    return prefix + "/api/pull";
                case RequestTypeEnum.OllamaDeleteModel:
                    return prefix + "/api/delete";
                case RequestTypeEnum.OllamaListModels:
                    return prefix + "/api/tags";
                case RequestTypeEnum.OllamaShowModelInformation:
                    return prefix + "/api/show";
                case RequestTypeEnum.OllamaListRunningModels:
                    return prefix + "/api/ps";
                case RequestTypeEnum.OllamaGenerateCompletion:
                    return prefix + "/api/generate";
                case RequestTypeEnum.OllamaGenerateChatCompletion:
                    return prefix + "/api/chat";
                case RequestTypeEnum.OllamaGenerateEmbeddings:
                    return prefix + "/api/embed";
                case RequestTypeEnum.OpenAIGenerateCompletion:
                    return prefix + "/v1/completions";
                case RequestTypeEnum.OpenAIGenerateChatCompletion:
                    return prefix + "/v1/chat/completions";
                case RequestTypeEnum.OpenAIGenerateEmbeddings:
                    return prefix + "/v1/embeddings";
                default:
                    throw new ArgumentException($"Unsupported request type {requestType.ToString()} for backend {frontend.Identifier}.");
            }
        }

        /// <summary>
        /// Build the URL for a given request.
        /// </summary>
        /// <param name="backend">Backend.</param>
        /// <param name="requestType">Request type.</param>
        /// <returns>URL.</returns>
        public static string BuildUrl(Backend backend, RequestTypeEnum requestType)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));

            if (backend.ApiFormat == ApiFormatEnum.Ollama)
            {
                switch (requestType)
                {
                    case RequestTypeEnum.OllamaPullModel:
                        return backend.UrlPrefix + "/api/pull";
                    case RequestTypeEnum.OllamaDeleteModel:
                        return backend.UrlPrefix + "/api/delete";
                    case RequestTypeEnum.OllamaListModels:
                        return backend.UrlPrefix + "/api/tags";
                    case RequestTypeEnum.OllamaShowModelInformation:
                        return backend.UrlPrefix + "/api/show";
                    case RequestTypeEnum.OllamaListRunningModels:
                        return backend.UrlPrefix + "/api/ps";
                    case RequestTypeEnum.OllamaGenerateCompletion:
                        return backend.UrlPrefix + "/api/generate";
                    case RequestTypeEnum.OllamaGenerateChatCompletion:
                        return backend.UrlPrefix + "/api/chat";
                    case RequestTypeEnum.OllamaGenerateEmbeddings:
                        return backend.UrlPrefix + "/api/embed";
                }
            }
            else if (backend.ApiFormat == ApiFormatEnum.OpenAI)
            {
                switch (requestType)
                {
                    case RequestTypeEnum.OpenAIGenerateCompletion:
                        return backend.UrlPrefix + "/v1/completions";
                    case RequestTypeEnum.OpenAIGenerateChatCompletion:
                        return backend.UrlPrefix + "/v1/chat/completions";
                    case RequestTypeEnum.OpenAIGenerateEmbeddings:
                        return backend.UrlPrefix + "/v1/embeddings";
                }
            }
            else
            {
                throw new ArgumentException($"Unsupported backend API format {backend.ApiFormat.ToString()} in backend {backend.Identifier}.");
            }

            throw new ArgumentException($"Unsupported request type {requestType.ToString()} for backend {backend.Identifier} with API format {backend.ApiFormat.ToString()}.");
        }

        /// <summary>
        /// Get the HTTP method used for a given request type.
        /// </summary>
        /// <param name="backend">Backend.</param>
        /// <param name="requestType">Request type.</param>
        /// <returns>HTTP method.</returns>
        public static HttpMethod GetMethod(Backend backend, RequestTypeEnum requestType)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            return GetMethod(backend.ApiFormat, requestType);
        }

        /// <summary>
        /// Get the HTTP method used for a given request type.
        /// </summary>
        /// <param name="apiFormat">API format.</param>
        /// <param name="requestType">Request type.</param>
        /// <returns>HTTP method.</returns>
        public static HttpMethod GetMethod(ApiFormatEnum apiFormat, RequestTypeEnum requestType)
        {
            if (apiFormat == ApiFormatEnum.Ollama)
            {
                switch (requestType)
                {
                    case RequestTypeEnum.OllamaListModels:
                    case RequestTypeEnum.OllamaListRunningModels:
                        return HttpMethod.Get;
                    case RequestTypeEnum.OllamaPullModel:
                    case RequestTypeEnum.OllamaShowModelInformation:
                    case RequestTypeEnum.OllamaGenerateCompletion:
                    case RequestTypeEnum.OllamaGenerateChatCompletion:
                    case RequestTypeEnum.OllamaGenerateEmbeddings:
                        return HttpMethod.Post;
                    case RequestTypeEnum.OllamaDeleteModel:
                        return HttpMethod.Delete;
                }
            }
            else if (apiFormat == ApiFormatEnum.OpenAI)
            {
                switch (requestType)
                {
                    case RequestTypeEnum.OpenAIGenerateCompletion:
                    case RequestTypeEnum.OpenAIGenerateChatCompletion:
                    case RequestTypeEnum.OpenAIGenerateEmbeddings:
                        return HttpMethod.Post;
                }
            }
            else
            {
                throw new ArgumentException($"Unsupported backend API format {apiFormat.ToString()}");
            }

            throw new ArgumentException($"Unsupported request type {requestType.ToString()} with API format {apiFormat.ToString()}.");
        }
    }
}
