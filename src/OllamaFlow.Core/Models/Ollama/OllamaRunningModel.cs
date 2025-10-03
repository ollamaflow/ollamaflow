namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama running model information.
    /// </summary>
    public class OllamaRunningModel
    {
        /// <summary>
        /// Model name including tag.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Model digest/hash.
        /// </summary>
        [JsonPropertyName("digest")]
        public string Digest { get; set; }

        /// <summary>
        /// Total memory size in bytes (RAM + VRAM).
        /// </summary>
        [JsonPropertyName("size")]
        public long? Size { get; set; }

        /// <summary>
        /// VRAM usage in bytes.
        /// </summary>
        [JsonPropertyName("size_vram")]
        public long? SizeVRAM { get; set; }

        /// <summary>
        /// When the model expires from memory.
        /// ISO 8601 format string.
        /// </summary>
        [JsonPropertyName("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Model details including format, family, parameter size, and quantization.
        /// Reusing the existing OllamaModelDetails class.
        /// </summary>
        [JsonPropertyName("details")]
        public OllamaModelDetails Details { get; set; }

        /// <summary>
        /// Gets the system RAM usage (total size minus VRAM).
        /// </summary>
        /// <returns>RAM usage in bytes.</returns>
        public long GetRAMUsage()
        {
            return (Size ?? 0) - (SizeVRAM ?? 0);
        }

        /// <summary>
        /// Gets the formatted total memory size.
        /// </summary>
        /// <returns>Human-readable size string.</returns>
        public string GetFormattedSize()
        {
            if (!Size.HasValue)
                return "Unknown";

            return FormatBytes(Size.Value);
        }

        /// <summary>
        /// Gets the formatted VRAM usage.
        /// </summary>
        /// <returns>Human-readable VRAM size string.</returns>
        public string GetFormattedVRAMSize()
        {
            if (!SizeVRAM.HasValue)
                return "Unknown";

            return FormatBytes(SizeVRAM.Value);
        }

        /// <summary>
        /// Gets the formatted RAM usage.
        /// </summary>
        /// <returns>Human-readable RAM size string.</returns>
        public string GetFormattedRAMSize()
        {
            return FormatBytes(GetRAMUsage());
        }

        /// <summary>
        /// Gets the percentage of memory in VRAM vs RAM.
        /// </summary>
        /// <returns>Percentage of memory in VRAM (0-100).</returns>
        public double? GetVRAMPercentage()
        {
            if (!Size.HasValue || Size.Value == 0 || !SizeVRAM.HasValue)
                return null;

            return (SizeVRAM.Value / (double)Size.Value) * 100.0;
        }

        /// <summary>
        /// Gets time until the model expires from memory.
        /// </summary>
        /// <returns>Time remaining or null if no expiration set.</returns>
        public TimeSpan? GetTimeUntilExpiration()
        {
            if (!ExpiresAt.HasValue)
                return null;

            var remaining = ExpiresAt.Value - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        /// <summary>
        /// Gets formatted time until expiration.
        /// </summary>
        /// <returns>Human-readable time remaining.</returns>
        public string GetFormattedTimeUntilExpiration()
        {
            var remaining = GetTimeUntilExpiration();
            if (!remaining.HasValue)
                return "No expiration";

            if (remaining.Value == TimeSpan.Zero)
                return "Expired";

            if (remaining.Value.TotalDays >= 1)
                return $"{(int)remaining.Value.TotalDays}d {remaining.Value.Hours}h";
            if (remaining.Value.TotalHours >= 1)
                return $"{(int)remaining.Value.TotalHours}h {remaining.Value.Minutes}m";
            if (remaining.Value.TotalMinutes >= 1)
                return $"{(int)remaining.Value.TotalMinutes}m {remaining.Value.Seconds}s";

            return $"{remaining.Value.Seconds}s";
        }

        /// <summary>
        /// Checks if the model has expired.
        /// </summary>
        /// <returns>True if the model has expired.</returns>
        public bool HasExpired()
        {
            return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
        }

        /// <summary>
        /// Gets estimated expiration time based on expires_at field.
        /// </summary>
        /// <returns>Estimated expiration DateTime or null.</returns>
        public DateTime? GetEstimatedExpiration()
        {
            return ExpiresAt;
        }

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
        /// Gets a memory usage summary.
        /// </summary>
        /// <returns>Summary string of memory usage.</returns>
        public string GetMemoryUsageSummary()
        {
            var parts = new List<string>();

            if (Size.HasValue)
                parts.Add($"Total: {GetFormattedSize()}");

            if (SizeVRAM.HasValue)
            {
                parts.Add($"VRAM: {GetFormattedVRAMSize()}");

                var vramPct = GetVRAMPercentage();
                if (vramPct.HasValue)
                    parts.Add($"({vramPct.Value:F1}%)");
            }

            var ramUsage = GetRAMUsage();
            if (ramUsage > 0)
                parts.Add($"RAM: {GetFormattedRAMSize()}");

            return string.Join(", ", parts);
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
        /// Ollama running model.
        /// </summary>
        public OllamaRunningModel()
        {
        }
    }
}