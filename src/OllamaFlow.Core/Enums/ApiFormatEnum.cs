namespace OllamaFlow.Core.Enums
{
    using System.Runtime.Serialization;

    /// <summary>
    /// API format supported by backends.
    /// </summary>
    public enum ApiFormatEnum
    {
        /// <summary>
        /// Ollama native API format.
        /// </summary>
        [EnumMember(Value = "Ollama")]
        Ollama,

        /// <summary>
        /// OpenAI compatible API format.
        /// </summary>
        [EnumMember(Value = "OpenAI")]
        OpenAI,

        /// <summary>
        /// Unknown API format.
        /// </summary>
        [EnumMember(Value = "Unknown")]
        Unknown
    }
}