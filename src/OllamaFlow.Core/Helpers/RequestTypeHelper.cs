namespace OllamaFlow.Core.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Settings;
    using WatsonWebserver.Core;

    /// <summary>
    /// Request type helper.
    /// </summary>
    public static class RequestTypeHelper
    {
        /// <summary>
        /// Boolean indicating if the request is an embeddings request.
        /// </summary>
        public static bool IsEmbeddingsRequest(RequestTypeEnum requestType)
        {
            return
                requestType == RequestTypeEnum.OllamaGenerateEmbeddings
                || requestType == RequestTypeEnum.OpenAIGenerateEmbeddings;
        }

        /// <summary>
        /// Boolean indicating if the request is a chat completion request.
        /// </summary>
        public static bool IsCompletionsRequest(RequestTypeEnum requestType)
        {
            return
                requestType == RequestTypeEnum.OllamaGenerateCompletion
                || requestType == RequestTypeEnum.OllamaGenerateChatCompletion
                || requestType == RequestTypeEnum.OpenAIGenerateCompletion
                || requestType == RequestTypeEnum.OpenAIGenerateChatCompletion;
        }
    }
}
