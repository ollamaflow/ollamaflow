namespace OllamaFlow.Core.Models.OpenAI
{
    using OllamaFlow.Core.Models.OpenAI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// OpenAI error message.
    /// </summary>
    public class OpenAIError
    {
        /// <summary>
        /// Error details.
        /// </summary>
        [JsonPropertyName("error")]
        public OpenAIErrorDetails Error { get; set; } = null;

        /// <summary>
        /// OpenAI error message.
        /// </summary>
        public OpenAIError()
        {

        }
    }
}
