namespace OllamaFlow.Core.Enums
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents the different types of requests in a format-agnostic way.
    /// Used to categorize requests regardless of the source API format (Ollama, OpenAI, etc.).
    /// </summary>
    public enum RequestTypeEnum
    {
        #region Admin

        /// <summary>
        /// Unknown or unrecognized request type.
        /// </summary>
        [EnumMember(Value = "Unknown")]
        Unknown,

        /// <summary>
        /// Root endpoint request.
        /// </summary>
        [EnumMember(Value = "Root")]
        Root,

        /// <summary>
        /// Validate connectivity request.
        /// </summary>
        [EnumMember(Value = "ValidateConnectivity")]
        ValidateConnectivity,

        /// <summary>
        /// Get favicon endpoint request.
        /// </summary>
        [EnumMember(Value = "GetFavicon")]
        GetFavicon,

        /// <summary>
        /// Exists favicon endpoint request.
        /// </summary>
        [EnumMember(Value = "ExistsFavicon")]
        ExistsFavicon,

        #endregion

        #region Ollama-APIs

        /// <summary>
        /// Ollama API to pull a model from the registry.
        /// </summary>
        [EnumMember(Value = "OllamaPullModel")]
        OllamaPullModel,

        /// <summary>
        /// Ollama API to delete a model.
        /// </summary>
        [EnumMember(Value = "OllamaDeleteModel")]
        OllamaDeleteModel,

        /// <summary>
        /// Ollama API to list available models.
        /// </summary>
        [EnumMember(Value = "OllamaListModels")]
        OllamaListModels,

        /// <summary>
        /// Ollama API to show detailed information about a model.
        /// </summary>
        [EnumMember(Value = "OllamaShowModelInformation")]
        OllamaShowModelInformation,

        /// <summary>
        /// Ollama API to list currently running models.
        /// </summary>
        [EnumMember(Value = "OllamaListRunningModels")]
        OllamaListRunningModels,

        /// <summary>
        /// Ollama API to generate a completion (non-chat).
        /// </summary>
        [EnumMember(Value = "OllamaGenerateCompletion")]
        OllamaGenerateCompletion,

        /// <summary>
        /// Ollama API to generate a chat completion.
        /// </summary>
        [EnumMember(Value = "OllamaGenerateChatCompletion")]
        OllamaGenerateChatCompletion,

        /// <summary>
        /// Ollama API to generate embeddings for a single input.
        /// </summary>
        [EnumMember(Value = "OllamaGenerateEmbeddings")]
        OllamaGenerateEmbeddings,

        #endregion

        #region OpenAI-APIs

        /// <summary>
        /// OpenAI API to generate a completion (non-chat).
        /// </summary>
        [EnumMember(Value = "OpenAIGenerateCompletion")]
        OpenAIGenerateCompletion,
        /// <summary>
        /// OpenAI API to generate a chat completion.
        /// </summary>
        [EnumMember(Value = "OpenAIGenerateChatCompletion")]
        OpenAIGenerateChatCompletion,
        /// <summary>
        /// OpenAI API to generate embeddings for a single input.
        /// </summary>
        [EnumMember(Value = "OpenAIGenerateEmbeddings")]
        OpenAIGenerateEmbeddings,

        #endregion

        #region Admin-APIs

        /// <summary>
        /// Admin API request to list frontends.
        /// </summary>
        [EnumMember(Value = "AdminListFrontends")]
        AdminGetFrontends,

        /// <summary>
        /// Admin API request to get a specific frontend.
        /// </summary>
        [EnumMember(Value = "AdminGetFrontend")]
        AdminGetFrontend,

        /// <summary>
        /// Admin API request to check existence of a specific frontend.
        /// </summary>
        [EnumMember(Value = "AdminExistsFrontend")]
        AdminExistsFrontend,

        /// <summary>
        /// Admin API request to create a frontend.
        /// </summary>
        [EnumMember(Value = "AdminCreateFrontend")]
        AdminCreateFrontend,

        /// <summary>
        /// Admin API request to update a frontend.
        /// </summary>
        [EnumMember(Value = "AdminUpdateFrontend")]
        AdminUpdateFrontend,

        /// <summary>
        /// Admin API request to delete a frontend.
        /// </summary>
        [EnumMember(Value = "AdminDeleteFrontend")]
        AdminDeleteFrontend,

        /// <summary>
        /// Admin API request to get backends.
        /// </summary>
        [EnumMember(Value = "AdminGetBackends")]
        AdminGetBackends,

        /// <summary>
        /// Admin API request to get a specific backend.
        /// </summary>
        [EnumMember(Value = "AdminGetBackend")]
        AdminGetBackend,

        /// <summary>
        /// Admin API request to check existence of a specific backend.
        /// </summary>
        [EnumMember(Value = "AdminExistsBackend")]
        AdminExistsBackend,

        /// <summary>
        /// Admin API request to create a backend.
        /// </summary>
        [EnumMember(Value = "AdminCreateBackend")]
        AdminCreateBackend,

        /// <summary>
        /// Admin API request to update a backend.
        /// </summary>
        [EnumMember(Value = "AdminUpdateBackend")]
        AdminUpdateBackend,

        /// <summary>
        /// Admin API request to delete a backend.
        /// </summary>
        [EnumMember(Value = "AdminDeleteBackend")]
        AdminDeleteBackend,

        /// <summary>
        /// Admin API request to get health status of all backends.
        /// </summary>
        [EnumMember(Value = "AdminGetBackendsHealth")]
        AdminGetBackendsHealth,

        /// <summary>
        /// Admin API request to get health status of a specific backend.
        /// </summary>
        [EnumMember(Value = "AdminGetBackendHealth")]
        AdminGetBackendHealth,

        #endregion
    }
}