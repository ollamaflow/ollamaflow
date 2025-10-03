namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama list running models result.
    /// </summary>
    public class OllamaListRunningModelsResult
    {
        /// <summary>
        /// List of models currently loaded in memory.
        /// </summary>
        [JsonPropertyName("models")]
        public List<OllamaRunningModel> Models { get; set; }

        /// <summary>
        /// Gets the total number of running models.
        /// </summary>
        /// <returns>Number of running models.</returns>
        public int GetRunningModelCount()
        {
            return Models?.Count ?? 0;
        }

        /// <summary>
        /// Gets the total VRAM usage of all running models in bytes.
        /// </summary>
        /// <returns>Total VRAM usage in bytes.</returns>
        public long GetTotalVRAMUsage()
        {
            if (Models == null || Models.Count == 0)
                return 0;

            return Models.Sum(m => m.SizeVRAM ?? 0);
        }

        /// <summary>
        /// Gets the total VRAM usage formatted as a string.
        /// </summary>
        /// <returns>Formatted VRAM usage.</returns>
        public string GetFormattedTotalVRAMUsage()
        {
            return FormatBytes(GetTotalVRAMUsage());
        }

        /// <summary>
        /// Gets the total system RAM usage of all running models in bytes.
        /// </summary>
        /// <returns>Total RAM usage in bytes.</returns>
        public long GetTotalRAMUsage()
        {
            if (Models == null || Models.Count == 0)
                return 0;

            return Models.Sum(m => (m.Size ?? 0) - (m.SizeVRAM ?? 0));
        }

        /// <summary>
        /// Gets the total system RAM usage formatted as a string.
        /// </summary>
        /// <returns>Formatted RAM usage.</returns>
        public string GetFormattedTotalRAMUsage()
        {
            return FormatBytes(GetTotalRAMUsage());
        }

        /// <summary>
        /// Gets the total memory usage (VRAM + RAM) of all running models.
        /// </summary>
        /// <returns>Total memory usage in bytes.</returns>
        public long GetTotalMemoryUsage()
        {
            if (Models == null || Models.Count == 0)
                return 0;

            return Models.Sum(m => m.Size ?? 0);
        }

        /// <summary>
        /// Gets the total memory usage formatted as a string.
        /// </summary>
        /// <returns>Formatted total memory usage.</returns>
        public string GetFormattedTotalMemoryUsage()
        {
            return FormatBytes(GetTotalMemoryUsage());
        }

        /// <summary>
        /// Finds a running model by name.
        /// </summary>
        /// <param name="modelName">The model name to search for.</param>
        /// <returns>The running model or null if not found.</returns>
        public OllamaRunningModel FindRunningModel(string modelName)
        {
            if (Models == null || string.IsNullOrEmpty(modelName))
                return null;

            return Models.FirstOrDefault(m =>
                m.Name?.Equals(modelName, StringComparison.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// Checks if a specific model is currently running.
        /// </summary>
        /// <param name="modelName">The model name to check.</param>
        /// <returns>True if the model is running.</returns>
        public bool IsModelRunning(string modelName)
        {
            return FindRunningModel(modelName) != null;
        }

        /// <summary>
        /// Gets models that will expire within a specified timeframe.
        /// </summary>
        /// <param name="within">Timespan to check for expiration.</param>
        /// <returns>List of models expiring within the timeframe.</returns>
        public List<OllamaRunningModel> GetModelsExpiringWithin(TimeSpan within)
        {
            if (Models == null || Models.Count == 0)
                return new List<OllamaRunningModel>();

            var expirationTime = DateTime.UtcNow.Add(within);

            return Models
                .Where(m => m.GetEstimatedExpiration() != null &&
                           m.GetEstimatedExpiration() <= expirationTime)
                .ToList();
        }

        /// <summary>
        /// Gets the model that will expire next.
        /// </summary>
        /// <returns>Model with the earliest expiration or null.</returns>
        public OllamaRunningModel GetNextExpiringModel()
        {
            if (Models == null || Models.Count == 0)
                return null;

            return Models
                .Where(m => m.GetEstimatedExpiration() != null)
                .OrderBy(m => m.GetEstimatedExpiration())
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets models sorted by memory usage.
        /// </summary>
        /// <param name="ascending">True for smallest first, false for largest first.</param>
        /// <returns>Sorted list of running models.</returns>
        public List<OllamaRunningModel> GetModelsSortedByMemoryUsage(bool ascending = false)
        {
            if (Models == null)
                return new List<OllamaRunningModel>();

            if (ascending)
                return Models.OrderBy(m => m.Size ?? 0).ToList();
            else
                return Models.OrderByDescending(m => m.Size ?? 0).ToList();
        }

        /// <summary>
        /// Gets models sorted by VRAM usage.
        /// </summary>
        /// <param name="ascending">True for smallest first, false for largest first.</param>
        /// <returns>Sorted list of running models.</returns>
        public List<OllamaRunningModel> GetModelsSortedByVRAMUsage(bool ascending = false)
        {
            if (Models == null)
                return new List<OllamaRunningModel>();

            if (ascending)
                return Models.OrderBy(m => m.SizeVRAM ?? 0).ToList();
            else
                return Models.OrderByDescending(m => m.SizeVRAM ?? 0).ToList();
        }

        /// <summary>
        /// Groups running models by family.
        /// </summary>
        /// <returns>Dictionary of models grouped by family.</returns>
        public Dictionary<string, List<OllamaRunningModel>> GroupByFamily()
        {
            if (Models == null || Models.Count == 0)
                return new Dictionary<string, List<OllamaRunningModel>>();

            return Models
                .Where(m => !string.IsNullOrEmpty(m.Details?.Family))
                .GroupBy(m => m.Details.Family)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Gets memory usage statistics for running models.
        /// </summary>
        /// <returns>Memory usage statistics.</returns>
        public OllamaRunningModelStatistics GetStatistics()
        {
            return new OllamaRunningModelStatistics(this);
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
        /// Ollama list running models result.
        /// </summary>
        public OllamaListRunningModelsResult()
        {
            Models = new List<OllamaRunningModel>();
        }
    }
}