namespace OllamaFlow.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Database.Sqlite;
    using OllamaFlow.Core.Services;
    using OllamaFlow.Core.Serialization;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// OllamaFlow server.
    /// </summary>
    public static class Program
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        #endregion

        #region Private-Members

        private static string _SoftwareVersion = "v1.0.0";
        private static CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private static OllamaFlowSettings _Settings = null;
        private static OllamaFlowDaemon _OllamaFlow = null;
        private static Serializer _Serializer = new Serializer();

        #endregion

        #region Entrypoint

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Task.</returns>
        public static async Task Main(string[] args)
        {
            Welcome();
            ParseArguments(args);
            InitializeSettings();
            InitializeDatabase();

            using (_OllamaFlow = new OllamaFlowDaemon(_Settings, _TokenSource))
            {
                EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                AssemblyLoadContext.Default.Unloading += (ctx) => waitHandle.Set();
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    waitHandle.Set();
                    eventArgs.Cancel = true;
                };

                bool waitHandleSignal = false;
                do
                {
                    waitHandleSignal = waitHandle.WaitOne(1000);
                }
                while (!waitHandleSignal);
            }
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        private static void Welcome()
        {
            Console.WriteLine("");
            Console.WriteLine(Constants.Logo);
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("OllamaFlow " + _SoftwareVersion);
            Console.WriteLine("");
        }

        private static void ParseArguments(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                }
            }
        }

        private static void InitializeSettings()
        {
            if (!File.Exists(Constants.SettingsFile))
            {
                Console.WriteLine("Settings file " + Constants.SettingsFile + " does not exist, creating");
                _Settings = new OllamaFlowSettings();

                _Settings.Webserver.Port = 43411;
                _Settings.Webserver.Ssl.Enable = false;

                _Settings.AdminBearerTokens.Add("ollamaflowadmin");

                File.WriteAllText(Constants.SettingsFile, _Serializer.SerializeJson(_Settings, true));
                Console.WriteLine("Created settings file " + Constants.SettingsFile + ", please modify and restart OllamaFlow");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Loading from settings file " + Constants.SettingsFile);
                _Settings = _Serializer.DeserializeJson<OllamaFlowSettings>(File.ReadAllText(Constants.SettingsFile));
            }
        }

        private static void InitializeDatabase()
        {
            if (!File.Exists(Constants.DatabaseFilename))
            {
                SqliteDatabaseDriver driver = new SqliteDatabaseDriver(_Settings, new LoggingModule(), _Serializer, _Settings.DatabaseFilename);
                driver.InitializeRepository();

                driver.Frontend.Create(new Frontend
                {
                    Identifier = "frontend1",
                    Name = "My first virtual Ollama",
                    Hostname = "*",
                    Backends = new List<string> { "backend1" }                    
                });

                driver.Backend.Create(new Backend
                {
                    Identifier = "backend1",
                    Name = "My localhost Ollama instance",
                    Hostname = "localhost",
                    Port = 11434,
                    Ssl = false
                });
            }
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}