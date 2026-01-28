import { renderHook, act } from "@testing-library/react";
import { useApiExplorer } from "#/hooks/useApiExplorer";

// Mock fetch globally
global.fetch = jest.fn();
const mockFetch = fetch as jest.MockedFunction<typeof fetch>;

// Mock AbortController
global.AbortController = jest.fn().mockImplementation(() => ({
  abort: jest.fn(),
  signal: { aborted: false },
}));

describe("useApiExplorer", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockFetch.mockClear();
  });

  describe("Initial State", () => {
    it("should return initial state values", () => {
      const { result } = renderHook(() => useApiExplorer());

      expect(result.current.baseUrl).toBe("http://localhost:43411/api/chat");
      expect(result.current.apiType).toBe("ollama");
      expect(result.current.requestType).toBe("chat");
      expect(result.current.modelName).toBe("llama2");
      expect(result.current.streamEnabled).toBe(true);
      expect(result.current.requestBody).toBe("");
      expect(result.current.responseBody).toBe("");
      expect(result.current.responsePreview).toBe("");
      expect(result.current.responseHeaders).toBe("");
      expect(result.current.responseStatus).toBe(null);
      expect(result.current.isSending).toBe(false);
      expect(result.current.abortController).toBe(null);
    });

    it("should have all required methods", () => {
      const { result } = renderHook(() => useApiExplorer());

      expect(typeof result.current.updateRequestBody).toBe("function");
      expect(typeof result.current.updateRequestBodyFromSettings).toBe(
        "function"
      );
      expect(typeof result.current.setRequestBodyManual).toBe("function");
      expect(typeof result.current.updateBaseUrl).toBe("function");
      expect(typeof result.current.clearResponse).toBe("function");
      expect(typeof result.current.sendRequest).toBe("function");
      expect(typeof result.current.stopRequest).toBe("function");
    });
  });

  describe("updateRequestBody", () => {
    it("should update request body with ollama chat format", () => {
      const { result } = renderHook(() => useApiExplorer());

      act(() => {
        result.current.updateRequestBody("ollama", "chat", "llama2", true);
      });

      expect(result.current.apiType).toBe("ollama");
      expect(result.current.requestType).toBe("chat");
      expect(result.current.modelName).toBe("llama2");
      expect(result.current.streamEnabled).toBe(true);
      expect(result.current.requestBody).toContain('"model": "llama2"');
      expect(result.current.requestBody).toContain('"messages"');
      expect(result.current.requestBody).toContain('"stream": true');
    });

    it("should update request body with openai chat format", () => {
      const { result } = renderHook(() => useApiExplorer());

      act(() => {
        result.current.updateRequestBody(
          "openai",
          "chat",
          "gpt-3.5-turbo",
          false
        );
      });

      expect(result.current.apiType).toBe("openai");
      expect(result.current.requestType).toBe("chat");
      expect(result.current.modelName).toBe("gpt-3.5-turbo");
      expect(result.current.streamEnabled).toBe(false);
      expect(result.current.requestBody).toContain('"model": "gpt-3.5-turbo"');
      expect(result.current.requestBody).toContain('"stream": false');
    });

    it("should update request body with ollama generate format", () => {
      const { result } = renderHook(() => useApiExplorer());

      act(() => {
        result.current.updateRequestBody("ollama", "generate", "llama2", true);
      });

      expect(result.current.requestBody).toContain('"model": "llama2"');
      expect(result.current.requestBody).toContain('"prompt"');
      expect(result.current.requestBody).toContain('"stream": true');
    });

    it("should update request body with ollama embeddings format", () => {
      const { result } = renderHook(() => useApiExplorer());

      act(() => {
        result.current.updateRequestBody(
          "ollama",
          "embeddings",
          "llama2",
          false
        );
      });

      expect(result.current.requestBody).toContain('"model": "llama2"');
      expect(result.current.requestBody).toContain('"input"');
    });
  });

  describe("setRequestBodyManual", () => {
    it("should set request body manually", () => {
      const { result } = renderHook(() => useApiExplorer());
      const customBody = '{"custom": "request body"}';

      act(() => {
        result.current.setRequestBodyManual(customBody);
      });

      expect(result.current.requestBody).toBe(customBody);
    });
  });

  describe("updateBaseUrl", () => {
    it("should update base URL", () => {
      const { result } = renderHook(() => useApiExplorer());
      const newUrl = "http://new-server.com/api";

      act(() => {
        result.current.updateBaseUrl(newUrl);
      });

      expect(result.current.baseUrl).toBe(newUrl);
    });
  });

  describe("clearResponse", () => {
    it("should clear all response data", () => {
      const { result } = renderHook(() => useApiExplorer());

      // Set some response data first
      act(() => {
        result.current.setRequestBodyManual('{"test": "data"}');
      });

      act(() => {
        result.current.clearResponse();
      });

      expect(result.current.responseBody).toBe("");
      expect(result.current.responsePreview).toBe("");
      expect(result.current.responseHeaders).toBe("");
      expect(result.current.responseStatus).toBe(null);
    });
  });

  describe("sendRequest", () => {
    it("should handle successful non-streaming response", async () => {
      const mockResponse = {
        ok: true,
        status: 200,
        statusText: "OK",
        headers: new Headers({ "content-type": "application/json" }),
        text: jest
          .fn()
          .mockResolvedValue('{"message": {"content": "Hello world"}}'),
      };

      mockFetch.mockResolvedValue(mockResponse as any);

      const { result } = renderHook(() => useApiExplorer());

      act(() => {
        result.current.setRequestBodyManual(
          '{"model": "llama2", "messages": [{"role": "user", "content": "Hello"}]}'
        );
      });

      await act(async () => {
        await result.current.sendRequest();
      });

      expect(mockFetch).toHaveBeenCalledWith(
        "http://localhost:43411/api/chat",
        {
          body: '{"model":"llama2","messages":[{"role":"user","content":"Hello"}]}',
          headers: { "Content-Type": "application/json" },
          method: "POST",
          signal: { aborted: false },
        }
      );

      expect(result.current.responseStatus).toEqual(
        expect.objectContaining({
          status: 200,
          statusText: "OK",
          type: "success",
        })
      );
    });

    it("should handle streaming response", async () => {
      const mockResponse = {
        ok: true,
        status: 200,
        statusText: "OK",
        headers: new Headers({ "content-type": "text/plain" }),
        body: {
          getReader: jest.fn().mockReturnValue({
            read: jest
              .fn()
              .mockResolvedValueOnce({
                done: false,
                value: new TextEncoder().encode(
                  '{"message": {"content": "Hello"}}\n'
                ),
              })
              .mockResolvedValueOnce({
                done: false,
                value: new TextEncoder().encode(
                  '{"message": {"content": " world"}}\n'
                ),
              })
              .mockResolvedValueOnce({
                done: true,
                value: undefined,
              }),
          }),
        },
      };

      mockFetch.mockResolvedValue(mockResponse as any);

      const { result } = renderHook(() => useApiExplorer());

      act(() => {
        result.current.setRequestBodyManual(
          '{"model": "llama2", "messages": [{"role": "user", "content": "Hello"}], "stream": true}'
        );
      });

      await act(async () => {
        await result.current.sendRequest();
      });

      expect(result.current.responsePreview).toContain("Hello world");
      expect(result.current.responseStatus).toEqual(
        expect.objectContaining({
          status: 200,
          type: "success",
        })
      );
    });

    it("should handle network error", async () => {
      mockFetch.mockRejectedValue(new Error("Network error"));

      const { result } = renderHook(() => useApiExplorer());

      act(() => {
        result.current.setRequestBodyManual('{"model": "llama2"}');
      });

      await act(async () => {
        await result.current.sendRequest();
      });

      expect(result.current.responseStatus).toEqual(
        expect.objectContaining({
          status: 500,
          statusText: "Network Error",
          type: "error",
        })
      );
      expect(result.current.responsePreview).toContain("Network Error");
    });

    it("should handle invalid JSON in request body", async () => {
      const { result } = renderHook(() => useApiExplorer());

      act(() => {
        result.current.setRequestBodyManual('{"invalid": json}');
      });

      await act(async () => {
        await result.current.sendRequest();
      });

      expect(result.current.responseStatus).toEqual(
        expect.objectContaining({
          status: 400,
          statusText: "Bad Request",
          type: "error",
        })
      );
      expect(result.current.responsePreview).toContain("Invalid JSON");
    });

    it("should use GET method for tags endpoint", async () => {
      const mockResponse = {
        ok: true,
        status: 200,
        statusText: "OK",
        headers: new Headers(),
        text: jest.fn().mockResolvedValue('{"models": []}'),
      };

      mockFetch.mockResolvedValue(mockResponse as any);

      const { result } = renderHook(() => useApiExplorer());

      act(() => {
        result.current.updateBaseUrl("http://localhost:43411/api/tags");
        result.current.setRequestBodyManual("{}");
      });

      await act(async () => {
        await result.current.sendRequest();
      });

      expect(mockFetch).toHaveBeenCalledWith(
        "http://localhost:43411/api/tags",
        expect.objectContaining({
          method: "GET",
          body: undefined,
        })
      );
    });

    it("should use DELETE method for delete endpoint", async () => {
      const mockResponse = {
        ok: true,
        status: 200,
        statusText: "OK",
        headers: new Headers(),
        text: jest.fn().mockResolvedValue('{"success": true}'),
      };

      mockFetch.mockResolvedValue(mockResponse as any);

      const { result } = renderHook(() => useApiExplorer());

      act(() => {
        result.current.updateBaseUrl("http://localhost:43411/api/delete");
        result.current.setRequestBodyManual('{"name": "llama2"}');
      });

      await act(async () => {
        await result.current.sendRequest();
      });

      expect(mockFetch).toHaveBeenCalledWith(
        "http://localhost:43411/api/delete",
        expect.objectContaining({
          method: "DELETE",
        })
      );
    });
  });

  describe("stopRequest", () => {
    it("should not throw error if no abort controller", () => {
      const { result } = renderHook(() => useApiExplorer());

      expect(() => {
        act(() => {
          result.current.stopRequest();
        });
      }).not.toThrow();
    });
  });

  describe("updateRequestBodyFromSettings", () => {
    it("should update request body using current settings", () => {
      const { result } = renderHook(() => useApiExplorer());

      // First update settings
      act(() => {
        result.current.updateRequestBody(
          "openai",
          "completions",
          "gpt-4",
          false
        );
      });

      // Then update from settings
      act(() => {
        result.current.updateRequestBodyFromSettings();
      });

      expect(result.current.requestBody).toContain('"model": "gpt-4"');
      expect(result.current.requestBody).toContain('"prompt"');
      expect(result.current.requestBody).toContain('"stream": false');
    });
  });

  describe("Response Processing", () => {
    it("should handle OpenAI streaming response format", async () => {
      const mockResponse = {
        ok: true,
        status: 200,
        statusText: "OK",
        headers: new Headers(),
        body: {
          getReader: jest.fn().mockReturnValue({
            read: jest
              .fn()
              .mockResolvedValueOnce({
                done: false,
                value: new TextEncoder().encode(
                  'data: {"choices": [{"delta": {"content": "Hello"}}]}\n\n'
                ),
              })
              .mockResolvedValueOnce({
                done: false,
                value: new TextEncoder().encode(
                  'data: {"choices": [{"delta": {"content": " world"}}]}\n\n'
                ),
              })
              .mockResolvedValueOnce({
                done: false,
                value: new TextEncoder().encode("data: [DONE]\n\n"),
              })
              .mockResolvedValueOnce({
                done: true,
                value: undefined,
              }),
          }),
        },
      };

      mockFetch.mockResolvedValue(mockResponse as any);

      const { result } = renderHook(() => useApiExplorer());

      act(() => {
        result.current.setRequestBodyManual(
          '{"model": "gpt-3.5-turbo", "stream": true}'
        );
      });

      await act(async () => {
        await result.current.sendRequest();
      });

      expect(result.current.responsePreview).toContain("Hello world");
    });

    it("should handle error responses", async () => {
      const mockResponse = {
        ok: false,
        status: 400,
        statusText: "Bad Request",
        headers: new Headers(),
        text: jest
          .fn()
          .mockResolvedValue('{"error": {"message": "Invalid request"}}'),
      };

      mockFetch.mockResolvedValue(mockResponse as any);

      const { result } = renderHook(() => useApiExplorer());

      act(() => {
        result.current.setRequestBodyManual('{"model": "llama2"}');
      });

      await act(async () => {
        await result.current.sendRequest();
      });

      expect(result.current.responseStatus).toEqual(
        expect.objectContaining({
          status: 400,
          type: "error",
        })
      );
      expect(result.current.responsePreview).toContain(
        "Error: Invalid request"
      );
    });
  });
});
