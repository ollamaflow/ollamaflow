namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Helper class for managing completion context across requests.
    /// </summary>
    public class OllamaCompletionContext
    {
        /// <summary>
        /// The context data.
        /// </summary>
        public List<int> Data { get; set; }

        /// <summary>
        /// The model this context is for.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// When this context was last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Creates a context from a completion result.
        /// </summary>
        /// <param name="result">The completion result.</param>
        /// <returns>Context object or null if no context available.</returns>
        public static OllamaCompletionContext FromResult(OllamaGenerateCompletionResult result)
        {
            if (result?.Context == null || result.Context.Count == 0)
                return null;

            return new OllamaCompletionContext
            {
                Data = result.Context,
                Model = result.Model,
                LastUpdated = result.CreatedAt ?? DateTime.UtcNow
            };
        }

        /// <summary>
        /// Checks if this context is valid for a given model.
        /// </summary>
        /// <param name="modelName">The model name to check.</param>
        /// <returns>True if context is valid for the model.</returns>
        public bool IsValidForModel(string modelName)
        {
            return !string.IsNullOrEmpty(Model) &&
                   Model.Equals(modelName, StringComparison.OrdinalIgnoreCase) &&
                   Data != null &&
                   Data.Count > 0;
        }

        /// <summary>
        /// Gets the age of this context.
        /// </summary>
        /// <returns>Time since last update.</returns>
        public TimeSpan GetAge()
        {
            return DateTime.UtcNow - LastUpdated;
        }

        /// <summary>
        /// Ollama completion context.
        /// </summary>
        public OllamaCompletionContext()
        {
            LastUpdated = DateTime.UtcNow;
        }
    }
}