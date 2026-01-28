import { http, HttpResponse } from 'msw';
import { mockEndpoint } from './setupTest';
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

export const handlers = [
  // Connectivity check
  http.head(mockEndpoint, ({ request, params, cookies }) => {
    return HttpResponse.text('Hello'); // Simulating connectivity exists
  }),

  // OpenAI endpoints
  http.post(mockEndpoint + 'v1/completions', ({ request, params, cookies }) => {
    return HttpResponse.json(mockCompletionResponse);
  }),

  http.post(mockEndpoint + 'v1/chat/completions', ({ request, params, cookies }) => {
    return HttpResponse.json(mockChatCompletionResponse);
  }),

  http.post(mockEndpoint + 'v1/embeddings', ({ request, params, cookies }) => {
    return HttpResponse.json(mockEmbeddingsResponse);
  }),

  // Ollama endpoints
  http.post(mockEndpoint + 'api/generate', ({ request, params, cookies }) => {
    return HttpResponse.json(mockOllamaCompletionResponse);
  }),

  http.post(mockEndpoint + 'api/chat', ({ request, params, cookies }) => {
    return HttpResponse.json(mockOllamaChatCompletionResponse);
  }),

  http.get(mockEndpoint + 'api/tags', ({ request, params, cookies }) => {
    return HttpResponse.json(mockModelListResponse);
  }),

  http.get(mockEndpoint + 'api/ps', ({ request, params, cookies }) => {
    return HttpResponse.json(mockModelListResponse);
  }),

  http.post(mockEndpoint + 'api/show', ({ request, params, cookies }) => {
    return HttpResponse.json(mockModelInformationResponse);
  }),

  http.post(mockEndpoint + 'api/pull', ({ request, params, cookies }) => {
    return HttpResponse.json(mockPullModelResponse);
  }),

  http.post(mockEndpoint + 'api/embed', ({ request, params, cookies }) => {
    return HttpResponse.json(mockOllamaEmbeddingsResponse);
  }),

  http.delete(mockEndpoint + 'api/delete', ({ request, params, cookies }) => {
    return HttpResponse.json({ success: true });
  }),

  // Frontend endpoints
  http.get(mockEndpoint + 'v1.0/frontends', ({ request, params, cookies }) => {
    return HttpResponse.json(mockFrontendListResponse);
  }),

  http.get(mockEndpoint + 'v1.0/frontends/:id', ({ request, params, cookies }) => {
    return HttpResponse.json(mockFrontend);
  }),

  http.head(mockEndpoint + 'v1.0/frontends/:id', ({ request, params, cookies }) => {
    return HttpResponse.text('', { status: 200 });
  }),

  http.put(mockEndpoint + 'v1.0/frontends', ({ request, params, cookies }) => {
    return HttpResponse.json(mockFrontend);
  }),

  http.put(mockEndpoint + 'v1.0/frontends/:id', ({ request, params, cookies }) => {
    return HttpResponse.json(mockFrontend);
  }),

  http.delete(mockEndpoint + 'v1.0/frontends/:id', ({ request, params, cookies }) => {
    return HttpResponse.json({ success: true }, { status: 200 });
  }),

  // Backend endpoints
  http.get(mockEndpoint + 'v1.0/backends', ({ request, params, cookies }) => {
    return HttpResponse.json(mockBackendListResponse);
  }),

  // Health endpoint must come before :id route to avoid route conflict
  http.get(mockEndpoint + 'v1.0/backends/health', ({ request, params, cookies }) => {
    return HttpResponse.json(mockBackendHealthListResponse);
  }),

  http.get(mockEndpoint + 'v1.0/backends/:id', ({ request, params, cookies }) => {
    // This endpoint is used by both read() and readHealth(), so return BackendHealth
    // which extends Backend and has all required fields
    return HttpResponse.json(mockBackendHealth);
  }),

  http.head(mockEndpoint + 'v1.0/backends/:id', ({ request, params, cookies }) => {
    return HttpResponse.text('', { status: 200 });
  }),

  http.put(mockEndpoint + 'v1.0/backends', ({ request, params, cookies }) => {
    return HttpResponse.json(mockBackend);
  }),

  http.put(mockEndpoint + 'v1.0/backends/:id', ({ request, params, cookies }) => {
    return HttpResponse.json(mockBackend);
  }),

  http.delete(mockEndpoint + 'v1.0/backends/:id', ({ request, params, cookies }) => {
    return HttpResponse.json({ success: true }, { status: 200 });
  }),
];
