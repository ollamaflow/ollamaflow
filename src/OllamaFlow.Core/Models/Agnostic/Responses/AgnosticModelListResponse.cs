namespace OllamaFlow.Core.Models.Agnostic.Responses
{
    using System.Collections.Generic;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models.Agnostic.Common;

    /// <summary>
    /// Agnostic model list response.
    /// </summary>
    public class AgnosticModelListResponse : AgnosticResponse
    {
        /// <summary>
        /// Object type, typically "list".
        /// </summary>
        public string Object { get; set; } = "list";

        /// <summary>
        /// List of available models.
        /// </summary>
        public List<AgnosticModel> Data { get; set; } = new List<AgnosticModel>();

        /// <summary>
        /// List of models (for Ollama compatibility).
        /// </summary>
        public List<AgnosticModel> Models { get; set; } = new List<AgnosticModel>();
    }
}