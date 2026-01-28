"""Ollama API methods for OllamaFlow SDK."""

from __future__ import annotations

import json
from typing import TYPE_CHECKING, Any, AsyncIterator, Optional

import httpx

from ollamaflow_sdk.models.ollama import (
    OllamaDeleteModelRequest,
    OllamaGenerateChatCompletionRequest,
    OllamaGenerateChatCompletionResult,
    OllamaGenerateCompletionRequest,
    OllamaGenerateCompletionResult,
    OllamaGenerateEmbeddingsRequest,
    OllamaGenerateEmbeddingsResult,
    OllamaLocalModel,
    OllamaPullModelRequest,
    OllamaPullModelResult,
    OllamaRunningModel,
    OllamaShowModelInfoRequest,
    OllamaShowModelInfoResult,
)

if TYPE_CHECKING:
    from ollamaflow_sdk.client import OllamaFlowSdk


class OllamaMethods:
    """Ollama API methods for interacting with OllamaFlow."""

    def __init__(self, sdk: "OllamaFlowSdk") -> None:
        """
        Initialize Ollama API methods.

        Args:
            sdk: Parent OllamaFlowSdk instance.
        """
        self._sdk = sdk

    async def list_local_models(self) -> list[OllamaLocalModel]:
        """
        List all locally available models.

        Returns:
            List of local models.
        """
        url = f"{self._sdk.endpoint}/api/tags"
        response = await self._sdk._get_async(url)

        if response is None:
            return []

        # Handle both array and object responses
        if isinstance(response, list):
            models_data = response
        elif isinstance(response, dict) and "models" in response:
            models_data = response["models"]
        else:
            return []

        return [OllamaLocalModel(**model) for model in models_data]

    async def list_running_models(self) -> list[OllamaRunningModel]:
        """
        List all currently running models.

        Returns:
            List of running models.
        """
        url = f"{self._sdk.endpoint}/api/ps"
        response = await self._sdk._get_async(url)

        if response is None:
            return []

        # Handle both array and object responses
        if isinstance(response, list):
            models_data = response
        elif isinstance(response, dict) and "models" in response:
            models_data = response["models"]
        else:
            return []

        return [OllamaRunningModel(**model) for model in models_data]

    async def show_model_info(
        self,
        request: OllamaShowModelInfoRequest,
    ) -> Optional[OllamaShowModelInfoResult]:
        """
        Show detailed information about a specific model.

        Args:
            request: Model info request.

        Returns:
            Model information, or None on failure.
        """
        url = f"{self._sdk.endpoint}/api/show"
        response = await self._sdk._post_async(url, request.model_dump(exclude_none=True))

        if response is None:
            return None

        return OllamaShowModelInfoResult(**response)

    async def generate_embeddings(
        self,
        request: OllamaGenerateEmbeddingsRequest,
    ) -> Optional[OllamaGenerateEmbeddingsResult]:
        """
        Generate embeddings for the provided input text.

        Args:
            request: Embeddings request.

        Returns:
            Embeddings result, or None on failure.
        """
        url = f"{self._sdk.endpoint}/api/embed"
        response = await self._sdk._post_async(url, request.model_dump(exclude_none=True))

        if response is None:
            return None

        return OllamaGenerateEmbeddingsResult(**response)

    async def generate_completion(
        self,
        request: OllamaGenerateCompletionRequest,
    ) -> Optional[OllamaGenerateCompletionResult]:
        """
        Generate a text completion (non-streaming).

        Args:
            request: Completion request.

        Returns:
            Completion result, or None on failure.
        """
        # Force non-streaming
        request_data = request.model_dump(exclude_none=True)
        request_data["stream"] = False

        url = f"{self._sdk.endpoint}/api/generate"
        response = await self._sdk._post_async(url, request_data)

        if response is None:
            return None

        return OllamaGenerateCompletionResult(**response)

    async def generate_completion_stream(
        self,
        request: OllamaGenerateCompletionRequest,
    ) -> AsyncIterator[OllamaGenerateCompletionResult]:
        """
        Generate a text completion with streaming.

        Args:
            request: Completion request.

        Yields:
            Completion chunks as they are generated.
        """
        # Force streaming
        request_data = request.model_dump(exclude_none=True)
        request_data["stream"] = True

        url = f"{self._sdk.endpoint}/api/generate"

        if self._sdk.log_requests:
            self._sdk.log("DEBUG", f"POST streaming request to {url}")

        async with httpx.AsyncClient(timeout=self._sdk._get_timeout()) as client:
            async with client.stream(
                "POST",
                url,
                json=request_data,
                headers=self._sdk._get_headers(),
            ) as response:
                if 200 <= response.status_code < 300:
                    async for line in response.aiter_lines():
                        if line.strip():
                            try:
                                chunk_data = json.loads(line)
                                yield OllamaGenerateCompletionResult(**chunk_data)
                                if chunk_data.get("done", False):
                                    break
                            except json.JSONDecodeError:
                                continue

    async def generate_chat_completion(
        self,
        request: OllamaGenerateChatCompletionRequest,
    ) -> Optional[OllamaGenerateChatCompletionResult]:
        """
        Generate a chat completion (non-streaming).

        Args:
            request: Chat completion request.

        Returns:
            Chat completion result, or None on failure.
        """
        # Force non-streaming
        request_data = request.model_dump(exclude_none=True)
        request_data["stream"] = False

        # Convert messages to dict format
        if "messages" in request_data:
            request_data["messages"] = [
                msg if isinstance(msg, dict) else msg
                for msg in request_data["messages"]
            ]

        url = f"{self._sdk.endpoint}/api/chat"
        response = await self._sdk._post_async(url, request_data)

        if response is None:
            return None

        return OllamaGenerateChatCompletionResult(**response)

    async def generate_chat_completion_stream(
        self,
        request: OllamaGenerateChatCompletionRequest,
    ) -> AsyncIterator[OllamaGenerateChatCompletionResult]:
        """
        Generate a chat completion with streaming.

        Args:
            request: Chat completion request.

        Yields:
            Chat completion chunks as they are generated.
        """
        # Force streaming
        request_data = request.model_dump(exclude_none=True)
        request_data["stream"] = True

        url = f"{self._sdk.endpoint}/api/chat"

        if self._sdk.log_requests:
            self._sdk.log("DEBUG", f"POST streaming request to {url}")

        async with httpx.AsyncClient(timeout=self._sdk._get_timeout()) as client:
            async with client.stream(
                "POST",
                url,
                json=request_data,
                headers=self._sdk._get_headers(),
            ) as response:
                if 200 <= response.status_code < 300:
                    async for line in response.aiter_lines():
                        if line.strip():
                            try:
                                chunk_data = json.loads(line)
                                yield OllamaGenerateChatCompletionResult(**chunk_data)
                                if chunk_data.get("done", False):
                                    break
                            except json.JSONDecodeError:
                                continue

    async def pull_model(
        self,
        request: OllamaPullModelRequest,
    ) -> AsyncIterator[OllamaPullModelResult]:
        """
        Pull a model from the Ollama registry.

        Args:
            request: Pull model request.

        Yields:
            Progress updates as the model is pulled.
        """
        url = f"{self._sdk.endpoint}/api/pull"

        if self._sdk.log_requests:
            self._sdk.log("DEBUG", f"POST streaming request to {url}")

        async with httpx.AsyncClient(timeout=self._sdk._get_timeout()) as client:
            async with client.stream(
                "POST",
                url,
                json=request.model_dump(exclude_none=True),
                headers=self._sdk._get_headers(),
            ) as response:
                if 200 <= response.status_code < 300:
                    async for line in response.aiter_lines():
                        if line.strip():
                            try:
                                chunk_data = json.loads(line)
                                result = OllamaPullModelResult(**chunk_data)
                                yield result
                                if result.status == "success":
                                    break
                            except json.JSONDecodeError:
                                continue

    async def delete_model(
        self,
        request: OllamaDeleteModelRequest,
    ) -> bool:
        """
        Delete a model from the local Ollama installation.

        Args:
            request: Delete model request.

        Returns:
            True if successful, False otherwise.
        """
        url = f"{self._sdk.endpoint}/api/delete"

        if self._sdk.log_requests:
            self._sdk.log("DEBUG", f"DELETE request to {url}")

        async with httpx.AsyncClient(timeout=self._sdk._get_timeout()) as client:
            try:
                response = await client.request(
                    "DELETE",
                    url,
                    json=request.model_dump(exclude_none=True),
                    headers=self._sdk._get_headers(),
                )
                return 200 <= response.status_code < 300
            except Exception as e:
                self._sdk.log("ERROR", f"Delete model failed: {e}")
                return False
