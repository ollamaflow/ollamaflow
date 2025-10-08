namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// OpenAI error details.
    /// </summary>
    public class OpenAIErrorDetails
    {
        /// <summary>
        /// Message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = null;

        /// <summary>
        /// Type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = null;

        /// <summary>
        /// Parameters.
        /// </summary>
        [JsonPropertyName("param")]
        public object Parameters { get; set; } = null;

        /// <summary>
        /// Code.
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = null;

        /// <summary>
        /// OpenAI error details.
        /// </summary>
        public OpenAIErrorDetails()
        {

        }
    }
}
