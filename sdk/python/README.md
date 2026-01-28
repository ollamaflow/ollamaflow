<img src="../../assets/logo.png" height="48">

# OllamaFlow SDK for Python

A Python SDK for interacting with OllamaFlow server instances - providing Ollama and OpenAI compatible API wrappers plus Frontend/Backend management.

> This package is part of the [OllamaFlow monorepo](../../README.md).

## Features

- **Ollama API Compatibility** - Generate, Chat, Embeddings, Model management
- **OpenAI API Compatibility** - Completions, Chat Completions, Embeddings
- **Admin APIs** - Manage Frontends and Backends (create/update/retrieve/delete, health)
- **Async Support** - Fully asynchronous APIs using asyncio and httpx
- **Streaming Support** - Stream completions and chat completions in real-time
- **Type Hints** - Full type annotations for IDE support and type checking
- **Pydantic Models** - Validated request/response models

## Requirements

- Python 3.9+
- httpx
- pydantic

## Installation

```bash
# Install from source
pip install -e .

# Or with development dependencies
pip install -e ".[dev]"
```

## Quick Start

```python
import asyncio
from ollamaflow_sdk import OllamaFlowSdk
from ollamaflow_sdk.models.ollama import (
    OllamaGenerateChatCompletionRequest,
    OllamaChatMessage,
)

async def main():
    # Initialize the SDK
    sdk = OllamaFlowSdk(
        endpoint="http://localhost:43411",
        bearer_token="your-admin-token",  # Optional, for admin APIs
    )

    # List available models
    models = await sdk.ollama.list_local_models()
    print(f"Found {len(models)} models")
    for model in models:
        print(f"  - {model.name}")

    # Generate a chat completion
    request = OllamaGenerateChatCompletionRequest(
        model="llama2",
        messages=[
            OllamaChatMessage(role="user", content="Hello! How are you?"),
        ],
    )
    result = await sdk.ollama.generate_chat_completion(request)
    print(f"Response: {result.message.content}")

asyncio.run(main())
```

## Streaming Example

```python
import asyncio
from ollamaflow_sdk import OllamaFlowSdk
from ollamaflow_sdk.models.ollama import (
    OllamaGenerateChatCompletionRequest,
    OllamaChatMessage,
)

async def main():
    sdk = OllamaFlowSdk("http://localhost:43411")

    request = OllamaGenerateChatCompletionRequest(
        model="llama2",
        messages=[
            OllamaChatMessage(role="user", content="Write a short poem about Python."),
        ],
    )

    # Stream the response
    async for chunk in sdk.ollama.generate_chat_completion_stream(request):
        if chunk.message:
            print(chunk.message.content, end="", flush=True)
    print()

asyncio.run(main())
```

## Admin API Example

```python
import asyncio
from ollamaflow_sdk import OllamaFlowSdk, Backend, Frontend

async def main():
    sdk = OllamaFlowSdk(
        endpoint="http://localhost:43411",
        bearer_token="ollamaflowadmin",
    )

    # List backends
    backends = await sdk.backend.retrieve_many()
    print(f"Found {len(backends)} backends")

    # Create a new backend
    new_backend = Backend(
        identifier="my-ollama",
        name="My Ollama Instance",
        hostname="192.168.1.100",
        port=11434,
    )
    created = await sdk.backend.create(new_backend)
    print(f"Created backend: {created.identifier}")

    # Check backend health
    health = await sdk.backend.retrieve_health("my-ollama")
    if health and health.healthy_since_utc:
        print(f"Backend is healthy since {health.healthy_since_utc}")

    # List frontends
    frontends = await sdk.frontend.retrieve_many()
    print(f"Found {len(frontends)} frontends")

asyncio.run(main())
```

## OpenAI-Compatible API

```python
import asyncio
from ollamaflow_sdk import OllamaFlowSdk
from ollamaflow_sdk.models.openai import (
    OpenAIGenerateChatCompletionRequest,
    OpenAIChatMessage,
)

async def main():
    sdk = OllamaFlowSdk("http://localhost:43411")

    # Use OpenAI-compatible API
    request = OpenAIGenerateChatCompletionRequest(
        model="llama2",
        messages=[
            OpenAIChatMessage(role="user", content="Hello!"),
        ],
        temperature=0.7,
        max_tokens=100,
    )
    result = await sdk.openai.generate_chat_completion(request)

    if result and result.choices:
        print(result.choices[0].message.content)

asyncio.run(main())
```

## API Reference

### OllamaFlowSdk

The main SDK client class.

```python
sdk = OllamaFlowSdk(
    endpoint="http://localhost:43411",  # Required
    bearer_token="your-token",           # Optional, for admin APIs
    timeout_ms=300000,                   # Optional, default 5 minutes
)

# Enable logging
sdk.log_requests = True
sdk.log_responses = True
sdk.logger = lambda level, msg: print(f"[{level}] {msg}")
```

### Ollama API Methods (`sdk.ollama`)

| Method | Description |
|--------|-------------|
| `list_local_models()` | List all locally available models |
| `list_running_models()` | List all currently running models |
| `show_model_info(request)` | Show detailed model information |
| `generate_completion(request)` | Generate text completion |
| `generate_completion_stream(request)` | Stream text completion |
| `generate_chat_completion(request)` | Generate chat completion |
| `generate_chat_completion_stream(request)` | Stream chat completion |
| `generate_embeddings(request)` | Generate embeddings |
| `pull_model(request)` | Pull a model (streaming) |
| `delete_model(request)` | Delete a model |

### OpenAI API Methods (`sdk.openai`)

| Method | Description |
|--------|-------------|
| `generate_completion(request)` | Generate text completion |
| `generate_completion_stream(request)` | Stream text completion |
| `generate_chat_completion(request)` | Generate chat completion |
| `generate_chat_completion_stream(request)` | Stream chat completion |
| `generate_embeddings(request)` | Generate embeddings |

### Backend Management (`sdk.backend`)

| Method | Description |
|--------|-------------|
| `retrieve_many()` | List all backends |
| `retrieve(identifier)` | Get a specific backend |
| `retrieve_all_health()` | Get health status for all backends |
| `retrieve_health(identifier)` | Get health status for a backend |
| `exists(identifier)` | Check if backend exists |
| `create(backend)` | Create a new backend |
| `update(identifier, backend)` | Update a backend |
| `delete(identifier)` | Delete a backend |

### Frontend Management (`sdk.frontend`)

| Method | Description |
|--------|-------------|
| `retrieve_many()` | List all frontends |
| `retrieve(identifier)` | Get a specific frontend |
| `exists(identifier)` | Check if frontend exists |
| `create(frontend)` | Create a new frontend |
| `update(identifier, frontend)` | Update a frontend |
| `delete(identifier)` | Delete a frontend |

## Running Tests

```bash
# Install dev dependencies
pip install -e ".[dev]"

# Run tests
pytest

# Run tests with coverage
pytest --cov=ollamaflow_sdk
```

## License

This project is licensed under the MIT License.
