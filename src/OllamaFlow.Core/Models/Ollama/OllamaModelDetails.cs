namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama model details.
    /// </summary>
    public class OllamaModelDetails
    {
        /// <summary>
        /// Parent model this was created from.
        /// </summary>
        [JsonPropertyName("parent_model")]
        public string ParentModel { get; set; }

        /// <summary>
        /// Model format (e.g., "gguf").
        /// </summary>
        [JsonPropertyName("format")]
        public string Format { get; set; }

        /// <summary>
        /// Model family (e.g., "llama", "mistral").
        /// </summary>
        [JsonPropertyName("family")]
        public string Family { get; set; }

        /// <summary>
        /// Model families/architectures.
        /// </summary>
        [JsonPropertyName("families")]
        public List<string> Families { get; set; }

        /// <summary>
        /// Parameter size (e.g., "7B", "13B", "70B").
        /// </summary>
        [JsonPropertyName("parameter_size")]
        public string ParameterSize { get; set; }

        /// <summary>
        /// Quantization level (e.g., "Q4_0", "Q4_K_M", "Q8_0").
        /// </summary>
        [JsonPropertyName("quantization_level")]
        public string QuantizationLevel { get; set; }

        /// <summary>
        /// Gets the estimated model size in billions of parameters.
        /// </summary>
        /// <returns>Number of billions of parameters, or null if cannot parse.</returns>
        public double? GetParameterSizeInBillions()
        {
            if (string.IsNullOrEmpty(ParameterSize))
                return null;

            // Remove 'B' suffix and try to parse
            var sizeStr = ParameterSize.TrimEnd('B', 'b').Trim();

            if (double.TryParse(sizeStr, out var size))
                return size;

            // Handle cases like "7.1" or "6.7"
            if (sizeStr.Contains('.') && double.TryParse(sizeStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var floatSize))
                return floatSize;

            return null;
        }

        /// <summary>
        /// Gets the quantization bits from the quantization level.
        /// </summary>
        /// <returns>Number of bits used for quantization, or null if cannot determine.</returns>
        public int? GetQuantizationBits()
        {
            if (string.IsNullOrEmpty(QuantizationLevel))
                return null;

            // Extract number from patterns like Q4_0, Q8_0, Q5_K_M
            if (QuantizationLevel.StartsWith("Q", StringComparison.OrdinalIgnoreCase))
            {
                var numberPart = QuantizationLevel.Substring(1);
                var underscoreIndex = numberPart.IndexOf('_');

                if (underscoreIndex > 0)
                    numberPart = numberPart.Substring(0, underscoreIndex);

                if (int.TryParse(numberPart, out var bits))
                    return bits;
            }

            return null;
        }

        /// <summary>
        /// Checks if this is a quantized model.
        /// </summary>
        /// <returns>True if the model is quantized.</returns>
        public bool IsQuantized()
        {
            return !string.IsNullOrEmpty(QuantizationLevel);
        }

        /// <summary>
        /// Checks if this model belongs to a specific family.
        /// </summary>
        /// <param name="familyName">The family name to check.</param>
        /// <returns>True if the model belongs to the specified family.</returns>
        public bool BelongsToFamily(string familyName)
        {
            if (string.IsNullOrEmpty(familyName))
                return false;

            if (!string.IsNullOrEmpty(Family) &&
                Family.Equals(familyName, StringComparison.OrdinalIgnoreCase))
                return true;

            if (Families != null &&
                Families.Any(f => f.Equals(familyName, StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        /// <summary>
        /// Ollama model details.
        /// </summary>
        public OllamaModelDetails()
        {
        }
    }
}