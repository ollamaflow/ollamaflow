namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Statistics about running models.
    /// </summary>
    public class OllamaRunningModelStatistics
    {
        /// <summary>
        /// Total number of running models.
        /// </summary>
        public int TotalRunningModels { get; set; }

        /// <summary>
        /// Total memory usage in bytes.
        /// </summary>
        public long TotalMemoryBytes { get; set; }

        /// <summary>
        /// Total memory usage formatted.
        /// </summary>
        public string TotalMemoryFormatted { get; set; }

        /// <summary>
        /// Total VRAM usage in bytes.
        /// </summary>
        public long TotalVRAMBytes { get; set; }

        /// <summary>
        /// Total VRAM usage formatted.
        /// </summary>
        public string TotalVRAMFormatted { get; set; }

        /// <summary>
        /// Total RAM usage in bytes.
        /// </summary>
        public long TotalRAMBytes { get; set; }

        /// <summary>
        /// Total RAM usage formatted.
        /// </summary>
        public string TotalRAMFormatted { get; set; }

        /// <summary>
        /// Average memory per model in bytes.
        /// </summary>
        public long AverageMemoryBytes { get; set; }

        /// <summary>
        /// Average memory per model formatted.
        /// </summary>
        public string AverageMemoryFormatted { get; set; }

        /// <summary>
        /// Model using the most memory.
        /// </summary>
        public OllamaRunningModel LargestModel { get; set; }

        /// <summary>
        /// Model using the least memory.
        /// </summary>
        public OllamaRunningModel SmallestModel { get; set; }

        /// <summary>
        /// Model expiring soonest.
        /// </summary>
        public OllamaRunningModel NextToExpire { get; set; }

        /// <summary>
        /// Number of models with VRAM allocation.
        /// </summary>
        public int ModelsUsingVRAM { get; set; }

        /// <summary>
        /// Average VRAM percentage across models.
        /// </summary>
        public double AverageVRAMPercentage { get; set; }

        /// <summary>
        /// Count of running models by family.
        /// </summary>
        public Dictionary<string, int> ModelsByFamily { get; set; }

        /// <summary>
        /// Creates statistics from running models result.
        /// </summary>
        /// <param name="result">The running models result.</param>
        public OllamaRunningModelStatistics(OllamaListRunningModelsResult result)
        {
            if (result?.Models == null || result.Models.Count == 0)
            {
                TotalRunningModels = 0;
                ModelsByFamily = new Dictionary<string, int>();
                return;
            }

            TotalRunningModels = result.Models.Count;
            TotalMemoryBytes = result.GetTotalMemoryUsage();
            TotalMemoryFormatted = result.GetFormattedTotalMemoryUsage();
            TotalVRAMBytes = result.GetTotalVRAMUsage();
            TotalVRAMFormatted = result.GetFormattedTotalVRAMUsage();
            TotalRAMBytes = result.GetTotalRAMUsage();
            TotalRAMFormatted = result.GetFormattedTotalRAMUsage();

            if (TotalRunningModels > 0)
            {
                AverageMemoryBytes = TotalMemoryBytes / TotalRunningModels;
                AverageMemoryFormatted = FormatBytes(AverageMemoryBytes);

                LargestModel = result.Models.OrderByDescending(m => m.Size ?? 0).FirstOrDefault();
                SmallestModel = result.Models.OrderBy(m => m.Size ?? 0).FirstOrDefault();
                NextToExpire = result.GetNextExpiringModel();

                ModelsUsingVRAM = result.Models.Count(m => m.SizeVRAM > 0);

                var vramPercentages = result.Models
                    .Select(m => m.GetVRAMPercentage())
                    .Where(p => p.HasValue)
                    .Select(p => p.Value)
                    .ToList();

                if (vramPercentages.Any())
                    AverageVRAMPercentage = vramPercentages.Average();
            }

            // Group by family
            ModelsByFamily = result.Models
                .Where(m => !string.IsNullOrEmpty(m.Details?.Family))
                .GroupBy(m => m.Details.Family)
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
                $"Running Models: {TotalRunningModels}",
                $"Total Memory: {TotalMemoryFormatted}",
                $"VRAM Usage: {TotalVRAMFormatted}",
                $"RAM Usage: {TotalRAMFormatted}",
                $"Average Memory: {AverageMemoryFormatted}"
            };

            if (ModelsUsingVRAM > 0)
            {
                lines.Add($"Models using VRAM: {ModelsUsingVRAM}/{TotalRunningModels}");
                lines.Add($"Average VRAM allocation: {AverageVRAMPercentage:F1}%");
            }

            if (NextToExpire != null)
            {
                lines.Add($"Next to expire: {NextToExpire.Name} in {NextToExpire.GetFormattedTimeUntilExpiration()}");
            }

            return string.Join("\n", lines);
        }
    }
}