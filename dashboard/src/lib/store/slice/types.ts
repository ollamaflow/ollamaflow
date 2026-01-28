export interface Frontend {
  Identifier: string;
  Name: string;
  Hostname: string;
  TimeoutMs: number;
  LoadBalancing: string;
  BlockHttp10: boolean;
  MaxRequestBodySize: number;
  Backends: string[];
  RequiredModels: string[];
  LogRequestFull: boolean;
  LogRequestBody: boolean;
  LogResponseBody: boolean;
  UseStickySessions: boolean;
  StickySessionExpirationMs: number;
  AllowRetries: boolean;
  PinnedEmbeddingsProperties: Record<string, any>;
  PinnedCompletionsProperties: Record<string, any>;
  AllowEmbeddings: boolean;
  AllowCompletions: boolean;
  Active: boolean;
  CreatedUtc: string;
  LastUpdateUtc: string;
}

export interface Backend {
  Identifier: string;
  Name: string;
  Hostname: string;
  Port: number;
  Ssl: boolean;
  ApiFormat: string;
  BearerToken?: string;
  Querystring?: string;
  Headers?: Record<string, string>;
  UnhealthyThreshold: number;
  HealthyThreshold: number;
  HealthCheckMethod: string;
  HealthCheckUrl: string;
  MaxParallelRequests: number;
  RateLimitRequestsThreshold: number;
  LogRequestFull: boolean;
  LogRequestBody: boolean;
  LogResponseBody: boolean;
  PinnedEmbeddingsProperties: Record<string, any>;
  PinnedCompletionsProperties: Record<string, any>;
  AllowEmbeddings: boolean;
  AllowCompletions: boolean;
  Labels: string[];
  Active: boolean;
  CreatedUtc: string;
  LastUpdateUtc: string;
  ActiveRequests: number;
}

export interface BackendHealth {
  Identifier: string;
  Name: string;
  Hostname: string;
  Port: number;
  Ssl: boolean;
  ApiFormat: string;
  BearerToken?: string;
  Querystring?: string;
  Headers?: Record<string, string>;
  UnhealthyThreshold: number;
  HealthyThreshold: number;
  HealthCheckMethod: string;
  HealthCheckUrl: string;
  MaxParallelRequests: number;
  RateLimitRequestsThreshold: number;
  LogRequestFull: boolean;
  LogRequestBody: boolean;
  LogResponseBody: boolean;
  PinnedEmbeddingsProperties: Record<string, any>;
  PinnedCompletionsProperties: Record<string, any>;
  AllowEmbeddings: boolean;
  AllowCompletions: boolean;
  Active: boolean;
  CreatedUtc: string;
  LastUpdateUtc: string;
  UnhealthySinceUtc: string;
  Downtime: string;
  ActiveRequests: number;
  Uptime: string;
  HealthySinceUtc: string;
}
