namespace Test.Automated
{
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Models.Ollama;
    using OllamaFlow.Core.Models.OpenAI;
    using OllamaFlow.Core.Serialization;
    using RestWrapper;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    internal static class Helpers
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.

        private static Serializer _Serializer = new Serializer();

        internal static Backend CreateOllamaBackend(
            OllamaFlowDaemon daemon,
            string identifier,
            string hostname,
            int port = 11434,
            string pinnedEmbedProperties = "",
            string pinnedCompletionProperties = "",
            bool allowEmbed = true,
            bool allowCompletions = true)
        {
            Backend backend = new Backend
            {
                Identifier = identifier,
                Name = identifier,
                Hostname = hostname,
                Port = port,
                ApiFormat = ApiFormatEnum.Ollama,
                HealthCheckMethod = "HEAD",
                HealthCheckUrl = "/",
                AllowEmbeddings = allowEmbed,
                AllowCompletions = allowCompletions
            };

            return daemon.Backends.Create(backend);
        }

        internal static Backend CreateVllmBackend(
            OllamaFlowDaemon daemon,
            string identifier,
            string hostname,
            int port = 11434,
            string pinnedEmbedProperties = "",
            string pinnedCompletionProperties = "",
            bool allowEmbed = true,
            bool allowCompletions = true)
        {
            Backend backend = new Backend
            {
                Identifier = identifier,
                Name = identifier,
                Hostname = hostname,
                Port = port,
                ApiFormat = ApiFormatEnum.OpenAI,
                HealthCheckMethod = "GET",
                HealthCheckUrl = "/health",
                AllowEmbeddings = allowEmbed,
                AllowCompletions = allowCompletions
            };

            return daemon.Backends.Create(backend);
        }

        internal static Frontend CreateFrontend(
            OllamaFlowDaemon daemon,
            string identifier,
            string hostname,
            LoadBalancingMode loadBalancingMode = LoadBalancingMode.RoundRobin,
            List<Backend> backends = null,
            List<string> requiredModels = null,
            bool useStickySessions = false,
            Dictionary<string, object> pinnedEmbedProperties = null,
            Dictionary<string, object> pinnedCompletionProperties = null,
            bool allowEmbed = true,
            bool allowCompletions = true,
            bool allowRetries = true)
        {
            Frontend frontend = new Frontend
            {
                Identifier = identifier,
                Name = identifier,
                Hostname = hostname,
                LoadBalancing = loadBalancingMode,
                RequiredModels = requiredModels,
                UseStickySessions = useStickySessions,
                PinnedEmbeddingsProperties = pinnedEmbedProperties,
                PinnedCompletionsProperties = pinnedCompletionProperties,
                AllowEmbeddings = allowEmbed,
                AllowCompletions = allowCompletions,
                AllowRetries = allowRetries
            };

            return daemon.Frontends.Create(frontend);
        }

        internal static string OllamaSingleEmbeddingsRequestBody(
            string model,
            string input)
        {
            OllamaGenerateEmbeddingsRequest request = new OllamaGenerateEmbeddingsRequest
            {
                Model = model
            };
            request.SetInput(input);
            return _Serializer.SerializeJson(request, false);
        }

        internal static string OllamaMultipleEmbeddingsRequestBody(
            string model,
            List<string> inputs)
        {
            OllamaGenerateEmbeddingsRequest request = new OllamaGenerateEmbeddingsRequest
            {
                Model = model
            };
            request.SetInputs(inputs);
            return _Serializer.SerializeJson(request, false);
        }

        internal static string OpenAISingleEmbeddingsRequestBody(
            string model,
            string input)
        {
            OpenAIGenerateEmbeddingsRequest request = new OpenAIGenerateEmbeddingsRequest
            {
                Model = model
            };
            request.SetInput(input);
            return _Serializer.SerializeJson(request, false);
        }

        internal static string OpenAIMultipleEmbeddingsRequestBody(
            string model,
            List<string> inputs)
        {
            OpenAIGenerateEmbeddingsRequest request = new OpenAIGenerateEmbeddingsRequest
            {
                Model = model
            };
            request.SetInputs(inputs);
            return _Serializer.SerializeJson(request, false);
        }

        internal static string OllamaCompletionsRequestBody(
            string model,
            string prompt)
        {
            OllamaGenerateCompletionRequest request = new OllamaGenerateCompletionRequest
            {
                Model = model,
                Prompt = prompt
            };
            return _Serializer.SerializeJson(request, false);
        }

        internal static string OllamaChatCompletionsRequestBody(
            string model,
            List<OllamaChatMessage> messages)
        {
            OllamaGenerateChatCompletionRequest request = new OllamaGenerateChatCompletionRequest
            {
                Model = model,
                Messages = messages
            };
            return _Serializer.SerializeJson(request, false);
        }

        internal static string OpenAICompletionsRequestBody(
            string model,
            string prompt)
        {
            OpenAIGenerateCompletionRequest request = new OpenAIGenerateCompletionRequest
            {
                Model = model
            };
            request.SetPrompt(prompt);
            return _Serializer.SerializeJson(request, false);
        }

        internal static string OpenAIChatCompletionsRequestBody(
            string model,
            List<OpenAIChatMessage> messages)
        {
            OpenAIGenerateChatCompletionRequest request = new OpenAIGenerateChatCompletionRequest
            {
                Model = model,
                Messages = messages
            };
            return _Serializer.SerializeJson(request, false);
        }

        internal static async Task<bool> WaitForHealthyBackend(
            OllamaFlowDaemon daemon,
            Backend backend,
            int timeoutMs = 10000,
            int intervalMs = 1000)
        {
            int waited = 0;

            while (waited < timeoutMs)
            {
                await Task.Delay(intervalMs);
                if (daemon.HealthCheck.IsHealthy(backend.Identifier)) return true;
            }

            Console.WriteLine("*** Backend " + backend.Identifier + " not healthy after " + timeoutMs + "ms");
            return false;
        }

        internal static async Task<bool> WaitForHealthyBackend(
            OllamaFlowDaemon daemon,
            string backendIdentifier,
            int timeoutMs = 10000,
            int intervalMs = 1000)
        {
            int waited = 0;

            while (waited < timeoutMs)
            {
                await Task.Delay(intervalMs);
                if (daemon.HealthCheck.IsHealthy(backendIdentifier)) return true;
            }

            Console.WriteLine("*** Backend " + backendIdentifier + " not healthy after " + timeoutMs + "ms");
            return false;
        }

        internal static void Cleanup(bool cleanupDatabase = true, bool cleanupLogs = true)
        {
            if (cleanupDatabase && File.Exists("ollamaflow.db")) File.Delete("./ollamaflow.db");
            if (cleanupLogs && Directory.Exists("./logs")) Directory.Delete("./logs", true);
        }

        internal static void RemoveDefaultRecords(OllamaFlowDaemon daemon)
        {
            foreach (Frontend frontend in daemon.Frontends.GetAll())
            {
                daemon.Frontends.Delete(frontend.Identifier);
            }

            foreach (Backend backend in daemon.Backends.GetAll())
            {
                daemon.Backends.Delete(backend.Identifier);
            }
        }

        internal static async Task<OllamaGenerateEmbeddingsResult> GetOllamaEmbeddingsResult(RestResponse resp)
        {
            if (resp == null) return null;
            if (!resp.IsSuccessStatusCode) return null;
            if (resp.ChunkedTransferEncoding)
            {
                string chunkData = "";

                while (true)
                {
                    ChunkData chunk = await resp.ReadChunkAsync();
                    if (chunk == null) break;
                    if (chunk.Data != null) chunkData += Encoding.UTF8.GetString(chunk.Data);
                    if (chunk.IsFinal) break;
                }

                return _Serializer.DeserializeJson<OllamaGenerateEmbeddingsResult>(chunkData);
            }
            else
            {
                return _Serializer.DeserializeJson<OllamaGenerateEmbeddingsResult>(resp.DataAsString);
            }
        }

        internal static async Task<OpenAIGenerateEmbeddingsResult> GetOpenAIEmbeddingsResult(RestResponse resp)
        {
            if (resp == null) return null;
            if (!resp.IsSuccessStatusCode) return null;
            if (resp.ChunkedTransferEncoding)
            {
                string chunkData = "";

                while (true)
                {
                    ChunkData chunk = await resp.ReadChunkAsync();
                    if (chunk == null) break;
                    if (chunk.Data != null) chunkData += Encoding.UTF8.GetString(chunk.Data);
                    if (chunk.IsFinal) break;
                }

                return _Serializer.DeserializeJson<OpenAIGenerateEmbeddingsResult>(chunkData);
            }
            else
            {
                return _Serializer.DeserializeJson<OpenAIGenerateEmbeddingsResult>(resp.DataAsString);
            }
        }

        internal static async Task<OllamaGenerateCompletionResult> GetOllamaCompletionsResult(RestResponse resp)
        {
            if (resp == null) return null;
            if (!resp.IsSuccessStatusCode) return null;

            string responseData = "";
            if (resp.ChunkedTransferEncoding)
            {
                while (true)
                {
                    ChunkData chunk = await resp.ReadChunkAsync();
                    if (chunk == null) break;
                    if (chunk.Data != null) responseData += Encoding.UTF8.GetString(chunk.Data);
                    if (chunk.IsFinal) break;
                }
            }
            else
                responseData = resp.DataAsString;

            return _Serializer.DeserializeJson<OllamaGenerateCompletionResult>(responseData);
        }

        internal static async Task<OllamaGenerateChatCompletionResult> GetOllamaChatCompletionsResult(RestResponse resp)
        {
            if (resp == null) return null;
            if (!resp.IsSuccessStatusCode) return null;

            string responseData = "";
            if (resp.ChunkedTransferEncoding)
            {
                while (true)
                {
                    ChunkData chunk = await resp.ReadChunkAsync();
                    if (chunk == null) break;
                    if (chunk.Data != null) responseData += Encoding.UTF8.GetString(chunk.Data);
                    if (chunk.IsFinal) break;
                }
            }
            else
                responseData = resp.DataAsString;

            return _Serializer.DeserializeJson<OllamaGenerateChatCompletionResult>(responseData);
        }

        internal static string OllamaStreamingCompletionsRequestBody(string model, string prompt, bool stream = true)
        {
            OllamaGenerateCompletionRequest request = new OllamaGenerateCompletionRequest
            {
                Model = model,
                Prompt = prompt,
                Stream = stream
            };
            return _Serializer.SerializeJson(request, false);
        }

        internal static string OllamaStreamingChatCompletionsRequestBody(string model, List<OllamaChatMessage> messages, bool stream = true)
        {
            OllamaGenerateChatCompletionRequest request = new OllamaGenerateChatCompletionRequest
            {
                Model = model,
                Messages = messages,
                Stream = stream
            };
            return _Serializer.SerializeJson(request, false);
        }

        internal static string OpenAIStreamingCompletionsRequestBody(string model, string prompt, bool stream = true)
        {
            OpenAIGenerateCompletionRequest request = new OpenAIGenerateCompletionRequest
            {
                Model = model,
                Stream = stream
            };
            request.SetPrompt(prompt);
            return _Serializer.SerializeJson(request, false);
        }

        internal static string OpenAIStreamingChatCompletionsRequestBody(string model, List<OpenAIChatMessage> messages, bool stream = true)
        {
            OpenAIGenerateChatCompletionRequest request = new OpenAIGenerateChatCompletionRequest
            {
                Model = model,
                Messages = messages,
                Stream = stream
            };
            return _Serializer.SerializeJson(request, false);
        }

        internal static async Task<OpenAIGenerateCompletionResult> GetOpenAICompletionsResult(RestResponse resp)
        {
            if (resp == null) return null;
            if (!resp.IsSuccessStatusCode) return null;

            string responseData = "";
            if (resp.ChunkedTransferEncoding)
            {
                while (true)
                {
                    ChunkData chunk = await resp.ReadChunkAsync();
                    if (chunk == null) break;
                    if (chunk.Data != null) responseData += Encoding.UTF8.GetString(chunk.Data);
                    if (chunk.IsFinal) break;
                }
            }
            else
            {
                responseData = resp.DataAsString;
            }

            return _Serializer.DeserializeJson<OpenAIGenerateCompletionResult>(responseData);
        }

        internal static async Task<OpenAIGenerateChatCompletionResult> GetOpenAIChatCompletionsResult(RestResponse resp)
        {
            if (resp == null) return null;
            if (!resp.IsSuccessStatusCode) return null;

            string responseData = "";
            if (resp.ChunkedTransferEncoding)
            {
                while (true)
                {
                    ChunkData chunk = await resp.ReadChunkAsync();
                    if (chunk == null) break;
                    if (chunk.Data != null) responseData += Encoding.UTF8.GetString(chunk.Data);
                    if (chunk.IsFinal) break;
                }
            }
            else
            {
                responseData = resp.DataAsString;
            }
            return _Serializer.DeserializeJson<OpenAIGenerateChatCompletionResult>(responseData);
        }

        internal static async Task<bool> WaitForModelSynchronization(OllamaFlowDaemon daemon, string backendIdentifier, string modelName, int timeoutMs = 300000)
        {
            try
            {
                Console.WriteLine($"Waiting for model {modelName} to be synchronized on backend {backendIdentifier}...");
                
                int waited = 0;
                int intervalMs = 5000;
                
                while (waited < timeoutMs)
                {
                    await Task.Delay(intervalMs);
                    waited += intervalMs;
                    
                    Backend backend = daemon.Backends.GetByIdentifier(backendIdentifier);
                    if (backend == null)
                    {
                        Console.WriteLine($"Backend {backendIdentifier} not found");
                        return false;
                    }
                    
                    string listModelsUrl = $"http://{backend.Hostname}:{backend.Port}/api/tags";
                    
                    using (RestRequest req = new RestRequest(listModelsUrl, HttpMethod.Get))
                    {
                        RestResponse resp = await req.SendAsync();
                        
                        if (resp != null && resp.IsSuccessStatusCode)
                        {
                            string responseData = resp.DataAsString;
                            if (responseData.Contains($"\"name\":\"{modelName}\"") || responseData.Contains($"\"name\":\"{modelName}:"))
                            {
                                Console.WriteLine($"Model {modelName} is now available on backend {backendIdentifier}");
                                return true;
                            }
                        }
                    }
                    
                    Console.WriteLine($"Model {modelName} not yet available on backend {backendIdentifier}, waiting... ({waited}/{timeoutMs}ms)");
                }
                
                Console.WriteLine($"Timeout waiting for model {modelName} on backend {backendIdentifier} after {timeoutMs}ms");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while waiting for model {modelName} on backend {backendIdentifier}: {ex.Message}");
                return false;
            }
        }

#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
