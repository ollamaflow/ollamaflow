"""Frontend models for OllamaFlow SDK."""

from datetime import datetime
from enum import Enum
from typing import Any, Optional

from pydantic import BaseModel, Field


class LoadBalancingMode(str, Enum):
    """Load balancing mode for frontend request distribution."""

    ROUND_ROBIN = "RoundRobin"
    RANDOM = "Random"


class Frontend(BaseModel):
    """Represents a frontend virtual endpoint in OllamaFlow."""

    identifier: Optional[str] = Field(default=None, alias="Identifier")
    """Unique identifier for this frontend."""

    name: Optional[str] = Field(default=None, alias="Name")
    """Human-readable name for this frontend."""

    hostname: str = Field(default="*", alias="Hostname")
    """Hostname pattern for this frontend. Use '*' for catch-all."""

    timeout_ms: int = Field(default=60000, alias="TimeoutMs", ge=0)
    """Request timeout in milliseconds."""

    load_balancing: LoadBalancingMode = Field(
        default=LoadBalancingMode.ROUND_ROBIN, alias="LoadBalancing"
    )
    """Load balancing mode for distributing requests."""

    block_http10: bool = Field(default=True, alias="BlockHttp10")
    """Whether to block HTTP/1.0 requests."""

    max_request_body_size: int = Field(
        default=536870912, alias="MaxRequestBodySize", ge=1  # 512MB
    )
    """Maximum request body size in bytes."""

    backends: list[str] = Field(default_factory=list, alias="Backends")
    """List of backend identifiers mapped to this frontend."""

    required_models: list[str] = Field(default_factory=list, alias="RequiredModels")
    """List of models that should be available on all backends."""

    log_request_full: bool = Field(default=False, alias="LogRequestFull")
    """Whether to log full request details."""

    log_request_body: bool = Field(default=False, alias="LogRequestBody")
    """Whether to log request bodies."""

    log_response_body: bool = Field(default=False, alias="LogResponseBody")
    """Whether to log response bodies."""

    use_sticky_sessions: bool = Field(default=False, alias="UseStickySessions")
    """Whether to use sticky sessions for this frontend."""

    sticky_session_expiration_ms: int = Field(
        default=1800000, alias="StickySessionExpirationMs", ge=10000, le=86400000
    )
    """Sticky session expiration time in milliseconds."""

    pinned_embeddings_properties: dict[str, Any] = Field(
        default_factory=dict, alias="PinnedEmbeddingsProperties"
    )
    """Properties to apply to all embeddings requests."""

    pinned_completions_properties: dict[str, Any] = Field(
        default_factory=dict, alias="PinnedCompletionsProperties"
    )
    """Properties to apply to all completions requests."""

    allow_embeddings: bool = Field(default=True, alias="AllowEmbeddings")
    """Whether embeddings requests are allowed."""

    allow_completions: bool = Field(default=True, alias="AllowCompletions")
    """Whether completions requests are allowed."""

    allow_retries: bool = Field(default=True, alias="AllowRetries")
    """Whether to allow automatic retries on failure."""

    active: bool = Field(default=True, alias="Active")
    """Whether this frontend is active."""

    created_utc: Optional[datetime] = Field(default=None, alias="CreatedUtc")
    """Timestamp when this frontend was created."""

    last_update_utc: Optional[datetime] = Field(default=None, alias="LastUpdateUtc")
    """Timestamp when this frontend was last updated."""

    model_config = {"populate_by_name": True}
