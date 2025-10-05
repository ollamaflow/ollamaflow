namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama tool call function details.
    /// </summary>
    public class OllamaToolCallFunction
    {
        /// <summary>
        /// Name of the function that was called (required).
        /// </summary>
        [JsonPropertyName("name")]
        public string Name
        {
            get => _Name;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Length > 64)
                        throw new ArgumentException("Function name must not exceed 64 characters", nameof(Name));

                    if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                        throw new ArgumentException("Function name must start with a letter or underscore and contain only letters, numbers, and underscores", nameof(Name));
                }
                _Name = value;
            }
        }

        /// <summary>
        /// Arguments passed to the function as a JSON string (required).
        /// </summary>
        [JsonPropertyName("arguments")]
        public string Arguments
        {
            get => _Arguments;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    // Validate that it's valid JSON
                    try
                    {
                        System.Text.Json.JsonDocument.Parse(value);
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        throw new ArgumentException($"Arguments must be valid JSON:{Environment.NewLine}{ex.ToString()}", nameof(Arguments));
                    }
                }
                _Arguments = value;
            }
        }

        private string _Name;
        private string _Arguments;

        /// <summary>
        /// Ollama tool call function.
        /// </summary>
        public OllamaToolCallFunction()
        {
        }
    }
}