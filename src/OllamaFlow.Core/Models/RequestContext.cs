namespace OllamaFlow.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text.Json.Serialization;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Core.Settings;
    using WatsonWebserver.Core;

    /// <summary>
    /// Request context.
    /// </summary>
    public class RequestContext
    {
        /// <summary>
        /// Request GUID.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Request type.
        /// </summary>
        public RequestTypeEnum RequestType
        {
            get => _UrlContext.RequestType;
        }

        /// <summary>
        /// API format.
        /// </summary>
        public ApiFormatEnum ApiFormat
        {
            get => _UrlContext.ApiFormat;
        }

        /// <summary>
        /// Content-type.
        /// </summary>
        public string ContentType { get; set; } = null;

        /// <summary>
        /// Content length.
        /// </summary>
        public long ContentLength
        {
            get
            {
                return _ContentLength;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(ContentLength));
                _ContentLength = value;
            }
        }

        /// <summary>
        /// HTTP method.
        /// </summary>
        public HttpMethod Method
        {
            get
            {
                if (_HttpContext != null) return _HttpContext.Request.Method;
                return HttpMethod.UNKNOWN;
            }
        }

        /// <summary>
        /// URL.
        /// </summary>
        public string Url
        {
            get
            {
                if (_HttpContext != null) return _HttpContext.Request.Url.Full;
                return null;
            }
        }

        /// <summary>
        /// URL context.
        /// </summary>
        public UrlContext UrlContext
        {
            get
            {
                return _UrlContext;
            }
        }

        /// <summary>
        /// Querystring.
        /// </summary>
        public string Querystring
        {
            get
            {
                if (_HttpContext != null) return _HttpContext.Request.Query.Querystring;
                return null;
            }
        }

        /// <summary>
        /// Headers.
        /// </summary>
        public NameValueCollection Headers
        {
            get
            {
                if (_HttpContext != null) return _HttpContext.Request.Headers;
                return new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
            }
        }

        /// <summary>
        /// Query.
        /// </summary>
        public NameValueCollection Query
        {
            get
            {
                if (_HttpContext != null) return _HttpContext.Request.Query.Elements;
                return new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
            }
        }

        /// <summary>
        /// HTTP context.
        /// </summary>
        [JsonIgnore]
        public HttpContextBase HttpContext
        {
            get => _HttpContext;
        }

        /// <summary>
        /// Boolean indicating if the request is an embeddings request.
        /// </summary>
        public bool IsEmbeddingsRequest
        {
            get
            {
                return RequestTypeHelper.IsEmbeddingsRequest(RequestType);
            }
        }

        /// <summary>
        /// Boolean indicating if the request is a chat completion request.
        /// </summary>
        public bool IsCompletionsRequest
        {
            get
            {
                return RequestTypeHelper.IsCompletionsRequest(RequestType);
            }
        }

        /// <summary>
        /// Client identifier.
        /// </summary>
        public string ClientIdentifier
        {
            get
            {
                if (_Settings != null && _Settings.StickyHeaders.Any() && Headers != null)
                {
                    foreach (string stickyHeader in _Settings.StickyHeaders)
                    {
                        string value = Headers[stickyHeader]; // lookups are case-insensitive
                        if (!String.IsNullOrEmpty(value)) return value;
                    }
                }

                if (_HttpContext != null && _HttpContext.Request != null && _HttpContext.Request.Source != null)
                {
                    return _HttpContext.Request.Source.IpAddress;
                }

                return "unknown";
            }
        }

        private static Serializer _Serializer = new Serializer();
        private OllamaFlowSettings _Settings = null;
        private HttpContextBase _HttpContext = null;
        private UrlContext _UrlContext = null;
        private long _ContentLength = 0;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestContext()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="ctx">HTTP context.</param>
        public RequestContext(OllamaFlowSettings settings, HttpContextBase ctx)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _HttpContext = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _UrlContext = new UrlContext(
                ctx.Request.Method,
                ctx.Request.Url.RawWithoutQuery,
                ctx.Request.Query.Elements,
                ctx.Request.Headers);
        }
    }
}
