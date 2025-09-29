namespace OllamaFlow.Core.Models.Agnostic.Common
{
    /// <summary>
    /// Agnostic message delta for streaming responses.
    /// Represents incremental content changes in a streaming response.
    /// </summary>
    public class AgnosticMessageDelta
    {
        /// <summary>
        /// The role of the message (only in first chunk).
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// The incremental content for this delta.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The name of the message sender (optional).
        /// </summary>
        public string Name { get; set; }
    }
}