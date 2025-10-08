namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Custom JSON converter for flexible embeddings handling (single array or array of arrays).
    /// </summary>
    public class OllamaEmbeddingsOutputConverter : JsonConverter<object>
    {
        /// <summary>
        /// Read.
        /// </summary>
        /// <param name="reader">Reader.</param>
        /// <param name="typeToConvert">Type to convert.</param>
        /// <param name="options">Options.</param>
        /// <returns>Object.</returns>
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException($"Unexpected token type: {reader.TokenType}. Expected StartArray.");
            }

            // Peek at the first element to determine structure
            var readerCopy = reader;
            readerCopy.Read();

            if (readerCopy.TokenType == JsonTokenType.Number)
            {
                // Single array of floats
                return ReadSingleEmbedding(ref reader);
            }
            else if (readerCopy.TokenType == JsonTokenType.StartArray)
            {
                // Array of arrays
                return ReadMultipleEmbeddings(ref reader);
            }
            else if (readerCopy.TokenType == JsonTokenType.EndArray)
            {
                // Empty array - skip to end and return empty list
                reader.Read();
                return new List<float>();
            }
            else
            {
                throw new JsonException($"Unexpected token type in array: {readerCopy.TokenType}. Expected Number or StartArray.");
            }
        }

        private List<float> ReadSingleEmbedding(ref Utf8JsonReader reader)
        {
            var embedding = new List<float>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                if (reader.TokenType != JsonTokenType.Number)
                    throw new JsonException($"Expected number in embedding array, got {reader.TokenType}");

                embedding.Add((float)reader.GetDouble());
            }

            if (embedding.Count == 0)
                throw new JsonException("Embedding array cannot be empty");

            return embedding;
        }

        private List<List<float>> ReadMultipleEmbeddings(ref Utf8JsonReader reader)
        {
            var embeddings = new List<List<float>>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException($"Expected array in embeddings array, got {reader.TokenType}");

                var embedding = new List<float>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    if (reader.TokenType != JsonTokenType.Number)
                        throw new JsonException($"Expected number in embedding array, got {reader.TokenType}");

                    embedding.Add((float)reader.GetDouble());
                }

                if (embedding.Count == 0)
                    throw new JsonException("Embedding array cannot be empty");

                embeddings.Add(embedding);
            }

            if (embeddings.Count == 0)
                throw new JsonException("Embeddings array cannot be empty");

            // Validate all embeddings have the same dimension
            var firstDimension = embeddings[0].Count;
            for (int i = 1; i < embeddings.Count; i++)
            {
                if (embeddings[i].Count != firstDimension)
                {
                    throw new JsonException($"All embeddings must have the same dimension. Expected {firstDimension}, got {embeddings[i].Count} at index {i}");
                }
            }

            return embeddings;
        }

        /// <summary>
        /// Write.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <param name="value">Value.</param>
        /// <param name="options">Options.</param>
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else if (value is List<float> singleEmbedding)
            {
                writer.WriteStartArray();
                foreach (var val in singleEmbedding)
                {
                    writer.WriteNumberValue(val);
                }
                writer.WriteEndArray();
            }
            else if (value is List<List<float>> multipleEmbeddings)
            {
                writer.WriteStartArray();
                foreach (var embedding in multipleEmbeddings)
                {
                    writer.WriteStartArray();
                    foreach (var val in embedding)
                    {
                        writer.WriteNumberValue(val);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
            }
            else
            {
                throw new JsonException($"Cannot serialize type {value.GetType()}");
            }
        }
    }
}