namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama list local models result.
    /// </summary>
    public class OllamaListLocalModelsResult
    {
        /// <summary>
        /// List of models available locally.
        /// </summary>
        [JsonPropertyName("models")]
        public List<OllamaLocalModel> Models { get; set; }

        /// <summary>
        /// Gets the total number of models.
        /// </summary>
        /// <returns>Number of local models.</returns>
        public int GetModelCount()
        {
            return Models?.Count ?? 0;
        }

        /// <summary>
        /// Gets the total size of all models in bytes.
        /// </summary>
        /// <returns>Total size in bytes.</returns>
        public long GetTotalSizeBytes()
        {
            if (Models == null || Models.Count == 0)
                return 0;

            return Models.Sum(m => m.Size ?? 0);
        }

        /// <summary>
        /// Gets the total size of all models in a human-readable format.
        /// </summary>
        /// <returns>Formatted total size string.</returns>
        public string GetFormattedTotalSize()
        {
            return FormatBytes(GetTotalSizeBytes());
        }

        /// <summary>
        /// Finds a model by name.
        /// </summary>
        /// <param name="modelName">The model name to search for.</param>
        /// <param name="exactMatch">Whether to require exact match or allow partial/case-insensitive match.</param>
        /// <returns>The matching model or null if not found.</returns>
        public OllamaLocalModel FindModel(string modelName, bool exactMatch = true)
        {
            if (Models == null || string.IsNullOrEmpty(modelName))
                return null;

            if (exactMatch)
            {
                return Models.FirstOrDefault(m => m.Name == modelName);
            }
            else
            {
                // Case-insensitive partial match
                return Models.FirstOrDefault(m =>
                    m.Name?.IndexOf(modelName, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        /// <summary>
        /// Gets all models from a specific family.
        /// </summary>
        /// <param name="family">The model family (e.g., "llama", "mistral").</param>
        /// <returns>List of models from the specified family.</returns>
        public List<OllamaLocalModel> GetModelsByFamily(string family)
        {
            if (Models == null || string.IsNullOrEmpty(family))
                return new List<OllamaLocalModel>();

            return Models
                .Where(m => m.Details?.BelongsToFamily(family) == true)
                .ToList();
        }

        /// <summary>
        /// Gets all quantized models.
        /// </summary>
        /// <returns>List of quantized models.</returns>
        public List<OllamaLocalModel> GetQuantizedModels()
        {
            if (Models == null)
                return new List<OllamaLocalModel>();

            return Models
                .Where(m => m.Details?.IsQuantized() == true)
                .ToList();
        }

        /// <summary>
        /// Gets models sorted by size.
        /// </summary>
        /// <param name="ascending">True for smallest first, false for largest first.</param>
        /// <returns>Sorted list of models.</returns>
        public List<OllamaLocalModel> GetModelsSortedBySize(bool ascending = true)
        {
            if (Models == null)
                return new List<OllamaLocalModel>();

            if (ascending)
                return Models.OrderBy(m => m.Size ?? 0).ToList();
            else
                return Models.OrderByDescending(m => m.Size ?? 0).ToList();
        }

        /// <summary>
        /// Gets models sorted by modification date.
        /// </summary>
        /// <param name="mostRecentFirst">True for most recent first.</param>
        /// <returns>Sorted list of models.</returns>
        public List<OllamaLocalModel> GetModelsSortedByDate(bool mostRecentFirst = true)
        {
            if (Models == null)
                return new List<OllamaLocalModel>();

            if (mostRecentFirst)
                return Models.OrderByDescending(m => m.ModifiedAt ?? DateTime.MinValue).ToList();
            else
                return Models.OrderBy(m => m.ModifiedAt ?? DateTime.MinValue).ToList();
        }

        /// <summary>
        /// Gets models that were modified within a specific time period.
        /// </summary>
        /// <param name="since">The start date/time.</param>
        /// <returns>List of models modified since the specified time.</returns>
        public List<OllamaLocalModel> GetModelsModifiedSince(DateTime since)
        {
            if (Models == null)
                return new List<OllamaLocalModel>();

            return Models
                .Where(m => m.ModifiedAt.HasValue && m.ModifiedAt.Value >= since)
                .ToList();
        }

        /// <summary>
        /// Groups models by their base name (without tag).
        /// </summary>
        /// <returns>Dictionary grouping models by base name.</returns>
        public Dictionary<string, List<OllamaLocalModel>> GroupModelsByBaseName()
        {
            if (Models == null || Models.Count == 0)
                return new Dictionary<string, List<OllamaLocalModel>>();

            return Models
                .GroupBy(m => m.GetBaseName())
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Gets statistics about the local models.
        /// </summary>
        /// <returns>Model statistics.</returns>
        public OllamaModelStatistics GetStatistics()
        {
            return new OllamaModelStatistics(this);
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
        /// Ollama list local models result.
        /// </summary>
        public OllamaListLocalModelsResult()
        {
            Models = new List<OllamaLocalModel>();
        }
    }
}