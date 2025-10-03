namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama local model information.
    /// </summary>
    public class OllamaLocalModel
    {
        /// <summary>
        /// Model name including tag (e.g., "llama2:latest").
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Model digest/hash.
        /// </summary>
        [JsonPropertyName("digest")]
        public string Digest { get; set; }

        /// <summary>
        /// Model size in bytes.
        /// </summary>
        [JsonPropertyName("size")]
        public long? Size { get; set; }

        /// <summary>
        /// When the model was last modified.
        /// </summary>
        [JsonPropertyName("modified_at")]
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// Model details including format, family, parameter size, and quantization.
        /// </summary>
        [JsonPropertyName("details")]
        public OllamaModelDetails Details { get; set; }

        /// <summary>
        /// Gets the model's base name without tag.
        /// </summary>
        /// <returns>Base model name.</returns>
        public string GetBaseName()
        {
            if (string.IsNullOrEmpty(Name))
                return string.Empty;

            var colonIndex = Name.IndexOf(':');
            return colonIndex > 0 ? Name.Substring(0, colonIndex) : Name;
        }

        /// <summary>
        /// Gets the model's tag.
        /// </summary>
        /// <returns>Model tag or "latest" if no tag specified.</returns>
        public string GetTag()
        {
            if (string.IsNullOrEmpty(Name))
                return "latest";

            var colonIndex = Name.IndexOf(':');
            return colonIndex > 0 ? Name.Substring(colonIndex + 1) : "latest";
        }

        /// <summary>
        /// Gets the formatted size of the model.
        /// </summary>
        /// <returns>Human-readable size string.</returns>
        public string GetFormattedSize()
        {
            if (!Size.HasValue)
                return "Unknown";

            return FormatBytes(Size.Value);
        }

        /// <summary>
        /// Gets a short digest identifier.
        /// </summary>
        /// <returns>First 12 characters of the digest.</returns>
        public string GetShortDigest()
        {
            if (string.IsNullOrEmpty(Digest))
                return string.Empty;

            // Extract hash from "sha256:hash" format if present
            var parts = Digest.Split(':');
            var hash = parts.Length == 2 ? parts[1] : Digest;

            return hash.Length > 12 ? hash.Substring(0, 12) : hash;
        }

        /// <summary>
        /// Gets the age of the model since last modification.
        /// </summary>
        /// <returns>Time since last modification.</returns>
        public TimeSpan? GetAge()
        {
            if (!ModifiedAt.HasValue)
                return null;

            return DateTime.UtcNow - ModifiedAt.Value;
        }

        /// <summary>
        /// Gets a formatted age string.
        /// </summary>
        /// <returns>Human-readable age string.</returns>
        public string GetFormattedAge()
        {
            var age = GetAge();
            if (!age.HasValue)
                return "Unknown";

            if (age.Value.TotalDays >= 365)
                return $"{(int)(age.Value.TotalDays / 365)} year(s) ago";
            if (age.Value.TotalDays >= 30)
                return $"{(int)(age.Value.TotalDays / 30)} month(s) ago";
            if (age.Value.TotalDays >= 1)
                return $"{(int)age.Value.TotalDays} day(s) ago";
            if (age.Value.TotalHours >= 1)
                return $"{(int)age.Value.TotalHours} hour(s) ago";
            if (age.Value.TotalMinutes >= 1)
                return $"{(int)age.Value.TotalMinutes} minute(s) ago";

            return "Just now";
        }

        /// <summary>
        /// Checks if this model matches a given pattern.
        /// </summary>
        /// <param name="pattern">Pattern to match (supports wildcards).</param>
        /// <returns>True if the model name matches the pattern.</returns>
        public bool MatchesPattern(string pattern)
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(pattern))
                return false;

            // Simple wildcard support
            if (pattern.Contains("*"))
            {
                var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                    .Replace("\\*", ".*") + "$";
                return System.Text.RegularExpressions.Regex.IsMatch(Name, regexPattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return Name.Equals(pattern, StringComparison.OrdinalIgnoreCase);
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
        /// Ollama local model.
        /// </summary>
        public OllamaLocalModel()
        {
        }
    }
}