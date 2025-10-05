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

    /// <summary>
    /// Test environment.
    /// </summary>
    public class TestEnvironment
    {
        /// <summary>
        /// OllamaFlow hostname.
        /// </summary>
        public string OllamaFlowHostname = "localhost";

        /// <summary>
        /// OllamaFlow port.
        /// </summary>
        public int OllamaFlowPort = 10000;

        /// <summary>
        /// Sticky header
        /// </summary>
        public string StickyHeader = "x-thread-id";

        /// <summary>
        /// Database filename.
        /// </summary>
        public string DatabaseFilename
        {
            get => _DatabaseFilename;
            set => _DatabaseFilename = (!String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(DatabaseFilename)));
        }

        /// <summary>
        /// Administrator bearer tokens for administrative REST APIs.
        /// </summary>
        public List<string> AdminBearerTokens
        {
            get => _AdminBearerTokens;
            set => _AdminBearerTokens = (value != null && value.Count > 0 ? value : throw new ArgumentException("At least one administrator bearer token must be supplied."));
        }

        /// <summary>
        /// List of headers to use to identify a node when evaluating session stickiness.
        /// </summary>
        public List<string> StickyHeaders
        {
            get => _StickyHeaders;
            set => _StickyHeaders = (value != null && value.Count > 0 ? value : new List<string>());
        }

        /// <summary>
        /// List of backends for the test.
        /// This property is usually defined by the test case.
        /// </summary>
        public List<Backend> Backends
        {
            get => _Backends;
            set => _Backends = (value != null ? value : new List<Backend>());
        }

        /// <summary>
        /// List of frontends for the test.
        /// This property is usually defined by the test case.
        /// </summary>
        public List<Frontend> Frontends
        {
            get => _Frontends;
            set => _Frontends = (value != null ? value : new List<Frontend>());
        }

        /// <summary>
        /// Embeddings model
        /// </summary>
        public string EmbeddingsModel
        {
            get => _EmbeddingsModel;
            set => _EmbeddingsModel = (!String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(EmbeddingsModel)));
        }

        /// <summary>
        /// Completions model
        /// </summary>
        public string CompletionsModel
        {
            get => _CompletionsModel;
            set => _CompletionsModel = (!String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(CompletionsModel)));
        }

        private string _DatabaseFilename = "ollamaflow.db";
        private List<string> _AdminBearerTokens = new List<string> { "ollamaflowadmin" };
        private List<string> _StickyHeaders = new List<string> { "x-thread-id", "x-conversation-id" };
        private List<Backend> _Backends = new List<Backend>();
        private List<Frontend> _Frontends = new List<Frontend>();
        private string _EmbeddingsModel = "all-minilm";
        private string _CompletionsModel = "gemma3:4b";

        /// <summary>
        /// Test environment.
        /// </summary>
        public TestEnvironment()
        {

        }
    }
}