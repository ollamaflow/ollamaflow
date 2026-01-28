"""Tests for Admin API methods (backend and frontend management)."""

import pytest
from pytest_httpx import HTTPXMock

from ollamaflow_sdk import OllamaFlowSdk
from ollamaflow_sdk.models.backend import Backend
from ollamaflow_sdk.models.frontend import Frontend


class TestBackendMethods:
    """Tests for Backend management methods."""

    @pytest.mark.asyncio
    async def test_retrieve_many(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
        mock_backend_response: dict,
    ) -> None:
        """Test retrieving all backends."""
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/backends",
            json=[mock_backend_response],
        )

        backends = await sdk.backend.retrieve_many()

        assert len(backends) == 1
        assert backends[0].identifier == "backend-1"
        assert backends[0].name == "Test Backend"

    @pytest.mark.asyncio
    async def test_retrieve(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
        mock_backend_response: dict,
    ) -> None:
        """Test retrieving a specific backend."""
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/backends/backend-1",
            json=mock_backend_response,
        )

        backend = await sdk.backend.retrieve("backend-1")

        assert backend is not None
        assert backend.identifier == "backend-1"
        assert backend.hostname == "localhost"

    @pytest.mark.asyncio
    async def test_retrieve_not_found(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
    ) -> None:
        """Test retrieving a non-existent backend."""
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/backends/nonexistent",
            status_code=404,
        )

        backend = await sdk.backend.retrieve("nonexistent")
        assert backend is None

    @pytest.mark.asyncio
    async def test_retrieve_requires_identifier(
        self,
        sdk: OllamaFlowSdk,
    ) -> None:
        """Test that retrieve requires an identifier."""
        with pytest.raises(ValueError, match="identifier is required"):
            await sdk.backend.retrieve("")

    @pytest.mark.asyncio
    async def test_exists_true(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
    ) -> None:
        """Test checking if backend exists (true case)."""
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/backends/backend-1",
            method="HEAD",
            status_code=200,
        )

        exists = await sdk.backend.exists("backend-1")
        assert exists is True

    @pytest.mark.asyncio
    async def test_exists_false(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
    ) -> None:
        """Test checking if backend exists (false case)."""
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/backends/nonexistent",
            method="HEAD",
            status_code=404,
        )

        exists = await sdk.backend.exists("nonexistent")
        assert exists is False

    @pytest.mark.asyncio
    async def test_create(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
        mock_backend_response: dict,
    ) -> None:
        """Test creating a backend."""
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/backends",
            method="PUT",
            json=mock_backend_response,
        )

        backend = Backend(
            identifier="backend-1",
            name="Test Backend",
            hostname="localhost",
            port=11434,
        )
        created = await sdk.backend.create(backend)

        assert created is not None
        assert created.identifier == "backend-1"

    @pytest.mark.asyncio
    async def test_update(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
        mock_backend_response: dict,
    ) -> None:
        """Test updating a backend."""
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/backends/backend-1",
            method="PUT",
            json=mock_backend_response,
        )

        backend = Backend(
            identifier="backend-1",
            name="Updated Backend",
            hostname="localhost",
            port=11434,
        )
        updated = await sdk.backend.update("backend-1", backend)

        assert updated is not None

    @pytest.mark.asyncio
    async def test_delete(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
    ) -> None:
        """Test deleting a backend."""
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/backends/backend-1",
            method="DELETE",
            status_code=200,
        )

        deleted = await sdk.backend.delete("backend-1")
        assert deleted is True

    @pytest.mark.asyncio
    async def test_retrieve_all_health(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
        mock_backend_response: dict,
    ) -> None:
        """Test retrieving health for all backends."""
        health_response = {**mock_backend_response, "HealthySinceUtc": "2024-01-15T10:00:00Z"}
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/backends/health",
            json=[health_response],
        )

        health = await sdk.backend.retrieve_all_health()

        assert len(health) == 1
        assert health[0].identifier == "backend-1"


class TestFrontendMethods:
    """Tests for Frontend management methods."""

    @pytest.mark.asyncio
    async def test_retrieve_many(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
        mock_frontend_response: dict,
    ) -> None:
        """Test retrieving all frontends."""
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/frontends",
            json=[mock_frontend_response],
        )

        frontends = await sdk.frontend.retrieve_many()

        assert len(frontends) == 1
        assert frontends[0].identifier == "frontend-1"
        assert frontends[0].name == "Test Frontend"

    @pytest.mark.asyncio
    async def test_retrieve(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
        mock_frontend_response: dict,
    ) -> None:
        """Test retrieving a specific frontend."""
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/frontends/frontend-1",
            json=mock_frontend_response,
        )

        frontend = await sdk.frontend.retrieve("frontend-1")

        assert frontend is not None
        assert frontend.identifier == "frontend-1"
        assert frontend.backends == ["backend-1"]

    @pytest.mark.asyncio
    async def test_create(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
        mock_frontend_response: dict,
    ) -> None:
        """Test creating a frontend."""
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/frontends",
            method="PUT",
            json=mock_frontend_response,
        )

        frontend = Frontend(
            identifier="frontend-1",
            name="Test Frontend",
            backends=["backend-1"],
        )
        created = await sdk.frontend.create(frontend)

        assert created is not None
        assert created.identifier == "frontend-1"

    @pytest.mark.asyncio
    async def test_delete(
        self,
        sdk: OllamaFlowSdk,
        httpx_mock: HTTPXMock,
    ) -> None:
        """Test deleting a frontend."""
        httpx_mock.add_response(
            url="http://localhost:43411/v1.0/frontends/frontend-1",
            method="DELETE",
            status_code=200,
        )

        deleted = await sdk.frontend.delete("frontend-1")
        assert deleted is True
