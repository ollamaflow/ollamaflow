namespace OllamaFlow.Core.Helpers
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents the different types of requests that can be proxied to Ollama.
    /// </summary>
    public enum RequestTypeEnum
    {
        #region Admin

        /// <summary>
        /// Root endpoint request.
        /// </summary>
        [EnumMember(Value = "Root")]
        Root,

        /// <summary>
        /// Health check or connectivity validation request.
        /// </summary>
        [EnumMember(Value = "ValidateConnectivity")]
        ValidateConnectivity,

        /// <summary>
        /// Unknown or unrecognized request type.
        /// </summary>
        [EnumMember(Value = "Unknown")]
        Unknown,

        #endregion

        #region Model-Management

        /// <summary>
        /// Pull a model from the registry.
        /// </summary>
        [EnumMember(Value = "PullModel")]
        PullModel,

        /// <summary>
        /// Push a model to the registry.
        /// </summary>
        [EnumMember(Value = "PushModel")]
        PushModel,

        /// <summary>
        /// Create a model from a Modelfile.
        /// </summary>
        [EnumMember(Value = "CreateModel")]
        CreateModel,

        /// <summary>
        /// Copy a model.
        /// </summary>
        [EnumMember(Value = "CopyModel")]
        CopyModel,

        /// <summary>
        /// Delete a model.
        /// </summary>
        [EnumMember(Value = "DeleteModel")]
        DeleteModel,

        /// <summary>
        /// List available models.
        /// </summary>
        [EnumMember(Value = "ListModels")]
        ListModels,

        /// <summary>
        /// List currently running models.
        /// </summary>
        [EnumMember(Value = "ListRunningModels")]
        ListRunningModels,

        /// <summary>
        /// Show detailed information about a model.
        /// </summary>
        [EnumMember(Value = "ShowModelInformation")]
        ShowModelInformation,

        /// <summary>
        /// Create or retrieve model blobs.
        /// </summary>
        [EnumMember(Value = "CreateBlob")]
        CreateBlob,

        /// <summary>
        /// Check if a blob exists.
        /// </summary>
        [EnumMember(Value = "CheckBlob")]
        CheckBlob,
        #endregion

        #region Generation
        /// <summary>
        /// Generate a completion (non-chat).
        /// </summary>
        [EnumMember(Value = "GenerateCompletion")]
        GenerateCompletion,

        /// <summary>
        /// Generate a chat completion.
        /// </summary>
        [EnumMember(Value = "GenerateChatCompletion")]
        GenerateChatCompletion,

        /// <summary>
        /// Generate embeddings for a single input.
        /// </summary>
        [EnumMember(Value = "GenerateEmbeddings")]
        GenerateEmbeddings,

        #endregion

        #region OpenAI-Compatibility

        /// <summary>
        /// OpenAI-compatible chat completions endpoint.
        /// </summary>
        [EnumMember(Value = "OpenAIChatCompletions")]
        OpenAIChatCompletions,

        /// <summary>
        /// OpenAI-compatible completions endpoint.
        /// </summary>
        [EnumMember(Value = "OpenAICompletions")]
        OpenAICompletions,

        /// <summary>
        /// OpenAI-compatible embeddings endpoint.
        /// </summary>
        [EnumMember(Value = "OpenAIEmbeddings")]
        OpenAIEmbeddings,

        /// <summary>
        /// OpenAI-compatible models list endpoint.
        /// </summary>
        [EnumMember(Value = "OpenAIListModels")]
        OpenAIListModels,

        /// <summary>
        /// OpenAI-compatible model retrieval endpoint.
        /// </summary>
        [EnumMember(Value = "OpenAIRetrieveModel")]
        OpenAIRetrieveModel,

        #endregion
    }
}