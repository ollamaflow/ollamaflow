namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama show model information result.
    /// </summary>
    public class OllamaShowModelInfoResult
    {
        /// <summary>
        /// Model license information.
        /// </summary>
        [JsonPropertyName("license")]
        public string License { get; set; }

        /// <summary>
        /// Model file content (when verbose is true).
        /// </summary>
        [JsonPropertyName("modelfile")]
        public string Modelfile { get; set; }

        /// <summary>
        /// Model parameters configuration.
        /// </summary>
        [JsonPropertyName("parameters")]
        public string Parameters { get; set; }

        /// <summary>
        /// Template used for prompts.
        /// </summary>
        [JsonPropertyName("template")]
        public string Template { get; set; }

        /// <summary>
        /// System message/prompt.
        /// </summary>
        [JsonPropertyName("system")]
        public string System { get; set; }

        /// <summary>
        /// Model details including format, family, families, parameter size, and quantization level.
        /// </summary>
        [JsonPropertyName("details")]
        public OllamaModelDetails Details { get; set; }

        /// <summary>
        /// Messages/examples used in the model.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<OllamaChatMessage> Messages { get; set; }

        /// <summary>
        /// Model information including general metadata.
        /// </summary>
        [JsonPropertyName("model_info")]
        public Dictionary<string, object> ModelInfo { get; set; }

        /// <summary>
        /// Modified timestamp.
        /// </summary>
        [JsonPropertyName("modified_at")]
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// Checks if this is a verbose response with full details.
        /// </summary>
        /// <returns>True if verbose information is included.</returns>
        public bool IsVerboseResponse()
        {
            return !string.IsNullOrEmpty(Modelfile) ||
                   !string.IsNullOrEmpty(Parameters) ||
                   !string.IsNullOrEmpty(Template);
        }

        /// <summary>
        /// Checks if the model has a system prompt configured.
        /// </summary>
        /// <returns>True if system prompt is present.</returns>
        public bool HasSystemPrompt()
        {
            return !string.IsNullOrEmpty(System);
        }

        /// <summary>
        /// Checks if the model has example messages.
        /// </summary>
        /// <returns>True if example messages are present.</returns>
        public bool HasExampleMessages()
        {
            return Messages != null && Messages.Count > 0;
        }

        /// <summary>
        /// Gets the model's parameter size from details if available.
        /// </summary>
        /// <returns>Parameter size string (e.g., "7B", "13B") or null.</returns>
        public string GetParameterSize()
        {
            return Details?.ParameterSize;
        }

        /// <summary>
        /// Gets the model's quantization level from details if available.
        /// </summary>
        /// <returns>Quantization level string (e.g., "Q4_0", "Q8_0") or null.</returns>
        public string GetQuantizationLevel()
        {
            return Details?.QuantizationLevel;
        }

        /// <summary>
        /// Parses the parameters string into a dictionary.
        /// </summary>
        /// <returns>Dictionary of parameter key-value pairs.</returns>
        public Dictionary<string, string> ParseParameters()
        {
            if (string.IsNullOrEmpty(Parameters))
                return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();
            var lines = Parameters.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                var parts = trimmedLine.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2)
                {
                    result[parts[0]] = parts[1];
                }
            }

            return result;
        }

        /// <summary>
        /// Ollama show model information result.
        /// </summary>
        public OllamaShowModelInfoResult()
        {
        }
    }
}