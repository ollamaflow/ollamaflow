"""Frontend management methods for OllamaFlow SDK."""

from __future__ import annotations

from typing import TYPE_CHECKING, Optional

from ollamaflow_sdk.models.frontend import Frontend

if TYPE_CHECKING:
    from ollamaflow_sdk.client import OllamaFlowSdk


class FrontendMethods:
    """Frontend management methods for OllamaFlow admin API."""

    def __init__(self, sdk: "OllamaFlowSdk") -> None:
        """
        Initialize frontend management methods.

        Args:
            sdk: Parent OllamaFlowSdk instance.
        """
        self._sdk = sdk

    async def retrieve_many(self) -> list[Frontend]:
        """
        Retrieve all frontends.

        Returns:
            List of frontends.
        """
        url = f"{self._sdk.endpoint}/v1.0/frontends"
        response = await self._sdk._get_async(url)

        if response is None:
            return []

        # Handle both array and object responses
        if isinstance(response, list):
            frontends_data = response
        elif isinstance(response, dict):
            if "data" in response:
                frontends_data = response["data"]
            elif "items" in response:
                frontends_data = response["items"]
            else:
                return []
        else:
            return []

        return [Frontend(**frontend) for frontend in frontends_data]

    async def retrieve(self, identifier: str) -> Optional[Frontend]:
        """
        Retrieve a specific frontend by identifier.

        Args:
            identifier: Frontend identifier.

        Returns:
            Frontend if found, None otherwise.
        """
        if not identifier:
            raise ValueError("identifier is required")

        url = f"{self._sdk.endpoint}/v1.0/frontends/{identifier}"
        response = await self._sdk._get_async(url)

        if response is None:
            return None

        return Frontend(**response)

    async def exists(self, identifier: str) -> bool:
        """
        Check if a frontend exists.

        Args:
            identifier: Frontend identifier.

        Returns:
            True if frontend exists, False otherwise.
        """
        if not identifier:
            raise ValueError("identifier is required")

        url = f"{self._sdk.endpoint}/v1.0/frontends/{identifier}"
        return await self._sdk._head_async(url)

    async def create(self, frontend: Frontend) -> Optional[Frontend]:
        """
        Create a new frontend.

        Args:
            frontend: Frontend configuration.

        Returns:
            Created frontend with server-assigned properties, or None on failure.
        """
        url = f"{self._sdk.endpoint}/v1.0/frontends"
        response = await self._sdk._put_async(
            url,
            frontend.model_dump(exclude_none=True, by_alias=True),
        )

        if response is None:
            return None

        return Frontend(**response)

    async def update(self, identifier: str, frontend: Frontend) -> Optional[Frontend]:
        """
        Update an existing frontend.

        Args:
            identifier: Frontend identifier.
            frontend: Updated frontend configuration.

        Returns:
            Updated frontend, or None on failure.
        """
        if not identifier:
            raise ValueError("identifier is required")

        url = f"{self._sdk.endpoint}/v1.0/frontends/{identifier}"
        response = await self._sdk._put_async(
            url,
            frontend.model_dump(exclude_none=True, by_alias=True),
        )

        if response is None:
            return None

        return Frontend(**response)

    async def delete(self, identifier: str) -> bool:
        """
        Delete a frontend.

        Args:
            identifier: Frontend identifier.

        Returns:
            True if successful, False otherwise.
        """
        if not identifier:
            raise ValueError("identifier is required")

        url = f"{self._sdk.endpoint}/v1.0/frontends/{identifier}"
        return await self._sdk._delete_async(url)
