namespace OllamaFlow.Core.Models.Agnostic.Requests
{
    using OllamaFlow.Core.Models.Agnostic.Base;

    /// <summary>
    /// Agnostic model information request.
    /// </summary>
    public class AgnosticModelInfoRequest : AgnosticRequest
    {
        /// <summary>
        /// The model to get information about.
        /// </summary>
        public string Model { get; set; }
    }
}