namespace Test.Ollama
{
    using System.Text;
    using System.Text.Json;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using RestWrapper;
    using HttpMethod = System.Net.Http.HttpMethod;

    /// <summary>
    /// Tests for Ollama API compatibility using a mock Ollama server.
    /// Tests various Ollama endpoints and response formats.
    /// </summary>
    public static class TestOllama
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

        private static Webserver? _mockOllamaServer;
        private static int _mockServerPort;
        private static string _mockServerUrl = "";
        private const int TestTimeoutMs = 30000;

        /// <summary>
        /// Main entry point for running Ollama API tests as a console application.
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== OllamaFlow Mock Ollama Server Tests ===");

            try
            {
                Setup();

                await RunAllTests();

                Console.WriteLine("\n=== All Tests Completed ===");
                Console.WriteLine("✅ All Ollama API tests passed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Test execution failed:{Environment.NewLine}{ex.ToString()}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
            finally
            {
                Cleanup();
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void Setup()
        {
            _mockServerPort = GetAvailablePort();
            _mockServerUrl = $"http://localhost:{_mockServerPort}";

            WebserverSettings settings = new WebserverSettings
            {
                Hostname = "localhost",
                Port = _mockServerPort
            };

            _mockOllamaServer = new Webserver(settings, DefaultRoute);
            _mockOllamaServer.Start();

            // Wait a moment for server to start
            Thread.Sleep(100);

            Console.WriteLine($"Mock Ollama server started on {_mockServerUrl}");
        }

        private static void Cleanup()
        {
            _mockOllamaServer?.Dispose();
            Console.WriteLine("Mock server disposed");
        }

        private static async Task RunAllTests()
        {
            await ChatCompletion_NonStreaming_ReturnsValidResponse();
            await ChatCompletion_Streaming_ReturnsChunkedResponse();
            await ChatCompletion_WithOptions_HandlesParameters();
            await Generate_NonStreaming_ReturnsValidResponse();
            await Generate_Streaming_ReturnsChunkedResponse();
            await ListModels_ReturnsValidModelList();
            await ShowModel_ReturnsModelDetails();
            await GenerateEmbeddings_SinglePrompt_ReturnsValidEmbeddings();
            await GenerateEmbeddings_BatchPrompts_ReturnsValidEmbeddings();
            await RootEndpoint_ReturnsOllamaResponse();
            await HeadRequest_ReturnsOkStatus();
            await InvalidModel_ReturnsError();
            await MalformedRequest_ReturnsBadRequest();
        }

        #region Chat Completion Tests

        public static async Task ChatCompletion_NonStreaming_ReturnsValidResponse()
        {
            Console.WriteLine("\n=== Ollama Chat Completion (Non-Streaming) Test ===");

            // Arrange
            var chatRequest = new
            {
                model = "llama2",
                messages = new[]
                {
                    new { role = "user", content = "What is 2+2?" }
                },
                stream = false
            };

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/api/chat");
            Console.WriteLine("REQUEST BODY:");
            string requestJson = JsonSerializer.Serialize(chatRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(requestJson);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/api/chat", HttpMethod.Post);
            string requestBody = JsonSerializer.Serialize(chatRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Content Type: {response.ContentType}");
            Console.WriteLine("RESPONSE BODY:");
            Console.WriteLine(response.DataAsString);

            // Assert
            AssertEqual(200, response.StatusCode);
            AssertEqual("application/json", response.ContentType);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            Console.WriteLine("\nRESPONSE VALIDATION:");
            Console.WriteLine($"Model: {root.GetProperty("model").GetString()}");
            Console.WriteLine($"Done: {root.GetProperty("done").GetBoolean()}");

            AssertTrue(root.TryGetProperty("message", out JsonElement message));
            Console.WriteLine($"Message Role: {message.GetProperty("role").GetString()}");
            string content = message.GetProperty("content").GetString();
            Console.WriteLine($"Message Content: {content}");

            AssertEqual("llama2", root.GetProperty("model").GetString());
            AssertTrue(root.GetProperty("done").GetBoolean());
            AssertEqual("assistant", message.GetProperty("role").GetString());
            AssertFalse(string.IsNullOrEmpty(content));

            Console.WriteLine("✅ Test completed successfully!");
        }

        public static async Task ChatCompletion_Streaming_ReturnsChunkedResponse()
        {
            Console.WriteLine("\n=== Ollama Chat Completion (Streaming) Test ===");

            // Arrange
            var chatRequest = new
            {
                model = "llama2",
                messages = new[]
                {
                    new { role = "user", content = "Tell me a short story" }
                },
                stream = true
            };

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/api/chat");
            Console.WriteLine("REQUEST BODY:");
            string requestJson = JsonSerializer.Serialize(chatRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(requestJson);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/api/chat", HttpMethod.Post);
            string requestBody = JsonSerializer.Serialize(chatRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Content Type: {response.ContentType}");

            // Assert
            AssertEqual(200, response.StatusCode);
            AssertEqual("application/x-ndjson", response.ContentType);

            string responseText = response.DataAsString;
            AssertNotEmpty(responseText);

            Console.WriteLine("STREAMING RESPONSE BODY (NDJSON):");
            Console.WriteLine(responseText);

            // Parse streaming chunks
            string[] chunks = responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine($"\nParsed {chunks.Length} streaming chunks:");

            for (int i = 0; i < chunks.Length && i < 10; i++) // Show first 10 chunks
            {
                Console.WriteLine($"Chunk {i + 1}: {chunks[i]}");
            }
            if (chunks.Length > 10)
            {
                Console.WriteLine($"... and {chunks.Length - 10} more chunks");
            }

            AssertTrue(chunks.Length > 1, "Should have multiple chunks");

            // Verify first chunk
            JsonDocument firstChunk = JsonDocument.Parse(chunks[0]);
            JsonElement firstRoot = firstChunk.RootElement;
            Console.WriteLine($"\nFirst Chunk Analysis:");
            Console.WriteLine($"Model: {firstRoot.GetProperty("model").GetString()}");
            Console.WriteLine($"Done: {firstRoot.GetProperty("done").GetBoolean()}");

            AssertEqual("llama2", firstRoot.GetProperty("model").GetString());
            AssertFalse(firstRoot.GetProperty("done").GetBoolean());

            // Verify last chunk
            JsonDocument lastChunk = JsonDocument.Parse(chunks.Last());
            JsonElement lastRoot = lastChunk.RootElement;
            Console.WriteLine($"\nLast Chunk Analysis:");
            Console.WriteLine($"Model: {lastRoot.GetProperty("model").GetString()}");
            Console.WriteLine($"Done: {lastRoot.GetProperty("done").GetBoolean()}");

            AssertTrue(lastRoot.GetProperty("done").GetBoolean());

            Console.WriteLine("✅ Streaming test completed successfully!");
        }

        public static async Task ChatCompletion_WithOptions_HandlesParameters()
        {
            Console.WriteLine("\n=== Ollama Chat Completion with Options Test ===");

            // Arrange
            var chatRequest = new
            {
                model = "llama2",
                messages = new[]
                {
                    new { role = "user", content = "Hello" }
                },
                stream = false,
                options = new
                {
                    temperature = 0.8,
                    top_p = 0.9,
                    top_k = 40,
                    repeat_penalty = 1.1,
                    seed = 12345,
                    num_ctx = 2048,
                    num_predict = 100
                }
            };

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/api/chat", HttpMethod.Post);
            string requestBody = JsonSerializer.Serialize(chatRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            // Assert
            AssertEqual(200, response.StatusCode);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            AssertEqual("llama2", root.GetProperty("model").GetString());
            AssertTrue(root.GetProperty("done").GetBoolean());

            // Verify the mock server received the options (this would be implementation-specific)
            AssertTrue(root.TryGetProperty("message", out JsonElement message));
            AssertEqual("assistant", message.GetProperty("role").GetString());

            Console.WriteLine("✅ Test completed successfully!");
        }

        #endregion

        #region Generate Completion Tests

        public static async Task Generate_NonStreaming_ReturnsValidResponse()
        {
            Console.WriteLine("\n=== Ollama Generate Completion (Non-Streaming) Test ===");

            // Arrange
            var generateRequest = new
            {
                model = "codellama",
                prompt = "def fibonacci(n):",
                stream = false
            };

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/api/generate", HttpMethod.Post);
            string requestBody = JsonSerializer.Serialize(generateRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            // Assert
            AssertEqual(200, response.StatusCode);
            AssertEqual("application/json", response.ContentType);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            AssertEqual("codellama", root.GetProperty("model").GetString());
            AssertTrue(root.GetProperty("done").GetBoolean());
            AssertTrue(root.TryGetProperty("response", out JsonElement responseText));
            AssertFalse(string.IsNullOrEmpty(responseText.GetString()));

            Console.WriteLine("✅ Test completed successfully!");
        }

        public static async Task Generate_Streaming_ReturnsChunkedResponse()
        {
            Console.WriteLine("\n=== Ollama Generate Completion (Streaming) Test ===");

            // Arrange
            var generateRequest = new
            {
                model = "codellama",
                prompt = "Write a Python function to calculate factorial",
                stream = true
            };

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/api/generate", HttpMethod.Post);
            string requestBody = JsonSerializer.Serialize(generateRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            // Assert
            AssertEqual(200, response.StatusCode);
            AssertEqual("application/x-ndjson", response.ContentType);

            string responseText = response.DataAsString;
            string[] chunks = responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            AssertTrue(chunks.Length > 1, "Should have multiple chunks");

            // Verify structure of chunks
            JsonDocument firstChunk = JsonDocument.Parse(chunks[0]);
            JsonElement firstRoot = firstChunk.RootElement;
            AssertEqual("codellama", firstRoot.GetProperty("model").GetString());
            AssertFalse(firstRoot.GetProperty("done").GetBoolean());
            AssertTrue(firstRoot.TryGetProperty("response", out _));

            JsonDocument lastChunk = JsonDocument.Parse(chunks.Last());
            JsonElement lastRoot = lastChunk.RootElement;
            AssertTrue(lastRoot.GetProperty("done").GetBoolean());

            Console.WriteLine("✅ Test completed successfully!");
        }

        #endregion

        #region Model Management Tests

        public static async Task ListModels_ReturnsValidModelList()
        {
            Console.WriteLine("\n=== Ollama List Models Test ===");

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/api/tags", HttpMethod.Get);
            RestResponse response = await client.SendAsync();

            // Assert
            AssertEqual(200, response.StatusCode);
            AssertEqual("application/json", response.ContentType);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            AssertTrue(root.TryGetProperty("models", out var models));
            List<JsonElement> modelArray = models.EnumerateArray().ToList();
            AssertNotEmpty(modelArray);

            // Verify each model has required fields
            foreach (var model in modelArray)
            {
                AssertTrue(model.TryGetProperty("name", out _));
                AssertTrue(model.TryGetProperty("size", out _));
                AssertTrue(model.TryGetProperty("digest", out _));
                AssertTrue(model.TryGetProperty("modified_at", out _));
            }

            Console.WriteLine("✅ Test completed successfully!");
        }

        public static async Task ShowModel_ReturnsModelDetails()
        {
            Console.WriteLine("\n=== Ollama Show Model Test ===");

            // Arrange
            var showRequest = new
            {
                name = "llama2"
            };

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/api/show", HttpMethod.Post);
            string requestBody = JsonSerializer.Serialize(showRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            // Assert
            AssertEqual(200, response.StatusCode);
            AssertEqual("application/json", response.ContentType);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            AssertTrue(root.TryGetProperty("modelfile", out _));
            AssertTrue(root.TryGetProperty("parameters", out _));
            AssertTrue(root.TryGetProperty("template", out _));
            AssertTrue(root.TryGetProperty("details", out var details));

            // Verify details structure
            AssertTrue(details.TryGetProperty("format", out _));
            AssertTrue(details.TryGetProperty("family", out _));
            AssertTrue(details.TryGetProperty("parameter_size", out _));

            Console.WriteLine("✅ Test completed successfully!");
        }

        #endregion

        #region Embedding Tests

        public static async Task GenerateEmbeddings_SinglePrompt_ReturnsValidEmbeddings()
        {
            Console.WriteLine("\n=== Ollama Generate Embeddings (Single Prompt) Test ===");

            // Arrange
            var embeddingRequest = new
            {
                model = "nomic-embed-text",
                prompt = "The quick brown fox jumps over the lazy dog"
            };

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/api/embeddings");
            Console.WriteLine("REQUEST BODY:");
            string requestJson = JsonSerializer.Serialize(embeddingRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(requestJson);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/api/embeddings", HttpMethod.Post);
            string requestBody = JsonSerializer.Serialize(embeddingRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Content Type: {response.ContentType}");
            Console.WriteLine("RESPONSE BODY:");
            string truncatedResponse = response.DataAsString.Length > 200 ? response.DataAsString.Substring(0, 200) + "..." : response.DataAsString;
            Console.WriteLine(truncatedResponse);

            // Assert
            AssertEqual(200, response.StatusCode);
            AssertEqual("application/json", response.ContentType);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            Console.WriteLine("\nRESPONSE VALIDATION:");
            AssertTrue(root.TryGetProperty("embedding", out var embedding));
            List<JsonElement> embeddingArray = embedding.EnumerateArray().ToList();
            Console.WriteLine($"Embedding Dimensions: {embeddingArray.Count}");
            AssertNotEmpty(embeddingArray);
            AssertTrue(embeddingArray.Count >= 384, "Should have at least 384 dimensions");

            // Verify all embedding values are numbers
            foreach (var value in embeddingArray)
            {
                AssertTrue(value.ValueKind == JsonValueKind.Number);
            }

            Console.WriteLine("✅ Test completed successfully!");
        }

        public static async Task GenerateEmbeddings_BatchPrompts_ReturnsValidEmbeddings()
        {
            Console.WriteLine("\n=== Ollama Generate Embeddings (Batch Prompts) Test ===");

            // Arrange
            var embeddingRequest = new
            {
                model = "nomic-embed-text",
                prompt = new[]
                {
                    "The quick brown fox jumps over the lazy dog",
                    "Machine learning is a subset of artificial intelligence",
                    "Natural language processing enables computers to understand human language"
                }
            };

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/api/embeddings");
            Console.WriteLine("REQUEST BODY:");
            string requestJson = JsonSerializer.Serialize(embeddingRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(requestJson);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/api/embeddings", HttpMethod.Post);
            string requestBody = JsonSerializer.Serialize(embeddingRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Content Type: {response.ContentType}");
            Console.WriteLine("RESPONSE BODY:");
            string truncatedResponse = response.DataAsString.Length > 300 ? response.DataAsString.Substring(0, 300) + "..." : response.DataAsString;
            Console.WriteLine(truncatedResponse);

            // Assert
            AssertEqual(200, response.StatusCode);
            AssertEqual("application/json", response.ContentType);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            Console.WriteLine("\nRESPONSE VALIDATION:");
            AssertTrue(root.TryGetProperty("embeddings", out var embeddings));
            List<JsonElement> embeddingsList = embeddings.EnumerateArray().ToList();
            Console.WriteLine($"Number of Embeddings: {embeddingsList.Count}");
            AssertEqual(3, embeddingsList.Count, "Should have 3 embeddings for 3 prompts");

            // Verify each embedding
            for (int i = 0; i < embeddingsList.Count; i++)
            {
                List<JsonElement> embeddingArray = embeddingsList[i].EnumerateArray().ToList();
                Console.WriteLine($"Embedding {i + 1} Dimensions: {embeddingArray.Count}");
                AssertNotEmpty(embeddingArray);
                AssertTrue(embeddingArray.Count >= 384, "Each embedding should have at least 384 dimensions");

                // Verify all embedding values are numbers
                foreach (var value in embeddingArray)
                {
                    AssertTrue(value.ValueKind == JsonValueKind.Number);
                }
            }

            Console.WriteLine("✅ Test completed successfully!");
        }

        #endregion

        #region Health and Connectivity Tests

        public static async Task RootEndpoint_ReturnsOllamaResponse()
        {
            Console.WriteLine("\n=== Ollama Root Endpoint Test ===");

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/", HttpMethod.Get);
            RestResponse response = await client.SendAsync();

            // Assert
            AssertEqual(200, response.StatusCode);
            AssertTrue(response.DataAsString.Contains("Ollama"));

            Console.WriteLine("✅ Test completed successfully!");
        }

        public static async Task HeadRequest_ReturnsOkStatus()
        {
            Console.WriteLine("\n=== Ollama HEAD Request Test ===");

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/", HttpMethod.Head);
            RestResponse response = await client.SendAsync();

            // Assert
            AssertEqual(200, response.StatusCode);

            Console.WriteLine("✅ Test completed successfully!");
        }

        #endregion

        #region Error Handling Tests

        public static async Task InvalidModel_ReturnsError()
        {
            Console.WriteLine("\n=== Ollama Invalid Model Error Test ===");

            // Arrange
            var chatRequest = new
            {
                model = "nonexistent-model",
                messages = new[]
                {
                    new { role = "user", content = "Hello" }
                }
            };

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/api/chat", HttpMethod.Post);
            string requestBody = JsonSerializer.Serialize(chatRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            // Assert
            AssertEqual(404, response.StatusCode);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            AssertTrue(root.TryGetProperty("error", out var error));
            AssertTrue(error.GetString().ToLower().Contains("model not found"));

            Console.WriteLine("✅ Test completed successfully!");
        }

        public static async Task MalformedRequest_ReturnsBadRequest()
        {
            Console.WriteLine("\n=== Ollama Malformed Request Test ===");

            // Arrange
            string malformedRequest = "{invalid json}";

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/api/chat", HttpMethod.Post);
            client.ContentType = "application/json";
            var response = await client.SendAsync(Encoding.UTF8.GetBytes(malformedRequest));

            // Assert
            AssertEqual(400, response.StatusCode);

            Console.WriteLine("✅ Test completed successfully!");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Default route handler for the mock Ollama server.
        /// Simulates Ollama API responses.
        /// </summary>
        private static async Task DefaultRoute(HttpContextBase ctx)
        {
            string path = ctx.Request.Url.RawWithoutQuery.ToLower();
            WatsonWebserver.Core.HttpMethod method = ctx.Request.Method;

            try
            {
                switch (path)
                {
                    case "/":
                        await HandleRootRequest(ctx);
                        break;
                    case "/api/chat":
                        await HandleChatRequest(ctx);
                        break;
                    case "/api/generate":
                        await HandleGenerateRequest(ctx);
                        break;
                    case "/api/tags":
                        await HandleTagsRequest(ctx);
                        break;
                    case "/api/show":
                        await HandleShowRequest(ctx);
                        break;
                    case "/api/embeddings":
                        await HandleEmbeddingsRequest(ctx);
                        break;
                    default:
                        ctx.Response.StatusCode = 404;
                        await ctx.Response.Send("Not Found");
                        break;
                }
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        private static async Task HandleRootRequest(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send("Ollama is running");
        }

        private static async Task HandleChatRequest(HttpContextBase ctx)
        {
            string requestBody = ctx.Request.DataAsString;
            if (string.IsNullOrEmpty(requestBody))
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(JsonSerializer.Serialize(new { error = "Empty request body" }));
                return;
            }

            JsonDocument request = JsonSerializer.Deserialize<JsonDocument>(requestBody);
            var root = request.RootElement;

            if (!root.TryGetProperty("model", out var modelElement))
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(JsonSerializer.Serialize(new { error = "Model is required" }));
                return;
            }

            string model = modelElement.GetString();
            if (model == "nonexistent-model")
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(JsonSerializer.Serialize(new { error = "Model not found" }));
                return;
            }

            bool isStreaming = root.TryGetProperty("stream", out JsonElement streamElement) &&
                             streamElement.GetBoolean();

            if (isStreaming)
            {
                await SendStreamingChatResponse(ctx, model);
            }
            else
            {
                await SendNonStreamingChatResponse(ctx, model);
            }
        }

        private static async Task SendNonStreamingChatResponse(HttpContextBase ctx, string model)
        {
            var response = new
            {
                model = model,
                created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                message = new
                {
                    role = "assistant",
                    content = "This is a mock response from the Ollama server."
                },
                done = true,
                total_duration = 2500000000L,
                load_duration = 500000000L,
                prompt_eval_count = 10,
                prompt_eval_duration = 800000000L,
                eval_count = 15,
                eval_duration = 1200000000L
            };

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(JsonSerializer.Serialize(response));
        }

        private static async Task SendStreamingChatResponse(HttpContextBase ctx, string model)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/x-ndjson";

            var chunks = new object[]
            {
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      message = new { role = "assistant", content = "" }, done = false },
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      message = new { role = "assistant", content = "This" }, done = false },
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      message = new { role = "assistant", content = " is" }, done = false },
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      message = new { role = "assistant", content = " a" }, done = false },
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      message = new { role = "assistant", content = " streaming" }, done = false },
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      message = new { role = "assistant", content = " response." }, done = false },
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      done = true, total_duration = 2500000000L, load_duration = 500000000L,
                      prompt_eval_count = 10, prompt_eval_duration = 800000000L,
                      eval_count = 15, eval_duration = 1200000000L }
            };

            StringBuilder responseBuilder = new StringBuilder();
            foreach (var chunk in chunks)
            {
                responseBuilder.AppendLine(JsonSerializer.Serialize(chunk));
            }

            await ctx.Response.Send(responseBuilder.ToString());
        }

        private static async Task HandleGenerateRequest(HttpContextBase ctx)
        {
            string requestBody = ctx.Request.DataAsString;
            JsonDocument request = JsonSerializer.Deserialize<JsonDocument>(requestBody);
            var root = request.RootElement;

            string model = root.GetProperty("model").GetString();
            bool isStreaming = root.TryGetProperty("stream", out JsonElement streamElement) &&
                             streamElement.GetBoolean();

            if (isStreaming)
            {
                await SendStreamingGenerateResponse(ctx, model);
            }
            else
            {
                await SendNonStreamingGenerateResponse(ctx, model);
            }
        }

        private static async Task SendNonStreamingGenerateResponse(HttpContextBase ctx, string model)
        {
            var response = new
            {
                model = model,
                created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                response = "    if n <= 1:\n        return n\n    else:\n        return fibonacci(n-1) + fibonacci(n-2)",
                done = true,
                context = new[] { 1, 2, 3, 4, 5 },
                total_duration = 3000000000L,
                load_duration = 600000000L,
                prompt_eval_count = 8,
                prompt_eval_duration = 900000000L,
                eval_count = 25,
                eval_duration = 1500000000L
            };

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(JsonSerializer.Serialize(response));
        }

        private static async Task SendStreamingGenerateResponse(HttpContextBase ctx, string model)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/x-ndjson";

            var chunks = new object[]
            {
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      response = "    if", done = false },
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      response = " n", done = false },
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      response = " <=", done = false },
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      response = " 1:", done = false },
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      response = "\n        return n", done = false },
                new { model = model, created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                      done = true, context = new[] { 1, 2, 3, 4, 5 },
                      total_duration = 3000000000L, load_duration = 600000000L,
                      prompt_eval_count = 8, prompt_eval_duration = 900000000L,
                      eval_count = 25, eval_duration = 1500000000L }
            };

            StringBuilder responseBuilder = new StringBuilder();
            foreach (var chunk in chunks)
            {
                responseBuilder.AppendLine(JsonSerializer.Serialize(chunk));
            }

            await ctx.Response.Send(responseBuilder.ToString());
        }

        private static async Task HandleTagsRequest(HttpContextBase ctx)
        {
            var response = new
            {
                models = new[]
                {
                    new { name = "llama2:latest", size = 3800000000L,
                          digest = "sha256:1234567890abcdef", modified_at = "2023-11-14T14:30:00.000Z" },
                    new { name = "codellama:latest", size = 3600000000L,
                          digest = "sha256:abcdef1234567890", modified_at = "2023-11-14T13:15:00.000Z" },
                    new { name = "nomic-embed-text:latest", size = 274000000L,
                          digest = "sha256:fedcba0987654321", modified_at = "2023-11-14T12:00:00.000Z" }
                }
            };

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(JsonSerializer.Serialize(response));
        }

        private static async Task HandleShowRequest(HttpContextBase ctx)
        {
            var response = new
            {
                modelfile = "FROM llama2:latest\nPARAMETER temperature 0.7\nSYSTEM \"You are a helpful assistant.\"",
                parameters = "temperature 0.7\ntop_p 0.9\nrepeat_penalty 1.1",
                template = "{{ .System }}\n\nUser: {{ .Prompt }}\n\nAssistant: ",
                details = new
                {
                    format = "gguf",
                    family = "llama",
                    families = new[] { "llama" },
                    parameter_size = "7B",
                    quantization_level = "Q4_0"
                }
            };

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(JsonSerializer.Serialize(response));
        }

        private static async Task HandleEmbeddingsRequest(HttpContextBase ctx)
        {
            string requestBody = ctx.Request.DataAsString;
            JsonDocument request = JsonSerializer.Deserialize<JsonDocument>(requestBody);
            JsonElement root = request.RootElement;

            Random random = new Random(42);

            // Check if prompt is a string or array
            if (root.TryGetProperty("prompt", out var promptElement))
            {
                if (promptElement.ValueKind == JsonValueKind.Array)
                {
                    // Batch embeddings
                    List<double[]> embeddingsList = new List<double[]>();
                    foreach (var prompt in promptElement.EnumerateArray())
                    {
                        double[] embedding = Enumerable.Range(0, 384)
                            .Select(_ => (random.NextDouble() - 0.5) * 2.0)
                            .ToArray();
                        embeddingsList.Add(embedding);
                    }

                    var response = new
                    {
                        embeddings = embeddingsList
                    };

                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(JsonSerializer.Serialize(response));
                }
                else
                {
                    // Single embedding
                    double[] embedding = Enumerable.Range(0, 384)
                        .Select(_ => (random.NextDouble() - 0.5) * 2.0)
                        .ToArray();

                    var response = new
                    {
                        embedding = embedding
                    };

                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(JsonSerializer.Serialize(response));
                }
            }
            else
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(JsonSerializer.Serialize(new { error = "Prompt is required" }));
            }
        }

        private static int GetAvailablePort()
        {
            using System.Net.Sockets.TcpListener socket = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, 0);
            socket.Start();
            int port = ((System.Net.IPEndPoint)socket.LocalEndpoint).Port;
            socket.Stop();
            return port;
        }

        #endregion

        #region Assertion Helpers

        private static void AssertEqual<T>(T expected, T actual, string? message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new Exception(message ?? $"Expected: {expected}, Actual: {actual}");
            }
        }

        private static void AssertTrue(bool condition, string? message = null)
        {
            if (!condition)
            {
                throw new Exception(message ?? "Assertion failed: condition was false");
            }
        }

        private static void AssertFalse(bool condition, string? message = null)
        {
            if (condition)
            {
                throw new Exception(message ?? "Assertion failed: condition was true");
            }
        }

        private static void AssertNotEmpty<T>(IEnumerable<T> collection)
        {
            if (!collection.Any())
            {
                throw new Exception("Collection should not be empty");
            }
        }

        #endregion

#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}