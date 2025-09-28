using System.Text;
using Xunit;
using Moq;
using OllamaFlow.Core.Services.Transformation.Inbound;
using OllamaFlow.Core.Serialization;
using OllamaFlow.Core.Enums;
using OllamaFlow.Core.Helpers;
using OllamaFlow.Core.Models.Agnostic.Requests;
using WatsonWebserver.Core;

namespace Test
{
    /// <summary>
    /// Unit tests for inbound transformers.
    /// </summary>
    public class InboundTransformerTests
    {
        private readonly Serializer _serializer;
        private readonly OpenAIToAgnosticTransformer _openAITransformer;
        private readonly OllamaToAgnosticTransformer _ollamaTransformer;

        public InboundTransformerTests()
        {
            _serializer = new Serializer();
            _openAITransformer = new OpenAIToAgnosticTransformer(_serializer);
            _ollamaTransformer = new OllamaToAgnosticTransformer(_serializer);
        }

        [Theory]
        [InlineData(RequestTypeEnum.GenerateChatCompletion)]
        [InlineData(RequestTypeEnum.GenerateCompletion)]
        [InlineData(RequestTypeEnum.GenerateEmbeddings)]
        [InlineData(RequestTypeEnum.ListModels)]
        [InlineData(RequestTypeEnum.ShowModelInformation)]
        public void OpenAITransformer_CanTransform_SupportedTypes_ReturnsTrue(RequestTypeEnum requestType)
        {
            Assert.True(_openAITransformer.CanTransform(ApiFormatEnum.OpenAI, requestType));
        }

        [Fact]
        public void OpenAITransformer_CanTransform_UnsupportedFormat_ReturnsFalse()
        {
            Assert.False(_openAITransformer.CanTransform(ApiFormatEnum.Ollama, RequestTypeEnum.GenerateChatCompletion));
        }

        [Fact]
        public async Task OpenAITransformer_ChatCompletion_TransformsCorrectly()
        {
            // Arrange
            var openAIRequest = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = "Hello!" }
                },
                stream = true,
                temperature = 0.7,
                max_tokens = 150,
                top_p = 0.9,
                n = 1,
                stop = new[] { "END" },
                frequency_penalty = 0.5,
                presence_penalty = 0.3,
                seed = 12345
            };

            HttpContextBase context = CreateMockContext("/v1/chat/completions", _serializer.SerializeJson(openAIRequest));

            // Act
            object result = await _openAITransformer.TransformAsync(context, RequestTypeEnum.GenerateChatCompletion);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<AgnosticChatRequest>(result);

            AgnosticChatRequest chatRequest = (AgnosticChatRequest)result;
            Assert.Equal("gpt-4", chatRequest.Model);
            Assert.Equal(2, chatRequest.Messages.Count);
            Assert.Equal("system", chatRequest.Messages[0].Role);
            Assert.Equal("You are a helpful assistant.", chatRequest.Messages[0].Content);
            Assert.Equal("user", chatRequest.Messages[1].Role);
            Assert.Equal("Hello!", chatRequest.Messages[1].Content);
            Assert.True(chatRequest.Stream);
            Assert.Equal(0.7, chatRequest.Temperature);
            Assert.Equal(150, chatRequest.MaxTokens);
            Assert.Equal(0.9, chatRequest.TopP);
            Assert.Equal(1, chatRequest.N);
            Assert.Single(chatRequest.Stop);
            Assert.Equal("END", chatRequest.Stop[0]);
            Assert.Equal(0.5, chatRequest.FrequencyPenalty);
            Assert.Equal(0.3, chatRequest.PresencePenalty);
            Assert.Equal(12345, chatRequest.Seed);
        }

        [Fact]
        public async Task OpenAITransformer_Completion_TransformsCorrectly()
        {
            // Arrange
            var openAIRequest = new
            {
                model = "text-davinci-003",
                prompt = "Complete this sentence:",
                stream = false,
                temperature = 0.8,
                max_tokens = 100
            };

            HttpContextBase context = CreateMockContext("/v1/completions", _serializer.SerializeJson(openAIRequest));

            // Act
            object result = await _openAITransformer.TransformAsync(context, RequestTypeEnum.GenerateCompletion);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<AgnosticCompletionRequest>(result);

            AgnosticCompletionRequest completionRequest = (AgnosticCompletionRequest)result;
            Assert.Equal("text-davinci-003", completionRequest.Model);
            Assert.Equal("Complete this sentence:", completionRequest.Prompt);
            Assert.False(completionRequest.Stream);
            Assert.Equal(0.8, completionRequest.Temperature);
            Assert.Equal(100, completionRequest.MaxTokens);
        }

        [Theory]
        [InlineData(RequestTypeEnum.GenerateChatCompletion)]
        [InlineData(RequestTypeEnum.GenerateCompletion)]
        [InlineData(RequestTypeEnum.GenerateEmbeddings)]
        [InlineData(RequestTypeEnum.ListModels)]
        [InlineData(RequestTypeEnum.ShowModelInformation)]
        public void OllamaTransformer_CanTransform_SupportedTypes_ReturnsTrue(RequestTypeEnum requestType)
        {
            Assert.True(_ollamaTransformer.CanTransform(ApiFormatEnum.Ollama, requestType));
        }

        [Fact]
        public void OllamaTransformer_CanTransform_UnsupportedFormat_ReturnsFalse()
        {
            Assert.False(_ollamaTransformer.CanTransform(ApiFormatEnum.OpenAI, RequestTypeEnum.GenerateChatCompletion));
        }

        [Fact]
        public async Task OllamaTransformer_ChatCompletion_TransformsCorrectly()
        {
            // Arrange
            var ollamaRequest = new
            {
                model = "llama2",
                messages = new[]
                {
                    new { role = "user", content = "What is the capital of France?" }
                },
                stream = true,
                options = new
                {
                    temperature = 0.6,
                    top_p = 0.8,
                    seed = 42
                }
            };

            HttpContextBase context = CreateMockContext("/api/chat", _serializer.SerializeJson(ollamaRequest));

            // Act
            object result = await _ollamaTransformer.TransformAsync(context, RequestTypeEnum.GenerateChatCompletion);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<AgnosticChatRequest>(result);

            AgnosticChatRequest chatRequest = (AgnosticChatRequest)result;
            Assert.Equal("llama2", chatRequest.Model);
            Assert.Single(chatRequest.Messages);
            Assert.Equal("user", chatRequest.Messages[0].Role);
            Assert.Equal("What is the capital of France?", chatRequest.Messages[0].Content);
            Assert.True(chatRequest.Stream);
            Assert.Equal(0.6, chatRequest.Temperature);
            Assert.Equal(0.8, chatRequest.TopP);
            Assert.Equal(42, chatRequest.Seed);
        }

        [Fact]
        public async Task OllamaTransformer_Generate_TransformsCorrectly()
        {
            // Arrange
            var ollamaRequest = new
            {
                model = "codellama",
                prompt = "def fibonacci(n):",
                stream = false,
                options = new
                {
                    temperature = 0.1,
                    top_p = 0.9
                }
            };

            HttpContextBase context = CreateMockContext("/api/generate", _serializer.SerializeJson(ollamaRequest));

            // Act
            object result = await _ollamaTransformer.TransformAsync(context, RequestTypeEnum.GenerateCompletion);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<AgnosticCompletionRequest>(result);

            AgnosticCompletionRequest completionRequest = (AgnosticCompletionRequest)result;
            Assert.Equal("codellama", completionRequest.Model);
            Assert.Equal("def fibonacci(n):", completionRequest.Prompt);
            Assert.False(completionRequest.Stream);
            Assert.Equal(0.1, completionRequest.Temperature);
            Assert.Equal(0.9, completionRequest.TopP);
        }

        [Fact]
        public async Task OpenAITransformer_EmptyBody_ThrowsException()
        {
            // Arrange
            HttpContextBase context = CreateMockContext("/v1/chat/completions", "");

            // Act & Assert
            await Assert.ThrowsAsync<OllamaFlow.Core.Services.Transformation.TransformationException>(
                () => _openAITransformer.TransformAsync(context, RequestTypeEnum.GenerateChatCompletion));
        }

        [Fact]
        public async Task OpenAITransformer_InvalidJson_ThrowsException()
        {
            // Arrange
            HttpContextBase context = CreateMockContext("/v1/chat/completions", "{invalid json}");

            // Act & Assert
            await Assert.ThrowsAsync<OllamaFlow.Core.Services.Transformation.TransformationException>(
                () => _openAITransformer.TransformAsync(context, RequestTypeEnum.GenerateChatCompletion));
        }

        private HttpContextBase CreateMockContext(string path, string requestBody)
        {
            Mock<HttpContextBase> mockContext = new Mock<HttpContextBase>();
            Mock<HttpRequestBase> mockRequest = new Mock<HttpRequestBase>();

            mockRequest.Setup(r => r.DataAsString).Returns(requestBody);
            mockRequest.Setup(r => r.DataAsBytes).Returns(Encoding.UTF8.GetBytes(requestBody ?? ""));
            mockRequest.Setup(r => r.ContentType).Returns("application/json");

            Mock<UrlDetails> mockUrl = new Mock<UrlDetails>();
            mockUrl.Setup(u => u.RawWithoutQuery).Returns(path);
            mockRequest.Setup(r => r.Url).Returns(mockUrl.Object);

            Mock<SourceDetails> mockSource = new Mock<SourceDetails>();
            mockSource.Setup(s => s.IpAddress).Returns("127.0.0.1");
            mockRequest.Setup(r => r.Source).Returns(mockSource.Object);

            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);

            return mockContext.Object;
        }
    }
}