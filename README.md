# OllamaFlow

<div align="center">
  <img src="https://github.com/jchristn/ollamaflow/blob/main/assets/icon.png?raw=true" width="200" height="184" alt="OllamaFlow">
  
  **Intelligent Load Balancing and Model Orchestration for Ollama**
  
  [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
  [![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com)
  [![Docker](https://img.shields.io/badge/Docker-available-blue.svg)](https://hub.docker.com/r/jchristn/ollamaflow)
  [![Documentation](https://img.shields.io/badge/Documentation-Available-brightgreen.svg)](https://ollamaflow.readme.io/)
  [![Web UI](https://img.shields.io/badge/Web%20UI-Dashboard-orange.svg)](https://github.com/ollamaflow/ui)
</div>

## 🚀 Scale Your AI Infrastructure

OllamaFlow is a lightweight, intelligent orchestration layer that unifies multiple AI backend instances into a high-availability inference cluster. Supporting both Ollama and OpenAI API formats on the frontend with native transformation capabilities, OllamaFlow scales AI workloads across multiple backends while ensuring zero-downtime model serving.

> 📖 **[Complete Documentation](https://ollamaflow.readme.io/)** | 🎨 **[Web UI Dashboard](https://github.com/ollamaflow/ui)**

### Why OllamaFlow?

- **🎯 Multiple Virtual Endpoints**: Create multiple frontend endpoints, each mapping to their own set of AI backends
- **🔄 Universal API Support**: Frontend supports both Ollama and OpenAI API formats with native transformation
- **🌐 Multi-Backend Support**: Connect to Ollama, OpenAI, [vLLM](https://vllm.ai), [SharpAI](https://github.com/jchristn/sharpai), and other OpenAI-compatible backends
- **⚖️ Smart Load Balancing**: Distribute requests intelligently across healthy backends
- **🔧 Automatic Model Sync**: Ensure all backends have the required models - automatically
- **❤️ Health Monitoring**: Real-time health checks with configurable thresholds
- **📊 Zero Downtime**: Seamlessly handle backend failures without dropping requests
- **🛠️ RESTful Admin API**: Full control through a comprehensive management API
- **🎨 Web Dashboard**: Optional web UI for visual cluster management and monitoring

## 🎨 Key Features

### Load Balancing
- **Round-robin** and **random** distribution strategies
- Request routing based on backend health and capacity
- Automatic failover for unhealthy backends
- Configurable rate limiting per backend

### Model Management
- **Automatic model discovery** across all backends
- **Intelligent synchronization** - pulls missing models automatically
- **Dynamic model requirements** - update required models on the fly
- **Parallel downloads** with configurable concurrency

### High Availability
- **Real-time health monitoring** with customizable check intervals
- **Automatic failover** for unhealthy backends
- **Request queuing** during high load
- **Connection pooling** for optimal performance

### Enterprise Ready
- **Bearer token authentication** for admin APIs
- **Comprehensive logging** with syslog support
- **Docker and Docker Compose** ready
- **SQLite database** for configuration persistence

## 🏃 Quick Start

### Using Docker (Recommended)

```bash
# Pull the image
docker pull jchristn/ollamaflow:v1.0.0

# Run with default configuration
docker run -d \
  -p 43411:43411 \
  -v $(pwd)/ollamaflow.json:/app/ollamaflow.json \
  jchristn/ollamaflow
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

OllamaFlow uses a simple JSON configuration file. Here's a minimal example:

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
  "RequiredModels": ["llama3", "mistral", "codellama"]
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
  "HealthCheckUrl": "/",
  "UnhealthyThreshold": 2,
  "BackendType": "Ollama"
}
```

## 📡 API Compatibility

OllamaFlow provides universal API compatibility with native transformation between formats:

### Frontend API Support
- ✅ **Ollama API** - Complete compatibility with all Ollama endpoints
- ✅ **OpenAI API** - Chat completions, embeddings, and model management
- ✅ **Native Transformation** - Automatic request/response format conversion

### Supported Endpoints

**Ollama Format:**
- ✅ `/api/generate` - Text generation
- ✅ `/api/chat` - Chat completions
- ✅ `/api/pull` - Model pulling
- ✅ `/api/push` - Model pushing
- ✅ `/api/show` - Model information
- ✅ `/api/tags` - List models
- ✅ `/api/ps` - Running models
- ✅ `/api/embed` - Embeddings
- ✅ `/api/delete` - Model deletion

**OpenAI Format:**
- ✅ `/v1/chat/completions` - Chat completions
- ✅ `/v1/completions` - Text completions
- ✅ `/v1/embeddings` - Text embeddings
- ✅ `/v1/models` - List available models

### Supported Backends
- **[Ollama](https://ollama.ai)** - Local AI runtime
- **[OpenAI](https://openai.com)** - OpenAI API services
- **[vLLM](https://vllm.ai)** - High-performance LLM inference
- **[SharpAI](https://github.com/jchristn/sharpai)** - .NET-based AI inference server
- **Any OpenAI-compatible API** - Universal backend support

## 🔧 Advanced Features

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

## 📊 Performance

OllamaFlow adds minimal overhead to your Ollama requests:

- **< 1ms** routing decision time
- **Negligible memory footprint** (~50MB)
- **High throughput** - handles thousands of requests per second
- **Efficient streaming** support for real-time responses

## 🛡️ Security

- Bearer token authentication for administrative APIs
- Request source IP forwarding for audit trails
- Configurable request size limits
- No external dependencies for core functionality

## 🌟 Use Cases

- **GPU Cluster Management**: Distribute AI workloads across multiple GPU servers
- **Multi-Provider Orchestration**: Combine local Ollama instances with cloud OpenAI services
- **API Format Unification**: Present a consistent interface regardless of backend type
- **High Availability**: Ensure your AI services stay online 24/7 with automatic failover
- **Development & Testing**: Easily switch between different model configurations and providers
- **Cost Optimization**: Route requests to the most cost-effective backend automatically
- **Multi-Tenant Scenarios**: Isolate workloads while sharing infrastructure
- **Migration Support**: Seamlessly migrate between different AI service providers

## 📚 Documentation & Resources

- **[Complete Documentation](https://ollamaflow.readme.io/)** - Comprehensive guides, API reference, and tutorials
- **[Web UI Dashboard](https://github.com/ollamaflow/ui)** - Visual cluster management interface
- **[Postman Collection](OllamaFlow.postman_collection.json)** - API testing and examples

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- The [Ollama](https://ollama.ai) team for creating an amazing local AI runtime
- All our contributors and users who make this project possible

---

<div align="center">
  <b>Ready to scale your AI infrastructure?</b><br>
  Get started with OllamaFlow today!<br><br>
  📖 <a href="https://ollamaflow.readme.io/"><b>Documentation</b></a> |
  🎨 <a href="https://github.com/ollamaflow/ui"><b>Web Dashboard</b></a>
</div>
