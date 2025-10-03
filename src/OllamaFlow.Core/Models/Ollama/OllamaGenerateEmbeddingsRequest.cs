namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama generate embeddings request.
    /// </summary>
    public class OllamaGenerateEmbeddingsRequest
    {
        /// <summary>
        /// Model name to use for generating embeddings (required).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Input text(s) to generate embeddings for (required).
        /// Can be a single string or an array of strings.
        /// Use SetInput() or SetInputs() to set values, and GetInputs() to retrieve as array.
        /// </summary>
        [JsonPropertyName("input")]
        [JsonConverter(typeof(OllamaEmbeddingsInputConverter))]
        public object Input
        {
            get => _Input;
            set => _Input = value;
        }

        /// <summary>
        /// Options for generating embeddings (optional).
        /// </summary>
        [JsonPropertyName("options")]
        public OllamaEmbeddingsOptions Options { get; set; } = null;

        /// <summary>
        /// How long to keep the model loaded in memory (optional).
        /// Examples: "5m", "10m", "1h", "never"
        /// </summary>
        [JsonPropertyName("keep_alive")]
        public string KeepAlive { get; set; } = null;

        /// <summary>
        /// Truncate inputs that exceed the model's context window (optional).
        /// </summary>
        [JsonPropertyName("truncate")]
        public bool? Truncate { get; set; } = null;

        // Private backing fields
        private object _Input;

        /// <summary>
        /// Sets a single input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        public void SetInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty", nameof(input));
            _Input = input;
        }

        /// <summary>
        /// Sets multiple input strings.
        /// </summary>
        /// <param name="inputs">The array of input strings.</param>
        public void SetInputs(List<string> inputs)
        {
            if (inputs == null || inputs.Count == 0)
                throw new ArgumentException("Inputs cannot be null or empty", nameof(inputs));
            if (inputs.Any(string.IsNullOrEmpty))
                throw new ArgumentException("Input array cannot contain null or empty strings", nameof(inputs));
            _Input = inputs;
        }

        /// <summary>
        /// Gets the input as a list of strings.
        /// If input is a single string, returns a list with one element.
        /// </summary>
        /// <returns>List of input strings.</returns>
        public List<string> GetInputs()
        {
            if (_Input == null)
                return null;

            if (_Input is string singleInput)
                return new List<string> { singleInput };

            if (_Input is List<string> multipleInputs)
                return multipleInputs;

            throw new InvalidOperationException($"Input is of unexpected type: {_Input.GetType()}");
        }

        /// <summary>
        /// Checks if the input is a single string.
        /// </summary>
        /// <returns>True if input is a single string, false otherwise.</returns>
        public bool IsSingleInput()
        {
            return _Input is string;
        }

        /// <summary>
        /// Ollama generate embeddings request.
        /// </summary>
        public OllamaGenerateEmbeddingsRequest()
        {
        }
    }
}