"""Ollama API models for OllamaFlow SDK."""

from datetime import datetime
from typing import Any, Optional

from pydantic import BaseModel, Field


class OllamaCompletionOptions(BaseModel):
    """Options for Ollama completion requests."""

    seed: Optional[int] = Field(default=None)
    """Random seed for generation."""

    num_predict: Optional[int] = Field(default=None, ge=-2)
    """Number of tokens to generate. -1 = infinite, -2 = fill context."""

    num_gpu: Optional[int] = Field(default=None, ge=-1)
    """Number of layers to offload to GPU. -1 = all layers."""

    temperature: Optional[float] = Field(default=None, ge=0.0, le=2.0)
    """Temperature for sampling (0.0 to 2.0)."""

    top_k: Optional[int] = Field(default=None, ge=0, le=100)
    """Top-k sampling parameter."""

    top_p: Optional[float] = Field(default=None, ge=0.0, le=1.0)
    """Top-p (nucleus) sampling parameter."""

    min_p: Optional[float] = Field(default=None, ge=0.0, le=1.0)
    """Min-p sampling parameter."""

    tfs_z: Optional[float] = Field(default=None, ge=0.0, le=1.0)
    """Tail free sampling parameter."""

    typical_p: Optional[float] = Field(default=None, ge=0.0, le=1.0)
    """Typical sampling parameter."""

    repeat_penalty: Optional[float] = Field(default=None, ge=0.0, le=2.0)
    """Repeat penalty."""

    repeat_last_n: Optional[int] = Field(default=None, ge=0)
    """Last n tokens to consider for repeat penalty."""

    presence_penalty: Optional[float] = Field(default=None, ge=-2.0, le=2.0)
    """Presence penalty."""

    frequency_penalty: Optional[float] = Field(default=None, ge=-2.0, le=2.0)
    """Frequency penalty."""

    mirostat: Optional[int] = Field(default=None, ge=0, le=2)
    """Mirostat sampling mode (0, 1, or 2)."""

    mirostat_tau: Optional[float] = Field(default=None, ge=0.0, le=10.0)
    """Mirostat target entropy tau."""

    mirostat_eta: Optional[float] = Field(default=None, ge=0.0, le=1.0)
    """Mirostat learning rate eta."""

    penalize_newline: Optional[bool] = Field(default=None)
    """Whether to penalize newline tokens."""

    stop: Optional[list[str]] = Field(default=None)
    """Stop sequences for generation."""

    numa: Optional[bool] = Field(default=None)
    """Enable NUMA support."""

    num_ctx: Optional[int] = Field(default=None, ge=128, le=1048576)
    """Context size."""

    num_batch: Optional[int] = Field(default=None, ge=1)
    """Batch size for prompt evaluation."""

    num_thread: Optional[int] = Field(default=None, ge=1, le=128)
    """Number of threads to use."""

    num_keep: Optional[int] = Field(default=None, ge=0)
    """Number of tokens to keep from initial prompt."""

    use_mlock: Optional[bool] = Field(default=None)
    """Use memory locking."""

    use_mmap: Optional[bool] = Field(default=None)
    """Use memory mapping."""

    vocab_only: Optional[bool] = Field(default=None)
    """Vocabulary only mode."""

    low_vram: Optional[bool] = Field(default=None)
    """Low VRAM mode."""

    f16_kv: Optional[bool] = Field(default=None)
    """F16 key-value storage."""

    main_gpu: Optional[int] = Field(default=None, ge=0)
    """Main GPU index."""

    logits_all: Optional[bool] = Field(default=None)
    """Logits all mode."""


class OllamaChatMessage(BaseModel):
    """A message in an Ollama chat conversation."""

    role: str
    """Role of the message sender (system, user, assistant)."""

    content: str
    """Content of the message."""

    images: Optional[list[str]] = Field(default=None)
    """Base64-encoded images for multimodal models."""


class OllamaGenerateCompletionRequest(BaseModel):
    """Request for Ollama text completion."""

    model: str
    """Model name to use for generation."""

    prompt: str
    """The prompt to generate a response for."""

    options: Optional[OllamaCompletionOptions] = Field(default=None)
    """Additional model parameters."""

    system: Optional[str] = Field(default=None)
    """System message to use."""

    template: Optional[str] = Field(default=None)
    """Custom prompt template."""

    context: Optional[list[int]] = Field(default=None)
    """Context from a previous request for conversation continuity."""

    stream: Optional[bool] = Field(default=None)
    """Enable streaming of generated text."""

    raw: Optional[bool] = Field(default=None)
    """If false, response will not include the prompt."""

    format: Optional[str] = Field(default=None)
    """Response format (e.g., 'json')."""

    images: Optional[list[str]] = Field(default=None)
    """Base64-encoded images for multimodal models."""

    keep_alive: Optional[str] = Field(default=None)
    """How long to keep the model loaded (e.g., '5m', '1h', 'never')."""


class OllamaGenerateCompletionResult(BaseModel):
    """Result from an Ollama text completion request."""

    model: Optional[str] = Field(default=None)
    """Model used for generation."""

    created_at: Optional[str] = Field(default=None)
    """Timestamp of generation."""

    response: Optional[str] = Field(default=None)
    """Generated text response."""

    done: Optional[bool] = Field(default=None)
    """Whether generation is complete."""

    done_reason: Optional[str] = Field(default=None)
    """Reason for completion."""

    context: Optional[list[int]] = Field(default=None)
    """Context for conversation continuity."""

    total_duration: Optional[int] = Field(default=None)
    """Total duration in nanoseconds."""

    load_duration: Optional[int] = Field(default=None)
    """Model load duration in nanoseconds."""

    prompt_eval_count: Optional[int] = Field(default=None)
    """Number of tokens in the prompt."""

    prompt_eval_duration: Optional[int] = Field(default=None)
    """Prompt evaluation duration in nanoseconds."""

    eval_count: Optional[int] = Field(default=None)
    """Number of generated tokens."""

    eval_duration: Optional[int] = Field(default=None)
    """Token generation duration in nanoseconds."""


class OllamaGenerateChatCompletionRequest(BaseModel):
    """Request for Ollama chat completion."""

    model: str
    """Model name to use for chat completion."""

    messages: list[OllamaChatMessage]
    """Messages in the conversation."""

    options: Optional[OllamaCompletionOptions] = Field(default=None)
    """Additional model parameters."""

    format: Optional[str] = Field(default=None)
    """Response format (e.g., 'json')."""

    template: Optional[str] = Field(default=None)
    """Custom prompt template."""

    stream: Optional[bool] = Field(default=None)
    """Enable streaming of generated text."""

    keep_alive: Optional[str] = Field(default=None)
    """How long to keep the model loaded."""

    tools: Optional[list[dict[str, Any]]] = Field(default=None)
    """Tools/functions available for the model to use."""


class OllamaGenerateChatCompletionResult(BaseModel):
    """Result from an Ollama chat completion request."""

    model: Optional[str] = Field(default=None)
    """Model used for generation."""

    created_at: Optional[str] = Field(default=None)
    """Timestamp of generation."""

    message: Optional[OllamaChatMessage] = Field(default=None)
    """Generated message."""

    done: Optional[bool] = Field(default=None)
    """Whether generation is complete."""

    done_reason: Optional[str] = Field(default=None)
    """Reason for completion."""

    total_duration: Optional[int] = Field(default=None)
    """Total duration in nanoseconds."""

    load_duration: Optional[int] = Field(default=None)
    """Model load duration in nanoseconds."""

    prompt_eval_count: Optional[int] = Field(default=None)
    """Number of tokens in the prompt."""

    prompt_eval_duration: Optional[int] = Field(default=None)
    """Prompt evaluation duration in nanoseconds."""

    eval_count: Optional[int] = Field(default=None)
    """Number of generated tokens."""

    eval_duration: Optional[int] = Field(default=None)
    """Token generation duration in nanoseconds."""


class OllamaGenerateEmbeddingsRequest(BaseModel):
    """Request for Ollama embeddings generation."""

    model: str
    """Model name to use for embeddings."""

    input: str | list[str]
    """Text or list of texts to generate embeddings for."""

    options: Optional[OllamaCompletionOptions] = Field(default=None)
    """Additional model parameters."""

    keep_alive: Optional[str] = Field(default=None)
    """How long to keep the model loaded."""


class OllamaGenerateEmbeddingsResult(BaseModel):
    """Result from an Ollama embeddings request."""

    model: Optional[str] = Field(default=None)
    """Model used for embeddings."""

    embeddings: Optional[list[list[float]]] = Field(default=None)
    """Generated embeddings vectors."""

    total_duration: Optional[int] = Field(default=None)
    """Total duration in nanoseconds."""

    load_duration: Optional[int] = Field(default=None)
    """Model load duration in nanoseconds."""

    prompt_eval_count: Optional[int] = Field(default=None)
    """Number of tokens processed."""


class OllamaModelDetails(BaseModel):
    """Details about an Ollama model."""

    parent_model: Optional[str] = Field(default=None)
    """Parent model name."""

    format: Optional[str] = Field(default=None)
    """Model format."""

    family: Optional[str] = Field(default=None)
    """Model family."""

    families: Optional[list[str]] = Field(default=None)
    """Model families."""

    parameter_size: Optional[str] = Field(default=None)
    """Parameter size."""

    quantization_level: Optional[str] = Field(default=None)
    """Quantization level."""


class OllamaLocalModel(BaseModel):
    """Information about a locally available Ollama model."""

    name: Optional[str] = Field(default=None)
    """Model name."""

    model: Optional[str] = Field(default=None)
    """Model identifier."""

    modified_at: Optional[str] = Field(default=None)
    """Last modification timestamp."""

    size: Optional[int] = Field(default=None)
    """Model size in bytes."""

    digest: Optional[str] = Field(default=None)
    """Model digest."""

    details: Optional[OllamaModelDetails] = Field(default=None)
    """Model details."""


class OllamaRunningModel(BaseModel):
    """Information about a currently running Ollama model."""

    name: Optional[str] = Field(default=None)
    """Model name."""

    model: Optional[str] = Field(default=None)
    """Model identifier."""

    size: Optional[int] = Field(default=None)
    """Model size in bytes."""

    digest: Optional[str] = Field(default=None)
    """Model digest."""

    details: Optional[OllamaModelDetails] = Field(default=None)
    """Model details."""

    expires_at: Optional[str] = Field(default=None)
    """When the model will be unloaded."""

    size_vram: Optional[int] = Field(default=None)
    """VRAM usage in bytes."""


class OllamaPullModelRequest(BaseModel):
    """Request to pull an Ollama model."""

    name: str
    """Model name to pull."""

    insecure: Optional[bool] = Field(default=None)
    """Allow insecure connections."""

    stream: Optional[bool] = Field(default=None)
    """Enable streaming of progress."""


class OllamaPullModelResult(BaseModel):
    """Progress update from an Ollama model pull."""

    status: Optional[str] = Field(default=None)
    """Current status of the pull."""

    digest: Optional[str] = Field(default=None)
    """Digest of the layer being pulled."""

    total: Optional[int] = Field(default=None)
    """Total size in bytes."""

    completed: Optional[int] = Field(default=None)
    """Completed size in bytes."""


class OllamaDeleteModelRequest(BaseModel):
    """Request to delete an Ollama model."""

    name: str
    """Model name to delete."""


class OllamaShowModelInfoRequest(BaseModel):
    """Request to show Ollama model information."""

    name: str
    """Model name to show information for."""

    verbose: Optional[bool] = Field(default=None)
    """Include verbose information."""


class OllamaShowModelInfoResult(BaseModel):
    """Result from an Ollama show model info request."""

    modelfile: Optional[str] = Field(default=None)
    """Model file content."""

    parameters: Optional[str] = Field(default=None)
    """Model parameters."""

    template: Optional[str] = Field(default=None)
    """Model template."""

    details: Optional[OllamaModelDetails] = Field(default=None)
    """Model details."""

    model_info: Optional[dict[str, Any]] = Field(default=None)
    """Additional model information."""
