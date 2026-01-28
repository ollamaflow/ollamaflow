<img src="../assets/logo.png" height="48">

# OllamaFlow SDKs

Client SDKs for interacting with OllamaFlow server instances.

> This directory is part of the [OllamaFlow monorepo](../README.md).

## Available SDKs

| SDK | Language | Status | Package |
|-----|----------|--------|---------|
| [C# SDK](csharp/) | C# / .NET | Stable | [NuGet](https://www.nuget.org/packages/OllamaFlow.Sdk/) |
| [JavaScript SDK](js/) | JavaScript / TypeScript | Stable | npm (coming soon) |
| [Python SDK](python/) | Python | Stable | PyPI (coming soon) |

## Features

All SDKs provide:

- **Ollama API Compatibility** – Generate, Chat, Embeddings, Model management
- **OpenAI API Compatibility** – Completions, Chat Completions, Embeddings
- **Admin APIs** – Manage Frontends and Backends (create/update/retrieve/delete, health)

## Quick Start

See the individual SDK README files for installation and usage instructions:

- [C# SDK Documentation](csharp/README.md)
- [JavaScript SDK Documentation](js/README.md)
- [Python SDK Documentation](python/README.md)

## OllamaFlow Server

These SDKs require a running OllamaFlow server. See the [main README](../README.md) for server setup instructions.

## License

All SDKs are licensed under the MIT License.
