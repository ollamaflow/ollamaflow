namespace OllamaFlow.Core.Models.Agnostic.Common
{
    using System;

    /// <summary>
    /// Agnostic streaming choice for chat completions.
    /// </summary>
    public class AgnosticStreamingChoice
    {
        /// <summary>
        /// Index of this choice in the list.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The streaming message delta (incremental content).
        /// </summary>
        public AgnosticMessageDelta Delta { get; set; }

        /// <summary>
        /// The reason the model stopped generating tokens.
        /// </summary>
        public string FinishReason { get; set; }
    }
}