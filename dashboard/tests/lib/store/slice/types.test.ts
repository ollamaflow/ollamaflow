import "@testing-library/jest-dom";
import { Frontend, Backend } from "#/lib/store/slice/types";

describe("Types", () => {
  describe("Frontend interface", () => {
    test("should have all required properties", () => {
      const frontend: Frontend = {
        Identifier: "test-id",
        Name: "Test Frontend",
        Hostname: "localhost",
        TimeoutMs: 1000,
        LoadBalancing: "round_robin",
        BlockHttp10: false,
        MaxRequestBodySize: 1000000,
        Backends: ["backend1"],
        RequiredModels: ["model1"],
        LogRequestFull: true,
        LogRequestBody: true,
        LogResponseBody: true,
        UseStickySessions: false,
        StickySessionExpirationMs: 1800000,
        Active: true,
        CreatedUtc: "2021-01-01T00:00:00Z",
        LastUpdateUtc: "2021-01-01T00:00:00Z",
        AllowRetries: false,
        PinnedEmbeddingsProperties: {},
        PinnedCompletionsProperties: {},
        AllowEmbeddings: true,
        AllowCompletions: true,
      };

      expect(frontend.Identifier).toBe("test-id");
      expect(frontend.Name).toBe("Test Frontend");
      expect(frontend.Hostname).toBe("localhost");
      expect(frontend.TimeoutMs).toBe(1000);
      expect(frontend.LoadBalancing).toBe("round_robin");
      expect(frontend.BlockHttp10).toBe(false);
      expect(frontend.MaxRequestBodySize).toBe(1000000);
      expect(frontend.Backends).toEqual(["backend1"]);
      expect(frontend.RequiredModels).toEqual(["model1"]);
      expect(frontend.LogRequestFull).toBe(true);
      expect(frontend.LogRequestBody).toBe(true);
      expect(frontend.LogResponseBody).toBe(true);
      expect(frontend.UseStickySessions).toBe(false);
      expect(frontend.StickySessionExpirationMs).toBe(1800000);
      expect(frontend.Active).toBe(true);
      expect(frontend.CreatedUtc).toBe("2021-01-01T00:00:00Z");
      expect(frontend.LastUpdateUtc).toBe("2021-01-01T00:00:00Z");
    });
  });

  describe("Backend interface", () => {
    test("should have all required properties", () => {
      const backend: Backend = {
        Identifier: "test-id",
        Name: "Test Backend",
        Hostname: "localhost",
        Port: 43411,
        Ssl: false,
        ApiFormat: "Ollama",
        PinnedEmbeddingsProperties: {},
        PinnedCompletionsProperties: {},
        AllowEmbeddings: true,
        AllowCompletions: true,
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
        CreatedUtc: "2021-01-01T00:00:00Z",
        LastUpdateUtc: "2021-01-01T00:00:00Z",
        ActiveRequests: 0,
      };

      expect(backend.Identifier).toBe("test-id");
      expect(backend.Name).toBe("Test Backend");
      expect(backend.Hostname).toBe("localhost");
      expect(backend.Port).toBe(43411);
      expect(backend.Ssl).toBe(false);
      expect(backend.UnhealthyThreshold).toBe(3);
      expect(backend.HealthyThreshold).toBe(3);
      expect(backend.HealthCheckMethod).toEqual("GET");
      expect(backend.HealthCheckUrl).toBe("/health");
      expect(backend.MaxParallelRequests).toBe(10);
      expect(backend.RateLimitRequestsThreshold).toBe(100);
      expect(backend.LogRequestFull).toBe(true);
      expect(backend.LogRequestBody).toBe(true);
      expect(backend.LogResponseBody).toBe(true);
      expect(backend.Active).toBe(true);
      expect(backend.CreatedUtc).toBe("2021-01-01T00:00:00Z");
      expect(backend.LastUpdateUtc).toBe("2021-01-01T00:00:00Z");
    });
  });
});
