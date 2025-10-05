namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama tool function definition.
    /// </summary>
    public class OllamaToolFunction
    {
        /// <summary>
        /// Name of the function (required).
        /// </summary>
        [JsonPropertyName("name")]
        public string Name
        {
            get => _Name;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    // Function names typically follow identifier rules
                    if (value.Length > 64)
                        throw new ArgumentException("Function name must not exceed 64 characters", nameof(Name));

                    // Basic validation for function name format
                    if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                        throw new ArgumentException("Function name must start with a letter or underscore and contain only letters, numbers, and underscores", nameof(Name));
                }
                _Name = value;
            }
        }

        /// <summary>
        /// Description of what the function does (required).
        /// </summary>
        [JsonPropertyName("description")]
        public string Description
        {
            get => _Description;
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Length > 1024)
                    throw new ArgumentException("Description must not exceed 1024 characters", nameof(Description));
                _Description = value;
            }
        }

        /// <summary>
        /// Parameters the function accepts, described as a JSON Schema object (required).
        /// </summary>
        [JsonPropertyName("parameters")]
        public OllamaToolParameters Parameters { get; set; } = null;

        private string _Name;
        private string _Description;

        /// <summary>
        /// Ollama tool function.
        /// </summary>
        public OllamaToolFunction()
        {
        }
    }
}