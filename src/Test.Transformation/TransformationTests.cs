namespace Test.Transformation
{
    using System.Linq;
    using System.Text.Json;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Services.Transformation.Outbound;
    using OllamaFlow.Core.Services.Transformation.Inbound;
    using OllamaFlow.Core.Services.Transformation.Streaming;
    using OllamaFlow.Core.Services.Transformation;
    using OllamaFlow.Core.Services.Transformation.Interfaces;
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Core.Helpers;
    using OllamaFlow.Core.Models.Agnostic.Requests;
    using OllamaFlow.Core.Models.Agnostic.Common;
    using OllamaFlow.Core.Models.Agnostic.Base;
    using OllamaFlow.Core.Models;

    /// <summary>
    /// Comprehensive tests for transformation services.
    /// Tests outbound, inbound, streaming, and pipeline transformation functionality.
    /// </summary>
    public static class TransformationTests
    {
        private static readonly Serializer _serializer = new Serializer();

        /// <summary>
        /// Main entry point for running transformation tests as a console application.
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== OllamaFlow Transformation Tests ===");

            try
            {
                // Run outbound transformation tests
                await RunOutboundTransformationTests();

                // Run inbound transformation tests
                await RunInboundTransformationTests();

                // Run streaming transformation tests
                await RunStreamingTransformationTests();

                // Run pipeline tests
                await RunPipelineTests();

                Console.WriteLine("\n=== All Tests Completed ===");
                Console.WriteLine("✅ All transformation tests passed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Test execution failed: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Environment.Exit(1);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        #region Outbound Transformation Tests

        private static async Task RunOutboundTransformationTests()
        {
            Console.WriteLine("\n=== Outbound Transformation Tests ===");

            try
            {
                await AgnosticToOpenAI_TransformChatRequest_CompleteMapping();
                await AgnosticToOllama_TransformChatRequest_CompleteMapping();
                Console.WriteLine("✅ All outbound transformation tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Outbound transformation test failed: {ex.Message}");
                throw;
            }
        }

        public static async Task AgnosticToOpenAI_TransformChatRequest_CompleteMapping()
        {
            Console.WriteLine("\n=== Agnostic to OpenAI Chat Request Transformation Test ===");

            // Arrange
            AgnosticChatRequest agnosticRequest = new AgnosticChatRequest
            {
                Model = "gpt-4",
                Messages = new List<AgnosticMessage>
                {
                    new() { Role = "system", Content = "You are a helpful assistant." },
                    new() { Role = "user", Content = "Hello!" }
                },
                Stream = true,
                Temperature = 0.7,
                MaxTokens = 150,
                TopP = 0.9,
                N = 1,
                Stop = new string[] { "END", "STOP" },
                FrequencyPenalty = 0.5,
                PresencePenalty = 0.3,
                Seed = 12345
            };

            Console.WriteLine("\nINPUT (Agnostic Format):");
            string inputJson = _serializer.SerializeJson(agnosticRequest);
            Console.WriteLine(inputJson);

            // Act
            AgnosticToOpenAITransformer transformer = new AgnosticToOpenAITransformer();
            AssertTrue(transformer.CanTransform(ApiFormatEnum.OpenAI, agnosticRequest));

            object result = await transformer.TransformAsync(agnosticRequest);

            // Assert
            AssertNotNull(result);

            string json = _serializer.SerializeJson(result);
            Console.WriteLine("\nOUTPUT (OpenAI Format):");
            Console.WriteLine(json);

            JsonDocument jsonDoc = JsonDocument.Parse(json);
            JsonElement root = jsonDoc.RootElement;

            Console.WriteLine("\nTRANSFORMATION VERIFICATION:");
            Console.WriteLine($"Model: {agnosticRequest.Model} -> {root.GetProperty("model").GetString()}");
            Console.WriteLine($"Stream: {agnosticRequest.Stream} -> {root.GetProperty("stream").GetBoolean()}");
            Console.WriteLine($"Temperature: {agnosticRequest.Temperature} -> {root.GetProperty("temperature").GetDouble()}");

            AssertEqual("gpt-4", root.GetProperty("model").GetString());
            AssertTrue(root.GetProperty("stream").GetBoolean());
            AssertEqual(0.7, root.GetProperty("temperature").GetDouble(), 0.01);

            List<JsonElement> messages = root.GetProperty("messages").EnumerateArray().ToList();
            AssertEqual(2, messages.Count);
            AssertEqual("system", messages[0].GetProperty("role").GetString());
            AssertEqual("You are a helpful assistant.", messages[0].GetProperty("content").GetString());

            Console.WriteLine("✅ Agnostic to OpenAI transformation verified successfully!");
        }

        public static async Task AgnosticToOllama_TransformChatRequest_CompleteMapping()
        {
            Console.WriteLine("\n=== Agnostic to Ollama Chat Request Transformation Test ===");

            // Arrange
            AgnosticChatRequest agnosticRequest = new AgnosticChatRequest
            {
                Model = "llama2",
                Messages = new List<AgnosticMessage>
                {
                    new() { Role = "user", Content = "What is the capital of France?" }
                },
                Stream = true,
                Temperature = 0.6,
                TopP = 0.8,
                Seed = 42,
                MaxTokens = 100
            };

            // Act
            AgnosticToOllamaTransformer transformer = new AgnosticToOllamaTransformer();
            AssertTrue(transformer.CanTransform(ApiFormatEnum.Ollama, agnosticRequest));

            object result = await transformer.TransformAsync(agnosticRequest);

            // Assert
            AssertNotNull(result);

            string json = _serializer.SerializeJson(result);
            Console.WriteLine("\nOUTPUT (Ollama Format):");
            Console.WriteLine(json);

            JsonDocument jsonDoc = JsonDocument.Parse(json);
            JsonElement root = jsonDoc.RootElement;

            AssertEqual("llama2", root.GetProperty("model").GetString());
            AssertTrue(root.GetProperty("stream").GetBoolean());

            List<JsonElement> messages = root.GetProperty("messages").EnumerateArray().ToList();
            AssertEqual(1, messages.Count);
            AssertEqual("user", messages[0].GetProperty("role").GetString());

            JsonElement options = root.GetProperty("options");
            AssertEqual(0.6, options.GetProperty("temperature").GetDouble(), 0.01);
            AssertEqual(0.8, options.GetProperty("top_p").GetDouble(), 0.01);
            AssertEqual(42, options.GetProperty("seed").GetInt32());
            AssertEqual(100, options.GetProperty("num_predict").GetInt32());

            Console.WriteLine("✅ Agnostic to Ollama transformation verified successfully!");
        }

        #endregion

        #region Inbound Transformation Tests

        private static async Task RunInboundTransformationTests()
        {
            Console.WriteLine("\n=== Inbound Transformation Tests ===");

            try
            {
                await OpenAITransformer_TransformChatRequest_CompleteMapping();
                await OllamaTransformer_TransformChatRequest_CompleteMapping();
                Console.WriteLine("✅ All inbound transformation tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Inbound transformation test failed: {ex.Message}");
                throw;
            }
        }

        public static async Task OpenAITransformer_TransformChatRequest_CompleteMapping()
        {
            Console.WriteLine("\n=== OpenAI to Agnostic Chat Request Transformation Test ===");

            // Arrange - Create OpenAI JSON request
            string openAIJson = @"{
                ""model"": ""gpt-4"",
                ""messages"": [
                    { ""role"": ""system"", ""content"": ""You are a helpful assistant."" },
                    { ""role"": ""user"", ""content"": ""Hello!"" }
                ],
                ""stream"": true,
                ""temperature"": 0.7,
                ""max_tokens"": 150,
                ""top_p"": 0.9,
                ""seed"": 12345
            }";

            Console.WriteLine("\nINPUT (OpenAI Format):");
            Console.WriteLine(openAIJson);

            // Deserialize to OpenAI request model
            OpenAIChatRequest openAIRequest = _serializer.DeserializeJson<OpenAIChatRequest>(openAIJson);

            // Act - Transform to agnostic
            AgnosticChatRequest agnosticRequest = TransformOpenAIToAgnostic(openAIRequest);

            // Assert
            AssertNotNull(agnosticRequest);

            Console.WriteLine("\nOUTPUT (Agnostic Format):");
            string agnosticJson = _serializer.SerializeJson(agnosticRequest);
            Console.WriteLine(agnosticJson);

            AssertEqual("gpt-4", agnosticRequest.Model);
            AssertEqual(2, agnosticRequest.Messages.Count);
            AssertEqual("system", agnosticRequest.Messages[0].Role);
            AssertEqual("You are a helpful assistant.", agnosticRequest.Messages[0].Content);
            AssertTrue(agnosticRequest.Stream);
            AssertEqual(0.7, agnosticRequest.Temperature!.Value, 0.01);
            AssertEqual(150, agnosticRequest.MaxTokens!.Value);
            AssertEqual(0.9, agnosticRequest.TopP!.Value, 0.01);
            AssertEqual(12345, agnosticRequest.Seed!.Value);

            Console.WriteLine("✅ OpenAI to Agnostic transformation verified successfully!");
            await Task.CompletedTask;
        }

        private static AgnosticChatRequest TransformOpenAIToAgnostic(OpenAIChatRequest openAIRequest)
        {
            AgnosticChatRequest agnosticRequest = new AgnosticChatRequest
            {
                Model = openAIRequest.Model,
                Messages = openAIRequest.Messages?.Select(m => new AgnosticMessage
                {
                    Role = m.Role,
                    Content = m.Content,
                    Name = m.Name
                }).ToList() ?? new List<AgnosticMessage>(),
                Stream = openAIRequest.Stream,
                Temperature = openAIRequest.Temperature,
                MaxTokens = openAIRequest.MaxTokens,
                TopP = openAIRequest.TopP,
                N = openAIRequest.N,
                Stop = openAIRequest.Stop,
                FrequencyPenalty = openAIRequest.FrequencyPenalty,
                PresencePenalty = openAIRequest.PresencePenalty,
                Seed = openAIRequest.Seed,
                SourceFormat = ApiFormatEnum.OpenAI
            };

            return agnosticRequest;
        }

        private class OpenAIChatRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("model")]
            public required string Model { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("messages")]
            public required List<OpenAIMessage> Messages { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("stream")]
            public bool Stream { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("temperature")]
            public double? Temperature { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("max_tokens")]
            public int? MaxTokens { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("top_p")]
            public double? TopP { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("n")]
            public int? N { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("stop")]
            public required string[] Stop { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("frequency_penalty")]
            public double? FrequencyPenalty { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("presence_penalty")]
            public double? PresencePenalty { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("seed")]
            public int? Seed { get; set; }
        }

        private class OpenAIMessage
        {
            [System.Text.Json.Serialization.JsonPropertyName("role")]
            public required string Role { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("content")]
            public required string Content { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public required string Name { get; set; }
        }

        public static async Task OllamaTransformer_TransformChatRequest_CompleteMapping()
        {
            Console.WriteLine("\n=== Ollama to Agnostic Chat Request Transformation Test ===");

            // Arrange - Create Ollama JSON request
            string ollamaJson = @"{
                ""model"": ""llama2"",
                ""messages"": [
                    { ""role"": ""user"", ""content"": ""What is the capital of France?"" }
                ],
                ""stream"": true,
                ""options"": {
                    ""temperature"": 0.6,
                    ""top_p"": 0.8,
                    ""seed"": 42,
                    ""num_predict"": 100
                }
            }";

            Console.WriteLine("\nINPUT (Ollama Format):");
            Console.WriteLine(ollamaJson);

            // Deserialize to Ollama request model
            OllamaChatRequest ollamaRequest = _serializer.DeserializeJson<OllamaChatRequest>(ollamaJson);

            // Act - Transform to agnostic
            AgnosticChatRequest agnosticRequest = TransformOllamaToAgnostic(ollamaRequest);

            // Assert
            AssertNotNull(agnosticRequest);

            Console.WriteLine("\nOUTPUT (Agnostic Format):");
            string agnosticJson = _serializer.SerializeJson(agnosticRequest);
            Console.WriteLine(agnosticJson);

            AssertEqual("llama2", agnosticRequest.Model);
            AssertEqual(1, agnosticRequest.Messages.Count);
            AssertEqual("user", agnosticRequest.Messages[0].Role);
            AssertEqual("What is the capital of France?", agnosticRequest.Messages[0].Content);
            AssertTrue(agnosticRequest.Stream);
            AssertEqual(0.6, agnosticRequest.Temperature!.Value, 0.01);
            AssertEqual(0.8, agnosticRequest.TopP!.Value, 0.01);
            AssertEqual(42, agnosticRequest.Seed!.Value);
            AssertEqual(100, agnosticRequest.MaxTokens!.Value);

            Console.WriteLine("✅ Ollama to Agnostic transformation verified successfully!");
            await Task.CompletedTask;
        }

        private static AgnosticChatRequest TransformOllamaToAgnostic(OllamaChatRequest ollamaRequest)
        {
            AgnosticChatRequest agnosticRequest = new AgnosticChatRequest
            {
                Model = ollamaRequest.Model,
                Messages = ollamaRequest.Messages?.Select(m => new AgnosticMessage
                {
                    Role = m.Role,
                    Content = m.Content
                }).ToList() ?? new List<AgnosticMessage>(),
                Stream = ollamaRequest.Stream,
                System = ollamaRequest.System,
                Template = ollamaRequest.Template,
                SourceFormat = ApiFormatEnum.Ollama
            };

            // Map options
            if (ollamaRequest.Options != null)
            {
                string optionsJson = _serializer.SerializeJson(ollamaRequest.Options);
                JsonDocument optionsDoc = JsonDocument.Parse(optionsJson);
                Dictionary<string, object> options = new Dictionary<string, object>();

                foreach (JsonProperty prop in optionsDoc.RootElement.EnumerateObject())
                {
                    options[prop.Name ?? ""] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.Number => prop.Value.TryGetInt32(out int intVal) ? (object)intVal : prop.Value.GetDouble(),
                        JsonValueKind.String => prop.Value.GetString() ?? "",
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => prop.Value.ToString()
                    };
                }

                if (options.TryGetValue("temperature", out object? temp))
                    agnosticRequest.Temperature = Convert.ToDouble(temp);

                if (options.TryGetValue("top_p", out object? topP))
                    agnosticRequest.TopP = Convert.ToDouble(topP);

                if (options.TryGetValue("seed", out object? seed))
                    agnosticRequest.Seed = Convert.ToInt32(seed);

                if (options.TryGetValue("num_predict", out object? numPredict))
                    agnosticRequest.MaxTokens = Convert.ToInt32(numPredict);

                agnosticRequest.Options = options;
            }

            return agnosticRequest;
        }

        private class OllamaChatRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("model")]
            public required string Model { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("messages")]
            public required List<OllamaMessage> Messages { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("stream")]
            public bool Stream { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("system")]
            public required string System { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("template")]
            public required string Template { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("options")]
            public required object Options { get; set; }
        }

        private class OllamaMessage
        {
            [System.Text.Json.Serialization.JsonPropertyName("role")]
            public required string Role { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("content")]
            public required string Content { get; set; }
        }

        #endregion

        #region Streaming Transformation Tests

        private static async Task RunStreamingTransformationTests()
        {
            Console.WriteLine("\n=== Streaming Transformation Tests ===");

            try
            {
                await TransformChunk_OpenAIToOllama_StandardChunk_TransformsCorrectly();
                await TransformChunk_OllamaToOpenAI_StandardChunk_TransformsCorrectly();
                Console.WriteLine("✅ All streaming transformation tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Streaming transformation test failed: {ex.Message}");
                throw;
            }
        }

        public static async Task TransformChunk_OpenAIToOllama_StandardChunk_TransformsCorrectly()
        {
            Console.WriteLine("\n=== OpenAI to Ollama Chunk Transformation Test ===");

            // Arrange
            StreamingTransformer transformer = new StreamingTransformer(_serializer);
            string openAIChunk = "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1699999999,\"model\":\"gpt-3.5-turbo\",\"choices\":[{\"index\":0,\"delta\":{\"role\":\"assistant\",\"content\":\"Hello\"}}]}\n\n";

            Console.WriteLine("\nINPUT (OpenAI SSE Chunk):");
            Console.WriteLine($"Raw: {openAIChunk.Replace("\n", "\\n")}");

            AssertTrue(transformer.CanTransformStream(ApiFormatEnum.OpenAI, ApiFormatEnum.Ollama, RequestTypeEnum.GenerateChatCompletion));

            // Act
            StreamingChunkResult result = await transformer.TransformChunkAsync(
                openAIChunk,
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateChatCompletion);

            // Assert
            AssertNotNull(result);
            AssertNull(result.Error);
            AssertNotNull(result.ChunkData);
            AssertEqual("application/x-ndjson", result.ContentType);
            AssertFalse(result.IsServerSentEvent);

            string chunkText = System.Text.Encoding.UTF8.GetString(result.ChunkData);
            Console.WriteLine("\nOUTPUT (Ollama NDJSON Chunk):");
            Console.WriteLine($"Raw: {chunkText.Replace("\n", "\\n")}");

            JsonDocument jsonDoc = JsonDocument.Parse(chunkText);
            JsonElement root = jsonDoc.RootElement;

            AssertEqual("gpt-3.5-turbo", root.GetProperty("model").GetString());
            AssertFalse(root.GetProperty("done").GetBoolean());

            JsonElement message = root.GetProperty("message");
            AssertEqual("assistant", message.GetProperty("role").GetString());
            AssertEqual("Hello", message.GetProperty("content").GetString());

            Console.WriteLine("✅ OpenAI to Ollama chunk transformation verified successfully!");
        }

        public static async Task TransformChunk_OllamaToOpenAI_StandardChunk_TransformsCorrectly()
        {
            Console.WriteLine("\n=== Ollama to OpenAI Chunk Transformation Test ===");

            // Arrange
            StreamingTransformer transformer = new StreamingTransformer(_serializer);
            string ollamaChunk = "{\"model\":\"llama2\",\"created_at\":\"2023-11-14T14:30:00.000Z\",\"message\":{\"role\":\"assistant\",\"content\":\"Hello\"},\"done\":false}\n";

            Console.WriteLine("\nINPUT (Ollama NDJSON Chunk):");
            Console.WriteLine(ollamaChunk);

            // Act
            StreamingChunkResult result = await transformer.TransformChunkAsync(
                ollamaChunk,
                ApiFormatEnum.Ollama,
                ApiFormatEnum.OpenAI,
                RequestTypeEnum.GenerateChatCompletion);

            // Assert
            AssertNotNull(result);
            AssertNull(result.Error);
            AssertNotNull(result.ChunkData);
            AssertEqual("text/plain", result.ContentType);
            AssertTrue(result.IsServerSentEvent);

            string chunkText = System.Text.Encoding.UTF8.GetString(result.ChunkData);
            Console.WriteLine("\nOUTPUT (OpenAI SSE Chunk):");
            Console.WriteLine($"Raw: {chunkText.Replace("\n", "\\n")}");

            AssertTrue(chunkText.StartsWith("data: "));
            AssertTrue(chunkText.EndsWith("\n\n"));

            string jsonData = chunkText.Substring(6, chunkText.Length - 8); // Remove "data: " and "\n\n"
            JsonDocument jsonDoc = JsonDocument.Parse(jsonData);
            JsonElement root = jsonDoc.RootElement;

            AssertEqual("chat.completion.chunk", root.GetProperty("object").GetString());
            AssertEqual("llama2", root.GetProperty("model").GetString());

            List<JsonElement> choices = root.GetProperty("choices").EnumerateArray().ToList();
            AssertEqual(1, choices.Count);

            JsonElement delta = choices[0].GetProperty("delta");
            AssertEqual("assistant", delta.GetProperty("role").GetString());
            AssertEqual("Hello", delta.GetProperty("content").GetString());

            Console.WriteLine("✅ Ollama to OpenAI chunk transformation verified successfully!");
        }

        #endregion

        #region Pipeline Tests

        private static async Task RunPipelineTests()
        {
            Console.WriteLine("\n=== Pipeline Transformation Tests ===");

            try
            {
                await CompleteWorkflow_OpenAIToOllama_ChatCompletion();
                Console.WriteLine("✅ All pipeline transformation tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Pipeline transformation test failed: {ex.Message}");
                throw;
            }
        }

        public static async Task CompleteWorkflow_OpenAIToOllama_ChatCompletion()
        {
            Console.WriteLine("\n=== Complete OpenAI to Ollama Pipeline Test ===");

            // Arrange - Create OpenAI JSON request
            string openAIJson = @"{
                ""model"": ""gpt-3.5-turbo"",
                ""messages"": [
                    { ""role"": ""system"", ""content"": ""You are a helpful assistant."" },
                    { ""role"": ""user"", ""content"": ""Hello, world!"" }
                ],
                ""stream"": true,
                ""temperature"": 0.7,
                ""max_tokens"": 100
            }";

            Console.WriteLine("\nINPUT (OpenAI Format):");
            Console.WriteLine(openAIJson);

            // Step 1: Deserialize OpenAI request
            OpenAIChatRequest openAIRequest = _serializer.DeserializeJson<OpenAIChatRequest>(openAIJson);

            // Step 2: Transform OpenAI -> Agnostic
            AgnosticChatRequest agnosticRequest = TransformOpenAIToAgnostic(openAIRequest);

            // Assert - Verify agnostic request
            AssertNotNull(agnosticRequest);
            AssertEqual("gpt-3.5-turbo", agnosticRequest.Model);
            AssertTrue(agnosticRequest.Stream);
            AssertEqual(0.7, agnosticRequest.Temperature!.Value, 0.01);
            AssertEqual(100, agnosticRequest.MaxTokens!.Value);
            AssertEqual(2, agnosticRequest.Messages.Count);

            Console.WriteLine("\nINTERMEDIATE (Agnostic Format):");
            Console.WriteLine(_serializer.SerializeJson(agnosticRequest));

            // Step 3: Transform Agnostic -> Ollama
            AgnosticToOllamaTransformer ollamaTransformer = new AgnosticToOllamaTransformer();
            AssertTrue(ollamaTransformer.CanTransform(ApiFormatEnum.Ollama, agnosticRequest));

            object ollamaRequest = await ollamaTransformer.TransformAsync(agnosticRequest);

            // Assert - Verify Ollama request format
            AssertNotNull(ollamaRequest);

            string ollamaJson = _serializer.SerializeJson(ollamaRequest);
            Console.WriteLine("\nOUTPUT (Ollama Format):");
            Console.WriteLine(ollamaJson);

            JsonDocument jsonDoc = JsonDocument.Parse(ollamaJson);
            JsonElement root = jsonDoc.RootElement;

            AssertEqual("gpt-3.5-turbo", root.GetProperty("model").GetString());
            AssertTrue(root.GetProperty("stream").GetBoolean());

            List<JsonElement> messages = root.GetProperty("messages").EnumerateArray().ToList();
            AssertEqual(2, messages.Count);

            JsonElement options = root.GetProperty("options");
            AssertEqual(0.7, options.GetProperty("temperature").GetDouble(), 0.01);
            AssertEqual(100, options.GetProperty("num_predict").GetInt32());

            Console.WriteLine("✅ Complete pipeline transformation verified successfully!");
        }

        #endregion

        #region Assertion Helpers

        private static void AssertEqual<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new Exception($"Expected: {expected}, Actual: {actual}");
            }
        }

        private static void AssertEqual(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance)
            {
                throw new Exception($"Expected: {expected}, Actual: {actual}, Tolerance: {tolerance}");
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

        private static void AssertNotNull(object? obj, string? message = null)
        {
            if (obj == null)
            {
                throw new Exception(message ?? "Object should not be null");
            }
        }

        private static void AssertNull(object? obj, string? message = null)
        {
            if (obj != null)
            {
                throw new Exception(message ?? "Object should be null");
            }
        }

        #endregion
    }
}