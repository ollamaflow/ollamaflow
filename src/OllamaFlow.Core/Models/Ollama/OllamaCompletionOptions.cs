namespace OllamaFlow.Core.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama completion options for model parameters.
    /// </summary>
    public class OllamaCompletionOptions
    {
        /// <summary>
        /// Random seed for generation (optional).
        /// </summary>
        [JsonPropertyName("seed")]
        public int? Seed
        {
            get => _Seed;
            set => _Seed = value; // Any integer value is valid for seed
        }

        /// <summary>
        /// Number of tokens to generate (optional, default: infinite).
        /// -1 = infinite, -2 = fill context
        /// </summary>
        [JsonPropertyName("num_predict")]
        public int? NumPredict
        {
            get => _NumPredict;
            set
            {
                if (value.HasValue && value.Value < -2)
                    throw new ArgumentOutOfRangeException(nameof(NumPredict), "NumPredict must be >= -2 (-1 for infinite, -2 for fill context)");
                _NumPredict = value;
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
        /// Temperature for sampling (optional, default: 0.8).
        /// Range: 0.0 to 2.0
        /// </summary>
        [JsonPropertyName("temperature")]
        public float? Temperature
        {
            get => _Temperature;
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 2.0f))
                    throw new ArgumentOutOfRangeException(nameof(Temperature), "Temperature must be between 0.0 and 2.0");
                _Temperature = value;
            }
        }

        /// <summary>
        /// Top-k sampling parameter (optional, default: 40).
        /// Range: 0 to 100
        /// </summary>
        [JsonPropertyName("top_k")]
        public int? TopK
        {
            get => _TopK;
            set
            {
                if (value.HasValue && (value.Value < 0 || value.Value > 100))
                    throw new ArgumentOutOfRangeException(nameof(TopK), "TopK must be between 0 and 100");
                _TopK = value;
            }
        }

        /// <summary>
        /// Top-p sampling parameter (optional, default: 0.9).
        /// Range: 0.0 to 1.0
        /// </summary>
        [JsonPropertyName("top_p")]
        public float? TopP
        {
            get => _TopP;
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 1.0f))
                    throw new ArgumentOutOfRangeException(nameof(TopP), "TopP must be between 0.0 and 1.0");
                _TopP = value;
            }
        }

        /// <summary>
        /// Min-p sampling parameter (optional, default: 0.0).
        /// Range: 0.0 to 1.0
        /// </summary>
        [JsonPropertyName("min_p")]
        public float? MinP
        {
            get => _MinP;
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 1.0f))
                    throw new ArgumentOutOfRangeException(nameof(MinP), "MinP must be between 0.0 and 1.0");
                _MinP = value;
            }
        }

        /// <summary>
        /// Tail free sampling parameter (optional, default: 1.0).
        /// Range: 0.0 to 1.0
        /// </summary>
        [JsonPropertyName("tfs_z")]
        public float? TfsZ
        {
            get => _TfsZ;
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 1.0f))
                    throw new ArgumentOutOfRangeException(nameof(TfsZ), "TfsZ must be between 0.0 and 1.0");
                _TfsZ = value;
            }
        }

        /// <summary>
        /// Typical sampling parameter (optional, default: 1.0).
        /// Range: 0.0 to 1.0
        /// </summary>
        [JsonPropertyName("typical_p")]
        public float? TypicalP
        {
            get => _TypicalP;
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 1.0f))
                    throw new ArgumentOutOfRangeException(nameof(TypicalP), "TypicalP must be between 0.0 and 1.0");
                _TypicalP = value;
            }
        }

        /// <summary>
        /// Repeat penalty (optional, default: 1.1).
        /// Range: 0.0 to 2.0
        /// </summary>
        [JsonPropertyName("repeat_penalty")]
        public float? RepeatPenalty
        {
            get => _RepeatPenalty;
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 2.0f))
                    throw new ArgumentOutOfRangeException(nameof(RepeatPenalty), "RepeatPenalty must be between 0.0 and 2.0");
                _RepeatPenalty = value;
            }
        }

        /// <summary>
        /// Last n tokens to consider for repeat penalty (optional, default: 64).
        /// Range: 0 to context size
        /// </summary>
        [JsonPropertyName("repeat_last_n")]
        public int? RepeatLastN
        {
            get => _RepeatLastN;
            set
            {
                if (value.HasValue && value.Value < 0)
                    throw new ArgumentOutOfRangeException(nameof(RepeatLastN), "RepeatLastN must be >= 0");
                _RepeatLastN = value;
            }
        }

        /// <summary>
        /// Presence penalty (optional, default: 0.0).
        /// Range: -2.0 to 2.0
        /// </summary>
        [JsonPropertyName("presence_penalty")]
        public float? PresencePenalty
        {
            get => _PresencePenalty;
            set
            {
                if (value.HasValue && (value.Value < -2.0f || value.Value > 2.0f))
                    throw new ArgumentOutOfRangeException(nameof(PresencePenalty), "PresencePenalty must be between -2.0 and 2.0");
                _PresencePenalty = value;
            }
        }

        /// <summary>
        /// Frequency penalty (optional, default: 0.0).
        /// Range: -2.0 to 2.0
        /// </summary>
        [JsonPropertyName("frequency_penalty")]
        public float? FrequencyPenalty
        {
            get => _FrequencyPenalty;
            set
            {
                if (value.HasValue && (value.Value < -2.0f || value.Value > 2.0f))
                    throw new ArgumentOutOfRangeException(nameof(FrequencyPenalty), "FrequencyPenalty must be between -2.0 and 2.0");
                _FrequencyPenalty = value;
            }
        }

        /// <summary>
        /// Mirostat sampling mode (0/1/2) (optional, default: 0).
        /// 0 = disabled, 1 = Mirostat v1, 2 = Mirostat v2
        /// </summary>
        [JsonPropertyName("mirostat")]
        public int? Mirostat
        {
            get => _Mirostat;
            set
            {
                if (value.HasValue && (value.Value < 0 || value.Value > 2))
                    throw new ArgumentOutOfRangeException(nameof(Mirostat), "Mirostat must be 0, 1, or 2");
                _Mirostat = value;
            }
        }

        /// <summary>
        /// Mirostat target entropy tau (optional, default: 5.0).
        /// Range: 0.0 to 10.0
        /// </summary>
        [JsonPropertyName("mirostat_tau")]
        public float? MirostatTau
        {
            get => _MirostatTau;
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 10.0f))
                    throw new ArgumentOutOfRangeException(nameof(MirostatTau), "MirostatTau must be between 0.0 and 10.0");
                _MirostatTau = value;
            }
        }

        /// <summary>
        /// Mirostat learning rate eta (optional, default: 0.1).
        /// Range: 0.0 to 1.0
        /// </summary>
        [JsonPropertyName("mirostat_eta")]
        public float? MirostatEta
        {
            get => _MirostatEta;
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 1.0f))
                    throw new ArgumentOutOfRangeException(nameof(MirostatEta), "MirostatEta must be between 0.0 and 1.0");
                _MirostatEta = value;
            }
        }

        /// <summary>
        /// Penalize newline tokens (optional, default: true).
        /// </summary>
        [JsonPropertyName("penalize_newline")]
        public bool? PenalizeNewline { get; set; }

        /// <summary>
        /// Stop sequences for generation (optional).
        /// </summary>
        [JsonPropertyName("stop")]
        public List<string> Stop { get; set; }

        /// <summary>
        /// Enable NUMA support (optional, default: false).
        /// </summary>
        [JsonPropertyName("numa")]
        public bool? Numa { get; set; }

        /// <summary>
        /// Context size (optional, default: 2048).
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
        /// Batch size for prompt evaluation (optional, default: 512).
        /// Range: 1 to context size
        /// </summary>
        [JsonPropertyName("num_batch")]
        public int? NumBatch
        {
            get => _NumBatch;
            set
            {
                if (value.HasValue && value.Value < 1)
                    throw new ArgumentOutOfRangeException(nameof(NumBatch), "NumBatch must be >= 1");
                _NumBatch = value;
            }
        }

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
        /// Number of tokens to keep from initial prompt (optional, default: 4).
        /// Range: 0 to context size
        /// </summary>
        [JsonPropertyName("num_keep")]
        public int? NumKeep
        {
            get => _NumKeep;
            set
            {
                if (value.HasValue && value.Value < 0)
                    throw new ArgumentOutOfRangeException(nameof(NumKeep), "NumKeep must be >= 0");
                _NumKeep = value;
            }
        }

        /// <summary>
        /// Use memory locking (optional, default: false).
        /// </summary>
        [JsonPropertyName("use_mlock")]
        public bool? UseMlock { get; set; }

        /// <summary>
        /// Use memory mapping (optional, default: true).
        /// </summary>
        [JsonPropertyName("use_mmap")]
        public bool? UseMmap { get; set; }

        /// <summary>
        /// Vocabulary only mode (optional, default: false).
        /// </summary>
        [JsonPropertyName("vocab_only")]
        public bool? VocabOnly { get; set; }

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
        /// Logits all mode (optional).
        /// </summary>
        [JsonPropertyName("logits_all")]
        public bool? LogitsAll { get; set; }

        // Private backing fields
        private int? _Seed;
        private int? _NumPredict;
        private int? _NumGpu;
        private float? _Temperature;
        private int? _TopK;
        private float? _TopP;
        private float? _MinP;
        private float? _TfsZ;
        private float? _TypicalP;
        private float? _RepeatPenalty;
        private int? _RepeatLastN;
        private float? _PresencePenalty;
        private float? _FrequencyPenalty;
        private int? _Mirostat;
        private float? _MirostatTau;
        private float? _MirostatEta;
        private int? _NumCtx;
        private int? _NumBatch;
        private int? _NumThread;
        private int? _NumKeep;
        private int? _MainGpu;

        /// <summary>
        /// Ollama completion options.
        /// </summary>
        public OllamaCompletionOptions()
        {
        }
    }
}