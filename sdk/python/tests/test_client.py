"""Tests for OllamaFlow SDK client."""

import pytest

from ollamaflow_sdk import OllamaFlowSdk


class TestOllamaFlowSdk:
    """Tests for OllamaFlowSdk class."""

    def test_init_with_endpoint(self) -> None:
        """Test SDK initialization with endpoint."""
        sdk = OllamaFlowSdk("http://localhost:43411")
        assert sdk.endpoint == "http://localhost:43411"
        assert sdk.bearer_token is None
        assert sdk.timeout_ms == 300000

    def test_init_with_bearer_token(self) -> None:
        """Test SDK initialization with bearer token."""
        sdk = OllamaFlowSdk(
            "http://localhost:43411",
            bearer_token="test-token",
        )
        assert sdk.bearer_token == "test-token"

    def test_init_with_custom_timeout(self) -> None:
        """Test SDK initialization with custom timeout."""
        sdk = OllamaFlowSdk(
            "http://localhost:43411",
            timeout_ms=60000,
        )
        assert sdk.timeout_ms == 60000

    def test_init_strips_trailing_slash(self) -> None:
        """Test that trailing slash is stripped from endpoint."""
        sdk = OllamaFlowSdk("http://localhost:43411/")
        assert sdk.endpoint == "http://localhost:43411"

    def test_init_requires_endpoint(self) -> None:
        """Test that endpoint is required."""
        with pytest.raises(ValueError, match="endpoint is required"):
            OllamaFlowSdk("")

    def test_timeout_setter_validates(self) -> None:
        """Test that timeout setter validates negative values."""
        sdk = OllamaFlowSdk("http://localhost:43411")
        with pytest.raises(ValueError, match="timeout_ms must be non-negative"):
            sdk.timeout_ms = -1

    def test_logging_properties(self) -> None:
        """Test logging property setters and getters."""
        sdk = OllamaFlowSdk("http://localhost:43411")

        assert sdk.log_requests is False
        assert sdk.log_responses is False
        assert sdk.logger is None

        sdk.log_requests = True
        sdk.log_responses = True
        sdk.logger = lambda level, msg: print(f"[{level}] {msg}")

        assert sdk.log_requests is True
        assert sdk.log_responses is True
        assert sdk.logger is not None

    def test_api_method_accessors(self) -> None:
        """Test that API method accessors are available."""
        sdk = OllamaFlowSdk("http://localhost:43411")

        assert sdk.ollama is not None
        assert sdk.openai is not None
        assert sdk.backend is not None
        assert sdk.frontend is not None

    def test_get_headers_without_token(self) -> None:
        """Test headers without bearer token."""
        sdk = OllamaFlowSdk("http://localhost:43411")
        headers = sdk._get_headers()
        assert "Content-Type" in headers
        assert headers["Content-Type"] == "application/json"
        assert "Authorization" not in headers

    def test_get_headers_with_token(self) -> None:
        """Test headers with bearer token."""
        sdk = OllamaFlowSdk(
            "http://localhost:43411",
            bearer_token="test-token",
        )
        headers = sdk._get_headers()
        assert "Authorization" in headers
        assert headers["Authorization"] == "Bearer test-token"
