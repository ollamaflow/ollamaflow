namespace OllamaFlow.Core.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// HTTP method helper.
    /// </summary>
    public static class HttpMethodHelper
    {
        /// <summary>
        /// Convert an HTTP method string to an HttpMethod.
        /// </summary>
        /// <param name="str">String.</param>
        /// <returns>HttpMethod.</returns>
        public static System.Net.Http.HttpMethod ToHttpMethod(string str)
        {
            if (string.IsNullOrEmpty(str)) throw new ArgumentNullException(nameof(str));
            str = str.ToLower();
            switch (str)
            {
                case "get":
                    return System.Net.Http.HttpMethod.Get;
                case "post":
                    return System.Net.Http.HttpMethod.Post;
                case "put":
                    return System.Net.Http.HttpMethod.Put;
                case "delete":
                    return System.Net.Http.HttpMethod.Delete;
                case "head":
                    return System.Net.Http.HttpMethod.Head;
                case "options":
                    return System.Net.Http.HttpMethod.Options;
                case "trace":
                    return System.Net.Http.HttpMethod.Trace;
                case "patch":
                    return System.Net.Http.HttpMethod.Patch;
                default:
                    throw new ArgumentException("Unknown HTTP method '" + str + "'.");
            }
        }

        /// <summary>
        /// Convert an HTTP method to a string.
        /// </summary>
        /// <param name="method">HttpMethod.</param>
        /// <returns>String.</returns>
        public static string FromHttpMethod(System.Net.Http.HttpMethod method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            return method.Method.ToUpper();
        }
    }
}
