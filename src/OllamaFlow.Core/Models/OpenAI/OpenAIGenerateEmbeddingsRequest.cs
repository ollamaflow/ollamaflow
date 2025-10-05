namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI generate embeddings request.
    /// </summary>
    public class OpenAIGenerateEmbeddingsRequest
    {
        /// <summary>
        /// ID of the model to use (required).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Input text to embed (required).
        /// Can be a string or an array of strings.
        /// </summary>
        [JsonPropertyName("input")]
        [JsonConverter(typeof(OpenAIEmbeddingsInputConverter))]
        public object Input
        {
            get => _Input;
            set => _Input = value;
        }

        /// <summary>
        /// The format to return the embeddings in.
        /// Can be either "float" or "base64".
        /// </summary>
        [JsonPropertyName("encoding_format")]
        public string EncodingFormat { get; set; } = null;

        /// <summary>
        /// The number of dimensions the resulting output embeddings should have.
        /// Only supported in some models.
        /// </summary>
        [JsonPropertyName("dimensions")]
        public int? Dimensions
        {
            get => _Dimensions;
            set
            {
                if (value.HasValue && value.Value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(Dimensions), "Dimensions must be greater than 0");
                _Dimensions = value;
            }
        }

        /// <summary>
        /// A unique identifier representing your end-user.
        /// </summary>
        [JsonPropertyName("user")]
        public string User { get; set; } = null;

        private object _Input;
        private int? _Dimensions;

        /// <summary>
        /// Sets a single input string.
        /// </summary>
        public void SetInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty", nameof(input));
            _Input = input;
        }

        /// <summary>
        /// Sets multiple input strings.
        /// </summary>
        public void SetInputs(List<string> inputs)
        {
            if (inputs == null || inputs.Count == 0)
                throw new ArgumentException("Inputs cannot be null or empty", nameof(inputs));
            if (inputs.Any(string.IsNullOrEmpty))
                throw new ArgumentException("Input array cannot contain null or empty strings", nameof(inputs));
            _Input = inputs;
        }

        /// <summary>
        /// OpenAI generate embeddings request.
        /// </summary>
        public OpenAIGenerateEmbeddingsRequest()
        {
        }
    }
}