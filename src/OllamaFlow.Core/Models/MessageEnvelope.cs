namespace OllamaFlow.Core.Models
{
    using OllamaFlow.Core.Enums;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Message envelope.
    /// </summary>
    public class MessageEnvelope
    {
        /// <summary>
        /// Type of message.
        /// </summary>
        public MessageTypeEnum Type { get; set; } = MessageTypeEnum.Telemetry;

        /// <summary>
        /// Message GUID.
        /// </summary>
        public Guid MessageGuid { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Data.
        /// </summary>
        public object Data { get; set; } = null;

        /// <summary>
        /// Metadata.
        /// </summary>
        public Dictionary<string, string> Metadata
        {
            get => _Metadata;
            set => _Metadata = (value != null ? value : new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase));
        }

        private Dictionary<string, string> _Metadata = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Message envelope.
        /// </summary>
        public MessageEnvelope()
        {

        }
    }
}
