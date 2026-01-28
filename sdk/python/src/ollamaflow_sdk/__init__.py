"""
OllamaFlow SDK for Python.

A Python SDK for interacting with OllamaFlow server instances - providing
Ollama and OpenAI compatible API wrappers plus Frontend/Backend management.
"""

from ollamaflow_sdk.client import OllamaFlowSdk
from ollamaflow_sdk.models.backend import Backend, BackendHealth
from ollamaflow_sdk.models.frontend import Frontend
from ollamaflow_sdk.models.ollama import (
    OllamaChatMessage,
    OllamaCompletionOptions,
    OllamaDeleteModelRequest,
    OllamaGenerateChatCompletionRequest,
    OllamaGenerateChatCompletionResult,
    OllamaGenerateCompletionRequest,
    OllamaGenerateCompletionResult,
    OllamaGenerateEmbeddingsRequest,
    OllamaGenerateEmbeddingsResult,
    OllamaLocalModel,
    OllamaModelDetails,
    OllamaPullModelRequest,
    OllamaPullModelResult,
    OllamaRunningModel,
    OllamaShowModelInfoRequest,
    OllamaShowModelInfoResult,
)
from ollamaflow_sdk.models.openai import (
    OpenAIChatChoice,
    OpenAIChatMessage,
    OpenAICompletionChoice,
    OpenAIEmbedding,
    OpenAIGenerateChatCompletionRequest,
    OpenAIGenerateChatCompletionResult,
    OpenAIGenerateCompletionRequest,
    OpenAIGenerateCompletionResult,
    OpenAIGenerateEmbeddingsRequest,
    OpenAIGenerateEmbeddingsResult,
    OpenAIUsage,
)

__version__ = "1.0.0"
__all__ = [
    # Main client
    "OllamaFlowSdk",
    # Backend/Frontend models
    "Backend",
    "BackendHealth",
    "Frontend",
    # Ollama models
    "OllamaChatMessage",
    "OllamaCompletionOptions",
    "OllamaDeleteModelRequest",
    "OllamaGenerateChatCompletionRequest",
    "OllamaGenerateChatCompletionResult",
    "OllamaGenerateCompletionRequest",
    "OllamaGenerateCompletionResult",
    "OllamaGenerateEmbeddingsRequest",
    "OllamaGenerateEmbeddingsResult",
    "OllamaLocalModel",
    "OllamaModelDetails",
    "OllamaPullModelRequest",
    "OllamaPullModelResult",
    "OllamaRunningModel",
    "OllamaShowModelInfoRequest",
    "OllamaShowModelInfoResult",
    # OpenAI models
    "OpenAIChatChoice",
    "OpenAIChatMessage",
    "OpenAICompletionChoice",
    "OpenAIEmbedding",
    "OpenAIGenerateChatCompletionRequest",
    "OpenAIGenerateChatCompletionResult",
    "OpenAIGenerateCompletionRequest",
    "OpenAIGenerateCompletionResult",
    "OpenAIGenerateEmbeddingsRequest",
    "OpenAIGenerateEmbeddingsResult",
    "OpenAIUsage",
]
