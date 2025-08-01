# OllamaFlow

<div align="center">
  <img src="https://github.com/jchristn/ollamaflow/blob/main/assets/icon.png?raw=true" width="200" height="184" alt="OllamaFlow">
  
  **Intelligent Load Balancing and Model Orchestration for Ollama**
  
  [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
  [![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com)
  [![Docker](https://img.shields.io/badge/Docker-available-blue.svg)](https://hub.docker.com/r/jchristn/ollamaflow)
</div>

## 🚀 Scale Your Ollama Infrastructure

OllamaFlow is a lightweight, intelligent orchestration layer that transforms multiple Ollama instances into a unified, high-availability AI inference cluster. Whether you're scaling AI workloads across multiple GPUs or ensuring zero-downtime model serving, OllamaFlow has you covered.

### Why OllamaFlow?

- **🎯 Multiple Virtual Endpoints**: Create multiple frontend endpoints, each mapping to their own set of Ollama backends
- **⚖️ Smart Load Balancing**: Distribute requests intelligently across healthy backends
- **🔄 Automatic Model Sync**: Ensure all backends have the required models - automatically
- **❤️ Health Monitoring**: Real-time health checks with configurable thresholds
- **📊 Zero Downtime**: Seamlessly handle backend failures without dropping requests
- **🛠️ RESTful Admin API**: Full control through a comprehensive management API

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
docker pull jchristn/ollamaflow

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
cd ollamaflow

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
    "Hostname": "localhost",
    "Port": 43411
  },
  "Logging": {
    "MinimumSeverity": "Info",
    "ConsoleLogging": true
  }
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

Backends represent your actual Ollama instances:

```json
{
  "Identifier": "gpu-1",
  "Name": "GPU Server 1",
  "Hostname": "192.168.1.100",
  "Port": 11434,
  "MaxParallelRequests": 4,
  "HealthCheckUrl": "/",
  "UnhealthyThreshold": 2
}
```

## 📡 API Compatibility

OllamaFlow is fully compatible with the Ollama API, supporting:

- ✅ `/api/generate` - Text generation
- ✅ `/api/chat` - Chat completions
- ✅ `/api/pull` - Model pulling
- ✅ `/api/push` - Model pushing
- ✅ `/api/show` - Model information
- ✅ `/api/tags` - List models
- ✅ `/api/ps` - Running models
- ✅ `/api/embed` - Embeddings
- ✅ `/api/delete` - Model deletion

## 🔧 Advanced Features

### Multi-Node Testing

Test with multiple Ollama instances using Docker Compose:

```bash
cd Docker
docker compose -f compose-ollama.yaml up -d
```

This spins up 4 Ollama instances on ports 11435-11438 for testing.

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

A complete **Postman collection** (`OllamaFlow.postman_collection.json`) is included in the repository root with examples for all API endpoints, both Ollama-compatible and administrative APIs.

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
- **CPU Infrastructure**: Perfect for dense CPU systems like Ampere processors
- **High Availability**: Ensure your AI services stay online 24/7
- **Development & Testing**: Easily switch between different model configurations
- **Cost Optimization**: Maximize hardware utilization across your infrastructure
- **Multi-Tenant Scenarios**: Isolate workloads while sharing infrastructure

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- The [Ollama](https://ollama.ai) team for creating an amazing local AI runtime
- All our contributors and users who make this project possible

---

<div align="center">
  <b>Ready to scale your AI infrastructure?</b><br>
  Get started with OllamaFlow today!
</div>