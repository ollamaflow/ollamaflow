"""Backend management methods for OllamaFlow SDK."""

from __future__ import annotations

from typing import TYPE_CHECKING, Optional

from ollamaflow_sdk.models.backend import Backend, BackendHealth

if TYPE_CHECKING:
    from ollamaflow_sdk.client import OllamaFlowSdk


class BackendMethods:
    """Backend management methods for OllamaFlow admin API."""

    def __init__(self, sdk: "OllamaFlowSdk") -> None:
        """
        Initialize backend management methods.

        Args:
            sdk: Parent OllamaFlowSdk instance.
        """
        self._sdk = sdk

    async def retrieve_many(self) -> list[Backend]:
        """
        Retrieve all backends.

        Returns:
            List of backends.
        """
        url = f"{self._sdk.endpoint}/v1.0/backends"
        response = await self._sdk._get_async(url)

        if response is None:
            return []

        # Handle both array and object responses
        if isinstance(response, list):
            backends_data = response
        elif isinstance(response, dict):
            if "data" in response:
                backends_data = response["data"]
            elif "items" in response:
                backends_data = response["items"]
            else:
                return []
        else:
            return []

        return [Backend(**backend) for backend in backends_data]

    async def retrieve(self, identifier: str) -> Optional[Backend]:
        """
        Retrieve a specific backend by identifier.

        Args:
            identifier: Backend identifier.

        Returns:
            Backend if found, None otherwise.
        """
        if not identifier:
            raise ValueError("identifier is required")

        url = f"{self._sdk.endpoint}/v1.0/backends/{identifier}"
        response = await self._sdk._get_async(url)

        if response is None:
            return None

        return Backend(**response)

    async def retrieve_all_health(self) -> list[BackendHealth]:
        """
        Retrieve health status for all backends.

        Returns:
            List of backends with health status.
        """
        url = f"{self._sdk.endpoint}/v1.0/backends/health"
        response = await self._sdk._get_async(url)

        if response is None:
            return []

        # Handle both array and object responses
        if isinstance(response, list):
            backends_data = response
        elif isinstance(response, dict):
            if "data" in response:
                backends_data = response["data"]
            elif "items" in response:
                backends_data = response["items"]
            else:
                return []
        else:
            return []

        return [BackendHealth(**backend) for backend in backends_data]

    async def retrieve_health(self, identifier: str) -> Optional[BackendHealth]:
        """
        Retrieve health status for a specific backend.

        Args:
            identifier: Backend identifier.

        Returns:
            Backend with health status if found, None otherwise.
        """
        if not identifier:
            raise ValueError("identifier is required")

        url = f"{self._sdk.endpoint}/v1.0/backends/{identifier}/health"
        response = await self._sdk._get_async(url)

        if response is None:
            return None

        return BackendHealth(**response)

    async def exists(self, identifier: str) -> bool:
        """
        Check if a backend exists.

        Args:
            identifier: Backend identifier.

        Returns:
            True if backend exists, False otherwise.
        """
        if not identifier:
            raise ValueError("identifier is required")

        url = f"{self._sdk.endpoint}/v1.0/backends/{identifier}"
        return await self._sdk._head_async(url)

    async def create(self, backend: Backend) -> Optional[Backend]:
        """
        Create a new backend.

        Args:
            backend: Backend configuration.

        Returns:
            Created backend with server-assigned properties, or None on failure.
        """
        url = f"{self._sdk.endpoint}/v1.0/backends"
        response = await self._sdk._put_async(
            url,
            backend.model_dump(exclude_none=True, by_alias=True),
        )

        if response is None:
            return None

        return Backend(**response)

    async def update(self, identifier: str, backend: Backend) -> Optional[Backend]:
        """
        Update an existing backend.

        Args:
            identifier: Backend identifier.
            backend: Updated backend configuration.

        Returns:
            Updated backend, or None on failure.
        """
        if not identifier:
            raise ValueError("identifier is required")

        url = f"{self._sdk.endpoint}/v1.0/backends/{identifier}"
        response = await self._sdk._put_async(
            url,
            backend.model_dump(exclude_none=True, by_alias=True),
        )

        if response is None:
            return None

        return Backend(**response)

    async def delete(self, identifier: str) -> bool:
        """
        Delete a backend.

        Args:
            identifier: Backend identifier.

        Returns:
            True if successful, False otherwise.
        """
        if not identifier:
            raise ValueError("identifier is required")

        url = f"{self._sdk.endpoint}/v1.0/backends/{identifier}"
        return await self._sdk._delete_async(url)
