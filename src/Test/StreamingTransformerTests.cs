using System.Text;
using Xunit;
using OllamaFlow.Core.Services.Transformation.Streaming;
using OllamaFlow.Core.Serialization;
using OllamaFlow.Core.Enums;
using OllamaFlow.Core.Helpers;

namespace Test
{
    /// <summary>
    /// Unit tests for the StreamingTransformer class.
    /// </summary>
    public class StreamingTransformerTests
    {
        private readonly StreamingTransformer _transformer;
        private readonly Serializer _serializer;

        public StreamingTransformerTests()
        {
            _serializer = new Serializer();
            _transformer = new StreamingTransformer(_serializer);
        }

        [Fact]
        public void CanTransformStream_SupportedFormats_ReturnsTrue()
        {
            // Test OpenAI to Ollama
            Assert.True(_transformer.CanTransformStream(
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateChatCompletion));

            // Test Ollama to OpenAI
            Assert.True(_transformer.CanTransformStream(
                ApiFormatEnum.Ollama,
                ApiFormatEnum.OpenAI,
                RequestTypeEnum.GenerateChatCompletion));

            // Test same format (pass-through)
            Assert.True(_transformer.CanTransformStream(
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.OpenAI,
                RequestTypeEnum.GenerateChatCompletion));
        }

        [Fact]
        public void CanTransformStream_UnsupportedRequestType_ReturnsFalse()
        {
            // Test unsupported request type (embeddings)
            Assert.False(_transformer.CanTransformStream(
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateEmbeddings));

            // Test unsupported request type (model list)
            Assert.False(_transformer.CanTransformStream(
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.ListModels));
        }

        [Fact]
        public async Task TransformChunkAsync_OpenAIToOllama_TransformsCorrectly()
        {
            // Arrange
            string openAIChunk = "data: {\"id\":\"test-123\",\"object\":\"chat.completion.chunk\",\"created\":1699999999,\"model\":\"gpt-3.5-turbo\",\"choices\":[{\"index\":0,\"delta\":{\"role\":\"assistant\",\"content\":\"Hello\"}}]}\n\n";

            // Act
            OllamaFlow.Core.Services.Transformation.Interfaces.StreamingChunkResult result = await _transformer.TransformChunkAsync(
                openAIChunk,
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateChatCompletion);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.NotNull(result.ChunkData);
            Assert.Equal("application/x-ndjson", result.ContentType);
            Assert.False(result.IsServerSentEvent);

            // Verify the transformed chunk structure
            string chunkText = Encoding.UTF8.GetString(result.ChunkData);
            Assert.Contains("\"model\":\"gpt-3.5-turbo\"", chunkText);
            Assert.Contains("\"message\":", chunkText);
            Assert.Contains("\"content\":\"Hello\"", chunkText);
            Assert.Contains("\"done\":false", chunkText);
        }

        [Fact]
        public async Task TransformChunkAsync_OllamaToOpenAI_TransformsCorrectly()
        {
            // Arrange
            string ollamaChunk = "{\"model\":\"llama2\",\"created_at\":\"2023-11-14T14:30:00.000Z\",\"message\":{\"role\":\"assistant\",\"content\":\"Hello\"},\"done\":false}\n";

            // Act
            OllamaFlow.Core.Services.Transformation.Interfaces.StreamingChunkResult result = await _transformer.TransformChunkAsync(
                ollamaChunk,
                ApiFormatEnum.Ollama,
                ApiFormatEnum.OpenAI,
                RequestTypeEnum.GenerateChatCompletion);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.NotNull(result.ChunkData);
            Assert.Equal("text/plain", result.ContentType);
            Assert.True(result.IsServerSentEvent);

            // Verify the transformed chunk structure
            string chunkText = Encoding.UTF8.GetString(result.ChunkData);
            Assert.StartsWith("data: ", chunkText);
            Assert.Contains("\"object\":\"chat.completion.chunk\"", chunkText);
            Assert.Contains("\"model\":\"llama2\"", chunkText);
            Assert.Contains("\"choices\":", chunkText);
            Assert.Contains("\"delta\":", chunkText);
            Assert.Contains("\"content\":\"Hello\"", chunkText);
        }

        [Fact]
        public async Task TransformChunkAsync_PassThrough_ReturnsOriginal()
        {
            // Arrange
            string originalChunk = "data: {\"test\":\"content\"}\n\n";

            // Act
            OllamaFlow.Core.Services.Transformation.Interfaces.StreamingChunkResult result = await _transformer.TransformChunkAsync(
                originalChunk,
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.OpenAI,
                RequestTypeEnum.GenerateChatCompletion);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.NotNull(result.ChunkData);
            Assert.Equal(originalChunk, Encoding.UTF8.GetString(result.ChunkData));
        }

        [Fact]
        public async Task TransformChunkAsync_OpenAIDoneChunk_TransformsCorrectly()
        {
            // Arrange
            string doneChunk = "data: [DONE]\n\n";

            // Act
            OllamaFlow.Core.Services.Transformation.Interfaces.StreamingChunkResult result = await _transformer.TransformChunkAsync(
                doneChunk,
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateChatCompletion);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.NotNull(result.ChunkData);

            string chunkText = Encoding.UTF8.GetString(result.ChunkData);
            Assert.Contains("\"done\":true", chunkText);
        }

        [Fact]
        public async Task TransformChunkAsync_InvalidJson_ReturnsError()
        {
            // Arrange
            string invalidChunk = "data: {invalid json}\n\n";

            // Act
            OllamaFlow.Core.Services.Transformation.Interfaces.StreamingChunkResult result = await _transformer.TransformChunkAsync(
                invalidChunk,
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateChatCompletion);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Contains("Invalid JSON", result.Error);
        }

        [Fact]
        public async Task CreateFinalChunkAsync_OpenAI_ReturnsCorrectFormat()
        {
            // Act
            OllamaFlow.Core.Services.Transformation.Interfaces.StreamingChunkResult result = await _transformer.CreateFinalChunkAsync(
                ApiFormatEnum.OpenAI,
                RequestTypeEnum.GenerateChatCompletion);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.NotNull(result.ChunkData);
            Assert.True(result.IsFinal);

            string chunkText = Encoding.UTF8.GetString(result.ChunkData);
            Assert.Equal("data: [DONE]\n\n", chunkText);
        }

        [Fact]
        public async Task CreateFinalChunkAsync_Ollama_ReturnsCorrectFormat()
        {
            // Act
            OllamaFlow.Core.Services.Transformation.Interfaces.StreamingChunkResult result = await _transformer.CreateFinalChunkAsync(
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateChatCompletion);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.NotNull(result.ChunkData);
            Assert.True(result.IsFinal);

            string chunkText = Encoding.UTF8.GetString(result.ChunkData);
            Assert.Contains("\"done\":true", chunkText);
        }

        [Fact]
        public async Task TransformChunkAsync_EmptyChunk_ReturnsEmpty()
        {
            // Act
            OllamaFlow.Core.Services.Transformation.Interfaces.StreamingChunkResult result = await _transformer.TransformChunkAsync(
                "",
                ApiFormatEnum.OpenAI,
                ApiFormatEnum.Ollama,
                RequestTypeEnum.GenerateChatCompletion);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Empty(result.ChunkData);
        }
    }
}