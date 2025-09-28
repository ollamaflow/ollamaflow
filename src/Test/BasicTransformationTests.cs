using System.Text;
using Xunit;
using OllamaFlow.Core.Services.Transformation.Inbound;
using OllamaFlow.Core.Services.Transformation.Outbound;
using OllamaFlow.Core.Services.Transformation.Streaming;
using OllamaFlow.Core.Serialization;
using OllamaFlow.Core.Enums;
using OllamaFlow.Core.Helpers;
using OllamaFlow.Core.Models.Agnostic.Requests;
using OllamaFlow.Core.Models.Agnostic.Common;

namespace Test
{
    /// <summary>
    /// Basic unit tests for transformation services without mocking Watson classes.
    /// </summary>
    public class BasicTransformationTests
    {
        private readonly Serializer _serializer;
        private readonly OpenAIToAgnosticTransformer _openAITransformer;
        private readonly OllamaToAgnosticTransformer _ollamaTransformer;
        private readonly AgnosticToOpenAITransformer _agnosticToOpenAITransformer;
        private readonly AgnosticToOllamaTransformer _agnosticToOllamaTransformer;
        private readonly StreamingTransformer _streamingTransformer;

        public BasicTransformationTests()
        {
            _serializer = new Serializer();
            _openAITransformer = new OpenAIToAgnosticTransformer(_serializer);
            _ollamaTransformer = new OllamaToAgnosticTransformer(_serializer);
            _agnosticToOpenAITransformer = new AgnosticToOpenAITransformer();
            _agnosticToOllamaTransformer = new AgnosticToOllamaTransformer();
            _streamingTransformer = new StreamingTransformer(_serializer);
        }

        [Fact]
        public void OpenAITransformer_CanTransform_ValidCombination_ReturnsTrue()
        {
            bool result = _openAITransformer.CanTransform(ApiFormatEnum.OpenAI, RequestTypeEnum.GenerateChatCompletion);
            Assert.True(result);
        }

        [Fact]
        public void OpenAITransformer_CanTransform_InvalidFormat_ReturnsFalse()
        {
            bool result = _openAITransformer.CanTransform(ApiFormatEnum.Ollama, RequestTypeEnum.GenerateChatCompletion);
            Assert.False(result);
        }

        [Fact]
        public void OllamaTransformer_CanTransform_ValidCombination_ReturnsTrue()
        {
            bool result = _ollamaTransformer.CanTransform(ApiFormatEnum.Ollama, RequestTypeEnum.GenerateChatCompletion);
            Assert.True(result);
        }

        [Fact]
        public void OllamaTransformer_CanTransform_InvalidFormat_ReturnsFalse()
        {
            bool result = _ollamaTransformer.CanTransform(ApiFormatEnum.OpenAI, RequestTypeEnum.GenerateChatCompletion);
            Assert.False(result);
        }

        [Fact]
        public void StreamingTransformer_CanTransformStream_ValidCombination_ReturnsTrue()
        {
            bool result = _streamingTransformer.CanTransformStream(
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateChatCompletion);
            Assert.True(result);
        }

        [Fact]
        public void StreamingTransformer_CanTransformStream_InvalidRequestType_ReturnsFalse()
        {
            bool result = _streamingTransformer.CanTransformStream(
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateEmbeddings);
            Assert.False(result);
        }

        [Fact]
        public async Task StreamingTransformer_TransformChunk_EmptyChunk_ReturnsEmpty()
        {
            OllamaFlow.Core.Services.Transformation.Interfaces.StreamingChunkResult result = await _streamingTransformer.TransformChunkAsync(
                "",
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateChatCompletion);

            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Empty(result.ChunkData);
        }

        [Fact]
        public async Task StreamingTransformer_CreateFinalChunk_OpenAI_ReturnsCorrectFormat()
        {
            OllamaFlow.Core.Services.Transformation.Interfaces.StreamingChunkResult result = await _streamingTransformer.CreateFinalChunkAsync(
                ApiFormatEnum.OpenAI,
                RequestTypeEnum.GenerateChatCompletion);

            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.NotNull(result.ChunkData);
            Assert.True(result.IsFinal);

            string chunkText = Encoding.UTF8.GetString(result.ChunkData);
            Assert.Equal("data: [DONE]\n\n", chunkText);
        }

        [Fact]
        public async Task StreamingTransformer_CreateFinalChunk_Ollama_ReturnsCorrectFormat()
        {
            OllamaFlow.Core.Services.Transformation.Interfaces.StreamingChunkResult result = await _streamingTransformer.CreateFinalChunkAsync(
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateChatCompletion);

            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.NotNull(result.ChunkData);
            Assert.True(result.IsFinal);

            string chunkText = Encoding.UTF8.GetString(result.ChunkData);
            Assert.Contains("\"done\":true", chunkText);
        }

        [Fact]
        public async Task AgnosticToOllama_Transform_ChatRequest_ReturnsValidJson()
        {
            AgnosticChatRequest agnosticRequest = new AgnosticChatRequest
            {
                Model = "llama2",
                Messages = new List<AgnosticMessage>
                {
                    new() { Role = "user", Content = "Hello!" }
                },
                Stream = true,
                Temperature = 0.8
            };

            object result = await _agnosticToOllamaTransformer.TransformAsync(agnosticRequest);

            Assert.NotNull(result);
            string json = _serializer.SerializeJson(result);
            Assert.Contains("llama2", json);
            Assert.Contains("true", json); // Stream should be true
            Assert.Contains("Hello!", json);
        }

        [Fact]
        public async Task AgnosticToOpenAI_Transform_ChatRequest_ReturnsValidJson()
        {
            AgnosticChatRequest agnosticRequest = new AgnosticChatRequest
            {
                Model = "gpt-3.5-turbo",
                Messages = new List<AgnosticMessage>
                {
                    new() { Role = "user", Content = "Hello!" }
                },
                Stream = false,
                Temperature = 0.7
            };

            object result = await _agnosticToOpenAITransformer.TransformAsync(agnosticRequest);

            Assert.NotNull(result);
            string json = _serializer.SerializeJson(result);
            Assert.Contains("gpt-3.5-turbo", json);
            Assert.Contains("false", json); // Stream should be false
            Assert.Contains("Hello!", json);
        }
    }
}