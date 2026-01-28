"""OpenAI-compatible API methods for OllamaFlow SDK."""

from __future__ import annotations

import json
from typing import TYPE_CHECKING, AsyncIterator, Optional

import httpx

from ollamaflow_sdk.models.openai import (
    OpenAIGenerateChatCompletionRequest,
    OpenAIGenerateChatCompletionResult,
    OpenAIGenerateCompletionRequest,
    OpenAIGenerateCompletionResult,
    OpenAIGenerateEmbeddingsRequest,
    OpenAIGenerateEmbeddingsResult,
)

if TYPE_CHECKING:
    from ollamaflow_sdk.client import OllamaFlowSdk


class OpenAIMethods:
    """OpenAI-compatible API methods for interacting with OllamaFlow."""

    def __init__(self, sdk: "OllamaFlowSdk") -> None:
        """
        Initialize OpenAI API methods.

        Args:
            sdk: Parent OllamaFlowSdk instance.
        """
        self._sdk = sdk

    async def generate_completion(
        self,
        request: OpenAIGenerateCompletionRequest,
    ) -> Optional[OpenAIGenerateCompletionResult]:
        """
        Generate a text completion using OpenAI-compatible API (non-streaming).

        Args:
            request: Completion request.

        Returns:
            Completion result, or None on failure.
        """
        # Force non-streaming
        request_data = request.model_dump(exclude_none=True)
        request_data["stream"] = False

        url = f"{self._sdk.endpoint}/v1/completions"
        response = await self._sdk._post_async(url, request_data)

        if response is None:
            return None

        return OpenAIGenerateCompletionResult(**response)

    async def generate_completion_stream(
        self,
        request: OpenAIGenerateCompletionRequest,
    ) -> AsyncIterator[OpenAIGenerateCompletionResult]:
        """
        Generate a text completion with streaming using OpenAI-compatible API.

        Args:
            request: Completion request.

        Yields:
            Completion chunks as they are generated.
        """
        # Force streaming
        request_data = request.model_dump(exclude_none=True)
        request_data["stream"] = True

        url = f"{self._sdk.endpoint}/v1/completions"

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
                        line = line.strip()
                        if line.startswith("data: "):
                            data_str = line[6:]
                            if data_str == "[DONE]":
                                break
                            try:
                                chunk_data = json.loads(data_str)
                                yield OpenAIGenerateCompletionResult(**chunk_data)
                            except json.JSONDecodeError:
                                continue
                        elif line and not line.startswith(":"):
                            try:
                                chunk_data = json.loads(line)
                                yield OpenAIGenerateCompletionResult(**chunk_data)
                            except json.JSONDecodeError:
                                continue

    async def generate_chat_completion(
        self,
        request: OpenAIGenerateChatCompletionRequest,
    ) -> Optional[OpenAIGenerateChatCompletionResult]:
        """
        Generate a chat completion using OpenAI-compatible API (non-streaming).

        Args:
            request: Chat completion request.

        Returns:
            Chat completion result, or None on failure.
        """
        # Force non-streaming
        request_data = request.model_dump(exclude_none=True)
        request_data["stream"] = False

        url = f"{self._sdk.endpoint}/v1/chat/completions"
        response = await self._sdk._post_async(url, request_data)

        if response is None:
            return None

        return OpenAIGenerateChatCompletionResult(**response)

    async def generate_chat_completion_stream(
        self,
        request: OpenAIGenerateChatCompletionRequest,
    ) -> AsyncIterator[OpenAIGenerateChatCompletionResult]:
        """
        Generate a chat completion with streaming using OpenAI-compatible API.

        Args:
            request: Chat completion request.

        Yields:
            Chat completion chunks as they are generated.
        """
        # Force streaming
        request_data = request.model_dump(exclude_none=True)
        request_data["stream"] = True

        url = f"{self._sdk.endpoint}/v1/chat/completions"

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
                        line = line.strip()
                        if line.startswith("data: "):
                            data_str = line[6:]
                            if data_str == "[DONE]":
                                break
                            try:
                                chunk_data = json.loads(data_str)
                                yield OpenAIGenerateChatCompletionResult(**chunk_data)
                            except json.JSONDecodeError:
                                continue
                        elif line and not line.startswith(":"):
                            try:
                                chunk_data = json.loads(line)
                                yield OpenAIGenerateChatCompletionResult(**chunk_data)
                            except json.JSONDecodeError:
                                continue

    async def generate_embeddings(
        self,
        request: OpenAIGenerateEmbeddingsRequest,
    ) -> Optional[OpenAIGenerateEmbeddingsResult]:
        """
        Generate embeddings using OpenAI-compatible API.

        Args:
            request: Embeddings request.

        Returns:
            Embeddings result, or None on failure.
        """
        url = f"{self._sdk.endpoint}/v1/embeddings"
        response = await self._sdk._post_async(url, request.model_dump(exclude_none=True))

        if response is None:
            return None

        return OpenAIGenerateEmbeddingsResult(**response)
