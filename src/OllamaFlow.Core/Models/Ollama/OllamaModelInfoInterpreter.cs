namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Helper class for interpreting model information.
    /// </summary>
    public class OllamaModelInfoInterpreter
    {
        private readonly OllamaShowModelInfoResult _result;

        /// <summary>
        /// Creates a new model info interpreter.
        /// </summary>
        /// <param name="result">The model info result to interpret.</param>
        public OllamaModelInfoInterpreter(OllamaShowModelInfoResult result)
        {
            _result = result ?? throw new ArgumentNullException(nameof(result));
        }

        /// <summary>
        /// Gets the estimated VRAM requirement in GB.
        /// </summary>
        /// <returns>Estimated VRAM in GB, or null if cannot estimate.</returns>
        public double? EstimateVRAMRequirement()
        {
            var paramSize = _result.Details?.GetParameterSizeInBillions();
            var quantBits = _result.Details?.GetQuantizationBits();

            if (!paramSize.HasValue)
                return null;

            // Rough estimation formula
            // Base: ~2 bytes per parameter for FP16
            // Quantized: bits/8 bytes per parameter
            double bytesPerParam = 2.0;

            if (quantBits.HasValue)
            {
                bytesPerParam = quantBits.Value / 8.0;
            }

            // Parameters in billions * bytes per param * 1.2 (overhead)
            var vramGB = (paramSize.Value * 1_000_000_000 * bytesPerParam * 1.2) / (1024.0 * 1024.0 * 1024.0);

            return Math.Round(vramGB, 1);
        }

        /// <summary>
        /// Gets the context length from parameters if available.
        /// </summary>
        /// <returns>Context length or null if not found.</returns>
        public int? GetContextLength()
        {
            var parameters = _result.ParseParameters();

            if (parameters.TryGetValue("num_ctx", out var ctxStr))
            {
                if (int.TryParse(ctxStr, out var ctx))
                    return ctx;
            }

            // Check model_info for context length
            if (_result.ModelInfo != null)
            {
                if (_result.ModelInfo.TryGetValue("context_length", out var ctxObj))
                {
                    if (ctxObj is long ctxLong)
                        return (int)ctxLong;
                    if (ctxObj is int ctxInt)
                        return ctxInt;
                    if (ctxObj is string ctxString && int.TryParse(ctxString, out var parsed))
                        return parsed;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the temperature setting from parameters if available.
        /// </summary>
        /// <returns>Temperature value or null if not found.</returns>
        public float? GetTemperature()
        {
            var parameters = _result.ParseParameters();

            if (parameters.TryGetValue("temperature", out var tempStr))
            {
                if (float.TryParse(tempStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var temp))
                    return temp;
            }

            return null;
        }

        /// <summary>
        /// Determines if this model is suitable for a given use case.
        /// </summary>
        /// <param name="requiredVRAM">Required VRAM in GB.</param>
        /// <param name="requiredContext">Required context length.</param>
        /// <returns>True if the model meets the requirements.</returns>
        public bool MeetsRequirements(double? requiredVRAM = null, int? requiredContext = null)
        {
            if (requiredVRAM.HasValue)
            {
                var estimatedVRAM = EstimateVRAMRequirement();
                if (!estimatedVRAM.HasValue || estimatedVRAM.Value > requiredVRAM.Value)
                    return false;
            }

            if (requiredContext.HasValue)
            {
                var contextLength = GetContextLength();
                if (!contextLength.HasValue || contextLength.Value < requiredContext.Value)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a summary of the model's key characteristics.
        /// </summary>
        /// <returns>Summary string.</returns>
        public string GetSummary()
        {
            var parts = new List<string>();

            if (_result.Details != null)
            {
                if (!string.IsNullOrEmpty(_result.Details.Family))
                    parts.Add($"Family: {_result.Details.Family}");

                if (!string.IsNullOrEmpty(_result.Details.ParameterSize))
                    parts.Add($"Size: {_result.Details.ParameterSize}");

                if (!string.IsNullOrEmpty(_result.Details.QuantizationLevel))
                    parts.Add($"Quantization: {_result.Details.QuantizationLevel}");
            }

            var vram = EstimateVRAMRequirement();
            if (vram.HasValue)
                parts.Add($"Est. VRAM: {vram.Value:F1} GB");

            var ctx = GetContextLength();
            if (ctx.HasValue)
                parts.Add($"Context: {ctx.Value:N0} tokens");

            return string.Join(", ", parts);
        }
    }
}