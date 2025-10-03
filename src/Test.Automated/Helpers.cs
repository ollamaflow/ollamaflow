namespace Test.Automated
{
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal static class Helpers
    {
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
                Hostname = _OllamaFlowHostname,
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

        internal static async Task<bool> WaitForHealthyBackend(OllamaFlowDaemon daemon, Backend backend, int timeoutMs = 10000, int intervalMs = 1000)
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
    }
}
