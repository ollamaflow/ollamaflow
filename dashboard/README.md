<img src="../assets/logo.png" height="48">

# OllamaFlow Web UI

A web interface for interacting with OllamaFlow configuration, built with Next.js and React.

> This package is part of the [OllamaFlow monorepo](../README.md).

## Documentation

Full API documentation is available at [OllamaFlow API Reference](https://ollamaflow.readme.io/reference/api-reference).

- Explore available endpoints
- See example requests and responses
- Learn about authentication and usage

For more details, visit: https://ollamaflow.readme.io/

## Features

- **Backends**: Manage and configure Ollama backends
- **Frontends**: Manage and configure Ollama frontends

## Requirements

- Node.js v18.20.4
- npm

## Quick Start

### Development Setup

#### Install dependencies:

```bash
npm install
```

#### Set the ollama instance URL

Update the `ollamaServerUrl` in [`src/constants/apiConfig.ts`](src/constants/apiConfig.ts) to point to your ollama instance:

```typescript
export const ollamaServerUrl = "http://localhost:43411";
```

#### Start the production server (for using ollama ui locally):

```bash
npm run build
```

```bash
npm run start
```

OR

#### Start the development server (for development, can be used to test web ui locally as well):

```bash
npm run dev
```

The application will be available at `http://localhost:3000`.

### Testing

Run the test suite:

```bash
# Run all tests
npm test

# Run tests with coverage
npm run test:coverage

# Watch mode for development
npm run test:watch
```

## Deployment Process

#### Build the Application

Prepare the app for production:

```bash
npm run build
```

#### Start the Production Server

Start the built application:

```bash
npm run start
```

The app will be available at http://localhost:3000.

### Code Quality

The project uses several tools to maintain code quality:

- ESLint for code linting
- Prettier for code formatting
- Jest for testing

## Development Guidelines

1. **Code Style**

   - Follow the Prettier configuration
   - Use TypeScript for type safety
   - Follow component-based architecture

2. **Testing**
   - Write unit tests for components
