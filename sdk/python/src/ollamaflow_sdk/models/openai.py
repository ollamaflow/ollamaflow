"""OpenAI API models for OllamaFlow SDK."""

from typing import Any, Optional

from pydantic import BaseModel, Field


class OpenAIChatMessage(BaseModel):
    """A message in an OpenAI chat conversation."""

    role: str
    """Role of the message sender (system, user, assistant, tool)."""

    content: Optional[str] = Field(default=None)
    """Content of the message."""

    name: Optional[str] = Field(default=None)
    """Name of the author of this message."""

    tool_calls: Optional[list[dict[str, Any]]] = Field(default=None)
    """Tool calls made by the assistant."""

    tool_call_id: Optional[str] = Field(default=None)
    """Tool call ID for tool responses."""


class OpenAIResponseFormat(BaseModel):
    """Response format specification for OpenAI requests."""

    type: str = Field(default="text")
    """Response format type ('text' or 'json_object')."""


class OpenAIGenerateCompletionRequest(BaseModel):
    """Request for OpenAI text completion."""

    model: str
    """Model ID to use for completion."""

    prompt: str | list[str]
    """The prompt(s) to generate completions for."""

    max_tokens: Optional[int] = Field(default=None, gt=0)
    """Maximum number of tokens to generate."""

    temperature: Optional[float] = Field(default=None, ge=0.0, le=2.0)
    """Sampling temperature."""

    top_p: Optional[float] = Field(default=None, ge=0.0, le=1.0)
    """Nucleus sampling parameter."""

    n: Optional[int] = Field(default=None, gt=0)
    """Number of completions to generate."""

    stream: Optional[bool] = Field(default=None)
    """Whether to stream partial progress."""

    stop: Optional[str | list[str]] = Field(default=None)
    """Sequences where the API will stop generating."""

    presence_penalty: Optional[float] = Field(default=None, ge=-2.0, le=2.0)
    """Presence penalty."""

    frequency_penalty: Optional[float] = Field(default=None, ge=-2.0, le=2.0)
    """Frequency penalty."""

    logit_bias: Optional[dict[str, float]] = Field(default=None)
    """Token logit bias."""

    logprobs: Optional[int] = Field(default=None, ge=0, le=5)
    """Include log probabilities."""

    echo: Optional[bool] = Field(default=None)
    """Echo back the prompt."""

    suffix: Optional[str] = Field(default=None)
    """Suffix to append to completions."""

    user: Optional[str] = Field(default=None)
    """Unique user identifier."""

    seed: Optional[int] = Field(default=None)
    """Random seed for deterministic generation."""


class OpenAIUsage(BaseModel):
    """Token usage information from OpenAI requests."""

    prompt_tokens: Optional[int] = Field(default=None)
    """Number of tokens in the prompt."""

    completion_tokens: Optional[int] = Field(default=None)
    """Number of tokens in the completion."""

    total_tokens: Optional[int] = Field(default=None)
    """Total number of tokens used."""


class OpenAICompletionChoice(BaseModel):
    """A completion choice from OpenAI."""

    text: Optional[str] = Field(default=None)
    """Generated text."""

    index: Optional[int] = Field(default=None)
    """Index of this choice."""

    logprobs: Optional[dict[str, Any]] = Field(default=None)
    """Log probabilities."""

    finish_reason: Optional[str] = Field(default=None)
    """Reason for completion finish."""


class OpenAIGenerateCompletionResult(BaseModel):
    """Result from an OpenAI text completion request."""

    id: Optional[str] = Field(default=None)
    """Unique completion ID."""

    object: Optional[str] = Field(default=None)
    """Object type."""

    created: Optional[int] = Field(default=None)
    """Creation timestamp."""

    model: Optional[str] = Field(default=None)
    """Model used."""

    choices: Optional[list[OpenAICompletionChoice]] = Field(default=None)
    """Completion choices."""

    usage: Optional[OpenAIUsage] = Field(default=None)
    """Token usage."""

    system_fingerprint: Optional[str] = Field(default=None)
    """System fingerprint."""


class OpenAIGenerateChatCompletionRequest(BaseModel):
    """Request for OpenAI chat completion."""

    model: str
    """Model ID to use for chat completion."""

    messages: list[OpenAIChatMessage]
    """Messages in the conversation."""

    max_tokens: Optional[int] = Field(default=None, gt=0)
    """Maximum number of tokens to generate."""

    temperature: Optional[float] = Field(default=None, ge=0.0, le=2.0)
    """Sampling temperature."""

    top_p: Optional[float] = Field(default=None, ge=0.0, le=1.0)
    """Nucleus sampling parameter."""

    n: Optional[int] = Field(default=None, gt=0)
    """Number of completions to generate."""

    stream: Optional[bool] = Field(default=None)
    """Whether to stream partial progress."""

    stop: Optional[str | list[str]] = Field(default=None)
    """Sequences where the API will stop generating."""

    presence_penalty: Optional[float] = Field(default=None, ge=-2.0, le=2.0)
    """Presence penalty."""

    frequency_penalty: Optional[float] = Field(default=None, ge=-2.0, le=2.0)
    """Frequency penalty."""

    logit_bias: Optional[dict[str, float]] = Field(default=None)
    """Token logit bias."""

    logprobs: Optional[bool] = Field(default=None)
    """Include log probabilities."""

    top_logprobs: Optional[int] = Field(default=None, ge=0, le=20)
    """Number of most likely tokens to return."""

    user: Optional[str] = Field(default=None)
    """Unique user identifier."""

    response_format: Optional[OpenAIResponseFormat] = Field(default=None)
    """Response format specification."""

    tools: Optional[list[dict[str, Any]]] = Field(default=None)
    """Tools/functions available for the model."""

    tool_choice: Optional[str | dict[str, Any]] = Field(default=None)
    """Controls which tool is called."""

    parallel_tool_calls: Optional[bool] = Field(default=None)
    """Enable parallel function calling."""

    seed: Optional[int] = Field(default=None)
    """Random seed for deterministic generation."""


class OpenAIChatChoice(BaseModel):
    """A chat completion choice from OpenAI."""

    index: Optional[int] = Field(default=None)
    """Index of this choice."""

    message: Optional[OpenAIChatMessage] = Field(default=None)
    """Generated message."""

    delta: Optional[OpenAIChatMessage] = Field(default=None)
    """Delta for streaming responses."""

    logprobs: Optional[dict[str, Any]] = Field(default=None)
    """Log probabilities."""

    finish_reason: Optional[str] = Field(default=None)
    """Reason for completion finish."""


class OpenAIGenerateChatCompletionResult(BaseModel):
    """Result from an OpenAI chat completion request."""

    id: Optional[str] = Field(default=None)
    """Unique completion ID."""

    object: Optional[str] = Field(default=None)
    """Object type."""

    created: Optional[int] = Field(default=None)
    """Creation timestamp."""

    model: Optional[str] = Field(default=None)
    """Model used."""

    choices: Optional[list[OpenAIChatChoice]] = Field(default=None)
    """Completion choices."""

    usage: Optional[OpenAIUsage] = Field(default=None)
    """Token usage."""

    system_fingerprint: Optional[str] = Field(default=None)
    """System fingerprint."""


class OpenAIGenerateEmbeddingsRequest(BaseModel):
    """Request for OpenAI embeddings generation."""

    model: str
    """Model ID to use for embeddings."""

    input: str | list[str]
    """Text or list of texts to generate embeddings for."""

    encoding_format: Optional[str] = Field(default=None)
    """Encoding format ('float' or 'base64')."""

    dimensions: Optional[int] = Field(default=None, gt=0)
    """Number of dimensions for the embeddings."""

    user: Optional[str] = Field(default=None)
    """Unique user identifier."""


class OpenAIEmbedding(BaseModel):
    """An embedding from OpenAI."""

    object: Optional[str] = Field(default=None)
    """Object type."""

    index: Optional[int] = Field(default=None)
    """Index of this embedding."""

    embedding: Optional[list[float]] = Field(default=None)
    """Embedding vector."""


class OpenAIGenerateEmbeddingsResult(BaseModel):
    """Result from an OpenAI embeddings request."""

    object: Optional[str] = Field(default=None)
    """Object type."""

    model: Optional[str] = Field(default=None)
    """Model used."""

    data: Optional[list[OpenAIEmbedding]] = Field(default=None)
    """Embedding data."""

    usage: Optional[OpenAIUsage] = Field(default=None)
    """Token usage."""
