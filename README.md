# OllamaFlow

<div align="center">
  <img src="https://github.com/jchristn/ollamaflow/blob/main/assets/icon.png?raw=true" width="200" height="184" alt="OllamaFlow">
  
  **Intelligent Load Balancing and Model Orchestration for Ollama and OpenAI Platforms**
  
  [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
  [![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com)
  [![Docker](https://img.shields.io/badge/Docker-available-blue.svg)](https://hub.docker.com/r/jchristn/ollamaflow)
  [![Documentation](https://img.shields.io/badge/Documentation-Available-brightgreen.svg)](https://ollamaflow.readme.io/)
  [![Web UI](https://img.shields.io/badge/Web%20UI-Dashboard-orange.svg)](https://github.com/ollamaflow/ui)
</div>

## 🚀 Scale Your AI Infrastructure

OllamaFlow is a lightweight, intelligent orchestration layer that unifies multiple AI backend instances into a high-availability inference cluster. Supporting both Ollama and OpenAI API formats on the frontend with native transformation capabilities, OllamaFlow delivers **scalability**, **high availability**, and **security control** - enabling you to scale AI workloads across multiple backends while ensuring zero-downtime model serving and fine-grained control over inference and embeddings deployments.

> 📖 **[Complete Documentation](https://ollamaflow.readme.io/)** | 🎨 **[Web UI Dashboard](https://github.com/ollamaflow/ui)**

### Why OllamaFlow?

- **🎯 Multiple Virtual Endpoints**: Create multiple frontend endpoints, each mapping to their own set of AI backends
- **🔄 Universal API Support**: Frontend supports both Ollama and OpenAI API formats
- **🌐 Multi-Backend Support**: Connect to Ollama, OpenAI, [vLLM](https://vllm.ai), [SharpAI](https://github.com/jchristn/sharpai), and other OpenAI-compatible backends
- **⚖️ Smart Load Balancing**: Distribute requests intelligently across healthy backends
- **🔒 Security and Control**: Fine-grained control over request types, parameter enforcement, and backend selection for secure inference and embeddings deployments
- **🔧 Automatic Model Sync**: Ensure all backends have the required models (Ollama-compatible backends only)
- **❤️ Health Monitoring**: Real-time health checks with configurable thresholds
- **📊 Zero Downtime**: Provide high-availability to mitigate effects of backend failures
- **🛠️ RESTful Admin API**: Full control through a comprehensive management API
- **🎨 Web Dashboard**: Optional web UI for visual cluster management and monitoring

## 🎨 Key Features

### Load Balancing
- **Round-robin** and **random** distribution strategies
- Request routing based on backend health and capacity
- Automatic failover for unhealthy backends
- Configurable rate limiting per backend
- Sticky sessions based on custom headers or IP address

### Model Management
- **Automatic model discovery** across all Ollama backends
- **Intelligent synchronization** - pulls missing models automatically on Ollama-compatible backends
- **Dynamic model requirements** - update required models on Ollama-compatible backends
- **Parallel downloads** with configurable concurrency

### High Availability
- **Real-time health monitoring** with customizable check intervals
- **Automatic failover** for unhealthy backends
- **Request queuing** during high load
- **Connection pooling** for optimal performance

### Security and Control
- **Request type restrictions** - Control embeddings and completions access at frontend and backend levels
- **Pinned request properties** - Enforce or override parameters for compliance (models, context size, temperature, etc.)
- **Backend selection** - Force backend selection based on labels attached to incoming requests for compliance and other use cases
- **Bearer token authentication** for admin APIs
- **Multi-tenant isolation** through separate virtual frontends

### Enterprise Ready
- **Comprehensive logging** with syslog support
- **Docker and Docker Compose** ready
- **SQLite database** for configuration persistence
- **Production-tested** for scalability and high availability

## 🏃 Quick Start

### Using Docker (Recommended)

```bash
# Pull the image
docker pull jchristn/ollamaflow:v1.1.0

# Run with default configuration
docker run -d \
  -p 43411:43411 \
  -v $(pwd)/ollamaflow.json:/app/ollamaflow.json \
  -v $(pwd)/ollamaflow.db:/app/ollamaflow.db \
  jchristn/ollamaflow:v1.1.0
```

### Using .NET

```bash
# Clone the repository
git clone https://github.com/jchristn/ollamaflow.git
cd ollamaflow/src

# Build and run
dotnet build
cd OllamaFlow.Server/bin/Debug/net8.0
dotnet OllamaFlow.Server.dll
```

## ⚙️ Configuration

OllamaFlow uses a simple JSON configuration file named `ollamaflow.json`. Here's a minimal example:

```json
{
  "Webserver": {
    "Hostname": "*",
    "Port": 43411
  },
  "Logging": {
    "MinimumSeverity": 6,
    "ConsoleLogging": true
  },
  "Frontends": ["..."],
  "Backends": ["..."]
}
```

### Frontend Configuration

Frontends define your virtual Ollama endpoints:

```json
{
  "Identifier": "main-frontend",
  "Name": "Production Ollama Frontend",
  "Hostname": "*",
  "LoadBalancing": "RoundRobin",
  "Backends": ["gpu-1", "gpu-2", "gpu-3"],
  "RequiredModels": ["llama3", "all-minilm"],
  "AllowEmbeddings": true,
  "AllowCompletions": true,
  "PinnedEmbeddingsProperties": {
    "model": "all-minilm"
  },
  "PinnedCompletionsProperties": {
    "model": "llama3",
    "options": {
      "num_ctx": 4096,
      "temperature": 0.3
    }
  }
}
```

### Backend Configuration

Backends represent your actual AI inference instances (Ollama, OpenAI, vLLM, SharpAI, etc.):

```json
{
  "Identifier": "gpu-1",
  "Name": "GPU Server 1",
  "Hostname": "192.168.1.100",
  "Port": 11434,
  "MaxParallelRequests": 4,
  "HealthCheckMethod": "HEAD",
  "HealthCheckUrl": "/",
  "UnhealthyThreshold": 2,
  "ApiFormat": "Ollama",
  "AllowEmbeddings": true,
  "AllowCompletions": true,
  "Labels": [
    "eu-central-1",
    "has-nvidia-gpu"
  ],
  "PinnedEmbeddingsProperties": {
    "model": "all-minilm"
  },
  "PinnedCompletionsProperties": {
    "model": "llama3",
    "options": {
      "num_ctx": 4096,
      "temperature": 0.3
    }
  }
}
```

## 📡 API Compatibility

OllamaFlow provides universal API compatibility with native transformation between formats:

### Frontend API Support
- ✅ **Ollama API** - Complete compatibility with all Ollama endpoints
- ✅ **OpenAI API** - Chat completions, embeddings, and model management

### Supported Endpoints

**Ollama API:**
- ✅ `/api/generate` - Text generation
- ✅ `/api/chat/generate` - Chat completions
- ✅ `/api/pull` - Model pulling
- ✅ `/api/push` - Model pushing
- ✅ `/api/show` - Model information
- ✅ `/api/tags` - List models
- ✅ `/api/ps` - Running models
- ✅ `/api/embed` - Embeddings
- ✅ `/api/delete` - Model deletion

**OpenAI API:**
- ✅ `/v1/chat/completions` - Chat completions
- ✅ `/v1/completions` - Text completions
- ✅ `/v1/embeddings` - Text embeddings

### Supported Backends
- **[Ollama](https://ollama.ai)** - Local AI runtime
- **[OpenAI](https://openai.com)** - OpenAI API services
- **[vLLM](https://vllm.ai)** - High-performance LLM inference
- **[SharpAI](https://github.com/jchristn/sharpai)** - .NET-based AI inference server
- **Any OpenAI-compatible API** - Universal backend support

## 🔧 Advanced Features

### Request Control & Security

OllamaFlow provides fine-grained control over request types, request parameters, and backend selection. 

#### Request Type Restrictions

Control which types of requests are allowed using `AllowEmbeddings` and `AllowCompletions` boolean properties:

- Set on **frontends** to control which request types clients can use those endpoint
- Set on **backends** to control which request types can be routed to that backend instance
- Both must be `true` for a request to succeed - if either the frontend or backend disallows a request type, it will fail

**Example use cases:**
- Dedicate specific frontends for embeddings-only workloads
- Reserve high-performance backends for completions only
- Create security boundaries between different request types

#### Pinned Request Properties

Force specific properties into requests using `PinnedEmbeddingsProperties` and `PinnedCompletionsProperties` dictionaries:

- Properties are **automatically appended** to requests that don't include them
- Properties **overwrite existing values** in the request for compliance enforcement
- Apply to both frontends and backends independently
- Support any valid request property (model, options, temperature, context size, stop tokens, etc.)
- **Structure must mirror the API request format** - for Ollama API, generation parameters go inside `options` object

**Example use cases:**
- **Model enforcement**: Ensure specific models are always used regardless of client request
- **Resource control**: Lock context sizes to prevent memory exhaustion
- **Quality assurance**: Standardize temperature and other generation parameters
- **Security compliance**: Override user-specified parameters to meet organizational policies

**Property precedence (highest to lowest):**
1. Backend pinned properties
2. Frontend pinned properties
3. Original user request properties

**Merge behavior:**
- Uses recursive JSON merging via [JsonMerge](https://github.com/jchristn/jsonmerge)
- Nested objects are merged intelligently (new properties added, existing properties overwritten)
- Arrays are completely replaced, not merged

```json
{
  "Identifier": "secured-frontend",
  "PinnedCompletionsProperties": {
    "model": "llama3",
    "options": {
      "temperature": 0.3,
      "num_ctx": 4096,
      "stop": ["[DONE]", "\n\n"]
    }
  }
}
```

#### Backend Selection

Numerous reasons exist why someone would want to dictate which backends can be used for a given operation, and the combination of `Backend.Labels` and requests with the `X-OllamaFlow-Label` allow you to do exactly this.  For example, a backend might be labeled `americas` whereas another might be labeled `europe`.  To ensure requests that are in scope for GDPR happen only in Europe, add the `X-OllamaFlow-Label: europe` header to the incoming request, which will force OllamaFlow to only consider backends with that label.

Backend:
```json
...
  "Labels": [
    "europe"
  ],
...
```

### Multi-Backend Testing

Test with multiple AI backend instances using Docker Compose:

```bash
cd Docker
docker compose -f compose-ollama.yaml up -d
```

This spins up 4 Ollama instances on ports 11435-11438 for testing load balancing and transformation capabilities.

### Admin API

Manage your cluster programmatically:

```bash
# List all backends
curl -H "Authorization: Bearer your-token" \
  http://localhost:43411/v1.0/backends

# Add a new backend
curl -X PUT \
  -H "Authorization: Bearer your-token" \
  -H "Content-Type: application/json" \
  -d '{"Identifier": "gpu-4", "Hostname": "192.168.1.104", "Port": 11434}' \
  http://localhost:43411/v1.0/backends
```

A complete **Postman collection** (`OllamaFlow.postman_collection.json`) is included in the repository root with examples for all API endpoints, including Ollama API, OpenAI API, and administrative APIs with native transformation examples.

For interactive API testing and experimentation, the **[OllamaFlow API Explorer](https://github.com/ollamaflow/apiexplorer)** provides a web-based dashboard for exploring and testing all OllamaFlow endpoints.

For a visual interface, check out the **[OllamaFlow Web UI](https://github.com/ollamaflow/ui)** which provides a dashboard for cluster management and monitoring.

## 🤝 Contributing

We welcome contributions! Whether it's:

- 🐛 Bug fixes
- ✨ New features
- 📚 Documentation improvements
- 💡 Feature requests

Please check out our [Contributing Guidelines](CONTRIBUTING.md) and feel free to:

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📚 Documentation & Resources

- **[Complete Documentation](https://ollamaflow.readme.io/)** - Comprehensive guides, API reference, and tutorials
- **[Web UI Dashboard](https://github.com/ollamaflow/ui)** - Visual cluster management interface
- **[API Explorer](https://github.com/ollamaflow/apiexplorer)** - Interactive web-based API testing and experimentation
- **[Postman Collection](OllamaFlow.postman_collection.json)** - API testing and examples

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- The [Ollama](https://ollama.ai) and [vLLM](https://vllm.ai) teams for creating amazing local AI tools and model runners
- All our contributors and users who make this project possible

---

<div align="center">
  <b>Ready to scale your AI infrastructure?</b><br>
  Get started with OllamaFlow today!<br><br>
  📖 <a href="https://ollamaflow.readme.io/"><b>Documentation</b></a> |
  🎨 <a href="https://github.com/ollamaflow/ui"><b>Web Dashboard</b></a> |
  🔬 <a href="https://github.com/ollamaflow/apiexplorer"><b>API Explorer</b></a>
</div>
