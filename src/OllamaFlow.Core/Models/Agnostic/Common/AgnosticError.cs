namespace OllamaFlow.Core.Models.Agnostic.Common
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error in an agnostic format.
    /// </summary>
    public class AgnosticError
    {
        /// <summary>
        /// Error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Error type or category.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Error code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Parameter that caused the error.
        /// </summary>
        public string Parameter { get; set; }

        /// <summary>
        /// Internal error details.
        /// </summary>
        public object Details { get; set; }

        /// <summary>
        /// Extension data for API-specific error features.
        /// </summary>
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
    }
}