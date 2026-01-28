namespace TestOllamaFlowSdk
{
    using GetSomeInput;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Models.Ollama;
    using OllamaFlow.Core.Models.OpenAI;
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Sdk;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// Test application for OllamaFlow SDK demonstrating all available Ollama methods.
    /// </summary>
    public static class Program
    {
        private static bool _RunForever = true;
        private static bool _Debug = false;
        private static string? _CurrentModel = null;
        private static string _Endpoint = "http://localhost:43411";
        private static OllamaFlowSdk? _Sdk = null;
        private static Serializer _Serializer = new Serializer();
        public static async Task Main(string[] args)
        {
            Console.WriteLine("OllamaFlow SDK Test Application");
            Console.WriteLine("===============================");
            Console.WriteLine();

            InitializeSdk();

            while (_RunForever)
            {
                string userInput = Inputty.GetString("Command [? for help]:", null, false);

                if (userInput.Equals("?")) ShowMenu();
                else if (userInput.Equals("q")) _RunForever = false;
                else if (userInput.Equals("cls")) Console.Clear();
                else if (userInput.Equals("debug")) ToggleDebug();
                else if (userInput.Equals("endpoint")) SetEndpoint();
                else if (userInput.Equals("model")) await SetCurrentModel();
                else if (userInput.Equals("models")) await TestListModels();
                else if (userInput.Equals("running")) await TestListRunningModels();
                else if (userInput.Equals("info")) await TestShowModelInfo();
                else if (userInput.Equals("pull")) await TestPullModel();
                else if (userInput.Equals("delete")) await TestDeleteModel();
                else if (userInput.Equals("completion")) await TestOllamaTextCompletion();
                else if (userInput.Equals("chat")) await TestOllamaChatCompletion();
                else if (userInput.Equals("stream-completion")) await TestOllamaStreamingTextCompletion();
                else if (userInput.Equals("stream-chat")) await TestOllamaStreamingChatCompletion();
                else if (userInput.Equals("embeddings-single")) await TestOllamaEmbeddingsSingle();
                else if (userInput.Equals("embeddings-multiple")) await TestOllamaEmbeddingsMultiple();
                else if (userInput.Equals("backends")) await TestListBackends();
                else if (userInput.Equals("backend-health")) await TestListBackendHealth();
                else if (userInput.Equals("backend-get")) await TestGetBackend();
                else if (userInput.Equals("backend-health-get")) await TestGetBackendHealth();
                else if (userInput.Equals("backend-exists")) await TestBackendExists();
                else if (userInput.Equals("backend-create")) await TestCreateBackend();
                else if (userInput.Equals("backend-update")) await TestUpdateBackend();
                else if (userInput.Equals("backend-delete")) await TestDeleteBackend();
                else if (userInput.Equals("frontends")) await TestListFrontends();
                else if (userInput.Equals("frontend-get")) await TestGetFrontend();
                else if (userInput.Equals("frontend-exists")) await TestFrontendExists();
                else if (userInput.Equals("frontend-create")) await TestCreateFrontend();
                else if (userInput.Equals("frontend-update")) await TestUpdateFrontend();
                else if (userInput.Equals("frontend-delete")) await TestDeleteFrontend();
                else if (userInput.Equals("openai-completion")) await TestOpenAICompletion();
                else if (userInput.Equals("openai-chat")) await TestOpenAIChatCompletion();
                else if (userInput.Equals("openai-embeddings-single")) await TestOpenAIEmbeddingsSingle();
                else if (userInput.Equals("openai-embeddings-multiple")) await TestOpenAIEmbeddingsMultiple();
                else if (userInput.Equals("openai-stream-completion")) await TestOpenAIStreamingCompletion();
                else if (userInput.Equals("openai-stream-chat")) await TestOpenAIStreamingChatCompletion();
                else
                {
                    Console.WriteLine("Unknown command. Type '?' for help.");
                }
            }

            _Sdk?.Dispose();
            Console.WriteLine("Goodbye!");
        }

        private static void InitializeSdk()
        {
            try
            {
                _Endpoint = Inputty.GetString("OllamaFlow server endpoint:", _Endpoint, false);
                string bearerToken = Inputty.GetString("Bearer token for admin API (optional):", null, true);

                _Sdk = new OllamaFlowSdk(_Endpoint)
                {
                    LogRequests = _Debug,
                    LogResponses = _Debug,
                    BearerToken = bearerToken,
                    Logger = (level, message) =>
                    {
                        if (_Debug || level.Equals("WARN", StringComparison.OrdinalIgnoreCase) || level.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"[{level}] {message}");
                        }
                    }
                };
                _Sdk.TimeoutMs = 60000000;
                Console.WriteLine($"SDK initialized with endpoint: {_Endpoint}");
                if (!string.IsNullOrEmpty(bearerToken))
                    Console.WriteLine($"Bearer token configured for admin API access");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing SDK: {ex.Message}");
            }
        }

        private static void ShowMenu()
        {
            Console.WriteLine();
            Console.WriteLine("Available commands:");
            Console.WriteLine("  ?                            help, this menu");
            Console.WriteLine("  q                            quit");
            Console.WriteLine("  cls                          clear the screen");
            Console.WriteLine("  debug                        enable or disable debug (enabled: " + _Debug + ")");
            Console.WriteLine("  endpoint                     set the OllamaFlow server endpoint (currently: " + _Endpoint + ")");
            Console.WriteLine("  model                        set the current model (currently: " + (_CurrentModel ?? "none") + ")");
            Console.WriteLine();
            Console.WriteLine("Backend Management:");
            Console.WriteLine("  backends                     list all backends");
            Console.WriteLine("  backend-health               list all backends with health status");
            Console.WriteLine("  backend-get                  get specific backend by identifier");
            Console.WriteLine("  backend-health-get           get specific backend health status");
            Console.WriteLine("  backend-exists               check if backend exists");
            Console.WriteLine("  backend-create               create new backend");
            Console.WriteLine("  backend-update               update existing backend");
            Console.WriteLine("  backend-delete               delete backend");
            Console.WriteLine();
            Console.WriteLine("Frontend Management:");
            Console.WriteLine("  frontends                    list all frontends");
            Console.WriteLine("  frontend-get                 get specific frontend by identifier");
            Console.WriteLine("  frontend-exists              check if frontend exists");
            Console.WriteLine("  frontend-create              create new frontend");
            Console.WriteLine("  frontend-update              update existing frontend");
            Console.WriteLine("  frontend-delete              delete frontend");
            Console.WriteLine();
            Console.WriteLine("Model Management:");
            Console.WriteLine("  models                       list available models");
            Console.WriteLine("  running                      list running models");
            Console.WriteLine("  info                         show model information");
            Console.WriteLine("  pull                         pull a model with streaming progress");
            Console.WriteLine("  delete                       delete a model");
            Console.WriteLine();
            Console.WriteLine("OpenAI-Compatible API:");
            Console.WriteLine("  openai-completion            generate text completion");
            Console.WriteLine("  openai-chat                  generate chat completion");
            Console.WriteLine("  openai-embeddings-single     generate embeddings for single text");
            Console.WriteLine("  openai-embeddings-multiple   generate embeddings for multiple texts");
            Console.WriteLine("  openai-stream-completion     stream text completion chunks");
            Console.WriteLine("  openai-stream-chat           stream chat completion chunks");
            Console.WriteLine();
            Console.WriteLine("AI Operations (Ollama API):");
            Console.WriteLine("  completion                   generate text completion (non-streaming, continuous conversation)");
            Console.WriteLine("  chat                         generate chat completion (non-streaming, continuous conversation)");
            Console.WriteLine("  stream-completion            generate streaming text completion (continuous conversation)");
            Console.WriteLine("  stream-chat                  generate streaming chat completion (continuous conversation)");
            Console.WriteLine("  embeddings-single            generate embeddings for single text");
            Console.WriteLine("  embeddings-multiple          generate embeddings for multiple texts");
            Console.WriteLine();
            Console.WriteLine("Note: Type 'q' to exit conversations");
            Console.WriteLine();
        }

        private static void ToggleDebug()
        {
            _Debug = !_Debug;
            if (_Sdk != null)
            {
                _Sdk.LogRequests = _Debug;
                _Sdk.LogResponses = _Debug;
            }
            Console.WriteLine("Debug mode: " + (_Debug ? "enabled" : "disabled"));
        }

        private static void SetEndpoint()
        {
            string newEndpoint = Inputty.GetString("OllamaFlow server endpoint:", _Endpoint, false);
            if (!string.IsNullOrEmpty(newEndpoint))
            {
                _Endpoint = newEndpoint;
                InitializeSdk();
            }
        }

        #region Backend-admin-Tests

        private static async Task TestListBackends()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Listing all backends...");
                var backends = await _Sdk.Backend.RetrieveMany();

                if (backends != null && backends.Count > 0)
                {
                    Console.WriteLine($"Found {backends.Count} backends:");
                    EnumerateResponse(backends);
                }
                else
                {
                    Console.WriteLine("No backends found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing backends: {ex.Message}");
            }
        }

        private static async Task TestListBackendHealth()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Listing all backends with health status...");
                var backends = await _Sdk.Backend.RetrieveAllHealth();

                if (backends != null && backends.Count > 0)
                {
                    Console.WriteLine($"Found {backends.Count} backends:");
                    EnumerateResponse(backends);
                }
                else
                {
                    Console.WriteLine("No backends found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing backend health: {ex.Message}");
            }
        }

        private static async Task TestGetBackend()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                string identifier = Inputty.GetString("Backend identifier:", null, false);
                if (string.IsNullOrEmpty(identifier))
                {
                    Console.WriteLine("Backend identifier is required.");
                    return;
                }

                Console.WriteLine($"Getting backend: {identifier}...");
                var backend = await _Sdk.Backend.Retrieve(identifier);

                if (backend != null)
                {
                    Console.WriteLine("Backend found:");
                    EnumerateResponse(backend);
                }
                else
                {
                    Console.WriteLine("Backend not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting backend: {ex.Message}");
            }
        }

        private static async Task TestGetBackendHealth()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                string identifier = Inputty.GetString("Backend identifier:", null, false);
                if (string.IsNullOrEmpty(identifier))
                {
                    Console.WriteLine("Backend identifier is required.");
                    return;
                }

                Console.WriteLine($"Getting backend health: {identifier}...");
                EnumerateResponse(await _Sdk.Backend.RetrieveHealth(identifier));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting backend health: {ex.Message}");
            }
        }

        private static async Task TestBackendExists()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                string identifier = Inputty.GetString("Backend identifier:", null, false);
                if (string.IsNullOrEmpty(identifier))
                {
                    Console.WriteLine("Backend identifier is required.");
                    return;
                }

                Console.WriteLine($"Checking if backend exists: {identifier}...");
                bool exists = await _Sdk.Backend.Exists(identifier);
                Console.WriteLine($"Backend '{identifier}' exists: {exists}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking backend existence: {ex.Message}");
            }
        }

        private static async Task TestCreateBackend()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Creating new backend...");

                Backend request = BuildObject<Backend>();

                if (string.IsNullOrEmpty(request.Identifier) || string.IsNullOrEmpty(request.Name))
                {
                    Console.WriteLine("Backend identifier and name are required.");
                    return;
                }

                Console.WriteLine($"Creating backend '{request.Identifier}'...");
                EnumerateResponse(await _Sdk.Backend.Create(request));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating backend: {ex.Message}");
            }
        }

        private static async Task TestUpdateBackend()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                string identifier = Inputty.GetString("Backend identifier to update:", null, false);
                if (string.IsNullOrEmpty(identifier))
                {
                    Console.WriteLine("Backend identifier is required.");
                    return;
                }

                Backend request = BuildObject<Backend>();
                Console.WriteLine($"Updating backend '{request.Identifier}'...");
                EnumerateResponse(await _Sdk.Backend.Update(identifier, request));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating backend: {ex.Message}");
            }
        }

        private static async Task TestDeleteBackend()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                string identifier = Inputty.GetString("Backend identifier to delete:", null, false);
                if (string.IsNullOrEmpty(identifier))
                {
                    Console.WriteLine("Backend identifier is required.");
                    return;
                }

                bool confirm = Inputty.GetBoolean($"Are you sure you want to delete backend '{identifier}'?", false);
                if (!confirm)
                {
                    Console.WriteLine("Delete cancelled.");
                    return;
                }

                Console.WriteLine($"Deleting backend: {identifier}...");
                bool success = await _Sdk.Backend.Delete(identifier);

                if (success)
                {
                    Console.WriteLine("Backend deleted successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to delete backend.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting backend: {ex.Message}");
            }
        }

        #endregion

        #region Frontend-admin-Tests

        private static async Task TestListFrontends()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Listing all frontends...");
                var frontends = await _Sdk.Frontend.RetrieveMany();

                if (frontends != null && frontends.Count > 0)
                {
                    Console.WriteLine($"Found {frontends.Count} frontends:");
                    EnumerateResponse(frontends);
                }
                else
                {
                    Console.WriteLine("No frontends found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing frontends: {ex.Message}");
            }
        }

        private static async Task TestGetFrontend()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                string identifier = Inputty.GetString("Frontend identifier:", null, false);
                if (string.IsNullOrEmpty(identifier))
                {
                    Console.WriteLine("Frontend identifier is required.");
                    return;
                }

                Console.WriteLine($"Getting frontend: {identifier}...");
                var frontend = await _Sdk.Frontend.Retrieve(identifier);

                if (frontend != null)
                {
                    Console.WriteLine("Frontend found:");
                    EnumerateResponse(frontend);
                }
                else
                {
                    Console.WriteLine("Frontend not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting frontend: {ex.Message}");
            }
        }

        private static async Task TestFrontendExists()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                string identifier = Inputty.GetString("Frontend identifier:", null, false);
                if (string.IsNullOrEmpty(identifier))
                {
                    Console.WriteLine("Frontend identifier is required.");
                    return;
                }

                Console.WriteLine($"Checking if frontend exists: {identifier}...");
                bool exists = await _Sdk.Frontend.Exists(identifier);
                Console.WriteLine($"Frontend '{identifier}' exists: {exists}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking frontend existence: {ex.Message}");
            }
        }

        private static async Task TestCreateFrontend()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Creating new frontend...");

                Frontend request = BuildObject<Frontend>();

                if (string.IsNullOrEmpty(request.Identifier) || string.IsNullOrEmpty(request.Name))
                {
                    Console.WriteLine("Frontend identifier and name are required.");
                    return;
                }

                Console.WriteLine($"Creating frontend '{request.Identifier}'...");
                EnumerateResponse(await _Sdk.Frontend.Create(request));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating frontend: {ex.Message}");
            }
        }

        private static async Task TestUpdateFrontend()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                string identifier = Inputty.GetString("Frontend identifier to update:", null, false);
                if (string.IsNullOrEmpty(identifier))
                {
                    Console.WriteLine("Frontend identifier is required.");
                    return;
                }

                Frontend request = BuildObject<Frontend>();
                Console.WriteLine($"Updating frontend '{request.Identifier}'...");
                EnumerateResponse(await _Sdk.Frontend.Update(identifier, request));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating frontend: {ex.Message}");
            }
        }

        private static async Task TestDeleteFrontend()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                string identifier = Inputty.GetString("Frontend identifier to delete:", null, false);
                if (string.IsNullOrEmpty(identifier))
                {
                    Console.WriteLine("Frontend identifier is required.");
                    return;
                }

                bool confirm = Inputty.GetBoolean($"Are you sure you want to delete frontend '{identifier}'?", false);
                if (!confirm)
                {
                    Console.WriteLine("Delete cancelled.");
                    return;
                }

                Console.WriteLine($"Deleting frontend: {identifier}...");
                bool success = await _Sdk.Frontend.Delete(identifier);

                if (success)
                {
                    Console.WriteLine("Frontend deleted successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to delete frontend.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting frontend: {ex.Message}");
            }
        }

        #endregion

        #region OpenAI-API-Tests

        private static async Task TestOpenAICompletion()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Testing OpenAI-compatible text completion...");
                var request = BuildObject<OpenAIGenerateCompletionRequest>();
                EnumerateResponse(await _Sdk.OpenAI.GenerateCompletion(request));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating OpenAI completion: {ex.Message}");
            }
        }

        private static async Task TestOpenAIChatCompletion()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Testing OpenAI-compatible chat completion...");
                var request = BuildObject<OpenAIGenerateChatCompletionRequest>();
                EnumerateResponse(await _Sdk.OpenAI.GenerateChatCompletion(request));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating OpenAI chat completion: {ex.Message}");
            }
        }

        private static async Task TestOpenAIEmbeddingsSingle()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Testing OpenAI-compatible single embeddings...");
                var request = BuildObject<OpenAIGenerateEmbeddingsRequest>();
                EnumerateResponse(await _Sdk.OpenAI.GenerateEmbeddings(request));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating OpenAI embeddings: {ex.Message}");
            }
        }

        private static async Task TestOpenAIEmbeddingsMultiple()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Testing OpenAI-compatible multiple embeddings...");
                var request = BuildObject<OpenAIGenerateEmbeddingsRequest>();
                EnumerateResponse(await _Sdk.OpenAI.GenerateEmbeddings(request));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating OpenAI multiple embeddings: {ex.Message}");
            }
        }

        private static async Task TestOpenAIStreamingCompletion()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Testing OpenAI-compatible streaming text completion...");
                var request = BuildObject<OpenAIGenerateCompletionRequest>();

                Console.Write("Completion: ");
                await foreach (var chunk in _Sdk.OpenAI.GenerateCompletionStream(request))
                {
                    if (chunk?.Choices != null && chunk.Choices.Count > 0)
                    {
                        var text = chunk.Choices[0]?.Text;
                        if (!string.IsNullOrEmpty(text))
                            Console.Write(text);
                    }
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error streaming OpenAI completion: {ex.Message}");
            }
        }

        private static async Task TestOpenAIStreamingChatCompletion()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Testing OpenAI-compatible streaming chat completion...");
                var request = BuildObject<OpenAIGenerateChatCompletionRequest>();

                Console.Write("Assistant: ");
                await foreach (var chunk in _Sdk.OpenAI.GenerateChatCompletionStream(request))
                {
                    if (chunk?.Choices != null && chunk.Choices.Count > 0)
                    {
                        var contentObj = chunk.Choices[0]?.Delta?.Content;
                        string? deltaText = null;
                        if (contentObj is string s)
                        {
                            deltaText = s;
                        }
                        else if (contentObj is JsonElement je)
                        {
                            if (je.ValueKind == JsonValueKind.String)
                                deltaText = je.GetString();
                            else
                                deltaText = je.ToString();
                        }

                        if (!string.IsNullOrEmpty(deltaText))
                            Console.Write(deltaText);
                    }
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error streaming OpenAI chat completion: {ex.Message}");
            }
        }

        #endregion

        #region Ollama-API-Tests

        private static async Task SetCurrentModel()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                var models = await _Sdk.Ollama.ListLocalModels();
                if (models == null || models.Count == 0)
                {
                    Console.WriteLine("No models available. Use 'pull' command to download a model.");
                    return;
                }

                Console.WriteLine("Available models:");
                for (int i = 0; i < models.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {models[i].Name}");
                }

                int selection = Inputty.GetInteger("Select model number:", 1, true, true);
                if (selection > 0 && selection <= models.Count)
                {
                    _CurrentModel = models[selection - 1].Name;
                    Console.WriteLine($"Current model set to: {_CurrentModel}");
                }
                else
                {
                    Console.WriteLine("Invalid selection.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting current model: {ex.Message}");
            }
        }

        private static async Task TestListModels()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Listing available models...");
                var models = await _Sdk.Ollama.ListLocalModels();

                if (models != null && models.Count > 0)
                {
                    Console.WriteLine($"Found {models.Count} models:");
                    foreach (var model in models)
                    {
                        Console.WriteLine($"  - {model.Name} (Size: {model.Size} bytes)");
                        if (model.Details != null)
                        {
                            Console.WriteLine($"    Format: {model.Details.Format}");
                            Console.WriteLine($"    Family: {model.Details.Family}");
                            Console.WriteLine($"    Parameter Size: {model.Details.ParameterSize}");
                        }
                    }

                    if (string.IsNullOrEmpty(_CurrentModel))
                    {
                        _CurrentModel = models[0].Name;
                        Console.WriteLine($"Auto-selected model: {_CurrentModel}");
                    }
                }
                else
                {
                    Console.WriteLine("No models found. Use 'pull' command to download a model.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing models: {ex.Message}");
            }
        }

        private static async Task TestListRunningModels()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("Listing running models...");
                var runningModels = await _Sdk.Ollama.ListRunningModels();

                if (runningModels != null && runningModels.Count > 0)
                {
                    Console.WriteLine($"Found {runningModels.Count} running models:");
                    foreach (var model in runningModels)
                    {
                        Console.WriteLine($"  - {model.Name}");
                        Console.WriteLine($"    Expires: {model.ExpiresAt}");
                        Console.WriteLine($"    Size: {model.Size} bytes");
                        if (model.Details != null)
                        {
                            Console.WriteLine($"    Format: {model.Details.Format}");
                            Console.WriteLine($"    Family: {model.Details.Family}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No running models found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing running models: {ex.Message}");
            }
        }

        private static async Task TestShowModelInfo()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                string modelName = Inputty.GetString("Model name for info:", _CurrentModel, false);
                if (string.IsNullOrEmpty(modelName))
                {
                    Console.WriteLine("Model name is required.");
                    return;
                }

                Console.WriteLine($"Getting model information for: {modelName}...");
                var request = new OllamaShowModelInfoRequest { Model = modelName };
                var result = await _Sdk.Ollama.ShowModelInfo(request);
                var response = JsonSerializer.Serialize(result);
                if (result != null)
                {
                    Console.WriteLine("Raw JSON Response from SDK:");
                    Console.WriteLine("===========================");
                    Console.WriteLine(response);
                }
                else
                {
                    Console.WriteLine("No response received.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting model info: {ex.Message}");
            }
        }

        private static async Task TestPullModel()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                string modelName = Inputty.GetString("Model name to pull (e.g., llama2):", null, false);
                if (string.IsNullOrEmpty(modelName))
                {
                    Console.WriteLine("Model name is required.");
                    return;
                }

                Console.WriteLine($"Pulling model with streaming progress: {modelName}...");
                var request = new OllamaPullModelRequest { Model = modelName };

                Console.WriteLine("Download progress:");
                await foreach (var progress in _Sdk.Ollama.PullModel(request))
                {
                    if (!string.IsNullOrEmpty(progress.Status))
                    {
                        Console.Write($"\rStatus: {progress.Status}");
                        if (progress.IsDownloadProgress())
                        {
                            var progressStr = progress.GetFormattedProgress();
                            if (!string.IsNullOrEmpty(progressStr))
                                Console.Write($" - {progressStr}");
                        }
                        Console.Write("                    ");
                    }

                    if (progress.IsComplete())
                    {
                        Console.WriteLine($"\nPull completed successfully with status: {progress.Status}");
                        if (progress.IsDownloadProgress())
                        {
                            var progressStr = progress.GetFormattedProgress();
                            if (!string.IsNullOrEmpty(progressStr))
                                Console.WriteLine($"Final progress: {progressStr}");
                        }
                        if (string.IsNullOrEmpty(_CurrentModel))
                        {
                            _CurrentModel = modelName;
                            Console.WriteLine($"Auto-selected newly pulled model: {_CurrentModel}");
                        }
                        break;
                    }

                    if (progress.HasError())
                    {
                        Console.WriteLine($"\nError: {progress.Error}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error pulling model: {ex.Message}");
            }
        }

        private static async Task TestDeleteModel()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            try
            {
                string modelName = Inputty.GetString("Model name to delete:", null, false);
                if (string.IsNullOrEmpty(modelName))
                {
                    Console.WriteLine("Model name is required.");
                    return;
                }

                bool confirm = Inputty.GetBoolean($"Are you sure you want to delete '{modelName}'?", false);
                if (!confirm)
                {
                    Console.WriteLine("Delete cancelled.");
                    return;
                }

                Console.WriteLine($"Deleting model: {modelName}...");
                var request = new OllamaDeleteModelRequest { Model = modelName };
                await _Sdk.Ollama.DeleteModel(request);
                Console.WriteLine("Model deleted successfully.");

                if (_CurrentModel == modelName)
                {
                    _CurrentModel = null;
                    Console.WriteLine("Current model selection cleared.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting model: {ex.Message}");
            }
        }

        private static async Task TestOllamaTextCompletion()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            if (string.IsNullOrEmpty(_CurrentModel))
            {
                Console.WriteLine("No model selected. Use 'model' command to select a model first.");
                return;
            }

            try
            {
                string modelName = _CurrentModel;
                float temperature = (float)Inputty.GetDecimal("Temperature (0.0-1.0):", 0.7m, true, true);
                int numPredict = Inputty.GetInteger("Number of tokens to predict:", 100, true, false);

                Console.WriteLine($"Starting non-streaming text completion session with model '{modelName}'...");
                Console.WriteLine("Type 'q' to exit the conversation.");
                Console.WriteLine();

                bool continueConversation = true;
                while (continueConversation)
                {
                    string prompt = Inputty.GetString("Prompt:", null, false);
                    if (string.IsNullOrEmpty(prompt))
                    {
                        Console.WriteLine("Prompt cannot be empty. Please try again.");
                        continue;
                    }
                    if (prompt.ToLower() == "q")
                    {
                        Console.WriteLine("Exiting text completion session.");
                        break;
                    }

                    Console.WriteLine($"Generating completion with model '{modelName}'...");
                    var request = new OllamaGenerateCompletion
                    {
                        Model = modelName,
                        Prompt = prompt,
                        Options = new OllamaCompletionOptions
                        {
                            Temperature = temperature,
                            NumPredict = numPredict
                        }
                    };

                    var result = await _Sdk.Ollama.GenerateCompletion(request);
                    if (result != null)
                    {
                        Console.WriteLine($"Completion: {result.Response}");
                    }
                    else
                    {
                        Console.WriteLine("No completion result received.");
                    }

                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating completion: {ex.Message}");
            }
        }

        private static async Task TestOllamaChatCompletion()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            if (string.IsNullOrEmpty(_CurrentModel))
            {
                Console.WriteLine("No model selected. Use 'model' command to select a model first.");
                return;
            }

            try
            {
                string modelName = _CurrentModel;
                float temperature = (float)Inputty.GetDecimal("Temperature (0.0-1.0):", 0.7m, true, true);
                int numPredict = Inputty.GetInteger("Number of tokens to predict:", 100, true, false);

                Console.WriteLine($"Starting non-streaming chat completion session with model '{modelName}'...");
                Console.WriteLine("Type 'q' to exit the conversation.");
                Console.WriteLine();

                var conversationHistory = new List<OllamaChatMessage>();
                bool continueConversation = true;
                while (continueConversation)
                {
                    string userMessage = Inputty.GetString("User message:", null, false);
                    if (string.IsNullOrEmpty(userMessage))
                    {
                        Console.WriteLine("Message cannot be empty. Please try again.");
                        continue;
                    }
                    if (userMessage.ToLower() == "q")
                    {
                        Console.WriteLine("Exiting chat completion session.");
                        break;
                    }

                    conversationHistory.Add(new OllamaChatMessage { Role = "user", Content = userMessage });

                    Console.WriteLine($"Generating chat completion with model '{modelName}'...");
                    var request = new OllamaGenerateChatCompletionRequest
                    {
                        Model = modelName,
                        Messages = conversationHistory,
                        Options = new OllamaCompletionOptions
                        {
                            Temperature = temperature,
                            NumPredict = numPredict
                        }
                    };

                    var result = await _Sdk.Ollama.GenerateChatCompletion(request);
                    if (result?.Message != null)
                    {
                        Console.WriteLine($"Assistant: {result.Message.Content}");
                        conversationHistory.Add(new OllamaChatMessage { Role = "assistant", Content = result.Message.Content });
                    }
                    else
                    {
                        Console.WriteLine("No chat completion result received.");
                    }

                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating chat completion: {ex.Message}");
            }
        }

        private static async Task TestOllamaStreamingTextCompletion()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            if (string.IsNullOrEmpty(_CurrentModel))
            {
                Console.WriteLine("No model selected. Use 'model' command to select a model first.");
                return;
            }

            try
            {
                string modelName = _CurrentModel;
                float temperature = (float)Inputty.GetDecimal("Temperature (0.0-1.0):", 0.7m, true, true);
                int numPredict = Inputty.GetInteger("Number of tokens to predict:", 100, true, false);

                Console.WriteLine($"Starting streaming text completion session with model '{modelName}'...");
                Console.WriteLine("Type 'q' to exit the conversation.");
                Console.WriteLine();

                bool continueConversation = true;
                while (continueConversation)
                {
                    string prompt = Inputty.GetString("Prompt:", null, false);
                    if (string.IsNullOrEmpty(prompt))
                    {
                        Console.WriteLine("Prompt cannot be empty. Please try again.");
                        continue;
                    }
                    if (prompt.ToLower() == "q")
                    {
                        Console.WriteLine("Exiting streaming text completion session.");
                        break;
                    }

                    Console.WriteLine($"Generating streaming completion with model '{modelName}'...");
                    var request = new OllamaGenerateCompletion
                    {
                        Model = modelName,
                        Prompt = prompt,
                        Options = new OllamaCompletionOptions
                        {
                            Temperature = temperature,
                            NumPredict = numPredict
                        }
                    };
                    Console.Write("Completion: ");

                    await foreach (var chunk in _Sdk.Ollama.GenerateCompletionStream(request))
                    {
                        if (chunk.Response != null)
                        {
                            Console.Write(chunk.Response);
                        }
                    }
                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating streaming completion: {ex.Message}");
            }
        }

        private static async Task TestOllamaStreamingChatCompletion()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            if (string.IsNullOrEmpty(_CurrentModel))
            {
                Console.WriteLine("No model selected. Use 'model' command to select a model first.");
                return;
            }

            try
            {
                string modelName = _CurrentModel;
                float temperature = (float)Inputty.GetDecimal("Temperature (0.0-1.0):", 0.7m, true, true);
                int numPredict = Inputty.GetInteger("Number of tokens to predict:", 100, true, false);

                Console.WriteLine($"Starting streaming chat completion session with model '{modelName}'...");
                Console.WriteLine("Type 'q' to exit the conversation.");
                Console.WriteLine();

                var conversationHistory = new List<OllamaChatMessage>();
                bool continueConversation = true;
                while (continueConversation)
                {
                    string userMessage = Inputty.GetString("User message:", null, false);
                    if (string.IsNullOrEmpty(userMessage))
                    {
                        Console.WriteLine("Message cannot be empty. Please try again.");
                        continue;
                    }
                    if (userMessage.ToLower() == "q")
                    {
                        Console.WriteLine("Exiting streaming chat completion session.");
                        break;
                    }

                    conversationHistory.Add(new OllamaChatMessage { Role = "user", Content = userMessage });

                    Console.WriteLine($"Generating streaming chat completion with model '{modelName}'...");
                    var request = new OllamaGenerateChatCompletionRequest
                    {
                        Model = modelName,
                        Messages = conversationHistory,
                        Options = new OllamaCompletionOptions
                        {
                            Temperature = temperature,
                            NumPredict = numPredict
                        }
                    };

                    Console.Write("Assistant: ");

                    string assistantResponse = "";
                    await foreach (var chunk in _Sdk.Ollama.GenerateChatCompletionStream(request))
                    {
                        if (chunk.Message?.Content != null)
                        {
                            Console.Write(chunk.Message.Content);
                            assistantResponse += chunk.Message.Content;
                        }
                    }
                    Console.WriteLine();

                    if (!string.IsNullOrEmpty(assistantResponse))
                        conversationHistory.Add(new OllamaChatMessage { Role = "assistant", Content = assistantResponse });

                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating streaming chat completion: {ex.Message}");
            }
        }

        private static async Task TestOllamaEmbeddingsSingle()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            if (string.IsNullOrEmpty(_CurrentModel))
            {
                Console.WriteLine("No model selected. Use 'model' command to select a model first.");
                return;
            }

            try
            {
                string input = Inputty.GetString("Input text for embeddings:", "This is a test sentence for generating embeddings.", false);

                Console.WriteLine($"Generating Ollama embeddings for single text with model '{_CurrentModel}'...");
                var request = new OllamaGenerateEmbeddingsRequest { Model = _CurrentModel, Input = input };
                var result = await _Sdk.Ollama.GenerateEmbeddings(request);
                if (result?.Embeddings != null)
                {
                    var embedding = result.GetEmbedding();
                    if (embedding != null)
                    {
                        Console.WriteLine($"Generated embeddings with {embedding.Count} dimensions");
                        Console.WriteLine($"[{string.Join(", ", embedding)}]");
                    }
                }
                else
                {
                    Console.WriteLine("No embeddings result received.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating Ollama embeddings: {ex.Message}");
            }
        }

        private static async Task TestOllamaEmbeddingsMultiple()
        {
            if (_Sdk == null)
            {
                Console.WriteLine("SDK not initialized.");
                return;
            }

            if (string.IsNullOrEmpty(_CurrentModel))
            {
                Console.WriteLine("No model selected. Use 'model' command to select a model first.");
                return;
            }

            try
            {
                Console.WriteLine("Enter multiple texts for embeddings (press ENTER on empty line to finish):");
                var inputs = new List<string>();
                while (true)
                {
                    string input = Inputty.GetString($"Text {inputs.Count + 1}:", null, true);
                    if (string.IsNullOrEmpty(input)) break;
                    inputs.Add(input);
                }
                if (inputs.Count == 0)
                {
                    Console.WriteLine("No texts provided.");
                    return;
                }

                Console.WriteLine($"Generating Ollama embeddings for {inputs.Count} texts with model '{_CurrentModel}'...");
                var request = new OllamaGenerateEmbeddingsRequest { Model = _CurrentModel };
                request.SetInputs(inputs);
                var result = await _Sdk.Ollama.GenerateEmbeddings(request);
                if (result != null)
                {
                    var embeddings = result.GetEmbeddings();
                    if (embeddings != null && embeddings.Count > 0)
                    {
                        Console.WriteLine($"Generated {embeddings.Count} embeddings");
                        for (int i = 0; i < embeddings.Count; i++)
                        {
                            Console.WriteLine($"Text {i + 1}: \"{inputs[i]}\"");
                            Console.WriteLine($"Embedding dimensions: {embeddings[i].Count}");
                            Console.WriteLine($"values: [{string.Join(", ", embeddings[i])}]");
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        Console.WriteLine("No embeddings data found in result.");
                    }
                }
                else
                {
                    Console.WriteLine("No embeddings result received.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating Ollama embeddings: {ex.Message}");
            }
        }

        #endregion
        
        private static T BuildObject<T>()
        {
            string json = Inputty.GetString("JSON :", null, false);
            return _Serializer.DeserializeJson<T>(json);
        }

        private static void EnumerateResponse(object? obj)
        {
            Console.WriteLine("");
            Console.Write("Response:");
            if (obj != null)
                Console.WriteLine(Environment.NewLine + _Serializer.SerializeJson(obj, true));
            else
                Console.WriteLine("(null)");
            Console.WriteLine("");
        }
    }
}

