"""Tests for Ollama API methods."""

import pytest
from pytest_httpx import HTTPXMock

from ollamaflow_sdk import OllamaFlowSdk
from ollamaflow_sdk.models.ollama import (
    OllamaChatMessage,
    OllamaGenerateChatCompletionRequest,
    OllamaGenerateCompletionRequest,
    OllamaGenerateEmbeddingsRequest,
    OllamaShowModelInfoRequest,
)


class TestOllamaMethods:
    """Tests for Ollama API methods."""

    @pytest.mark.asyncio
    async def test_list_local_models(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
        mock_models_response: dict,
    ) -> None:
        """Test listing local models."""
        httpx_mock.add_response(
            url="http://localhost:43411/api/tags",
            json=mock_models_response,
        )

        models = await sdk.ollama.list_local_models()

        assert len(models) == 2
        assert models[0].name == "llama2:latest"
        assert models[1].name == "mistral:latest"

    @pytest.mark.asyncio
    async def test_list_local_models_empty(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
    ) -> None:
        """Test listing local models when none exist."""
        httpx_mock.add_response(
            url="http://localhost:43411/api/tags",
            json={"models": []},
        )

        models = await sdk.ollama.list_local_models()
        assert len(models) == 0

    @pytest.mark.asyncio
    async def test_list_running_models(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
    ) -> None:
        """Test listing running models."""
        httpx_mock.add_response(
            url="http://localhost:43411/api/ps",
            json={
                "models": [
                    {
                        "name": "llama2:latest",
                        "model": "llama2:latest",
                        "size": 3825819519,
                    }
                ]
            },
        )

        models = await sdk.ollama.list_running_models()

        assert len(models) == 1
        assert models[0].name == "llama2:latest"

    @pytest.mark.asyncio
    async def test_show_model_info(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
    ) -> None:
        """Test showing model info."""
        httpx_mock.add_response(
            url="http://localhost:43411/api/show",
            json={
                "modelfile": "FROM llama2",
                "parameters": "temperature 0.7",
                "template": "{{ .Prompt }}",
                "details": {
                    "family": "llama",
                    "parameter_size": "7B",
                },
            },
        )

        request = OllamaShowModelInfoRequest(name="llama2:latest")
        result = await sdk.ollama.show_model_info(request)

        assert result is not None
        assert result.modelfile == "FROM llama2"
        assert result.details is not None
        assert result.details.family == "llama"

    @pytest.mark.asyncio
    async def test_generate_embeddings(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
        mock_embeddings_response: dict,
    ) -> None:
        """Test generating embeddings."""
        httpx_mock.add_response(
            url="http://localhost:43411/api/embed",
            json=mock_embeddings_response,
        )

        request = OllamaGenerateEmbeddingsRequest(
            model="all-minilm",
            input="Hello, world!",
        )
        result = await sdk.ollama.generate_embeddings(request)

        assert result is not None
        assert result.model == "all-minilm:latest"
        assert result.embeddings is not None
        assert len(result.embeddings) == 1
        assert len(result.embeddings[0]) == 5

    @pytest.mark.asyncio
    async def test_generate_completion(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
        mock_completion_response: dict,
    ) -> None:
        """Test generating completion."""
        httpx_mock.add_response(
            url="http://localhost:43411/api/generate",
            json=mock_completion_response,
        )

        request = OllamaGenerateCompletionRequest(
            model="llama2",
            prompt="The meaning of life is",
        )
        result = await sdk.ollama.generate_completion(request)

        assert result is not None
        assert result.model == "llama2:latest"
        assert result.response is not None
        assert result.done is True

    @pytest.mark.asyncio
    async def test_generate_chat_completion(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
        mock_chat_completion_response: dict,
    ) -> None:
        """Test generating chat completion."""
        httpx_mock.add_response(
            url="http://localhost:43411/api/chat",
            json=mock_chat_completion_response,
        )

        request = OllamaGenerateChatCompletionRequest(
            model="llama2",
            messages=[
                OllamaChatMessage(role="user", content="Hello!"),
            ],
        )
        result = await sdk.ollama.generate_chat_completion(request)

        assert result is not None
        assert result.model == "llama2:latest"
        assert result.message is not None
        assert result.message.role == "assistant"
        assert result.done is True

    @pytest.mark.asyncio
    async def test_generate_completion_failure(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
    ) -> None:
        """Test handling completion failure."""
        httpx_mock.add_response(
            url="http://localhost:43411/api/generate",
            status_code=500,
        )

        request = OllamaGenerateCompletionRequest(
            model="llama2",
            prompt="Hello",
        )
        result = await sdk.ollama.generate_completion(request)

        assert result is None
