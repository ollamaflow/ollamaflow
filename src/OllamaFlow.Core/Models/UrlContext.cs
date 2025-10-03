namespace OllamaFlow.Core.Models
{
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using UrlMatcher;
    using WatsonWebserver.Core;

    /// <summary>
    /// URL context.
    /// </summary>
    public class UrlContext
    {
        /// <summary>
        /// Method to invoke to emit log messages.
        /// </summary>
        public Action<string> Logger { get; set; } = null;

        /// <summary>
        /// Request type.
        /// </summary>
        public RequestTypeEnum RequestType
        {
            get
            {
                return _RequestType;
            }
        }

        /// <summary>
        /// API format.
        /// </summary>
        public ApiFormatEnum ApiFormat
        {
            get
            {
                return _ApiFormat;
            }
        }

        /// <summary>
        /// HTTP method.
        /// </summary>
        public HttpMethod HttpMethod
        {
            get
            {
                return _HttpMethod;
            }
        }

        /// <summary>
        /// Query.
        /// </summary>
        public NameValueCollection Query
        {
            get
            {
                return _Query;
            }
        }

        /// <summary>
        /// Headers.
        /// </summary>
        public NameValueCollection Headers
        {
            get
            {
                return _Headers;
            }
        }

        /// <summary>
        /// URL parameters.
        /// </summary>
        public NameValueCollection UrlParameters
        {
            get
            {
                return _UrlParameters;
            }
        }

        private static Serializer _Serializer = new Serializer();
        private HttpMethod _HttpMethod = HttpMethod.GET;
        private string _Url = null;
        private NameValueCollection _Query = null;
        private NameValueCollection _Headers = null;
        private NameValueCollection _UrlParameters = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
        private RequestTypeEnum _RequestType = RequestTypeEnum.Unknown;
        private ApiFormatEnum _ApiFormat = ApiFormatEnum.Unknown;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="url">URL.</param>
        /// <param name="query">Query.</param>
        /// <param name="headers">Headers.</param>
        public UrlContext(
            HttpMethod method, 
            string url, 
            NameValueCollection query = null, 
            NameValueCollection headers = null)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            _HttpMethod = method;
            _Url = url;
            _Query = NormalizeNameValueCollection(query);
            _Headers = NormalizeNameValueCollection(headers);
            _RequestType = UrlAndMethodToRequestType();
            _ApiFormat = SetApiFormat();
        }

        /// <summary>
        /// Check if a parameter exists.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>True if exists.</returns>
        public bool ParameterExists(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (_UrlParameters != null && _UrlParameters.AllKeys != null)
            {
                if (_UrlParameters.AllKeys.Contains(key))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieve URL parameter.
        /// </summary>
        /// <param name="key">Parameter.</param>
        /// <returns>Value or null.</returns>
        public string GetParameter(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string ret = null;
            if (_UrlParameters != null)
            {
                if (_UrlParameters.AllKeys.Contains(key, StringComparer.InvariantCultureIgnoreCase))
                {
                    ret = _UrlParameters[key];
                }
            }
            return ret;
        }

        /// <summary>
        /// Retrieve query value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Value or null.</returns>
        public string GetQueryValue(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            if (_Query != null)
            {
                foreach (string existingKey in _Query.Keys)
                {
                    if (existingKey != null && existingKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        return _Query[existingKey];
                }

                return null;
            }

            return null;
        }

        /// <summary>
        /// Retrieve header value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Value or null.</returns>
        public string GetHeaderValue(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            if (_Headers != null)
            {
                foreach (string existingKey in _Headers.Keys)
                {
                    if (existingKey != null && existingKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        return _Headers[existingKey];
                }

                return null;
            }

            return null;
        }

        /// <summary>
        /// Check if a query key exists.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>True if exists.</returns>
        public bool QueryExists(string key)
        {
            if (_Query != null && !string.IsNullOrEmpty(key))
            {
                foreach (string existingKey in _Query.Keys)
                {
                    if (existingKey != null && existingKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Check if a header exists.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>True if exists.</returns>
        public bool HeaderExists(string key)
        {
            if (_Headers != null && !string.IsNullOrEmpty(key))
            {
                foreach (string existingKey in _Headers.Keys)
                {
                    if (existingKey != null && existingKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        private RequestTypeEnum UrlAndMethodToRequestType()
        {
            _UrlParameters = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            Matcher matcher = new Matcher(_Url);

            if (_HttpMethod == HttpMethod.GET)
            {
                if (matcher.Match("/", out _UrlParameters)) return RequestTypeEnum.Root;
                if (matcher.Match("/favicon.ico", out _UrlParameters)) return RequestTypeEnum.GetFavicon;

                if (matcher.Match("/api/tags", out _UrlParameters)) return RequestTypeEnum.OllamaListModels;
                if (matcher.Match("/api/ps", out _UrlParameters)) return RequestTypeEnum.OllamaListRunningModels;

                if (matcher.Match("/v1.0/frontends", out _UrlParameters)) return RequestTypeEnum.AdminGetFrontends;
                if (matcher.Match("/v1.0/frontends/{identifier}", out _UrlParameters)) return RequestTypeEnum.AdminGetFrontend;

                if (matcher.Match("/v1.0/backends", out _UrlParameters)) return RequestTypeEnum.AdminGetBackends;
                if (matcher.Match("/v1.0/backends/health", out _UrlParameters)) return RequestTypeEnum.AdminGetBackendsHealth;
                if (matcher.Match("/v1.0/backends/{identifier}", out _UrlParameters)) return RequestTypeEnum.AdminGetBackend;
                if (matcher.Match("/v1.0/backends/{identifier}/health", out _UrlParameters)) return RequestTypeEnum.AdminGetBackendHealth;
            }
            else if (_HttpMethod == HttpMethod.HEAD)
            {
                if (matcher.Match("/", out _UrlParameters)) return RequestTypeEnum.ValidateConnectivity;
                if (matcher.Match("/favicon.ico", out _UrlParameters)) return RequestTypeEnum.ExistsFavicon;

                if (matcher.Match("/v1.0/frontends/{identifier}", out _UrlParameters)) return RequestTypeEnum.AdminExistsFrontend;

                if (matcher.Match("/v1.0/backends/{identifier}", out _UrlParameters)) return RequestTypeEnum.AdminExistsBackend;
            }
            else if (_HttpMethod == HttpMethod.PUT)
            {
                if (matcher.Match("/v1.0/frontends", out _UrlParameters)) return RequestTypeEnum.AdminCreateFrontend;
                if (matcher.Match("/v1.0/frontends/{identifier}", out _UrlParameters)) return RequestTypeEnum.AdminUpdateFrontend;

                if (matcher.Match("/v1.0/backends", out _UrlParameters)) return RequestTypeEnum.AdminCreateBackend;
                if (matcher.Match("/v1.0/backends/{identifier}", out _UrlParameters)) return RequestTypeEnum.AdminUpdateBackend;
            }
            else if (_HttpMethod == HttpMethod.POST)
            {
                if (matcher.Match("/api/pull", out _UrlParameters)) return RequestTypeEnum.OllamaPullModel;
                if (matcher.Match("/api/show", out _UrlParameters)) return RequestTypeEnum.OllamaShowModelInformation;
                if (matcher.Match("/api/generate", out _UrlParameters)) return RequestTypeEnum.OllamaGenerateCompletion;
                if (matcher.Match("/api/chat", out _UrlParameters)) return RequestTypeEnum.OllamaGenerateChatCompletion;
                if (matcher.Match("/api/embed", out _UrlParameters)) return RequestTypeEnum.OllamaGenerateEmbeddings;

                if (matcher.Match("/v1/completions", out _UrlParameters)) return RequestTypeEnum.OpenAIGenerateCompletion;
                if (matcher.Match("/v1/chat/completions", out _UrlParameters)) return RequestTypeEnum.OpenAIGenerateChatCompletion;
                if (matcher.Match("/v1/embeddings", out _UrlParameters)) return RequestTypeEnum.OpenAIGenerateEmbeddings;
            }
            else if (_HttpMethod == HttpMethod.DELETE)
            {
                if (matcher.Match("/api/delete", out _UrlParameters)) return RequestTypeEnum.OllamaDeleteModel;

                if (matcher.Match("/v1.0/frontends/{identifier}", out _UrlParameters)) return RequestTypeEnum.AdminDeleteFrontend;
                if (matcher.Match("/v1.0/backends/{identifier}", out _UrlParameters)) return RequestTypeEnum.AdminDeleteBackend;
            }

            return RequestTypeEnum.Unknown;
        }

        private ApiFormatEnum SetApiFormat()
        {
            if (_Url.StartsWith("/api/")) return ApiFormatEnum.Ollama;
            else if (_Url.StartsWith("/v1/")) return ApiFormatEnum.OpenAI;
            else return ApiFormatEnum.Unknown;
        }

        private NameValueCollection NormalizeNameValueCollection(NameValueCollection nvc)
        {
            NameValueCollection ret = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            if (nvc != null)
            {
                foreach (string key in nvc.AllKeys)
                {
                    ret.Add(key, nvc[key]);
                }
            }

            return ret;
        }
    }
}
