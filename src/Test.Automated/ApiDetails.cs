namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// API details containing request, response, headers, status, and exception.
    /// </summary>
    public class ApiDetails
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

        /// <summary>
        /// Name of the step.
        /// </summary>
        public string Step { get; set; } = null;
        
        /// <summary>
        /// Request.
        /// </summary>
        public object Request { get; set; } = null;

        /// <summary>
        /// Response.
        /// </summary>
        public object Response { get; set; } = null;

        /// <summary>
        /// Headers.
        /// </summary>
        public NameValueCollection Headers { get; set; } = new NameValueCollection();
        
        /// <summary>
        /// Status code.
        /// </summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        /// Start timestamp in UTC time.
        /// </summary>
        public DateTime StartUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// End timestamp in UTC time.
        /// </summary>
        public DateTime EndUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Runtime.
        /// </summary>
        public TimeSpan Runtime
        {
            get => (EndUtc - StartUtc);
        }

        /// <summary>
        /// API details containing request, response, headers, status, and exception.
        /// </summary>
        public ApiDetails()
        {

        }

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
