namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    public class TestResult
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

        public string Name { get; set; } = null;
        public TestEnvironment TestEnvironemnt { get; set; } = null;
        public bool Success { get; set; } = false;
        public object Request { get; set; } = null;
        public object Response { get; set; } = null;
        public NameValueCollection Headers { get; set; } = new NameValueCollection();
        public int StatusCode { get; set; } = 0;
        public DateTime StartUtc { get; set; } = DateTime.UtcNow;
        public DateTime EndUtc { get; set; } = DateTime.UtcNow;
        public TimeSpan Runtime { get; set; } = new TimeSpan(0);

        [JsonIgnore]
        public Exception Exception { get; set; } = null;

        public TestResult()
        {

        }

        public override string ToString()
        {
            return $"{Name} success {Success} runtime {Runtime.TotalMilliseconds}ms";
        }

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
