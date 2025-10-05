namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama embeddings options.
    /// </summary>
    public class OllamaEmbeddingsOptions
    {
        /// <summary>
        /// Number of threads to use (optional).
        /// Range: 1 to 128
        /// </summary>
        [JsonPropertyName("num_thread")]
        public int? NumThread
        {
            get => _NumThread;
            set
            {
                if (value.HasValue && (value.Value < 1 || value.Value > 128))
                    throw new ArgumentOutOfRangeException(nameof(NumThread), "NumThread must be between 1 and 128");
                _NumThread = value;
            }
        }

        /// <summary>
        /// Context size (optional).
        /// Range: 128 to 1048576
        /// </summary>
        [JsonPropertyName("num_ctx")]
        public int? NumCtx
        {
            get => _NumCtx;
            set
            {
                if (value.HasValue && (value.Value < 128 || value.Value > 1048576))
                    throw new ArgumentOutOfRangeException(nameof(NumCtx), "NumCtx must be between 128 and 1048576");
                _NumCtx = value;
            }
        }

        /// <summary>
        /// Number of layers to offload to GPU (optional).
        /// 0 = no GPU, -1 = all layers
        /// </summary>
        [JsonPropertyName("num_gpu")]
        public int? NumGpu
        {
            get => _NumGpu;
            set
            {
                if (value.HasValue && value.Value < -1)
                    throw new ArgumentOutOfRangeException(nameof(NumGpu), "NumGpu must be >= -1 (-1 for all layers)");
                _NumGpu = value;
            }
        }

        /// <summary>
        /// Main GPU index (optional).
        /// Range: 0 to number of GPUs - 1
        /// </summary>
        [JsonPropertyName("main_gpu")]
        public int? MainGpu
        {
            get => _MainGpu;
            set
            {
                if (value.HasValue && value.Value < 0)
                    throw new ArgumentOutOfRangeException(nameof(MainGpu), "MainGpu must be >= 0");
                _MainGpu = value;
            }
        }

        /// <summary>
        /// Low VRAM mode (optional, default: false).
        /// </summary>
        [JsonPropertyName("low_vram")]
        public bool? LowVram { get; set; }

        /// <summary>
        /// F16 key-value storage (optional, default: true).
        /// </summary>
        [JsonPropertyName("f16_kv")]
        public bool? F16Kv { get; set; }

        /// <summary>
        /// Enable NUMA support (optional, default: false).
        /// </summary>
        [JsonPropertyName("numa")]
        public bool? Numa { get; set; }

        /// <summary>
        /// Use memory mapping (optional, default: true).
        /// </summary>
        [JsonPropertyName("use_mmap")]
        public bool? UseMmap { get; set; }

        /// <summary>
        /// Use memory locking (optional, default: false).
        /// </summary>
        [JsonPropertyName("use_mlock")]
        public bool? UseMlock { get; set; }

        private int? _NumThread;
        private int? _NumCtx;
        private int? _NumGpu;
        private int? _MainGpu;

        /// <summary>
        /// Ollama embeddings options.
        /// </summary>
        public OllamaEmbeddingsOptions()
        {
        }
    }
}