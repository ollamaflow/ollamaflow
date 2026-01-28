export interface Credentials {
  email: string;
  password: string;
}

export interface Credentials {
  /**
   * The username for basic auth
   */
  email: string;

  /**
   * The password for basic auth
   */
  password: string;
}

export interface SdkConfig {
  /**
   * The API endpoint base URL
   */
  endpoint: string;

  /**
   * The bearer token for authentication
   */
  bearerToken?: string;
}

export type GenerateCompletionResponse = {
  model: string;
  created_at: string;
  response: string;
  done: boolean;
  done_reason: string;
  context: Array<number>;
  total_duration: number;
  load_duration: number;
  prompt_eval_count: number;
  prompt_eval_duration: number;
  eval_count: number;
  eval_duration: number;
};

export type GenerateChatCompletionResponse = {
  model: string;
  created_at: string;
  message: {
    role: string;
    content: string;
  };
  done_reason: string;
  done: boolean;
  total_duration: number;
  load_duration: number;
  prompt_eval_count: number;
  prompt_eval_duration: number;
  eval_count: number;
  eval_duration: number;
};

export type Model = {
  name: string;
  model: string;
  modified_at: string;
  size: number;
  digest: string;
  details: {
    parent_model: string;
    format: string;
    family: string;
    families?: Array<string>;
    parameter_size: string;
    quantization_level: string;
  };
};

export type ModelListResponse = {
  models: Array<Model>;
};

export type Frontend = {
  Identifier: string;
  Name: string;
  Hostname: string;
  TimeoutMs: number;
  LoadBalancing: string;
  BlockHttp10: boolean;
  MaxRequestBodySize: number;
  Backends: Array<string>;
  RequiredModels: Array<any>;
  LogRequestFull: boolean;
  LogRequestBody: boolean;
  LogResponseBody: boolean;
  UseStickySessions: boolean;
  StickySessionExpirationMs: number;
  PinnedEmbeddingsProperties: any;
  PinnedCompletionsProperties: any;
  AllowEmbeddings: boolean;
  AllowCompletions: boolean;
  AllowRetries: boolean;
  Active: boolean;
  CreatedUtc: string;
  LastUpdateUtc: string;
};

export type FrontendCreateRequest = {
  Identifier: string;
  Name: string;
  Hostname: string;
  TimeoutMs: number;
  LoadBalancing: string;
  BlockHttp10: boolean;
  LogRequestFull: boolean;
  LogRequestBody: boolean;
  LogResponseBody: boolean;
  MaxRequestBodySize: number;
  AllowRetries: boolean;
  AllowEmbeddings: boolean;
  AllowCompletions: boolean;
  PinnedEmbeddingsProperties: any;
  PinnedCompletionsProperties: any;
  Backends: Array<string>;
  RequiredModels: Array<string>;
};

export type Backend = {
  Identifier: string;
  Name: string;
  Hostname: string;
  Port: number;
  Ssl: boolean;
  UnhealthyThreshold: number;
  HealthyThreshold: number;
  HealthCheckMethod: string;
  HealthCheckUrl: string;
  MaxParallelRequests: number;
  RateLimitRequestsThreshold: number;
  LogRequestFull: boolean;
  LogRequestBody: boolean;
  LogResponseBody: boolean;
  ApiFormat: string;
  Labels: Array<any>;
  PinnedEmbeddingsProperties: any;
  PinnedCompletionsProperties: any;
  AllowEmbeddings: boolean;
  AllowCompletions: boolean;
  Active: boolean;
  CreatedUtc: string;
  LastUpdateUtc: string;
  ActiveRequests: number;
  IsSticky: boolean;
};

export type BackendCreateRequest = {
  Identifier: string;
  Name: string;
  Hostname: string;
  Port: number;
  Ssl: boolean;
  UnhealthyThreshold: number;
  HealthyThreshold: number;
  HealthCheckMethod: string;
  HealthCheckUrl: string;
  MaxParallelRequests: number;
  RateLimitRequestsThreshold: number;
  LogRequestBody: boolean;
  LogResponseBody: boolean;
  ApiFormat: string;
  AllowEmbeddings: boolean;
  AllowCompletions: boolean;
  PinnedEmbeddingsProperties: any;
  PinnedCompletionsProperties: any;
};

export type BackendHealth = {
  Identifier: string;
  Name: string;
  Hostname: string;
  Port: number;
  Ssl: boolean;
  UnhealthyThreshold: number;
  HealthyThreshold: number;
  HealthCheckMethod: string;
  HealthCheckUrl: string;
  MaxParallelRequests: number;
  RateLimitRequestsThreshold: number;
  LogRequestFull: boolean;
  LogRequestBody: boolean;
  LogResponseBody: boolean;
  ApiFormat: string;
  Labels: Array<string>;
  PinnedEmbeddingsProperties: any;
  PinnedCompletionsProperties: any;
  AllowEmbeddings: boolean;
  AllowCompletions: boolean;
  Active: boolean;
  CreatedUtc: string;
  LastUpdateUtc: string;
  UnhealthySinceUtc: string;
  Downtime: string;
  ActiveRequests: number;
  IsSticky: boolean;
};
