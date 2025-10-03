namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI generate completion request.
    /// Note: The completions endpoint is legacy. Use chat completions for new applications.
    /// </summary>
    public class OpenAIGenerateCompletionRequest
    {
        /// <summary>
        /// ID of the model to use (required).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// The prompt(s) to generate completions for (required).
        /// Can be a string or an array of strings.
        /// </summary>
        [JsonPropertyName("prompt")]
        [JsonConverter(typeof(OpenAIPromptInputConverter))]
        public object Prompt
        {
            get => _Prompt;
            set => _Prompt = value;
        }

        /// <summary>
        /// The suffix that comes after a completion of inserted text.
        /// </summary>
        [JsonPropertyName("suffix")]
        public string Suffix { get; set; } = null;

        /// <summary>
        /// The maximum number of tokens to generate.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        public int? MaxTokens
        {
            get => _MaxTokens;
            set
            {
                if (value.HasValue && value.Value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(MaxTokens), "MaxTokens must be greater than 0");
                _MaxTokens = value;
            }
        }

        /// <summary>
        /// Temperature for sampling (0-2).
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
        /// Nucleus sampling parameter (0-1).
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
        /// Number of completions to generate for each prompt.
        /// </summary>
        [JsonPropertyName("n")]
        public int? N
        {
            get => _N;
            set
            {
                if (value.HasValue && value.Value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(N), "N must be greater than 0");
                _N = value;
            }
        }

        /// <summary>
        /// Whether to stream partial progress.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool? Stream { get; set; } = null;

        /// <summary>
        /// Include the log probabilities on the logprobs most likely tokens.
        /// </summary>
        [JsonPropertyName("logprobs")]
        public int? Logprobs
        {
            get => _Logprobs;
            set
            {
                if (value.HasValue && (value.Value < 0 || value.Value > 5))
                    throw new ArgumentOutOfRangeException(nameof(Logprobs), "Logprobs must be between 0 and 5");
                _Logprobs = value;
            }
        }

        /// <summary>
        /// Echo back the prompt in addition to the completion.
        /// </summary>
        [JsonPropertyName("echo")]
        public bool? Echo { get; set; } = null;

        /// <summary>
        /// Up to 4 sequences where the API will stop generating further tokens.
        /// </summary>
        [JsonPropertyName("stop")]
        [JsonConverter(typeof(OpenAIStopInputConverter))]
        public object Stop { get; set; } = null;

        /// <summary>
        /// Presence penalty (-2.0 to 2.0).
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
        /// Frequency penalty (-2.0 to 2.0).
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
        /// Generates best_of completions server-side and returns the best.
        /// </summary>
        [JsonPropertyName("best_of")]
        public int? BestOf
        {
            get => _BestOf;
            set
            {
                if (value.HasValue && value.Value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(BestOf), "BestOf must be greater than 0");
                _BestOf = value;
            }
        }

        /// <summary>
        /// Modify the likelihood of specified tokens appearing in the completion.
        /// </summary>
        [JsonPropertyName("logit_bias")]
        public Dictionary<string, float> LogitBias { get; set; } = null;

        /// <summary>
        /// A unique identifier representing your end-user.
        /// </summary>
        [JsonPropertyName("user")]
        public string User { get; set; } = null;

        /// <summary>
        /// Random seed for deterministic generation.
        /// </summary>
        [JsonPropertyName("seed")]
        public int? Seed { get; set; } = null;

        // Private backing fields
        private object _Prompt;
        private int? _MaxTokens;
        private float? _Temperature;
        private float? _TopP;
        private int? _N;
        private int? _Logprobs;
        private float? _PresencePenalty;
        private float? _FrequencyPenalty;
        private int? _BestOf;

        /// <summary>
        /// Sets a single prompt string.
        /// </summary>
        public void SetPrompt(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
                throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
            _Prompt = prompt;
        }

        /// <summary>
        /// Sets multiple prompts.
        /// </summary>
        public void SetPrompts(List<string> prompts)
        {
            if (prompts == null || prompts.Count == 0)
                throw new ArgumentException("Prompts cannot be null or empty", nameof(prompts));
            if (prompts.Any(string.IsNullOrEmpty))
                throw new ArgumentException("Prompts cannot contain null or empty strings", nameof(prompts));
            _Prompt = prompts;
        }

        /// <summary>
        /// OpenAI generate completion request.
        /// </summary>
        public OpenAIGenerateCompletionRequest()
        {
        }
    }
}