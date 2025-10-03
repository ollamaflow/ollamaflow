namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama pull model result message for streaming updates during model download.
    /// </summary>
    public class OllamaPullModelResultMessage
    {
        /// <summary>
        /// Current status of the pull operation.
        /// Examples: "pulling manifest", "downloading", "verifying sha256 digest", "writing manifest", "removing any unused layers", "success"
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Digest of the layer being downloaded.
        /// Format: "sha256:hash"
        /// </summary>
        [JsonPropertyName("digest")]
        public string Digest { get; set; }

        /// <summary>
        /// Total size of the current layer in bytes.
        /// </summary>
        [JsonPropertyName("total")]
        public long? Total { get; set; }

        /// <summary>
        /// Number of bytes completed for the current layer.
        /// </summary>
        [JsonPropertyName("completed")]
        public long? Completed { get; set; }

        /// <summary>
        /// Error message if the pull operation failed.
        /// </summary>
        [JsonPropertyName("error")]
        public string Error { get; set; }

        /// <summary>
        /// Gets the download progress as a percentage.
        /// </summary>
        /// <returns>Progress percentage (0-100), or null if data unavailable.</returns>
        public double? GetProgressPercentage()
        {
            if (Total.HasValue && Total.Value > 0 && Completed.HasValue)
            {
                return (Completed.Value / (double)Total.Value) * 100.0;
            }
            return null;
        }

        /// <summary>
        /// Gets the remaining bytes to download.
        /// </summary>
        /// <returns>Remaining bytes, or null if data unavailable.</returns>
        public long? GetRemainingBytes()
        {
            if (Total.HasValue && Completed.HasValue)
            {
                return Total.Value - Completed.Value;
            }
            return null;
        }

        /// <summary>
        /// Formats the total size in a human-readable format.
        /// </summary>
        /// <returns>Formatted size string (e.g., "1.5 GB").</returns>
        public string GetFormattedTotalSize()
        {
            if (!Total.HasValue)
                return null;

            return FormatBytes(Total.Value);
        }

        /// <summary>
        /// Formats the completed size in a human-readable format.
        /// </summary>
        /// <returns>Formatted size string (e.g., "750 MB").</returns>
        public string GetFormattedCompletedSize()
        {
            if (!Completed.HasValue)
                return null;

            return FormatBytes(Completed.Value);
        }

        /// <summary>
        /// Gets a formatted progress string.
        /// </summary>
        /// <returns>Progress string (e.g., "750 MB / 1.5 GB (50%)").</returns>
        public string GetFormattedProgress()
        {
            if (!Total.HasValue || !Completed.HasValue)
                return null;

            var percentage = GetProgressPercentage();
            var percentStr = percentage.HasValue ? $" ({percentage.Value:F1}%)" : "";

            return $"{GetFormattedCompletedSize()} / {GetFormattedTotalSize()}{percentStr}";
        }

        /// <summary>
        /// Checks if this is a download progress message.
        /// </summary>
        /// <returns>True if this message contains download progress information.</returns>
        public bool IsDownloadProgress()
        {
            return Total.HasValue && Completed.HasValue && !string.IsNullOrEmpty(Digest);
        }

        /// <summary>
        /// Checks if the operation is complete.
        /// </summary>
        /// <returns>True if the status indicates completion.</returns>
        public bool IsComplete()
        {
            return Status?.Equals("success", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Checks if the operation has failed.
        /// </summary>
        /// <returns>True if an error occurred.</returns>
        public bool HasError()
        {
            return !string.IsNullOrEmpty(Error);
        }

        /// <summary>
        /// Checks if this is a status-only message (no download progress).
        /// </summary>
        /// <returns>True if this is a status message without progress data.</returns>
        public bool IsStatusMessage()
        {
            return !string.IsNullOrEmpty(Status) && !Total.HasValue && !Completed.HasValue;
        }

        /// <summary>
        /// Gets the layer identifier from the digest.
        /// </summary>
        /// <returns>Short form of the digest (first 12 characters of hash), or null if no digest.</returns>
        public string GetLayerId()
        {
            if (string.IsNullOrEmpty(Digest))
                return null;

            // Extract hash from "sha256:hash" format
            var parts = Digest.Split(':');
            if (parts.Length == 2 && parts[1].Length >= 12)
            {
                return parts[1].Substring(0, 12);
            }

            return Digest.Length > 12 ? Digest.Substring(0, 12) : Digest;
        }

        /// <summary>
        /// Estimates time remaining based on a given download rate.
        /// </summary>
        /// <param name="bytesPerSecond">Current download rate in bytes per second.</param>
        /// <returns>Estimated time remaining, or null if cannot be calculated.</returns>
        public TimeSpan? EstimateTimeRemaining(double bytesPerSecond)
        {
            if (bytesPerSecond <= 0)
                return null;

            var remaining = GetRemainingBytes();
            if (!remaining.HasValue || remaining.Value <= 0)
                return null;

            var secondsRemaining = remaining.Value / bytesPerSecond;
            return TimeSpan.FromSeconds(secondsRemaining);
        }

        /// <summary>
        /// Formats bytes into human-readable format.
        /// </summary>
        /// <param name="bytes">Number of bytes.</param>
        /// <returns>Formatted string (e.g., "1.5 GB").</returns>
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
        /// Ollama pull model result message.
        /// </summary>
        public OllamaPullModelResultMessage()
        {
        }
    }
}