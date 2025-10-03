namespace OllamaFlow.Core.Models.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI generate chat completion request.
    /// </summary>
    public class OpenAIGenerateChatCompletionRequest
    {
        /// <summary>
        /// ID of the model to use (required).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Messages for the chat conversation (required).
        /// </summary>
        [JsonPropertyName("messages")]
        public List<OpenAIChatMessage> Messages { get; set; } = null;

        /// <summary>
        /// Response format specification.
        /// </summary>
        [JsonPropertyName("response_format")]
        public OpenAIResponseFormat ResponseFormat { get; set; } = null;

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
        /// Number of chat completions to generate for each input message.
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
        /// Modify the likelihood of specified tokens appearing in the completion.
        /// </summary>
        [JsonPropertyName("logit_bias")]
        public Dictionary<string, float> LogitBias { get; set; } = null;

        /// <summary>
        /// Include the log probabilities on the logprobs most likely tokens.
        /// </summary>
        [JsonPropertyName("logprobs")]
        public bool? Logprobs { get; set; } = null;

        /// <summary>
        /// Number of most likely tokens to return at each token position.
        /// </summary>
        [JsonPropertyName("top_logprobs")]
        public int? TopLogprobs
        {
            get => _TopLogprobs;
            set
            {
                if (value.HasValue && (value.Value < 0 || value.Value > 20))
                    throw new ArgumentOutOfRangeException(nameof(TopLogprobs), "TopLogprobs must be between 0 and 20");
                _TopLogprobs = value;
            }
        }

        /// <summary>
        /// A unique identifier representing your end-user.
        /// </summary>
        [JsonPropertyName("user")]
        public string User { get; set; } = null;

        /// <summary>
        /// Functions/tools the model may call.
        /// </summary>
        [JsonPropertyName("tools")]
        public List<OpenAITool> Tools { get; set; } = null;

        /// <summary>
        /// Controls which (if any) tool is called by the model.
        /// </summary>
        [JsonPropertyName("tool_choice")]
        public object ToolChoice { get; set; } = null;

        /// <summary>
        /// Whether to enable parallel function calling.
        /// </summary>
        [JsonPropertyName("parallel_tool_calls")]
        public bool? ParallelToolCalls { get; set; } = null;

        /// <summary>
        /// Random seed for deterministic generation.
        /// </summary>
        [JsonPropertyName("seed")]
        public int? Seed { get; set; } = null;

        // Private backing fields
        private int? _MaxTokens;
        private float? _Temperature;
        private float? _TopP;
        private int? _N;
        private float? _PresencePenalty;
        private float? _FrequencyPenalty;
        private int? _TopLogprobs;

        /// <summary>
        /// OpenAI generate chat completion request.
        /// </summary>
        public OpenAIGenerateChatCompletionRequest()
        {
        }
    }
}