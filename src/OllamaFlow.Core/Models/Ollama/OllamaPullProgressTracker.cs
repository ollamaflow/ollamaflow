namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Helper class for tracking pull operation progress across multiple messages.
    /// </summary>
    public class OllamaPullProgressTracker
    {
        private DateTime _startTime;
        private long _lastCompleted;
        private DateTime _lastUpdate;
        private double _downloadRate;

        /// <summary>
        /// Total bytes to download across all layers.
        /// </summary>
        public long TotalBytes { get; private set; }

        /// <summary>
        /// Total bytes completed across all layers.
        /// </summary>
        public long CompletedBytes { get; private set; }

        /// <summary>
        /// Current status message.
        /// </summary>
        public string CurrentStatus { get; private set; }

        /// <summary>
        /// Current layer being downloaded.
        /// </summary>
        public string CurrentLayer { get; private set; }

        /// <summary>
        /// Number of layers completed.
        /// </summary>
        public int LayersCompleted { get; private set; }

        /// <summary>
        /// Gets the overall progress percentage.
        /// </summary>
        public double OverallProgress => TotalBytes > 0 ? (CompletedBytes / (double)TotalBytes) * 100.0 : 0;

        /// <summary>
        /// Gets the current download rate in bytes per second.
        /// </summary>
        public double DownloadRate => _downloadRate;

        /// <summary>
        /// Gets the elapsed time since the pull started.
        /// </summary>
        public TimeSpan ElapsedTime => DateTime.UtcNow - _startTime;

        /// <summary>
        /// Updates the tracker with a new message.
        /// </summary>
        /// <param name="message">The pull result message.</param>
        public void Update(OllamaPullModelResultMessage message)
        {
            if (message == null)
                return;

            CurrentStatus = message.Status;

            if (message.IsDownloadProgress())
            {
                // Update current layer
                if (CurrentLayer != message.Digest)
                {
                    if (!string.IsNullOrEmpty(CurrentLayer))
                        LayersCompleted++;

                    CurrentLayer = message.Digest;
                    _lastCompleted = 0;
                }

                // Update progress
                if (message.Total.HasValue && message.Completed.HasValue)
                {
                    // Calculate rate if we have previous data
                    if (_lastCompleted > 0 && message.Completed.Value > _lastCompleted)
                    {
                        var timeDiff = (DateTime.UtcNow - _lastUpdate).TotalSeconds;
                        if (timeDiff > 0)
                        {
                            var bytesDiff = message.Completed.Value - _lastCompleted;
                            _downloadRate = bytesDiff / timeDiff;
                        }
                    }

                    _lastCompleted = message.Completed.Value;
                    _lastUpdate = DateTime.UtcNow;

                    // Update totals (simplified - doesn't track multiple layers perfectly)
                    if (message.Total.Value > TotalBytes)
                        TotalBytes = message.Total.Value;

                    CompletedBytes = message.Completed.Value;
                }
            }
            else if (message.IsComplete())
            {
                CompletedBytes = TotalBytes;
                if (!string.IsNullOrEmpty(CurrentLayer))
                    LayersCompleted++;
            }
        }

        /// <summary>
        /// Estimates time remaining for the entire pull operation.
        /// </summary>
        /// <returns>Estimated time remaining, or null if cannot be calculated.</returns>
        public TimeSpan? EstimateTimeRemaining()
        {
            if (_downloadRate <= 0)
                return null;

            var remaining = TotalBytes - CompletedBytes;
            if (remaining <= 0)
                return null;

            return TimeSpan.FromSeconds(remaining / _downloadRate);
        }

        /// <summary>
        /// Gets a formatted summary of the pull progress.
        /// </summary>
        /// <returns>Summary string.</returns>
        public string GetSummary()
        {
            var progressStr = $"{OverallProgress:F1}%";
            var rateStr = _downloadRate > 0 ? $" at {FormatBytes((long)_downloadRate)}/s" : "";
            var timeStr = "";

            var remaining = EstimateTimeRemaining();
            if (remaining.HasValue)
            {
                timeStr = $" - {FormatTimeSpan(remaining.Value)} remaining";
            }

            return $"{CurrentStatus}: {progressStr}{rateStr}{timeStr}";
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
        /// Formats a timespan into human-readable format.
        /// </summary>
        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            if (timeSpan.TotalMinutes >= 1)
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            return $"{timeSpan.Seconds}s";
        }

        /// <summary>
        /// Creates a new pull progress tracker.
        /// </summary>
        public OllamaPullProgressTracker()
        {
            _startTime = DateTime.UtcNow;
            _lastUpdate = DateTime.UtcNow;
        }
    }
}