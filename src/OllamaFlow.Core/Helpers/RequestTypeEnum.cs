namespace OllamaFlow.Core.Helpers
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

        #region Admin-API

        /// <summary>
        /// Admin API request to list frontends.
        /// </summary>
        [EnumMember(Value = "AdminListFrontends")]
        AdminListFrontends,

        /// <summary>
        /// Admin API request to get a specific frontend.
        /// </summary>
        [EnumMember(Value = "AdminGetFrontend")]
        AdminGetFrontend,

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
        /// Admin API request to list backends.
        /// </summary>
        [EnumMember(Value = "AdminListBackends")]
        AdminListBackends,

        /// <summary>
        /// Admin API request to get a specific backend.
        /// </summary>
        [EnumMember(Value = "AdminGetBackend")]
        AdminGetBackend,

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

        /// <summary>
        /// Admin API request to list all sessions.
        /// </summary>
        [EnumMember(Value = "AdminListSessions")]
        AdminListSessions,

        /// <summary>
        /// Admin API request to get sessions for a specific client.
        /// </summary>
        [EnumMember(Value = "AdminGetClientSessions")]
        AdminGetClientSessions,

        /// <summary>
        /// Admin API request to delete sessions for a specific client.
        /// </summary>
        [EnumMember(Value = "AdminDeleteClientSessions")]
        AdminDeleteClientSessions,

        /// <summary>
        /// Admin API request to delete all sessions.
        /// </summary>
        [EnumMember(Value = "AdminDeleteAllSessions")]
        AdminDeleteAllSessions

        #endregion

    }
}