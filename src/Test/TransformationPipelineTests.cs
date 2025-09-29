using System.Text;
using Xunit;
using Moq;
using OllamaFlow.Core.Enums;
using OllamaFlow.Core.Services.Transformation;
using OllamaFlow.Core.Services.Transformation.Interfaces;
using OllamaFlow.Core.Serialization;
using OllamaFlow.Core.Helpers;
using OllamaFlow.Core.Models.Agnostic.Base;
using OllamaFlow.Core.Models.Agnostic.Requests;
using WatsonWebserver.Core;
using HttpMethod = WatsonWebserver.Core.HttpMethod;
using OllamaFlow.Core.Models;

namespace Test
{
    /// <summary>
    /// Unit tests for the TransformationPipeline class.
    /// </summary>
    public class TransformationPipelineTests
    {
        private readonly TransformationPipeline _pipeline;
        private readonly Serializer _serializer;

        public TransformationPipelineTests()
        {
            _serializer = new Serializer();
            _pipeline = new TransformationPipeline(_serializer);
        }

        [Fact]
        public void GetStreamingTransformer_ReturnsValidTransformer()
        {
            // Act
            OllamaFlow.Core.Services.Transformation.Interfaces.IStreamingTransformer transformer = _pipeline.GetStreamingTransformer();

            // Assert
            Assert.NotNull(transformer);
            Assert.IsType<OllamaFlow.Core.Services.Transformation.Streaming.StreamingTransformer>(transformer);
        }

        [Fact]
        public void SupportsStreamingTransformation_SupportedFormats_ReturnsTrue()
        {
            // Test OpenAI to Ollama chat completion
            Assert.True(_pipeline.SupportsStreamingTransformation(
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateChatCompletion));

            // Test Ollama to OpenAI completion
            Assert.True(_pipeline.SupportsStreamingTransformation(
                ApiFormatEnum.Ollama,
                ApiFormatEnum.OpenAI,
                RequestTypeEnum.GenerateCompletion));

            // Test same format
            Assert.True(_pipeline.SupportsStreamingTransformation(
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.OpenAI,
                RequestTypeEnum.GenerateChatCompletion));
        }

        [Fact]
        public void SupportsStreamingTransformation_UnsupportedFormats_ReturnsFalse()
        {
            // Test unsupported request types
            Assert.False(_pipeline.SupportsStreamingTransformation(
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateEmbeddings));

            Assert.False(_pipeline.SupportsStreamingTransformation(
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.ListModels));
        }

        [Fact]
        public async Task TransformInboundAsync_OpenAIChatRequest_ReturnsAgnosticRequest()
        {
            // Arrange
            var openAIRequest = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = "Hello, world!" }
                },
                stream = true,
                temperature = 0.7,
                max_tokens = 100
            };

            string requestJson = _serializer.SerializeJson(openAIRequest);
            HttpContextBase mockContext = CreateMockHttpContext("/v1/chat/completions", "POST", requestJson);

            // Act
            object result = await _pipeline.TransformInboundAsync(mockContext, ApiFormatEnum.OpenAI);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<AgnosticChatRequest>(result);

            AgnosticChatRequest chatRequest = (AgnosticChatRequest)result;
            Assert.Equal("gpt-3.5-turbo", chatRequest.Model);
            Assert.True(chatRequest.Stream);
            Assert.Equal(0.7, chatRequest.Temperature);
            Assert.Equal(100, chatRequest.MaxTokens);
            Assert.Single(chatRequest.Messages);
            Assert.Equal("user", chatRequest.Messages[0].Role);
            Assert.Equal("Hello, world!", chatRequest.Messages[0].Content);
        }

        [Fact]
        public async Task TransformOutboundAsync_AgnosticToOllama_ReturnsOllamaRequest()
        {
            // Arrange
            var agnosticRequest = new AgnosticChatRequest
            {
                Model = "llama2",
                Messages = new List<OllamaFlow.Core.Models.Agnostic.Common.AgnosticMessage>
                {
                    new() { Role = "user", Content = "Hello!" }
                },
                Stream = true,
                Temperature = 0.8
            };

            // Act
            object result = await _pipeline.TransformOutboundAsync(agnosticRequest, ApiFormatEnum.Ollama);

            // Assert
            Assert.NotNull(result);
            // The result should be a properly formatted object for Ollama
            string json = _serializer.SerializeJson(result);
            Assert.Contains("\"model\":\"llama2\"", json);
            Assert.Contains("\"stream\":true", json);
            Assert.Contains("\"messages\":", json);
            Assert.Contains("\"temperature\":0.8", json);
        }

        [Fact]
        public async Task TransformInboundAsync_OllamaChatRequest_ReturnsAgnosticRequest()
        {
            // Arrange
            var ollamaRequest = new
            {
                model = "llama2",
                messages = new[]
                {
                    new { role = "user", content = "Test message" }
                },
                stream = false,
                options = new { temperature = 0.5 }
            };

            string requestJson = _serializer.SerializeJson(ollamaRequest);
            HttpContextBase mockContext = CreateMockHttpContext("/api/chat", "POST", requestJson);

            // Act
            object result = await _pipeline.TransformInboundAsync(mockContext, ApiFormatEnum.Ollama);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<AgnosticChatRequest>(result);

            AgnosticChatRequest chatRequest = (AgnosticChatRequest)result;
            Assert.Equal("llama2", chatRequest.Model);
            Assert.False(chatRequest.Stream);
            Assert.Equal(0.5, chatRequest.Temperature);
            Assert.Single(chatRequest.Messages);
            Assert.Equal("user", chatRequest.Messages[0].Role);
            Assert.Equal("Test message", chatRequest.Messages[0].Content);
        }

        private HttpContextBase CreateMockHttpContext(string path, string method, string requestBody)
        {
            Mock<HttpContextBase> mockContext = new Mock<HttpContextBase>();
            Mock<HttpRequestBase> mockRequest = new Mock<HttpRequestBase>();
            Mock<UrlDetails> mockUrl = new Mock<UrlDetails>();

            // Setup URL
            mockUrl.Setup(u => u.RawWithoutQuery).Returns(path);
            mockRequest.Setup(r => r.Url).Returns(mockUrl.Object);

            // Setup method
            mockRequest.Setup(r => r.Method).Returns(HttpMethod.GET);
            if (method == "POST") mockRequest.Setup(r => r.Method).Returns(HttpMethod.POST);

            // Setup request body
            mockRequest.Setup(r => r.DataAsString).Returns(requestBody);
            mockRequest.Setup(r => r.DataAsBytes).Returns(Encoding.UTF8.GetBytes(requestBody));
            mockRequest.Setup(r => r.ContentType).Returns("application/json");

            // Setup headers
            Mock<System.Collections.Specialized.NameValueCollection> mockHeaders = new Mock<System.Collections.Specialized.NameValueCollection>();
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);

            // Setup source
            Mock<SourceDetails> mockSource = new Mock<SourceDetails>();
            mockSource.Setup(s => s.IpAddress).Returns("127.0.0.1");
            mockRequest.Setup(r => r.Source).Returns(mockSource.Object);

            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);

            return mockContext.Object;
        }
    }
}