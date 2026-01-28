import { OllamaflowSdk } from '../src';
import { api } from './setupTest';
import {
  mockCompletionResponse,
  mockChatCompletionResponse,
  mockEmbeddingsResponse,
  mockOllamaCompletionResponse,
  mockOllamaChatCompletionResponse,
  mockModelListResponse,
  mockModelInformationResponse,
  mockPullModelResponse,
  mockOllamaEmbeddingsResponse,
  mockFrontend,
  mockFrontendListResponse,
  mockBackend,
  mockBackendListResponse,
  mockBackendHealth,
  mockBackendHealthListResponse,
} from './mockData';
import { getServer } from './server';
import { handlers } from './handlers';

const server = getServer(handlers);

describe('OllamaflowSdk', () => {
  beforeAll(() => {
    server.listen();
  });
  afterEach(() => {
    server.resetHandlers();
  });
  afterAll(() => {
    server.close();
  });

  describe('sdk', () => {
    it('should validate connectivity', async () => {
      const response = await api.validateConnectivity();
      expect(response).toBe(true);
    });
  });

  describe('OpenAI', () => {
    it('should generate a completion', async () => {
      const response = await api.OpenAI.generateCompletion({
        model: 'llama3.1:latest',
        prompt: 'Once upon a time, in a distant galaxy,',
        max_tokens: 512,
        temperature: 0.7,
        top_p: 0.9,
        top_k: 50,
        n: 1,
        stream: false,
        logprobs: null,
        echo: false,
        stop: ['<|endoftext|>', '<|im_end|>'],
        presence_penalty: 0.0,
        frequency_penalty: 0.0,
        repetition_penalty: 1.0,
      });
      expect(response).toEqual(mockCompletionResponse);
    });

    it('should generate a chat completion', async () => {
      const response = await api.OpenAI.generateChatCompletion({
        model: 'llama3.1:latest',
        messages: [{ role: 'user', content: 'Hello, how are you?' }],
        temperature: 0.7,
      });
      expect(response).toEqual(mockChatCompletionResponse);
    });

    it('should generate embeddings', async () => {
      const response = await api.OpenAI.generateEmbeddings({
        model: 'llama3.1:latest',
        input: 'Hello world',
      });
      expect(response).toEqual(mockEmbeddingsResponse);
    });
  });

  describe('Ollama', () => {
    it('should generate a completion', async () => {
      const response = await api.Ollama.generateCompletion({
        model: 'llama3.1:latest',
        prompt: 'Once upon a time',
        stream: false,
      });
      expect(response).toEqual(mockOllamaCompletionResponse);
    });

    it('should generate a chat completion', async () => {
      const response = await api.Ollama.generateChatCompletion({
        model: 'llama3.1:latest',
        messages: [{ role: 'user', content: 'Hello!' }],
        stream: false,
      });
      expect(response).toEqual(mockOllamaChatCompletionResponse);
    });

    it('should list local models', async () => {
      const response = await api.Ollama.listLocalModel();
      expect(response).toEqual(mockModelListResponse);
    });

    it('should list running models', async () => {
      const response = await api.Ollama.listRunningModel();
      expect(response).toEqual(mockModelListResponse);
    });

    it('should get model information', async () => {
      const response = await api.Ollama.modelInformation({ name: 'llama3.1:latest' });
      expect(response).toEqual(mockModelInformationResponse);
    });

    it('should pull a model', async () => {
      const response = await api.Ollama.pullModel({ name: 'llama3.1:latest' });
      expect(response).toEqual(mockPullModelResponse);
    });

    it('should generate embeddings', async () => {
      const response = await api.Ollama.generateEmbeddings({
        model: 'llama3.1:latest',
        input: 'Hello world',
      });
      expect(response).toEqual(mockOllamaEmbeddingsResponse);
    });

    it('should delete a model', async () => {
      const response = await api.Ollama.deleteModel({ name: 'llama3.1:latest' });
      expect(response).toBe(true);
    });
  });

  describe('Frontend', () => {
    it('should read all frontends', async () => {
      const response = await api.Frontend.readAll();
      expect(response).toEqual(mockFrontendListResponse);
    });

    it('should read a single frontend', async () => {
      const response = await api.Frontend.read('frontend-1');
      expect(response).toEqual(mockFrontend);
    });

    it('should check if a frontend exists', async () => {
      const response = await api.Frontend.exist('frontend-1');
      expect(response).toBe(true);
    });

    it('should create a frontend', async () => {
      const frontendCreateRequest = {
        Identifier: 'frontend-1',
        Name: 'Test Frontend',
        Hostname: 'example.com',
        TimeoutMs: 30000,
        LoadBalancing: 'round-robin',
        BlockHttp10: false,
        LogRequestFull: false,
        LogRequestBody: false,
        LogResponseBody: false,
        MaxRequestBodySize: 1048576,
        AllowRetries: true,
        AllowEmbeddings: true,
        AllowCompletions: true,
        PinnedEmbeddingsProperties: null,
        PinnedCompletionsProperties: null,
        Backends: ['backend-1'],
        RequiredModels: [],
      };
      const response = await api.Frontend.create(frontendCreateRequest);
      expect(response).toEqual(mockFrontend);
    });

    it('should update a frontend', async () => {
      const response = await api.Frontend.update(mockFrontend);
      expect(response).toEqual(mockFrontend);
    });

    it('should delete a frontend', async () => {
      const response = await api.Frontend.delete('frontend-1');
      expect(response).toBe(true);
    });
  });

  describe('Backend', () => {
    it('should read all backends', async () => {
      const response = await api.Backend.readAll();
      expect(response).toEqual(mockBackendListResponse);
    });

    it('should read a single backend', async () => {
      const response = await api.Backend.read('backend-1');
      // Note: read() and readHealth() share the same endpoint, so both return BackendHealth
      // BackendHealth extends Backend, so this is valid
      expect(response.Identifier).toBe(mockBackend.Identifier);
      expect(response.Name).toBe(mockBackend.Name);
      expect(response.Hostname).toBe(mockBackend.Hostname);
    });

    it('should check if a backend exists', async () => {
      const response = await api.Backend.exist('backend-1');
      expect(response).toBe(true);
    });

    it('should create a backend', async () => {
      const backendCreateRequest = {
        Identifier: 'backend-1',
        Name: 'Test Backend',
        Hostname: 'localhost',
        Port: 11434,
        Ssl: false,
        UnhealthyThreshold: 3,
        HealthyThreshold: 2,
        HealthCheckMethod: 'GET',
        HealthCheckUrl: '/api/tags',
        MaxParallelRequests: 10,
        RateLimitRequestsThreshold: 100,
        LogRequestBody: false,
        LogResponseBody: false,
        ApiFormat: 'ollama',
        AllowEmbeddings: true,
        AllowCompletions: true,
        PinnedEmbeddingsProperties: null,
        PinnedCompletionsProperties: null,
      };
      const response = await api.Backend.create(backendCreateRequest);
      expect(response).toEqual(mockBackend);
    });

    it('should update a backend', async () => {
      const response = await api.Backend.update(mockBackend);
      expect(response).toEqual(mockBackend);
    });

    it('should delete a backend', async () => {
      const response = await api.Backend.delete('backend-1');
      expect(response).toBe(true);
    });

    it('should read all backend health', async () => {
      const response = await api.Backend.readAllHealth();
      expect(response).toEqual(mockBackendHealthListResponse);
    });

    it('should read a single backend health', async () => {
      const response = await api.Backend.readHealth('backend-1');
      expect(response).toEqual(mockBackendHealth);
    });
  });

  describe('Null Exception Tests', () => {
    describe('OpenAI', () => {
      it('should throw ArgumentNullException when generateCompletion request is null', async () => {
        await expect(api.OpenAI.generateCompletion(null as any)).rejects.toThrow(
          'ArgumentNullException: request is null or empty'
        );
      });

      it('should throw ArgumentNullException when generateChatCompletion request is null', async () => {
        await expect(api.OpenAI.generateChatCompletion(null as any)).rejects.toThrow(
          'ArgumentNullException: request is null or empty'
        );
      });

      it('should throw ArgumentNullException when generateEmbeddings request is null', async () => {
        await expect(api.OpenAI.generateEmbeddings(null as any)).rejects.toThrow(
          'ArgumentNullException: request is null or empty'
        );
      });
    });

    describe('Ollama', () => {
      it('should throw ArgumentNullException when generateCompletion request is null', async () => {
        await expect(api.Ollama.generateCompletion(null as any)).rejects.toThrow(
          'ArgumentNullException: request is null or empty'
        );
      });

      it('should throw ArgumentNullException when generateChatCompletion request is null', async () => {
        await expect(api.Ollama.generateChatCompletion(null as any)).rejects.toThrow(
          'ArgumentNullException: request is null or empty'
        );
      });

      it('should throw ArgumentNullException when pullModel request is null', async () => {
        await expect(api.Ollama.pullModel(null as any)).rejects.toThrow(
          'ArgumentNullException: request is null or empty'
        );
      });

      it('should throw ArgumentNullException when modelInformation request is null', async () => {
        await expect(api.Ollama.modelInformation(null as any)).rejects.toThrow(
          'ArgumentNullException: request is null or empty'
        );
      });

      it('should throw ArgumentNullException when deleteModel request is null', async () => {
        await expect(api.Ollama.deleteModel(null as any)).rejects.toThrow(
          'ArgumentNullException: request is null or empty'
        );
      });

      it('should throw ArgumentNullException when generateEmbeddings request is null', async () => {
        await expect(api.Ollama.generateEmbeddings(null as any)).rejects.toThrow(
          'ArgumentNullException: request is null or empty'
        );
      });
    });

    describe('Frontend', () => {
      it('should throw ArgumentNullException when read id is null', async () => {
        await expect(api.Frontend.read(null as any)).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when read id is empty string', async () => {
        await expect(api.Frontend.read('')).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when exist id is null', async () => {
        await expect(api.Frontend.exist(null as any)).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when exist id is empty string', async () => {
        await expect(api.Frontend.exist('')).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when delete id is null', async () => {
        await expect(api.Frontend.delete(null as any)).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when delete id is empty string', async () => {
        await expect(api.Frontend.delete('')).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when create frontend is null', async () => {
        await expect(api.Frontend.create(null as any)).rejects.toThrow(
          'ArgumentNullException: frontend is null or empty'
        );
      });

      it('should throw error when update frontend is null', async () => {
        await expect(api.Frontend.update(null as any)).rejects.toThrow(
          "Cannot read properties of null (reading 'Identifier')"
        );
      });

      it('should throw ArgumentNullException when update frontend has null Identifier', async () => {
        await expect(api.Frontend.update({ ...mockFrontend, Identifier: null as any })).rejects.toThrow(
          'ArgumentNullException: frontend.Identifier is null or empty'
        );
      });

      it('should throw ArgumentNullException when update frontend has empty Identifier', async () => {
        await expect(api.Frontend.update({ ...mockFrontend, Identifier: '' })).rejects.toThrow(
          'ArgumentNullException: frontend.Identifier is null or empty'
        );
      });
    });

    describe('Backend', () => {
      it('should throw ArgumentNullException when read id is null', async () => {
        await expect(api.Backend.read(null as any)).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when read id is empty string', async () => {
        await expect(api.Backend.read('')).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when exist id is null', async () => {
        await expect(api.Backend.exist(null as any)).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when exist id is empty string', async () => {
        await expect(api.Backend.exist('')).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when delete id is null', async () => {
        await expect(api.Backend.delete(null as any)).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when delete id is empty string', async () => {
        await expect(api.Backend.delete('')).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when create backend is null', async () => {
        await expect(api.Backend.create(null as any)).rejects.toThrow(
          'ArgumentNullException: backend is null or empty'
        );
      });

      it('should throw error when update backend is null', async () => {
        await expect(api.Backend.update(null as any)).rejects.toThrow(
          "Cannot read properties of null (reading 'Identifier')"
        );
      });

      it('should throw ArgumentNullException when update backend has null Identifier', async () => {
        await expect(api.Backend.update({ ...mockBackend, Identifier: null as any })).rejects.toThrow(
          'ArgumentNullException: backend.Identifier is null or empty'
        );
      });

      it('should throw ArgumentNullException when update backend has empty Identifier', async () => {
        await expect(api.Backend.update({ ...mockBackend, Identifier: '' })).rejects.toThrow(
          'ArgumentNullException: backend.Identifier is null or empty'
        );
      });

      it('should throw ArgumentNullException when readHealth id is null', async () => {
        await expect(api.Backend.readHealth(null as any)).rejects.toThrow('ArgumentNullException: id is null or empty');
      });

      it('should throw ArgumentNullException when readHealth id is empty string', async () => {
        await expect(api.Backend.readHealth('')).rejects.toThrow('ArgumentNullException: id is null or empty');
      });
    });
  });
});
