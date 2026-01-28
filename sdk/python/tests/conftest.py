"""Pytest configuration and fixtures for OllamaFlow SDK tests."""

import pytest

from ollamaflow_sdk import OllamaFlowSdk


@pytest.fixture
def sdk() -> OllamaFlowSdk:
    """Create an OllamaFlowSdk instance for testing."""
    return OllamaFlowSdk(
        endpoint="http://localhost:43411",
        bearer_token="test-token",
    )


@pytest.fixture
def mock_models_response() -> dict:
    """Mock response for list models endpoint."""
    return {
        "models": [
            {
                "name": "llama2:latest",
                "model": "llama2:latest",
                "modified_at": "2024-01-15T10:30:00Z",
                "size": 3825819519,
                "digest": "sha256:abc123",
                "details": {
                    "parent_model": "",
                    "format": "gguf",
                    "family": "llama",
                    "parameter_size": "7B",
                    "quantization_level": "Q4_0",
                },
            },
            {
                "name": "mistral:latest",
                "model": "mistral:latest",
                "modified_at": "2024-01-14T09:00:00Z",
                "size": 4109853184,
                "digest": "sha256:def456",
                "details": {
                    "parent_model": "",
                    "format": "gguf",
                    "family": "mistral",
                    "parameter_size": "7B",
                    "quantization_level": "Q4_0",
                },
            },
        ]
    }


@pytest.fixture
def mock_completion_response() -> dict:
    """Mock response for generate completion endpoint."""
    return {
        "model": "llama2:latest",
        "created_at": "2024-01-15T10:30:00Z",
        "response": "The meaning of life is to find happiness and fulfillment.",
        "done": True,
        "done_reason": "stop",
        "context": [1, 2, 3, 4, 5],
        "total_duration": 5000000000,
        "load_duration": 1000000000,
        "prompt_eval_count": 10,
        "prompt_eval_duration": 500000000,
        "eval_count": 15,
        "eval_duration": 3500000000,
    }


@pytest.fixture
def mock_chat_completion_response() -> dict:
    """Mock response for generate chat completion endpoint."""
    return {
        "model": "llama2:latest",
        "created_at": "2024-01-15T10:30:00Z",
        "message": {
            "role": "assistant",
            "content": "Hello! How can I help you today?",
        },
        "done": True,
        "done_reason": "stop",
        "total_duration": 5000000000,
        "load_duration": 1000000000,
        "prompt_eval_count": 10,
        "prompt_eval_duration": 500000000,
        "eval_count": 15,
        "eval_duration": 3500000000,
    }


@pytest.fixture
def mock_embeddings_response() -> dict:
    """Mock response for generate embeddings endpoint."""
    return {
        "model": "all-minilm:latest",
        "embeddings": [[0.1, 0.2, 0.3, 0.4, 0.5]],
        "total_duration": 1000000000,
        "load_duration": 500000000,
        "prompt_eval_count": 5,
    }


@pytest.fixture
def mock_backend_response() -> dict:
    """Mock response for backend endpoint."""
    return {
        "Identifier": "backend-1",
        "Name": "Test Backend",
        "Hostname": "localhost",
        "Port": 11434,
        "Ssl": False,
        "UnhealthyThreshold": 2,
        "HealthyThreshold": 2,
        "HealthCheckMethod": "GET",
        "HealthCheckUrl": "/",
        "MaxParallelRequests": 4,
        "RateLimitRequestsThreshold": 10,
        "ApiFormat": "Ollama",
        "Labels": ["test"],
        "AllowEmbeddings": True,
        "AllowCompletions": True,
        "Active": True,
    }


@pytest.fixture
def mock_frontend_response() -> dict:
    """Mock response for frontend endpoint."""
    return {
        "Identifier": "frontend-1",
        "Name": "Test Frontend",
        "Hostname": "*",
        "TimeoutMs": 60000,
        "LoadBalancing": "RoundRobin",
        "BlockHttp10": True,
        "MaxRequestBodySize": 536870912,
        "Backends": ["backend-1"],
        "RequiredModels": ["llama2:latest"],
        "AllowEmbeddings": True,
        "AllowCompletions": True,
        "AllowRetries": True,
        "Active": True,
    }
