"""Tests for OllamaFlow SDK models."""

import pytest

from ollamaflow_sdk.models.backend import ApiFormat, Backend, BackendHealth
from ollamaflow_sdk.models.frontend import Frontend, LoadBalancingMode
from ollamaflow_sdk.models.ollama import (
    OllamaChatMessage,
    OllamaCompletionOptions,
    OllamaGenerateChatCompletionRequest,
    OllamaGenerateCompletionRequest,
    OllamaGenerateEmbeddingsRequest,
    OllamaLocalModel,
    OllamaModelDetails,
)
from ollamaflow_sdk.models.openai import (
    OpenAIChatMessage,
    OpenAIGenerateChatCompletionRequest,
    OpenAIGenerateEmbeddingsRequest,
)


class TestBackendModel:
    """Tests for Backend model."""

    def test_default_values(self) -> None:
        """Test default values for Backend model."""
        backend = Backend(hostname="localhost")
        assert backend.hostname == "localhost"
        assert backend.port == 11434
        assert backend.ssl is False
        assert backend.api_format == ApiFormat.OLLAMA
        assert backend.allow_embeddings is True
        assert backend.allow_completions is True

    def test_from_api_response(self, mock_backend_response: dict) -> None:
        """Test creating Backend from API response."""
        backend = Backend(**mock_backend_response)
        assert backend.identifier == "backend-1"
        assert backend.name == "Test Backend"
        assert backend.hostname == "localhost"
        assert backend.port == 11434
        assert backend.labels == ["test"]

    def test_serialization_by_alias(self) -> None:
        """Test that model serializes with aliases."""
        backend = Backend(
            identifier="test-backend",
            hostname="localhost",
            port=11434,
        )
        data = backend.model_dump(by_alias=True)
        assert "Identifier" in data
        assert "Hostname" in data
        assert "Port" in data


class TestFrontendModel:
    """Tests for Frontend model."""

    def test_default_values(self) -> None:
        """Test default values for Frontend model."""
        frontend = Frontend()
        assert frontend.hostname == "*"
        assert frontend.timeout_ms == 60000
        assert frontend.load_balancing == LoadBalancingMode.ROUND_ROBIN
        assert frontend.allow_embeddings is True

    def test_from_api_response(self, mock_frontend_response: dict) -> None:
        """Test creating Frontend from API response."""
        frontend = Frontend(**mock_frontend_response)
        assert frontend.identifier == "frontend-1"
        assert frontend.name == "Test Frontend"
        assert frontend.backends == ["backend-1"]
        assert frontend.required_models == ["llama2:latest"]


class TestOllamaModels:
    """Tests for Ollama API models."""

    def test_chat_message(self) -> None:
        """Test OllamaChatMessage model."""
        message = OllamaChatMessage(
            role="user",
            content="Hello, world!",
        )
        assert message.role == "user"
        assert message.content == "Hello, world!"

    def test_completion_options_validation(self) -> None:
        """Test OllamaCompletionOptions validation."""
        # Valid options
        options = OllamaCompletionOptions(
            temperature=0.7,
            top_p=0.9,
            top_k=40,
        )
        assert options.temperature == 0.7

        # Invalid temperature
        with pytest.raises(ValueError):
            OllamaCompletionOptions(temperature=3.0)

    def test_generate_completion_request(self) -> None:
        """Test OllamaGenerateCompletionRequest model."""
        request = OllamaGenerateCompletionRequest(
            model="llama2",
            prompt="Hello, world!",
            options=OllamaCompletionOptions(temperature=0.7),
        )
        assert request.model == "llama2"
        assert request.prompt == "Hello, world!"
        assert request.options is not None
        assert request.options.temperature == 0.7

    def test_generate_chat_completion_request(self) -> None:
        """Test OllamaGenerateChatCompletionRequest model."""
        request = OllamaGenerateChatCompletionRequest(
            model="llama2",
            messages=[
                OllamaChatMessage(role="user", content="Hello!"),
            ],
        )
        assert request.model == "llama2"
        assert len(request.messages) == 1

    def test_generate_embeddings_request(self) -> None:
        """Test OllamaGenerateEmbeddingsRequest model."""
        request = OllamaGenerateEmbeddingsRequest(
            model="all-minilm",
            input="Hello, world!",
        )
        assert request.model == "all-minilm"
        assert request.input == "Hello, world!"

    def test_local_model(self) -> None:
        """Test OllamaLocalModel model."""
        model = OllamaLocalModel(
            name="llama2:latest",
            model="llama2:latest",
            size=3825819519,
            details=OllamaModelDetails(
                family="llama",
                parameter_size="7B",
            ),
        )
        assert model.name == "llama2:latest"
        assert model.details is not None
        assert model.details.family == "llama"


class TestOpenAIModels:
    """Tests for OpenAI API models."""

    def test_chat_message(self) -> None:
        """Test OpenAIChatMessage model."""
        message = OpenAIChatMessage(
            role="user",
            content="Hello, world!",
        )
        assert message.role == "user"
        assert message.content == "Hello, world!"

    def test_generate_chat_completion_request(self) -> None:
        """Test OpenAIGenerateChatCompletionRequest model."""
        request = OpenAIGenerateChatCompletionRequest(
            model="gpt-3.5-turbo",
            messages=[
                OpenAIChatMessage(role="user", content="Hello!"),
            ],
            temperature=0.7,
            max_tokens=100,
        )
        assert request.model == "gpt-3.5-turbo"
        assert len(request.messages) == 1
        assert request.temperature == 0.7
        assert request.max_tokens == 100

    def test_generate_embeddings_request(self) -> None:
        """Test OpenAIGenerateEmbeddingsRequest model."""
        request = OpenAIGenerateEmbeddingsRequest(
            model="text-embedding-ada-002",
            input="Hello, world!",
        )
        assert request.model == "text-embedding-ada-002"
        assert request.input == "Hello, world!"

    def test_temperature_validation(self) -> None:
        """Test temperature validation in OpenAI request."""
        with pytest.raises(ValueError):
            OpenAIGenerateChatCompletionRequest(
                model="gpt-3.5-turbo",
                messages=[OpenAIChatMessage(role="user", content="Hi")],
                temperature=3.0,  # Invalid: must be 0-2
            )
