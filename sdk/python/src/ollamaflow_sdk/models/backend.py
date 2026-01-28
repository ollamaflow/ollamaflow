"""Backend models for OllamaFlow SDK."""

from datetime import datetime
from enum import Enum
from typing import Any, Optional

from pydantic import BaseModel, Field


class ApiFormat(str, Enum):
    """API format supported by a backend."""

    OLLAMA = "Ollama"
    OPENAI = "OpenAI"


class Backend(BaseModel):
    """Represents a backend AI inference instance in OllamaFlow."""

    identifier: Optional[str] = Field(default=None, alias="Identifier")
    """Unique identifier for this backend."""

    name: Optional[str] = Field(default=None, alias="Name")
    """Human-readable name for this backend."""

    hostname: str = Field(default="localhost", alias="Hostname")
    """Hostname or IP address of the backend."""

    port: int = Field(default=11434, alias="Port", ge=0, le=65535)
    """TCP port of the backend."""

    ssl: bool = Field(default=False, alias="Ssl")
    """Whether to use SSL/TLS for connections."""

    unhealthy_threshold: int = Field(default=2, alias="UnhealthyThreshold", ge=1)
    """Number of consecutive failed health checks before marking as unhealthy."""

    healthy_threshold: int = Field(default=2, alias="HealthyThreshold", ge=1)
    """Number of consecutive successful health checks before marking as healthy."""

    health_check_method: str = Field(default="GET", alias="HealthCheckMethod")
    """HTTP method to use for health checks (GET or HEAD)."""

    health_check_url: str = Field(default="/", alias="HealthCheckUrl")
    """URL path to use for health checks."""

    max_parallel_requests: int = Field(default=4, alias="MaxParallelRequests", ge=1)
    """Maximum number of parallel requests allowed to this backend."""

    rate_limit_requests_threshold: int = Field(
        default=10, alias="RateLimitRequestsThreshold", ge=1
    )
    """Threshold at which rate limiting begins."""

    log_request_full: bool = Field(default=False, alias="LogRequestFull")
    """Whether to log full request details."""

    log_request_body: bool = Field(default=False, alias="LogRequestBody")
    """Whether to log request bodies."""

    log_response_body: bool = Field(default=False, alias="LogResponseBody")
    """Whether to log response bodies."""

    api_format: ApiFormat = Field(default=ApiFormat.OLLAMA, alias="ApiFormat")
    """API format supported by this backend."""

    labels: list[str] = Field(default_factory=list, alias="Labels")
    """Labels for backend selection in load balancing."""

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

    bearer_token: Optional[str] = Field(default=None, alias="BearerToken")
    """Bearer token for authentication with this backend."""

    querystring: Optional[str] = Field(default=None, alias="Querystring")
    """Query string to append to backend URLs."""

    headers: dict[str, str] = Field(default_factory=dict, alias="Headers")
    """Custom headers to include in requests to this backend."""

    active: bool = Field(default=True, alias="Active")
    """Whether this backend is active."""

    created_utc: Optional[datetime] = Field(default=None, alias="CreatedUtc")
    """Timestamp when this backend was created."""

    last_update_utc: Optional[datetime] = Field(default=None, alias="LastUpdateUtc")
    """Timestamp when this backend was last updated."""

    active_requests: int = Field(default=0, alias="ActiveRequests", ge=0)
    """Number of currently active requests."""

    is_sticky: bool = Field(default=False, alias="IsSticky")
    """Whether this backend was selected due to session stickiness."""

    model_config = {"populate_by_name": True}


class BackendHealth(Backend):
    """Backend with additional health status information."""

    healthy_since_utc: Optional[datetime] = Field(default=None, alias="HealthySinceUtc")
    """Timestamp when this backend was last seen as healthy."""

    unhealthy_since_utc: Optional[datetime] = Field(default=None, alias="UnhealthySinceUtc")
    """Timestamp when this backend was last seen as unhealthy."""

    uptime: Optional[str] = Field(default=None, alias="Uptime")
    """Duration the backend has been healthy."""

    downtime: Optional[str] = Field(default=None, alias="Downtime")
    """Duration the backend has been unhealthy."""
