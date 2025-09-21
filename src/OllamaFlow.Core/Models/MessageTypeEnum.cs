namespace OllamaFlow.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Message type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageTypeEnum
    {
        /// <summary>
        /// Telemetry.
        /// </summary>
        [JsonPropertyName("Telemetry")]
        Telemetry
    }
}
