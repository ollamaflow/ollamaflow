# Change Log

## Current Version

v1.1.x

- Added support for OpenAI backends such as vLLM
- Major internal refactor and rearchitecture
- Support for allow/disallow of embeddings or completion requests
- Request property pinning for compliance and enforcement
- Request routing control through `Backend.Labels` and `X-OllamaFlow-Label` header
- Expose APIs natively through OllamaFlowDaemon for direct integration into C# applications
- Implemented IDisposable, cancellation tokens, and configure await
- Bugfixes

## Previous Versions

v1.0.x

- Initial release
