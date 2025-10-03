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

            if (backend.ApiFormat == ApiFormatEnum.Ollama)
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
            else if (backend.ApiFormat == ApiFormatEnum.OpenAI)
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
                throw new ArgumentException($"Unsupported backend API format {backend.ApiFormat.ToString()} in backend {backend.Identifier}.");
            }

            throw new ArgumentException($"Unsupported request type {requestType.ToString()} for backend {backend.Identifier} with API format {backend.ApiFormat.ToString()}.");
        }
    }
}
