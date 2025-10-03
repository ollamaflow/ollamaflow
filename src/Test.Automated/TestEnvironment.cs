namespace Test.Automated
{
    using System;
    using System.Collections.Specialized;
    using System.Net.WebSockets;
    using System.Runtime.CompilerServices;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Models.Ollama;
    using OllamaFlow.Core.Serialization;
    using RestWrapper;

    public class TestEnvironment
    {
        public string OllamaFlowHostname = "localhost";
        public int OllamaFlowPort = 10000;
        public string StickyHeader = "x-thread-id";
        
        public string OllamaUrl = "http://astra:11434";
        public List<string> OllamaRequiredModels = new List<string> { "all-minilm", "gemma3:4b" };
        public string OllamaEmbeddingsModel = "all-minilm";
        public string OllamaCompletionsModel = "gemma3:4b";

        public string VllmUrl = "http://34.55.208.75:8000";
        public List<string> VllmModels = new List<string> { "Qwen/Qwen2.5-3B" };
        public string VllmCompletionsModel = "Qwen/Qwen2.5-3B";

        public TestEnvironment()
        {

        }
    }
}