<img src="https://github.com/jchristn/ollamaflow/blob/main/assets/icon.png?raw=true" width="140" height="128" alt="OllamaFlow">

# OllamaFlow

OllamaFlow is a lightweight intelligent load-balancer for Ollama.  OllamaFlow will distribute requests received on a virtual frontend to mapped backend Ollama servers based on their health and availability.  OllamaFlow will ensure that backend Ollama servers behind a virtual frontend are kept up to date with a list of required models.

## Help, Feedback, Contribute

If you have any issues or feedback, please file an issue here in Github. We'd love to have you help by contributing code for new features, optimization to the existing codebase, ideas for future releases, or fixes!

## New in v1.0.x

- Initial release

## Default Configuration

By default, OllamaFlow server will listen on `http://localhost:43411/` and is configured to connect to a local Ollama instance on `http://localhost:11434`.  If you point your browser to `http://localhost:43411/` you will see a default page indicating that the node is operational.  `HEAD` requests to this URL will also return a `200/OK`.

## Example

```csharp
$ cd /path/to/src-directory
$ dotnet build
$ cd OllamaFlow.Server/bin/Debug/net8.0
$ dotnet OllamaFlow.Server.dll

       _ _                  __ _
   ___| | |__ _ _ __  __ _ / _| |_____ __ __
  / _ \ | / _` | '  \/ _` |  _| / _ \ V  V /
  \___/_|_\__,_|_|_|_\__,_|_| |_\___/\_/\_/


OllamaFlow v1.0.0

Loading from settings file ./ollamaflow.json
2025-06-07 15:42:18 laptop Debug [HealthCheckService] starting healthcheck task for backend backend2 backend2 localhost:11436
2025-06-07 15:42:18 laptop Debug [HealthCheckService] starting healthcheck task for backend backend4 backend4 localhost:11438
2025-06-07 15:42:18 laptop Debug [HealthCheckService] starting healthcheck task for backend backend1 backend1 localhost:11435
2025-06-07 15:42:18 laptop Debug [ModelDiscoveryService] starting model discovery task for backend backend1 backend1 localhost:11435
2025-06-07 15:42:18 laptop Debug [ModelSynchronizationService] starting model synchronization task for backend backend3 backend3 localhost:11437
```

## Docker

A Docker image is available in [Docker Hub](https://hub.docker.com/r/jchristn/ollamaflow) under `jchristn/ollamaflow`.  Use the Docker Compose start (`compose-up.sh` and `compose-up.bat`) and stop (`compose-down.sh` and `compose-down.bat`) scripts in the `Docker` directory if you wish to run within Docker Compose.  Ensure that you have a valid configuration file (e.g. `ollamaflow.json`) exposed into your container.

The default `ollamaflow.json` file is configured to use only one backend, that is, on `http://localhost:11434`. 

If you wish to use multiple, use the `compose-ollama.yaml` file in the `Docker` directory, i.e. `docker compose -f compose-ollama.yaml up -d`, to start four separate Ollama containers on ports 11435, 11436, 11437, and 11438.  Modify the `ollamaflow.json` file accordingly, or refer to `ollamaflow-4node.json` as an example.  If you wish to use this file directly in your testing, simply copy `ollamaflow-4node.json` to `ollamaflow.json` (but first make a backup of `ollamaflow.json` for future use).  The `compose-ollama.yaml` file defines four volumes which you may want to clean up after testing.

## Version History

Refer to CHANGELOG.md for version history.
