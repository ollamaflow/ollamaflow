namespace Test.OpenAI
{
    using System.Text;
    using System.Text.Json;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using RestWrapper;
    using HttpMethod = System.Net.Http.HttpMethod;

    /// <summary>
    /// Tests for OpenAI API compatibility using a mock OpenAI server.
    /// Tests various OpenAI endpoints and response formats with detailed logging.
    /// </summary>
    public static class TestOpenAI
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.

        private static Webserver? _mockOpenAIServer;
        private static int _mockServerPort;
        private static string _mockServerUrl = "";
        private const int TestTimeoutMs = 30000;

        /// <summary>
        /// Main entry point for running OpenAI API tests as a console application.
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== OllamaFlow Mock OpenAI Server Tests ===");

            try
            {
                Setup();

                await RunAllTests();

                Console.WriteLine("\n=== All Tests Completed ===");
                Console.WriteLine("✅ All OpenAI API tests passed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Test execution failed: {ex.Message}");
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

            _mockOpenAIServer = new Webserver(settings, DefaultRoute);
            _mockOpenAIServer.Start();

            // Wait a moment for server to start
            Thread.Sleep(100);

            Console.WriteLine($"Mock OpenAI server started on {_mockServerUrl}");
        }

        private static void Cleanup()
        {
            _mockOpenAIServer?.Dispose();
            Console.WriteLine("Mock server disposed");
        }

        private static async Task RunAllTests()
        {
            await ChatCompletion_NonStreaming_ReturnsValidResponse();
            await ChatCompletion_Streaming_ReturnsServerSentEvents();
            await ChatCompletion_WithOptions_HandlesParameters();
            await TextCompletion_NonStreaming_ReturnsValidResponse();
            await TextCompletion_Streaming_ReturnsServerSentEvents();
            await Embeddings_SingleInput_ReturnsValidEmbeddingVector();
            await Embeddings_BatchInputs_ReturnsValidEmbeddingVectors();
            await ListModels_ReturnsAvailableModels();
            await RetrieveModel_ReturnsModelDetails();
            await RootEndpoint_ReturnsHealthStatus();
            await InvalidModel_Returns404Error();
            await MalformedRequest_ReturnsBadRequest();
        }

        #region Chat Completion Tests

        public static async Task ChatCompletion_NonStreaming_ReturnsValidResponse()
        {
            Console.WriteLine("\n=== OpenAI Chat Completion (Non-Streaming) Test ===");

            // Arrange
            var chatRequest = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = "What is the capital of France?" }
                },
                temperature = 0.7,
                max_tokens = 100,
                stream = false
            };

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/v1/chat/completions");
            Console.WriteLine("REQUEST HEADERS:");
            Console.WriteLine("Authorization: Bearer mock-api-key");
            Console.WriteLine("Content-Type: application/json");
            Console.WriteLine("\nREQUEST BODY:");
            string requestJson = JsonSerializer.Serialize(chatRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(requestJson);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/v1/chat/completions", HttpMethod.Post);
            client.Authorization.BearerToken = "mock-api-key";
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
            Console.WriteLine($"Object: {root.GetProperty("object").GetString()}");
            Console.WriteLine($"Model: {root.GetProperty("model").GetString()}");
            Console.WriteLine($"ID: {root.GetProperty("id").GetString()}");

            AssertEqual("chat.completion", root.GetProperty("object").GetString());
            AssertEqual("gpt-3.5-turbo", root.GetProperty("model").GetString());

            List<JsonElement> choices = root.GetProperty("choices").EnumerateArray().ToList();
            Console.WriteLine($"Choices Count: {choices.Count}");

            JsonElement firstChoice = choices.First();
            JsonElement message = firstChoice.GetProperty("message");
            Console.WriteLine($"Message Role: {message.GetProperty("role").GetString()}");
            Console.WriteLine($"Message Content: {message.GetProperty("content").GetString()}");
            Console.WriteLine($"Finish Reason: {firstChoice.GetProperty("finish_reason").GetString()}");

            AssertNotEmpty(choices);
            AssertEqual("assistant", message.GetProperty("role").GetString());
            AssertFalse(string.IsNullOrEmpty(message.GetProperty("content").GetString()));

            // Check usage statistics
            JsonElement usage = root.GetProperty("usage");
            Console.WriteLine($"Usage - Prompt Tokens: {usage.GetProperty("prompt_tokens").GetInt32()}");
            Console.WriteLine($"Usage - Completion Tokens: {usage.GetProperty("completion_tokens").GetInt32()}");
            Console.WriteLine($"Usage - Total Tokens: {usage.GetProperty("total_tokens").GetInt32()}");

            Console.WriteLine("✅ Test completed successfully!");
        }

        public static async Task ChatCompletion_Streaming_ReturnsServerSentEvents()
        {
            Console.WriteLine("\n=== OpenAI Chat Completion (Streaming) Test ===");

            // Arrange
            var chatRequest = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "user", content = "Write a haiku about programming" }
                },
                temperature = 0.8,
                stream = true
            };

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/v1/chat/completions");
            Console.WriteLine("REQUEST HEADERS:");
            Console.WriteLine("Authorization: Bearer mock-api-key");
            Console.WriteLine("Content-Type: application/json");
            Console.WriteLine("\nREQUEST BODY:");
            string requestJson = JsonSerializer.Serialize(chatRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(requestJson);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/v1/chat/completions", HttpMethod.Post);
            client.Authorization.BearerToken = "mock-api-key";
            string requestBody = JsonSerializer.Serialize(chatRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Content Type: {response.ContentType}");

            // Assert
            AssertEqual(200, response.StatusCode);
            AssertEqual("text/plain", response.ContentType);

            string responseText = response.DataAsString;
            Console.WriteLine("STREAMING RESPONSE BODY (Server-Sent Events):");
            Console.WriteLine(responseText);

            // Parse Server-Sent Events
            string[] lines = responseText.Split('\n');
            List<string> chunks = new List<string>();

            foreach (string line in lines)
            {
                if (line.StartsWith("data: ") && line != "data: [DONE]")
                {
                    string jsonData = line.Substring(6); // Remove "data: "
                    chunks.Add(jsonData);
                }
            }

            Console.WriteLine($"\nParsed {chunks.Count} streaming chunks:");
            for (int i = 0; i < chunks.Count && i < 5; i++)
            {
                Console.WriteLine($"Chunk {i + 1}: {chunks[i]}");

                if (i == 0 || i == chunks.Count - 1) // Analyze first and last chunks
                {
                    JsonDocument chunkDoc = JsonDocument.Parse(chunks[i]);
                    JsonElement chunkRoot = chunkDoc.RootElement;
                    Console.WriteLine($"  Object: {chunkRoot.GetProperty("object").GetString()}");
                    Console.WriteLine($"  Model: {chunkRoot.GetProperty("model").GetString()}");

                    JsonElement chunkChoices = chunkRoot.GetProperty("choices").EnumerateArray().First();
                    JsonElement delta = chunkChoices.GetProperty("delta");
                    if (delta.TryGetProperty("content", out JsonElement content))
                    {
                        Console.WriteLine($"  Content: \"{content.GetString()}\"");
                    }
                    if (chunkChoices.TryGetProperty("finish_reason", out JsonElement finishReason) && finishReason.ValueKind != JsonValueKind.Null)
                    {
                        Console.WriteLine($"  Finish Reason: {finishReason.GetString()}");
                    }
                }
            }

            AssertTrue(chunks.Count > 1, "Should have multiple streaming chunks");
            AssertTrue(responseText.Contains("data: [DONE]"), "Should end with DONE marker");

            Console.WriteLine("✅ Streaming test completed successfully!");
        }

        public static async Task ChatCompletion_WithOptions_HandlesParameters()
        {
            Console.WriteLine("\n=== OpenAI Chat Completion with Options Test ===");

            // Arrange
            var chatRequest = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = "Hello" }
                },
                temperature = 0.8,
                top_p = 0.9,
                max_tokens = 100,
                presence_penalty = 0.5,
                frequency_penalty = 0.5,
                seed = 12345,
                stream = false
            };

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/v1/chat/completions");
            Console.WriteLine("REQUEST BODY:");
            string requestJson = JsonSerializer.Serialize(chatRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(requestJson);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/v1/chat/completions", HttpMethod.Post);
            client.Authorization.BearerToken = "mock-api-key";
            string requestBody = JsonSerializer.Serialize(chatRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine("RESPONSE BODY:");
            Console.WriteLine(response.DataAsString);

            // Assert
            AssertEqual(200, response.StatusCode);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            AssertEqual("chat.completion", root.GetProperty("object").GetString());
            AssertEqual("gpt-3.5-turbo", root.GetProperty("model").GetString());

            List<JsonElement> choices = root.GetProperty("choices").EnumerateArray().ToList();
            JsonElement message = choices.First().GetProperty("message");
            AssertEqual("assistant", message.GetProperty("role").GetString());
            AssertFalse(string.IsNullOrEmpty(message.GetProperty("content").GetString()));

            Console.WriteLine("✅ Test completed successfully!");
        }

        #endregion

        #region Text Completion Tests

        public static async Task TextCompletion_NonStreaming_ReturnsValidResponse()
        {
            Console.WriteLine("\n=== OpenAI Text Completion (Non-Streaming) Test ===");

            // Arrange
            var completionRequest = new
            {
                model = "text-davinci-003",
                prompt = "The future of artificial intelligence is",
                max_tokens = 50,
                temperature = 0.9,
                top_p = 1.0,
                n = 1,
                stream = false
            };

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/v1/completions");
            Console.WriteLine("REQUEST BODY:");
            string requestJson = JsonSerializer.Serialize(completionRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(requestJson);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/v1/completions", HttpMethod.Post);
            client.Authorization.BearerToken = "mock-api-key";
            string requestBody = JsonSerializer.Serialize(completionRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine("RESPONSE BODY:");
            Console.WriteLine(response.DataAsString);

            // Assert
            AssertEqual(200, response.StatusCode);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            Console.WriteLine("\nRESPONSE VALIDATION:");
            Console.WriteLine($"Object: {root.GetProperty("object").GetString()}");
            Console.WriteLine($"Model: {root.GetProperty("model").GetString()}");

            AssertEqual("text_completion", root.GetProperty("object").GetString());
            AssertEqual("text-davinci-003", root.GetProperty("model").GetString());

            List<JsonElement> choices = root.GetProperty("choices").EnumerateArray().ToList();
            JsonElement firstChoice = choices.First();
            Console.WriteLine($"Generated Text: {firstChoice.GetProperty("text").GetString()}");
            Console.WriteLine($"Finish Reason: {firstChoice.GetProperty("finish_reason").GetString()}");

            AssertNotEmpty(choices);
            AssertFalse(string.IsNullOrEmpty(firstChoice.GetProperty("text").GetString()));

            Console.WriteLine("✅ Test completed successfully!");
        }

        public static async Task TextCompletion_Streaming_ReturnsServerSentEvents()
        {
            Console.WriteLine("\n=== OpenAI Text Completion (Streaming) Test ===");

            // Arrange
            var completionRequest = new
            {
                model = "text-davinci-003",
                prompt = "Once upon a time",
                max_tokens = 50,
                temperature = 0.7,
                stream = true
            };

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/v1/completions");
            Console.WriteLine("REQUEST BODY:");
            string requestJson = JsonSerializer.Serialize(completionRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(requestJson);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/v1/completions", HttpMethod.Post);
            client.Authorization.BearerToken = "mock-api-key";
            string requestBody = JsonSerializer.Serialize(completionRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Content Type: {response.ContentType}");

            // Assert
            AssertEqual(200, response.StatusCode);
            AssertEqual("text/plain", response.ContentType);

            string responseText = response.DataAsString;
            Console.WriteLine("STREAMING RESPONSE BODY (Server-Sent Events):");
            Console.WriteLine(responseText);

            // Parse Server-Sent Events
            string[] lines = responseText.Split('\n');
            List<string> chunks = new List<string>();

            foreach (string line in lines)
            {
                if (line.StartsWith("data: ") && line != "data: [DONE]")
                {
                    string jsonData = line.Substring(6);
                    chunks.Add(jsonData);
                }
            }

            Console.WriteLine($"\nParsed {chunks.Count} streaming chunks:");
            for (int i = 0; i < chunks.Count && i < 5; i++)
            {
                Console.WriteLine($"Chunk {i + 1}: {chunks[i]}");

                JsonDocument chunkDoc = JsonDocument.Parse(chunks[i]);
                JsonElement chunkRoot = chunkDoc.RootElement;
                Console.WriteLine($"  Object: {chunkRoot.GetProperty("object").GetString()}");
                Console.WriteLine($"  Model: {chunkRoot.GetProperty("model").GetString()}");

                JsonElement chunkChoices = chunkRoot.GetProperty("choices").EnumerateArray().First();
                if (chunkChoices.TryGetProperty("text", out JsonElement text))
                {
                    Console.WriteLine($"  Text: \"{text.GetString()}\"");
                }
            }

            AssertTrue(chunks.Count > 1, "Should have multiple streaming chunks");
            AssertTrue(responseText.Contains("data: [DONE]"), "Should end with DONE marker");

            Console.WriteLine("✅ Streaming test completed successfully!");
        }

        #endregion

        #region Embeddings Tests

        public static async Task Embeddings_SingleInput_ReturnsValidEmbeddingVector()
        {
            Console.WriteLine("\n=== OpenAI Embeddings (Single Input) Test ===");

            // Arrange
            var embeddingRequest = new
            {
                model = "text-embedding-ada-002",
                input = "The quick brown fox jumps over the lazy dog",
                user = "test-user"
            };

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/v1/embeddings");
            Console.WriteLine("REQUEST BODY:");
            string requestJson = JsonSerializer.Serialize(embeddingRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(requestJson);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/v1/embeddings", HttpMethod.Post);
            client.Authorization.BearerToken = "mock-api-key";
            string requestBody = JsonSerializer.Serialize(embeddingRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine("RESPONSE BODY:");
            Console.WriteLine(response.DataAsString);

            // Assert
            AssertEqual(200, response.StatusCode);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            Console.WriteLine("\nRESPONSE VALIDATION:");
            Console.WriteLine($"Object: {root.GetProperty("object").GetString()}");
            Console.WriteLine($"Model: {root.GetProperty("model").GetString()}");

            AssertEqual("list", root.GetProperty("object").GetString());
            AssertEqual("text-embedding-ada-002", root.GetProperty("model").GetString());

            JsonElement data = root.GetProperty("data").EnumerateArray().First();
            List<JsonElement> embedding = data.GetProperty("embedding").EnumerateArray().ToList();

            Console.WriteLine($"Embedding Dimensions: {embedding.Count}");
            Console.WriteLine($"First 5 values: [{string.Join(", ", embedding.Take(5).Select(v => v.GetDouble().ToString("F6")))}]");

            AssertTrue(embedding.Count >= 1536, "Should have at least 1536 dimensions"); // Ada-002 has 1536
            foreach (var value in embedding)
            {
                AssertTrue(value.ValueKind == JsonValueKind.Number);
            }

            Console.WriteLine("✅ Test completed successfully!");
        }

        public static async Task Embeddings_BatchInputs_ReturnsValidEmbeddingVectors()
        {
            Console.WriteLine("\n=== OpenAI Embeddings (Batch Inputs) Test ===");

            // Arrange
            var embeddingRequest = new
            {
                model = "text-embedding-ada-002",
                input = new[]
                {
                    "The quick brown fox jumps over the lazy dog",
                    "Machine learning is a subset of artificial intelligence",
                    "Natural language processing enables computers to understand human language"
                },
                user = "test-user"
            };

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/v1/embeddings");
            Console.WriteLine("REQUEST BODY:");
            string requestJson = JsonSerializer.Serialize(embeddingRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(requestJson);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/v1/embeddings", HttpMethod.Post);
            client.Authorization.BearerToken = "mock-api-key";
            string requestBody = JsonSerializer.Serialize(embeddingRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine("RESPONSE BODY:");
            string truncatedResponse = response.DataAsString.Length > 300 ? response.DataAsString.Substring(0, 300) + "..." : response.DataAsString;
            Console.WriteLine(truncatedResponse);

            // Assert
            AssertEqual(200, response.StatusCode);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            Console.WriteLine("\nRESPONSE VALIDATION:");
            Console.WriteLine($"Object: {root.GetProperty("object").GetString()}");
            Console.WriteLine($"Model: {root.GetProperty("model").GetString()}");

            AssertEqual("list", root.GetProperty("object").GetString());
            AssertEqual("text-embedding-ada-002", root.GetProperty("model").GetString());

            List<JsonElement> dataArray = root.GetProperty("data").EnumerateArray().ToList();
            Console.WriteLine($"Number of Embeddings: {dataArray.Count}");
            AssertEqual(3, dataArray.Count, "Should have 3 embeddings for 3 inputs");

            // Verify each embedding
            for (int i = 0; i < dataArray.Count; i++)
            {
                JsonElement embeddingObj = dataArray[i];
                AssertEqual("embedding", embeddingObj.GetProperty("object").GetString());
                AssertEqual(i, embeddingObj.GetProperty("index").GetInt32());

                List<JsonElement> embedding = embeddingObj.GetProperty("embedding").EnumerateArray().ToList();
                Console.WriteLine($"Embedding {i + 1} Dimensions: {embedding.Count}");
                AssertNotEmpty(embedding);
                AssertTrue(embedding.Count >= 1536, "Each embedding should have at least 1536 dimensions");

                // Verify all embedding values are numbers
                foreach (var value in embedding)
                {
                    AssertTrue(value.ValueKind == JsonValueKind.Number);
                }
            }

            Console.WriteLine("✅ Test completed successfully!");
        }

        #endregion

        #region Models Tests

        public static async Task ListModels_ReturnsAvailableModels()
        {
            Console.WriteLine("\n=== OpenAI List Models Test ===");

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/v1/models");

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/v1/models", HttpMethod.Get);
            client.Authorization.BearerToken = "mock-api-key";
            RestResponse response = await client.SendAsync();

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine("RESPONSE BODY:");
            Console.WriteLine(response.DataAsString);

            // Assert
            AssertEqual(200, response.StatusCode);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            Console.WriteLine("\nRESPONSE VALIDATION:");
            Console.WriteLine($"Object: {root.GetProperty("object").GetString()}");

            AssertEqual("list", root.GetProperty("object").GetString());

            List<JsonElement> models = root.GetProperty("data").EnumerateArray().ToList();
            Console.WriteLine($"Available Models Count: {models.Count}");

            foreach (JsonElement model in models.Take(3)) // Show first 3 models
            {
                Console.WriteLine($"Model: {model.GetProperty("id").GetString()}");
                Console.WriteLine($"  Object: {model.GetProperty("object").GetString()}");
                Console.WriteLine($"  Created: {model.GetProperty("created").GetInt64()}");
                Console.WriteLine($"  Owned By: {model.GetProperty("owned_by").GetString()}");
            }

            AssertNotEmpty(models);
            foreach (var model in models)
            {
                AssertEqual("model", model.GetProperty("object").GetString());
            }

            Console.WriteLine("✅ Test completed successfully!");
        }

        public static async Task RetrieveModel_ReturnsModelDetails()
        {
            Console.WriteLine("\n=== OpenAI Retrieve Model Test ===");

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/v1/models/gpt-3.5-turbo");

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/v1/models/gpt-3.5-turbo", HttpMethod.Get);
            client.Authorization.BearerToken = "mock-api-key";
            RestResponse response = await client.SendAsync();

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine("RESPONSE BODY:");
            Console.WriteLine(response.DataAsString);

            // Assert
            AssertEqual(200, response.StatusCode);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement root = jsonDoc.RootElement;

            Console.WriteLine("\nRESPONSE VALIDATION:");
            Console.WriteLine($"ID: {root.GetProperty("id").GetString()}");
            Console.WriteLine($"Object: {root.GetProperty("object").GetString()}");
            Console.WriteLine($"Owned By: {root.GetProperty("owned_by").GetString()}");

            AssertEqual("gpt-3.5-turbo", root.GetProperty("id").GetString());
            AssertEqual("model", root.GetProperty("object").GetString());
            AssertTrue(root.TryGetProperty("created", out _));
            AssertTrue(root.TryGetProperty("owned_by", out _));

            Console.WriteLine("✅ Test completed successfully!");
        }

        public static async Task RootEndpoint_ReturnsHealthStatus()
        {
            Console.WriteLine("\n=== OpenAI Root Endpoint Test ===");

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/");

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/", HttpMethod.Get);
            RestResponse response = await client.SendAsync();

            Console.WriteLine($"\nReceived RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine("RESPONSE BODY:");
            Console.WriteLine(response.DataAsString);

            // Assert
            AssertEqual(200, response.StatusCode);
            AssertTrue(response.DataAsString.Contains("OpenAI") || response.DataAsString.Contains("OK"));

            Console.WriteLine("✅ Test completed successfully!");
        }

        #endregion

        #region Error Handling Tests

        public static async Task InvalidModel_Returns404Error()
        {
            Console.WriteLine("\n=== OpenAI Invalid Model Error Test ===");

            // Arrange
            var chatRequest = new
            {
                model = "nonexistent-model",
                messages = new[]
                {
                    new { role = "user", content = "Hello" }
                }
            };

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/v1/chat/completions");
            Console.WriteLine("REQUEST BODY:");
            string requestJson = JsonSerializer.Serialize(chatRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(requestJson);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/v1/chat/completions", HttpMethod.Post);
            client.Authorization.BearerToken = "mock-api-key";
            string requestBody = JsonSerializer.Serialize(chatRequest);
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(requestBody));

            Console.WriteLine($"\nReceived ERROR RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine("ERROR RESPONSE BODY:");
            Console.WriteLine(response.DataAsString);

            // Assert
            AssertEqual(404, response.StatusCode);

            JsonDocument jsonDoc = JsonDocument.Parse(response.DataAsString);
            JsonElement error = jsonDoc.RootElement.GetProperty("error");

            Console.WriteLine("\nERROR VALIDATION:");
            Console.WriteLine($"Error Type: {error.GetProperty("type").GetString()}");
            Console.WriteLine($"Error Message: {error.GetProperty("message").GetString()}");

            AssertEqual("invalid_request_error", error.GetProperty("type").GetString());
            AssertTrue(error.GetProperty("message").GetString().ToLower().Contains("model"));

            Console.WriteLine("✅ Error handling test completed successfully!");
        }

        public static async Task MalformedRequest_ReturnsBadRequest()
        {
            Console.WriteLine("\n=== OpenAI Malformed Request Test ===");

            // Arrange
            string malformedRequest = "{invalid json}";

            Console.WriteLine($"\nSending REQUEST to: {_mockServerUrl}/v1/chat/completions");
            Console.WriteLine("REQUEST BODY:");
            Console.WriteLine(malformedRequest);

            // Act
            using RestRequest client = new RestRequest(_mockServerUrl + "/v1/chat/completions", HttpMethod.Post);
            client.Authorization.BearerToken = "mock-api-key";
            client.ContentType = "application/json";
            RestResponse response = await client.SendAsync(Encoding.UTF8.GetBytes(malformedRequest));

            Console.WriteLine($"\nReceived ERROR RESPONSE:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine("ERROR RESPONSE BODY:");
            Console.WriteLine(response.DataAsString);

            // Assert
            AssertEqual(400, response.StatusCode);

            Console.WriteLine("✅ Error handling test completed successfully!");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Default route handler for the mock OpenAI server.
        /// Simulates OpenAI API responses with detailed logging.
        /// </summary>
        private static async Task DefaultRoute(HttpContextBase ctx)
        {
            string path = ctx.Request.Url.RawWithoutQuery.ToLower();
            WatsonWebserver.Core.HttpMethod method = ctx.Request.Method;

            Console.WriteLine($"\n[MOCK SERVER] Received {method} request to: {path}");
            if (ctx.Request.Headers != null)
            {
                Console.WriteLine("[MOCK SERVER] Headers:");
                for (int i = 0; i < ctx.Request.Headers.Count; i++)
                {
                    string key = ctx.Request.Headers.GetKey(i);
                    string value = ctx.Request.Headers.Get(i);
                    Console.WriteLine($"  {key}: {value}");
                }
            }

            if (!string.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                Console.WriteLine($"[MOCK SERVER] Request Body: {ctx.Request.DataAsString}");
            }

            try
            {
                switch (path)
                {
                    case "/v1/chat/completions":
                        await HandleChatCompletions(ctx);
                        break;
                    case "/v1/completions":
                        await HandleCompletions(ctx);
                        break;
                    case "/v1/embeddings":
                        await HandleEmbeddings(ctx);
                        break;
                    case "/v1/models":
                        await HandleModels(ctx);
                        break;
                    case "/":
                        await HandleRootEndpoint(ctx);
                        break;
                    default:
                        if (path.StartsWith("/v1/models/"))
                        {
                            await HandleRetrieveModel(ctx, path);
                        }
                        else
                        {
                        ctx.Response.StatusCode = 404;
                        await ctx.Response.Send(JsonSerializer.Serialize(new
                        {
                            error = new
                            {
                                type = "invalid_request_error",
                                message = $"Unknown endpoint: {path}"
                            }
                        }));
                        }
                        break;
                }
            }
            catch (JsonException)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        type = "invalid_request_error",
                        message = "Invalid JSON in request body"
                    }
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MOCK SERVER] Error: {ex.Message}");
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        type = "server_error",
                        message = ex.Message
                    }
                }));
            }
        }

        private static async Task HandleChatCompletions(HttpContextBase ctx)
        {
            string requestBody = ctx.Request.DataAsString;
            if (string.IsNullOrEmpty(requestBody))
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(JsonSerializer.Serialize(new
                {
                    error = new { type = "invalid_request_error", message = "Empty request body" }
                }));
                return;
            }

            JsonDocument? request = null;
            JsonElement root;

            try
            {
                request = JsonSerializer.Deserialize<JsonDocument>(requestBody);
                root = request.RootElement;
            }
            catch (JsonException)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(JsonSerializer.Serialize(new
                {
                    error = new { type = "invalid_request_error", message = "Invalid JSON in request body" }
                }));
                return;
            }

            if (!root.TryGetProperty("model", out var modelElement))
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(JsonSerializer.Serialize(new
                {
                    error = new { type = "invalid_request_error", message = "Model is required" }
                }));
                return;
            }

            string model = modelElement.GetString();
            if (model == "nonexistent-model")
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(JsonSerializer.Serialize(new
                {
                    error = new { type = "invalid_request_error", message = "Model not found" }
                }));
                return;
            }

            bool isStreaming = root.TryGetProperty("stream", out JsonElement streamElement) && streamElement.GetBoolean();

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
                id = "chatcmpl-" + Guid.NewGuid().ToString("N")[..10],
                @object = "chat.completion",
                created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                model = model,
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        message = new
                        {
                            role = "assistant",
                            content = "This is a mock response from the OpenAI-compatible server. The capital of France is Paris."
                        },
                        finish_reason = "stop"
                    }
                },
                usage = new
                {
                    prompt_tokens = 25,
                    completion_tokens = 15,
                    total_tokens = 40
                }
            };

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            string responseJson = JsonSerializer.Serialize(response);
            Console.WriteLine($"[MOCK SERVER] Sending response: {responseJson}");
            await ctx.Response.Send(responseJson);
        }

        private static async Task SendStreamingChatResponse(HttpContextBase ctx, string model)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/plain";

            var chunks = new object[]
            {
                new { id = "chatcmpl-123", @object = "chat.completion.chunk", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                      model = model, choices = new[] { new { index = 0, delta = new { role = "assistant" }, finish_reason = (string)null } } },
                new { id = "chatcmpl-123", @object = "chat.completion.chunk", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                      model = model, choices = new[] { new { index = 0, delta = new { content = "Code" }, finish_reason = (string)null } } },
                new { id = "chatcmpl-123", @object = "chat.completion.chunk", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                      model = model, choices = new[] { new { index = 0, delta = new { content = " flows" }, finish_reason = (string)null } } },
                new { id = "chatcmpl-123", @object = "chat.completion.chunk", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                      model = model, choices = new[] { new { index = 0, delta = new { content = " like" }, finish_reason = (string)null } } },
                new { id = "chatcmpl-123", @object = "chat.completion.chunk", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                      model = model, choices = new[] { new { index = 0, delta = new { content = " water" }, finish_reason = (string)null } } },
                new { id = "chatcmpl-123", @object = "chat.completion.chunk", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                      model = model, choices = new[] { new { index = 0, delta = new { }, finish_reason = "stop" } } }
            };

            StringBuilder responseBuilder = new StringBuilder();
            foreach (var chunk in chunks)
            {
                string chunkJson = JsonSerializer.Serialize(chunk);
                responseBuilder.AppendLine($"data: {chunkJson}");
                responseBuilder.AppendLine();
            }
            responseBuilder.AppendLine("data: [DONE]");
            responseBuilder.AppendLine();

            string responseText = responseBuilder.ToString();
            Console.WriteLine($"[MOCK SERVER] Sending streaming response: {responseText.Replace("\n", "\\n")}");
            await ctx.Response.Send(responseText);
        }

        private static async Task HandleCompletions(HttpContextBase ctx)
        {
            string requestBody = ctx.Request.DataAsString;
            JsonDocument request = JsonSerializer.Deserialize<JsonDocument>(requestBody);
            JsonElement root = request.RootElement;

            string model = root.TryGetProperty("model", out var modelElement) ? modelElement.GetString() : "text-davinci-003";
            bool isStreaming = root.TryGetProperty("stream", out JsonElement streamElement) && streamElement.GetBoolean();

            if (isStreaming)
            {
                await SendStreamingCompletionResponse(ctx, model);
            }
            else
            {
                await SendNonStreamingCompletionResponse(ctx, model);
            }
        }

        private static async Task SendNonStreamingCompletionResponse(HttpContextBase ctx, string model)
        {
            var response = new
            {
                id = "cmpl-" + Guid.NewGuid().ToString("N")[..10],
                @object = "text_completion",
                created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                model = "text-davinci-003",
                choices = new[]
                {
                    new
                    {
                        text = " bright, with AI assistants helping people accomplish amazing things through natural conversation.",
                        index = 0,
                        finish_reason = "stop"
                    }
                },
                usage = new { prompt_tokens = 20, completion_tokens = 30, total_tokens = 50 }
            };

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            string responseJson = JsonSerializer.Serialize(response);
            Console.WriteLine($"[MOCK SERVER] Sending completion response: {responseJson}");
            await ctx.Response.Send(responseJson);
        }

        private static async Task SendStreamingCompletionResponse(HttpContextBase ctx, string model)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/plain";

            var chunks = new object[]
            {
                new { id = "cmpl-123", @object = "text_completion", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                      model = model, choices = new[] { new { text = " in", index = 0, finish_reason = (string)null } } },
                new { id = "cmpl-123", @object = "text_completion", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                      model = model, choices = new[] { new { text = " a", index = 0, finish_reason = (string)null } } },
                new { id = "cmpl-123", @object = "text_completion", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                      model = model, choices = new[] { new { text = " land", index = 0, finish_reason = (string)null } } },
                new { id = "cmpl-123", @object = "text_completion", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                      model = model, choices = new[] { new { text = " far", index = 0, finish_reason = (string)null } } },
                new { id = "cmpl-123", @object = "text_completion", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                      model = model, choices = new[] { new { text = " away", index = 0, finish_reason = (string)null } } },
                new { id = "cmpl-123", @object = "text_completion", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                      model = model, choices = new[] { new { text = "", index = 0, finish_reason = "stop" } } }
            };

            StringBuilder responseBuilder = new StringBuilder();
            foreach (var chunk in chunks)
            {
                string chunkJson = JsonSerializer.Serialize(chunk);
                responseBuilder.AppendLine($"data: {chunkJson}");
                responseBuilder.AppendLine();
            }
            responseBuilder.AppendLine("data: [DONE]");
            responseBuilder.AppendLine();

            string responseText = responseBuilder.ToString();
            Console.WriteLine($"[MOCK SERVER] Sending streaming completion response: {responseText.Replace("\n", "\\n")}");
            await ctx.Response.Send(responseText);
        }

        private static async Task HandleEmbeddings(HttpContextBase ctx)
        {
            string requestBody = ctx.Request.DataAsString;
            JsonDocument request = JsonSerializer.Deserialize<JsonDocument>(requestBody);
            JsonElement root = request.RootElement;

            Random random = new Random(42);
            string model = root.TryGetProperty("model", out var modelElement) ? modelElement.GetString() : "text-embedding-ada-002";

            // Check if input is a string or array
            if (root.TryGetProperty("input", out var inputElement))
            {
                List<object> dataList = new List<object>();
                int totalTokens = 0;

                if (inputElement.ValueKind == JsonValueKind.Array)
                {
                    // Batch embeddings
                    int index = 0;
                    foreach (var inputItem in inputElement.EnumerateArray())
                    {
                        double[] embedding = Enumerable.Range(0, 1536)
                            .Select(_ => (random.NextDouble() - 0.5) * 2.0)
                            .ToArray();

                        dataList.Add(new
                        {
                            @object = "embedding",
                            embedding = embedding,
                            index = index
                        });

                        totalTokens += 10;
                        index++;
                    }
                }
                else
                {
                    // Single embedding
                    double[] embedding = Enumerable.Range(0, 1536)
                        .Select(_ => (random.NextDouble() - 0.5) * 2.0)
                        .ToArray();

                    dataList.Add(new
                    {
                        @object = "embedding",
                        embedding = embedding,
                        index = 0
                    });

                    totalTokens = 12;
                }

                var response = new
                {
                    @object = "list",
                    data = dataList,
                    model = model,
                    usage = new { prompt_tokens = totalTokens, total_tokens = totalTokens }
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                string responseJson = JsonSerializer.Serialize(response);
                Console.WriteLine($"[MOCK SERVER] Sending embeddings response (truncated): {responseJson.Substring(0, Math.Min(200, responseJson.Length))}...");
                await ctx.Response.Send(responseJson);
            }
            else
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(JsonSerializer.Serialize(new
                {
                    error = new { type = "invalid_request_error", message = "Input is required" }
                }));
            }
        }

        private static async Task HandleModels(HttpContextBase ctx)
        {
            var response = new
            {
                @object = "list",
                data = new[]
                {
                    new { id = "gpt-4", @object = "model", created = 1687882411L, owned_by = "openai" },
                    new { id = "gpt-3.5-turbo", @object = "model", created = 1677649963L, owned_by = "openai" },
                    new { id = "text-davinci-003", @object = "model", created = 1669599635L, owned_by = "openai-internal" },
                    new { id = "text-embedding-ada-002", @object = "model", created = 1671217299L, owned_by = "openai-internal" }
                }
            };

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            string responseJson = JsonSerializer.Serialize(response);
            Console.WriteLine($"[MOCK SERVER] Sending models response: {responseJson}");
            await ctx.Response.Send(responseJson);
        }

        private static async Task HandleRetrieveModel(HttpContextBase ctx, string path)
        {
            string modelId = path.Substring("/v1/models/".Length);

            var response = new
            {
                id = modelId,
                @object = "model",
                created = 1677649963L,
                owned_by = "openai"
            };

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            string responseJson = JsonSerializer.Serialize(response);
            Console.WriteLine($"[MOCK SERVER] Sending retrieve model response: {responseJson}");
            await ctx.Response.Send(responseJson);
        }

        private static async Task HandleRootEndpoint(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send("OpenAI-compatible API is running");
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

#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }
}