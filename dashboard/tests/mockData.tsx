import { Backend, Frontend, BackendHealth } from "#/lib/store/slice/types";

export const mockBackendIdentifier = "backend-1";
export const mockFrontendIdentifier = "frontend-1";

export const mockBackendData: Backend = {
  Identifier: mockBackendIdentifier,
  Name: "Backend 1",
  ApiFormat: "Ollama",
  Hostname: "localhost",
  PinnedEmbeddingsProperties: {},
  PinnedCompletionsProperties: {},
  AllowEmbeddings: true,
  AllowCompletions: true,
  Port: 43411,
  Ssl: false,
  UnhealthyThreshold: 3,
  Labels: [],
  HealthyThreshold: 3,
  HealthCheckMethod: "GET",
  HealthCheckUrl: "/health",
  MaxParallelRequests: 10,
  RateLimitRequestsThreshold: 100,
  LogRequestFull: true,
  LogRequestBody: true,
  LogResponseBody: true,
  Active: true,
  CreatedUtc: "2021-01-01",
  LastUpdateUtc: "2021-01-01",
  ActiveRequests: 32,
};

export const mockFrontendData: Frontend = {
  Identifier: mockFrontendIdentifier,
  Name: "Frontend 1",
  Hostname: "localhost",
  TimeoutMs: 1000,
  PinnedEmbeddingsProperties: {},
  PinnedCompletionsProperties: {},
  AllowEmbeddings: true,
  AllowCompletions: true,
  AllowRetries: false,
  LoadBalancing: "round_robin",
  BlockHttp10: false,
  MaxRequestBodySize: 1000000,
  Backends: ["1"],
  RequiredModels: ["model1", "model2"],
  LogRequestFull: true,
  LogRequestBody: true,
  LogResponseBody: true,
  UseStickySessions: false,
  StickySessionExpirationMs: 1800000,
  Active: true,
  CreatedUtc: "2021-01-01",
  LastUpdateUtc: "2021-01-01",
};

export const mockBackendHealthData: BackendHealth = {
  Identifier: mockBackendIdentifier,
  Name: "Backend 123",
  ApiFormat: "Ollama",
  Hostname: "localhost",
  PinnedEmbeddingsProperties: {},
  PinnedCompletionsProperties: {},
  AllowEmbeddings: true,
  AllowCompletions: true,
  Port: 43411,
  Ssl: false,
  UnhealthyThreshold: 3,
  HealthyThreshold: 3,
  HealthCheckMethod: "GET",
  HealthCheckUrl: "/health",
  MaxParallelRequests: 10,
  RateLimitRequestsThreshold: 100,
  LogRequestFull: true,
  LogRequestBody: true,
  LogResponseBody: true,
  Active: true,
  CreatedUtc: "2021-01-01",
  LastUpdateUtc: "2021-01-01",
  UnhealthySinceUtc: "",
  Downtime: "0s",
  ActiveRequests: 32,
  Uptime: "99.9%",
  HealthySinceUtc: "2021-01-01",
};

export const mockBackendListData = [mockBackendData];
export const mockBackendHealthListData = [mockBackendHealthData];

// Authentication mock data
export const mockValidAccessKey = "valid-access-key-123";
export const mockInvalidAccessKey = "invalid-access-key";
export const mockValidServerUrl = "https://valid-server.com";
export const mockInvalidServerUrl = "https://invalid-server.com";
export const mockConnectivityResponse = { status: "ok" };
export const mockFrontendTestResponse = [mockFrontendData];
