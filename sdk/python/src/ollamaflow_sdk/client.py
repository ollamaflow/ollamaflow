"""Main OllamaFlow SDK client."""

from typing import Any, Callable, Optional

import httpx

from ollamaflow_sdk.backend import BackendMethods
from ollamaflow_sdk.frontend import FrontendMethods
from ollamaflow_sdk.ollama_api import OllamaMethods
from ollamaflow_sdk.openai_api import OpenAIMethods


class OllamaFlowSdk:
    """
    OllamaFlow SDK client for interacting with OllamaFlow server instances.

    Provides access to Ollama API, OpenAI-compatible API, and admin APIs
    for managing frontends and backends.

    Example:
        >>> sdk = OllamaFlowSdk("http://localhost:43411")
        >>> models = await sdk.ollama.list_local_models()
        >>> print(f"Found {len(models)} models")
    """

    def __init__(
        self,
        endpoint: str,
        bearer_token: Optional[str] = None,
        timeout_ms: int = 300000,
    ) -> None:
        """
        Initialize the OllamaFlow SDK.

        Args:
            endpoint: OllamaFlow server endpoint URL (e.g., "http://localhost:43411").
            bearer_token: Bearer token for authentication with admin APIs.
            timeout_ms: Request timeout in milliseconds. Default is 300000 (5 minutes).
        """
        if not endpoint:
            raise ValueError("endpoint is required")

        self._endpoint = endpoint.rstrip("/")
        self._bearer_token = bearer_token
        self._timeout_ms = timeout_ms
        self._log_requests = False
        self._log_responses = False
        self._logger: Optional[Callable[[str, str], None]] = None

        # Initialize API method classes
        self._ollama = OllamaMethods(self)
        self._openai = OpenAIMethods(self)
        self._backend = BackendMethods(self)
        self._frontend = FrontendMethods(self)

    @property
    def endpoint(self) -> str:
        """Get the OllamaFlow server endpoint URL."""
        return self._endpoint

    @property
    def bearer_token(self) -> Optional[str]:
        """Get the bearer token for authentication."""
        return self._bearer_token

    @bearer_token.setter
    def bearer_token(self, value: Optional[str]) -> None:
        """Set the bearer token for authentication."""
        self._bearer_token = value

    @property
    def timeout_ms(self) -> int:
        """Get the request timeout in milliseconds."""
        return self._timeout_ms

    @timeout_ms.setter
    def timeout_ms(self, value: int) -> None:
        """Set the request timeout in milliseconds."""
        if value < 0:
            raise ValueError("timeout_ms must be non-negative")
        self._timeout_ms = value

    @property
    def log_requests(self) -> bool:
        """Get whether request logging is enabled."""
        return self._log_requests

    @log_requests.setter
    def log_requests(self, value: bool) -> None:
        """Set whether to log requests."""
        self._log_requests = value

    @property
    def log_responses(self) -> bool:
        """Get whether response logging is enabled."""
        return self._log_responses

    @log_responses.setter
    def log_responses(self, value: bool) -> None:
        """Set whether to log responses."""
        self._log_responses = value

    @property
    def logger(self) -> Optional[Callable[[str, str], None]]:
        """Get the logger function."""
        return self._logger

    @logger.setter
    def logger(self, value: Optional[Callable[[str, str], None]]) -> None:
        """Set the logger function. Should accept (level, message) parameters."""
        self._logger = value

    @property
    def ollama(self) -> OllamaMethods:
        """Access Ollama API methods."""
        return self._ollama

    @property
    def openai(self) -> OpenAIMethods:
        """Access OpenAI-compatible API methods."""
        return self._openai

    @property
    def backend(self) -> BackendMethods:
        """Access backend management methods."""
        return self._backend

    @property
    def frontend(self) -> FrontendMethods:
        """Access frontend management methods."""
        return self._frontend

    def log(self, level: str, message: str) -> None:
        """
        Log a message using the configured logger.

        Args:
            level: Log level (e.g., "DEBUG", "INFO", "WARN", "ERROR").
            message: Message to log.
        """
        if self._logger and message:
            self._logger(level, message)

    def _get_headers(self) -> dict[str, str]:
        """Get common headers for API requests."""
        headers: dict[str, str] = {"Content-Type": "application/json"}
        if self._bearer_token:
            headers["Authorization"] = f"Bearer {self._bearer_token}"
        return headers

    def _get_timeout(self) -> httpx.Timeout:
        """Get timeout configuration for httpx."""
        timeout_seconds = self._timeout_ms / 1000.0
        return httpx.Timeout(timeout_seconds, connect=30.0)

    async def _post_async(
        self,
        url: str,
        data: dict[str, Any],
    ) -> Optional[dict[str, Any]]:
        """
        Send a POST request and return the JSON response.

        Args:
            url: Full URL to send the request to.
            data: Request body data.

        Returns:
            Response JSON as a dictionary, or None on failure.
        """
        if self._log_requests:
            self.log("DEBUG", f"POST request to {url}")

        async with httpx.AsyncClient(timeout=self._get_timeout()) as client:
            try:
                response = await client.post(
                    url,
                    json=data,
                    headers=self._get_headers(),
                )

                if self._log_responses:
                    self.log("DEBUG", f"Response from {url} (status {response.status_code})")

                if 200 <= response.status_code < 300:
                    if response.text:
                        return response.json()
                    return None
                else:
                    self.log("WARN", f"Non-success from {url}: {response.status_code}")
                    return None

            except Exception as e:
                self.log("ERROR", f"Request failed: {e}")
                return None

    async def _get_async(self, url: str) -> Optional[dict[str, Any]]:
        """
        Send a GET request and return the JSON response.

        Args:
            url: Full URL to send the request to.

        Returns:
            Response JSON as a dictionary, or None on failure.
        """
        if self._log_requests:
            self.log("DEBUG", f"GET request to {url}")

        async with httpx.AsyncClient(timeout=self._get_timeout()) as client:
            try:
                response = await client.get(
                    url,
                    headers=self._get_headers(),
                )

                if self._log_responses:
                    self.log("DEBUG", f"Response from {url} (status {response.status_code})")

                if 200 <= response.status_code < 300:
                    if response.text:
                        return response.json()
                    return None
                else:
                    self.log("WARN", f"Non-success from {url}: {response.status_code}")
                    return None

            except Exception as e:
                self.log("ERROR", f"Request failed: {e}")
                return None

    async def _put_async(
        self,
        url: str,
        data: dict[str, Any],
    ) -> Optional[dict[str, Any]]:
        """
        Send a PUT request and return the JSON response.

        Args:
            url: Full URL to send the request to.
            data: Request body data.

        Returns:
            Response JSON as a dictionary, or None on failure.
        """
        if self._log_requests:
            self.log("DEBUG", f"PUT request to {url}")

        async with httpx.AsyncClient(timeout=self._get_timeout()) as client:
            try:
                response = await client.put(
                    url,
                    json=data,
                    headers=self._get_headers(),
                )

                if self._log_responses:
                    self.log("DEBUG", f"Response from {url} (status {response.status_code})")

                if 200 <= response.status_code < 300:
                    if response.text:
                        return response.json()
                    return None
                else:
                    self.log("WARN", f"Non-success from {url}: {response.status_code}")
                    return None

            except Exception as e:
                self.log("ERROR", f"Request failed: {e}")
                return None

    async def _delete_async(self, url: str) -> bool:
        """
        Send a DELETE request.

        Args:
            url: Full URL to send the request to.

        Returns:
            True if successful, False otherwise.
        """
        if self._log_requests:
            self.log("DEBUG", f"DELETE request to {url}")

        async with httpx.AsyncClient(timeout=self._get_timeout()) as client:
            try:
                response = await client.delete(
                    url,
                    headers=self._get_headers(),
                )

                if self._log_responses:
                    self.log("DEBUG", f"Response from {url} (status {response.status_code})")

                return 200 <= response.status_code < 300

            except Exception as e:
                self.log("ERROR", f"Request failed: {e}")
                return False

    async def _head_async(self, url: str) -> bool:
        """
        Send a HEAD request.

        Args:
            url: Full URL to send the request to.

        Returns:
            True if successful, False otherwise.
        """
        if self._log_requests:
            self.log("DEBUG", f"HEAD request to {url}")

        async with httpx.AsyncClient(timeout=self._get_timeout()) as client:
            try:
                response = await client.head(
                    url,
                    headers=self._get_headers(),
                )

                if self._log_responses:
                    self.log("DEBUG", f"Response from {url} (status {response.status_code})")

                return 200 <= response.status_code < 300

            except Exception as e:
                self.log("ERROR", f"Request failed: {e}")
                return False
