namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Statistics about local models.
    /// </summary>
    public class OllamaModelStatistics
    {
        /// <summary>
        /// Total number of models.
        /// </summary>
        public int TotalModels { get; set; }

        /// <summary>
        /// Total size of all models in bytes.
        /// </summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>
        /// Total size formatted as string.
        /// </summary>
        public string TotalSizeFormatted { get; set; }

        /// <summary>
        /// Average model size in bytes.
        /// </summary>
        public long AverageSizeBytes { get; set; }

        /// <summary>
        /// Average model size formatted as string.
        /// </summary>
        public string AverageSizeFormatted { get; set; }

        /// <summary>
        /// Largest model.
        /// </summary>
        public OllamaLocalModel LargestModel { get; set; }

        /// <summary>
        /// Smallest model.
        /// </summary>
        public OllamaLocalModel SmallestModel { get; set; }

        /// <summary>
        /// Most recently modified model.
        /// </summary>
        public OllamaLocalModel NewestModel { get; set; }

        /// <summary>
        /// Oldest modified model.
        /// </summary>
        public OllamaLocalModel OldestModel { get; set; }

        /// <summary>
        /// Count of models by family.
        /// </summary>
        public Dictionary<string, int> ModelsByFamily { get; set; }

        /// <summary>
        /// Count of models by quantization level.
        /// </summary>
        public Dictionary<string, int> ModelsByQuantization { get; set; }

        /// <summary>
        /// Count of models by parameter size.
        /// </summary>
        public Dictionary<string, int> ModelsByParameterSize { get; set; }

        /// <summary>
        /// Creates statistics from a list result.
        /// </summary>
        /// <param name="result">The list models result.</param>
        public OllamaModelStatistics(OllamaListLocalModelsResult result)
        {
            if (result?.Models == null || result.Models.Count == 0)
            {
                TotalModels = 0;
                ModelsByFamily = new Dictionary<string, int>();
                ModelsByQuantization = new Dictionary<string, int>();
                ModelsByParameterSize = new Dictionary<string, int>();
                return;
            }

            TotalModels = result.Models.Count;
            TotalSizeBytes = result.GetTotalSizeBytes();
            TotalSizeFormatted = result.GetFormattedTotalSize();

            if (TotalModels > 0)
            {
                AverageSizeBytes = TotalSizeBytes / TotalModels;
                AverageSizeFormatted = FormatBytes(AverageSizeBytes);

                LargestModel = result.Models.OrderByDescending(m => m.Size ?? 0).FirstOrDefault();
                SmallestModel = result.Models.OrderBy(m => m.Size ?? 0).FirstOrDefault();
                NewestModel = result.Models.OrderByDescending(m => m.ModifiedAt).FirstOrDefault();
                OldestModel = result.Models.OrderBy(m => m.ModifiedAt).FirstOrDefault();
            }

            // Group by family
            ModelsByFamily = result.Models
                .Where(m => !string.IsNullOrEmpty(m.Details?.Family))
                .GroupBy(m => m.Details.Family)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by quantization
            ModelsByQuantization = result.Models
                .Where(m => !string.IsNullOrEmpty(m.Details?.QuantizationLevel))
                .GroupBy(m => m.Details.QuantizationLevel)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by parameter size
            ModelsByParameterSize = result.Models
                .Where(m => !string.IsNullOrEmpty(m.Details?.ParameterSize))
                .GroupBy(m => m.Details.ParameterSize)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Formats bytes into human-readable format.
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:F2} {sizes[order]}";
        }

        /// <summary>
        /// Gets a summary of the statistics.
        /// </summary>
        /// <returns>Summary string.</returns>
        public string GetSummary()
        {
            var lines = new List<string>
            {
                $"Total Models: {TotalModels}",
                $"Total Size: {TotalSizeFormatted}",
                $"Average Size: {AverageSizeFormatted}"
            };

            if (ModelsByFamily.Count > 0)
            {
                var topFamily = ModelsByFamily.OrderByDescending(kvp => kvp.Value).First();
                lines.Add($"Most Common Family: {topFamily.Key} ({topFamily.Value} models)");
            }

            if (NewestModel != null)
            {
                lines.Add($"Most Recent: {NewestModel.Name} ({NewestModel.GetFormattedAge()})");
            }

            return string.Join("\n", lines);
        }
    }
}