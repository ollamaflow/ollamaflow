import { OllamaflowSdk } from 'ollamaflow-sdk';

var api = new OllamaflowSdk({ endpoint: 'http://view.homedns.org:11434/', bearerToken: 'ollamaflowadmin' });

//region OpenAI
const generateCompletionOpenAI = async () => {
  try {
    const response = await api.OpenAI.generateCompletion(
      {
        model: 'qwen2.5:7b',
        prompt: 'Once upon a time, in a distant galaxy,',
        max_tokens: 512,
        temperature: 0.7,
        top_p: 0.9,
        top_k: 50,
        n: 1,
        stream: true,
        logprobs: null,
        echo: false,
        stop: ['<|endoftext|>', '<|im_end|>'],
        presence_penalty: 0.0,
        frequency_penalty: 0.0,
        repetition_penalty: 1.0,
        best_of: 1,
        logit_bias: {},
        seed: null,
        suffix: null,
        use_beam_search: false,
        length_penalty: 1.0,
        early_stopping: false,
        skip_special_tokens: true,
        spaces_between_special_tokens: true,
        include_stop_str_in_output: false,
        ignore_eos: false,
        min_tokens: 0,
        stop_token_ids: [],
        bad_words: [],
        response_format: {
          type: 'text',
        },
        guided_json: null,
        guided_regex: null,
        guided_choice: null,
        guided_grammar: null,
        guided_decoding_backend: null,
        guided_whitespace_pattern: null,
      },
      (token) => {
        console.log(token, 'token');
      }
    );
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

generateCompletionOpenAI();

const generateChatCompletionOpenAIFlow = async () => {
  try {
    const response = await api.OpenAI.generateChatCompletion({
      model: 'llama3.1:latest',
      messages: [
        {
          role: 'system',
          content: 'You are a helpful assistant.',
        },
        {
          role: 'user',
          content: 'Hello, how can you help me today?',
        },
      ],
      temperature: 0.7,
      top_p: 0.9,
      top_k: 50,
      max_tokens: 512,
      stream: false,
      stop: ['<|endoftext|>', '<|im_end|>'],
      presence_penalty: 0.0,
      frequency_penalty: 0.0,
      repetition_penalty: 1.0,
      n: 1,
      best_of: 1,
      logit_bias: {},
      logprobs: null,
      top_logprobs: null,
      seed: null,
      use_beam_search: false,
      length_penalty: 1.0,
      early_stopping: false,
      skip_special_tokens: true,
      spaces_between_special_tokens: true,
      include_stop_str_in_output: false,
      ignore_eos: false,
      min_tokens: 0,
      stop_token_ids: [],
      bad_words: [],
      response_format: {
        type: 'text',
      },
      guided_json: null,
      guided_regex: null,
      guided_choice: null,
      guided_grammar: null,
      guided_decoding_backend: null,
      guided_whitespace_pattern: null,
    });
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// generateChatCompletionOpenAIFlow();

const generatEmbeddingsOpenAIFlow = async () => {
  try {
    const response = await api.OpenAI.generateEmbeddings({
      model: 'llama3.1:latest',
      input: 'The quick brown fox jumps over the lazy dog',
      encoding_format: 'float',
      dimensions: null,
      user: null,
    });
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// generatEmbeddingsOpenAIFlow();

//endregion

//region Ollama

const generateCompletionOllama = async () => {
  try {
    const response = await api.Ollama.generateCompletion({
      model: 'llama3.1:latest',
      prompt:
        'system: you are a helpful AI assistant, always be nice.\nuser: give me a very long overview of the C programming language.\nassistant:',
      stream: false,
      options: {
        num_predict: 1000,
        temperature: 0.8,
        top_p: 0.9,
        repeat_penalty: 1.1,
        stop: ['\nuser:'],
        num_ctx: 2048,
        num_batch: 512,
      },
    });
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// generateCompletionOllama();

const generateChatCompletionOllama = async () => {
  try {
    const response = await api.Ollama.generateChatCompletion({
      model: 'llama3.1:latest',
      stream: false,
      messages: [
        {
          role: 'system',
          content: 'you are a helpful AI assistant.  be nice',
        },
        {
          role: 'user',
          content: 'what can you tell me about botox',
        },
      ],
      options: {
        num_keep: 5,
        seed: 42,
        num_predict: 100,
        top_k: 20,
        top_p: 0.9,
        min_p: 0,
        tfs_z: 0.5,
        typical_p: 0.7,
        repeat_last_n: 33,
        temperature: 0.8,
        repeat_penalty: 1.2,
        presence_penalty: 1.5,
        frequency_penalty: 1,
        mirostat: 1,
        mirostat_tau: 0.8,
        mirostat_eta: 0.6,
        penalize_newline: true,
        numa: false,
        num_ctx: 1024,
        num_batch: 2,
        num_gpu: 1,
        main_gpu: 0,
        low_vram: false,
        f16_kv: true,
        vocab_only: false,
        use_mmap: true,
        use_mlock: false,
        num_thread: 8,
      },
    });
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// generateChatCompletionOllama();

const pullModel = async () => {
  try {
    const response = await api.Ollama.pullModel({ name: 'llama3.1' }, (token) => {
      console.log(token);
    });
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// pullModel();

const modelInformation = async () => {
  try {
    const response = await api.Ollama.modelInformation({ name: 'llama3.1:latest' });
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// modelInformation();

const deleteModel = async () => {
  try {
    const response = await api.Ollama.deleteModel({ name: 'llama3.1:latest' });
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// deleteModel();

const listLocalModel = async () => {
  try {
    const response = await api.Ollama.listLocalModel();
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// listLocalModel();

const listRunningModel = async () => {
  try {
    const response = await api.Ollama.listRunningModel();
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// listRunningModel();

const generateEmbeddingsOllama = async () => {
  try {
    const response = await api.Ollama.generateEmbeddings({
      model: 'all-minilm',
      input: 'asdf',
    });
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// generateEmbeddingsOllama();
//endregion

//region Frontend

const createFrontend = async () => {
  try {
    const response = await api.Frontend.create({
      Identifier: 'frontend1-test',
      Name: 'Default Ollama frontend',
      Hostname: 'localhost',
      TimeoutMs: 60000,
      LoadBalancing: 'RoundRobin',
      BlockHttp10: true,
      LogRequestFull: false,
      LogRequestBody: false,
      LogResponseBody: false,
      MaxRequestBodySize: 536870912,
      AllowRetries: true,
      AllowEmbeddings: true,
      AllowCompletions: true,
      PinnedEmbeddingsProperties: {},
      PinnedCompletionsProperties: {},
      Backends: ['backend1', 'backend2', 'backend3', 'backend4'],
      RequiredModels: ['all-minilm', 'qwen2.5:7b'],
    });
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// createFrontend();

const updateFrontend = async () => {
  try {
    const response = await api.Frontend.update({
      Identifier: 'frontend1-test',
      Name: 'Default Ollama frontend [updated]',
      Hostname: 'localhost',
      TimeoutMs: 60000,
      LoadBalancing: 'RoundRobin',
      BlockHttp10: true,
      MaxRequestBodySize: 536870912,
      Backends: ['backend1', 'backend2', 'backend3', 'backend4'],
      RequiredModels: ['all-minilm', 'qwen2.5:7b'],
      LogRequestFull: false,
      LogRequestBody: false,
      LogResponseBody: false,
      UseStickySessions: false,
      StickySessionExpirationMs: 1800000,
      PinnedEmbeddingsProperties: {},
      PinnedCompletionsProperties: {},
      AllowEmbeddings: true,
      AllowCompletions: true,
      AllowRetries: true,
      Active: true,
      CreatedUtc: '2025-11-21T10:53:23.289519Z',
      LastUpdateUtc: '2025-11-21T10:53:23.289519Z',
    });
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};
// updateFrontend();

const readFrontend = async () => {
  try {
    const response = await api.Frontend.read('frontend1-test');
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};
// readFrontend();

const readAllFrontends = async () => {
  try {
    const response = await api.Frontend.readAll();
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};
// readAllFrontends();

const existFrontend = async () => {
  try {
    const response = await api.Frontend.exist('frontend1-test');
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};
// existFrontend();

const deleteFrontend = async () => {
  try {
    const response = await api.Frontend.delete('frontend1-test');
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};
// deleteFrontend();
//endregion

//region Backend

const createBackend = async () => {
  try {
    const response = await api.Backend.create({
      Identifier: 'test-backend',
      Name: 'vllm',
      Hostname: '34.55.208.75',
      Port: 8000,
      Ssl: false,
      UnhealthyThreshold: 2,
      HealthyThreshold: 2,
      HealthCheckMethod: 'GET',
      HealthCheckUrl: '/health',
      MaxParallelRequests: 4,
      RateLimitRequestsThreshold: 10,
      LogRequestBody: false,
      LogResponseBody: false,
      ApiFormat: 'OpenAI',
      AllowEmbeddings: true,
      AllowCompletions: true,
      PinnedEmbeddingsProperties: {},
      PinnedCompletionsProperties: {},
    });
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// createBackend();

const updateBackend = async () => {
  try {
    const response = await api.Backend.update({
      Identifier: 'test-backend',
      Name: 'vllm[updated]',
      Hostname: '34.55.208.75',
      Port: 8000,
      Ssl: false,
      UnhealthyThreshold: 2,
      HealthyThreshold: 2,
      HealthCheckMethod: 'GET',
      HealthCheckUrl: '/health',
      MaxParallelRequests: 4,
      RateLimitRequestsThreshold: 10,
      LogRequestFull: false,
      LogRequestBody: false,
      LogResponseBody: false,
      ApiFormat: 'OpenAI',
      Labels: [],
      PinnedEmbeddingsProperties: {},
      PinnedCompletionsProperties: {},
      AllowEmbeddings: true,
      AllowCompletions: true,
      Active: true,
      CreatedUtc: '2025-11-21T10:57:38.085370Z',
      LastUpdateUtc: '2025-11-21T10:57:38.085370Z',
      ActiveRequests: 0,
      IsSticky: false,
    });
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};

// updateBackend();

const readBackend = async () => {
  try {
    const response = await api.Backend.read('test-backend');
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};
// readBackend();

const readAllBackends = async () => {
  try {
    const response = await api.Backend.readAll();
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};
// readAllBackends();

const existBackend = async () => {
  try {
    const response = await api.Backend.exist('test-backend');
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};
// existBackend();

const deleteBackend = async () => {
  try {
    const response = await api.Backend.delete('test-backend');
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};
// deleteBackend();

const readAllHealthBackends = async () => {
  try {
    const response = await api.Backend.readAllHealth();
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};
// readAllHealthBackends();

const readHealthBackend = async () => {
  try {
    const response = await api.Backend.readHealth('vllm');
    console.log(response, 'response');
  } catch (error) {
    console.log(error, 'error');
  }
};
// readHealthBackend();

//endregion
