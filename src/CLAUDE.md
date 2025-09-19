# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

OllamaFlow is an intelligent load balancer and orchestration layer for Ollama AI instances. It provides high-availability AI inference clusters through smart load balancing, automatic model synchronization, health monitoring, and a RESTful admin API.

## Architecture

### Solution Structure
- **OllamaFlow.Core**: Main business logic library containing services, database access, and core functionality
- **OllamaFlow.Server**: Console application that hosts the OllamaFlow daemon
- **Test**: Test project for unit and integration tests

### Key Components

#### Core Services (`OllamaFlow.Core/Services/`)
- **GatewayService**: Handles request routing and proxy functionality to backend Ollama instances
- **HealthCheckService**: Monitors backend health with configurable check intervals and thresholds
- **ModelDiscoveryService**: Discovers available models across all backends
- **ModelSynchronizationService**: Automatically synchronizes required models to backends
- **FrontendService**: Manages virtual frontend endpoints and their configurations
- **BackendService**: Manages backend Ollama instances and their status

#### Database Layer (`OllamaFlow.Core/Database/`)
- Uses SQLite with WatsonORM for data persistence
- Separate interfaces and implementations for Frontend and Backend data access
- Database driver abstraction allows for future database providers

#### Main Classes
- **OllamaFlowDaemon**: Main orchestrator that initializes and coordinates all services
- **OllamaFrontend**: Represents virtual endpoints that clients connect to
- **OllamaBackend**: Represents physical Ollama instances in the cluster
- **OllamaFlowSettings**: Configuration management with JSON serialization

## Development Commands

### Build and Run
```bash
# Build the entire solution
dotnet build

# Run the server (from OllamaFlow.Server directory)
dotnet run

# Run from built binaries
cd OllamaFlow.Server/bin/Debug/net8.0
dotnet OllamaFlow.Server.dll
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Docker Development
```bash
# Build Docker image (from OllamaFlow.Server directory)
docker build -t ollamaflow .

# Run with Docker Compose (from Docker directory)
docker compose -f compose-ollama.yaml up -d
```

## Configuration

### Settings File
The application uses `ollamaflow.json` for configuration. If it doesn't exist, a default configuration is created on first run with:
- Webserver port: 43411
- Default admin bearer token: "ollamaflowadmin"
- SQLite database: `ollamaflow.db`

### Database Initialization
On first run, the application creates:
- A default frontend named "frontend1" mapping to "backend1"
- A default backend "backend1" pointing to localhost:11434

## Key Patterns

### Request Flow
1. Client requests hit the GatewayService
2. GatewayService determines the appropriate frontend based on hostname
3. Load balancing logic selects a healthy backend from the frontend's backend list
4. Request is proxied to the selected backend Ollama instance
5. Response is streamed back to the client

### Health Monitoring
- HealthCheckService runs periodic health checks on all backends
- Backends are marked unhealthy after configurable consecutive failures
- Only healthy backends participate in load balancing

### Model Management
- ModelDiscoveryService discovers available models on each backend
- ModelSynchronizationService ensures required models are available on all frontends' backends
- Model pulling happens automatically and in parallel when possible

## Dependencies

### Key NuGet Packages
- **Watson**: Web server framework for HTTP handling
- **WatsonORM.Sqlite**: SQLite ORM for data persistence
- **RestWrapper**: HTTP client wrapper for backend communication
- **SyslogLogging**: Structured logging with syslog support
- **ExpressionTree**: Dynamic LINQ expressions for database queries

## Admin API

The application exposes a RESTful admin API at `/v1.0/` endpoints:
- Frontends management: `/v1.0/frontends`
- Backends management: `/v1.0/backends`
- Health status: `/v1.0/health`
- Authentication via Bearer tokens specified in settings

## Load Balancing

Supports multiple load balancing strategies:
- **RoundRobin**: Cycles through backends sequentially
- **Random**: Randomly selects from healthy backends
- Configured per frontend in the database

## Security Notes

- Admin APIs require Bearer token authentication
- No external API keys or secrets are stored in code
- Database contains configuration only, no sensitive data
- Request source IP forwarding for audit trails

## Coding Standards and Style Rules

These rules must be followed STRICTLY throughout the codebase:

### Namespace and Using Statements
- Namespace declaration should always be at the top
- Using statements should be contained INSIDE the namespace block
- Microsoft and standard system library usings first, in alphabetical order
- Other using statements follow, in alphabetical order

### Documentation Requirements
- All public members, constructors, and public methods must have XML code documentation
- No code documentation should be applied to private members or private methods
- Document default values, minimum values, and maximum values where appropriate
- Document which exceptions public methods can throw using `/// <exception>` tags
- Document thread safety guarantees in XML comments
- Document nullability in XML comments

### Naming Conventions
- Private class member variable names must start with underscore and be Pascal cased: `_FooBar` (not `_fooBar`)
- Do not use `var` when defining variables - use actual type names

### Property and Member Design
- All public members should have explicit getters and setters using backing variables when value requires range or null validation
- Avoid using constant values for things developers may want to configure - use public members with backing private members set to reasonable defaults

### Async Programming
- Async calls should use `.ConfigureAwait(false)` where appropriate
- Every async method should accept a CancellationToken as input parameter, unless the class has a CancellationToken/CancellationTokenSource as member
- Async calls should check cancellation requests at appropriate places
- When implementing a method that returns IEnumerable, also create an async variant with CancellationToken

### Data Structures and LINQ
- Do not use tuples unless absolutely necessary
- Prefer LINQ methods over manual loops when readability is not compromised
- Use `.Any()` instead of `.Count() > 0` for existence checks
- Use `.FirstOrDefault()` with null checks rather than `.First()` when element might not exist
- Be aware of multiple enumeration issues - consider `.ToList()` when needed

### File Organization
- Limit each file to containing exactly one class or exactly one enum
- Do not nest multiple classes or multiple enums in a single file

### Exception Handling
- Use specific exception types rather than generic Exception
- Always include meaningful error messages with context
- Consider using custom exception types for domain-specific errors
- Use exception filters when appropriate: `catch (SqlException ex) when (ex.Number == 2601)`

### Resource Management
- Implement IDisposable/IAsyncDisposable when holding unmanaged resources or disposable objects
- Use 'using' statements or 'using' declarations for IDisposable objects
- Follow the full Dispose pattern with `protected virtual void Dispose(bool disposing)`
- Always call `base.Dispose()` in derived classes

### Null Safety
- Use nullable reference types (enable `<Nullable>enable</Nullable>` in project files)
- Validate input parameters with guard clauses at method start
- Use `ArgumentNullException.ThrowIfNull()` for .NET 6+ or manual null checks
- Consider using the Result pattern or Option/Maybe types for methods that can fail
- Proactively identify and eliminate situations where null might cause exceptions

### Thread Safety
- Use Interlocked operations for simple atomic operations
- Prefer ReaderWriterLockSlim over lock for read-heavy scenarios

### Implementation Guidelines
- Do not make assumptions about opaque class members or methods - ask for implementation details
- If code uses manually prepared SQL strings, assume there's a good reason and maintain the pattern
- Always compile code and ensure it's free of errors and warnings
- Analyze and ensure README accuracy if it exists