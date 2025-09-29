namespace OllamaFlow.Core.Models.Agnostic.Responses
{
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models.Agnostic.Common;

    /// <summary>
    /// Agnostic model information response.
    /// </summary>
    public class AgnosticModelInfoResponse : AgnosticResponse
    {
        /// <summary>
        /// The model information.
        /// </summary>
        public AgnosticModel Model { get; set; }

        /// <summary>
        /// Model template (for Ollama compatibility).
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Model parameters (for Ollama compatibility).
        /// </summary>
        public object Parameters { get; set; }

        /// <summary>
        /// Model system prompt (for Ollama compatibility).
        /// </summary>
        public string System { get; set; }
    }
}