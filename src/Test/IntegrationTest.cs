namespace Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Database.Sqlite;
    using OllamaFlow.Core.Serialization;
    using RestWrapper;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    public static class IntegrationTest
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

        private static CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private static OllamaFlowSettings _Settings = null;
        private static Serializer _Serializer = new Serializer();
        private static LoadBalancingMode _LoadBalancingMode = LoadBalancingMode.RoundRobin;

        private static WebserverSettings _Server1Settings = null;
        private static Webserver _Server1 = null;

        private static WebserverSettings _Server2Settings = null;
        private static Webserver _Server2 = null;

        private static WebserverSettings _Server3Settings = null;
        private static Webserver _Server3 = null;

        private static int _NumRequests = 8;

        public static async Task IntegrationMain(string[] args)
        {
            Console.WriteLine("");
            Console.WriteLine("Use typical Ollama requests, but use server port 43411");
            InitializeSettings();
            InitializeDatabase();
            InitializeBackends();

            string url;

            using (OllamaFlowDaemon ollamaFlow = new OllamaFlowDaemon(_Settings, _TokenSource))
            {
                using (_Server1 = new Webserver(_Server1Settings, Server1Route))
                {
                    using (_Server2 = new Webserver(_Server2Settings, Server2Route))
                    {
                        using (_Server3 = new Webserver(_Server3Settings, Server3Route))
                        {
                            _Server1.Start();
                            _Server2.Start();
                            _Server3.Start();

                            #region Should-Succeed

                            Console.WriteLine("");
                            Console.WriteLine("-------------");
                            Console.WriteLine("Success cases");
                            Console.WriteLine("-------------");

                            for (int i = 0; i < _NumRequests; i++)
                            {
                                Console.WriteLine("");
                                Console.WriteLine("Request " + i);

                                url = "http://localhost:8000/";

                                using (RestRequest req = new RestRequest(url))
                                {
                                    using (RestResponse resp = await req.SendAsync())
                                    {
                                        Console.WriteLine("| Response (" + resp.StatusCode + "): " + resp.DataAsString);
                                    }
                                }
                            }

                            #endregion

                            Console.WriteLine("");
                            Console.WriteLine("Press ENTER to end");
                            Console.ReadLine();
                        }
                    }
                }
            }
        }

        private static void InitializeSettings()
        {
            _Settings = new OllamaFlowSettings();
            _Settings.Logging.MinimumSeverity = 0;
            File.WriteAllBytes("./ollamaflow.json", Encoding.UTF8.GetBytes(_Serializer.SerializeJson(_Settings, true)));
        }

        private static void InitializeDatabase()
        {
            if (File.Exists(Constants.DatabaseFilename)) File.Delete(Constants.DatabaseFilename);

            SqliteDatabaseDriver driver = new SqliteDatabaseDriver(_Settings, new LoggingModule(), _Serializer, _Settings.DatabaseFilename);
            driver.InitializeRepository();

            driver.Frontend.Create(new Frontend
            {
                Identifier = "virtual-ollama",
                Name = "Virtual Ollama",
                Hostname = "localhost",
                LoadBalancing = _LoadBalancingMode,
                Backends = new List<string> { "ollama1", "ollama2", "ollama3" }
            });

            driver.Backend.Create(new Backend
            {
                Identifier = "ollama1",
                Name = "Ollama 1",
                Hostname = "localhost",
                Port = 8001,
                Ssl = false
            });

            driver.Backend.Create(new Backend
            {
                Identifier = "ollama2",
                Name = "Ollama 2",
                Hostname = "localhost",
                Port = 8002,
                Ssl = false
            });

            driver.Backend.Create(new Backend
            {
                Identifier = "ollama3",
                Name = "Ollama 3",
                Hostname = "localhost",
                Port = 8003,
                Ssl = false
            });
        }

        private static void InitializeBackends()
        {
            _Server1Settings = new WebserverSettings();
            _Server1Settings.Hostname = "localhost";
            _Server1Settings.Port = 8001;

            _Server2Settings = new WebserverSettings();
            _Server2Settings.Hostname = "localhost";
            _Server2Settings.Port = 8002;

            _Server3Settings = new WebserverSettings();
            _Server3Settings.Hostname = "localhost";
            _Server3Settings.Port = 8003;
        }

        private static async Task Server1Route(HttpContextBase ctx)
        {
            Console.WriteLine("| Server 1");
            Console.WriteLine("| Received URL: " + ctx.Request.Url.Full);
            if (!String.IsNullOrEmpty(ctx.Request.Query.Querystring))
                Console.WriteLine("| Querystring: " + ctx.Request.Query.Querystring);
            if (ctx.Request.Headers.Count > 0)
            {
                Console.WriteLine("| Headers:");
                foreach (string key in ctx.Request.Headers.AllKeys)
                {
                    Console.WriteLine("  | " + key + ": " + ctx.Request.Headers.Get(key));
                }
            }
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send("Hello from server 1: " + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery);
            return;
        }

        private static async Task Server2Route(HttpContextBase ctx)
        {
            Console.WriteLine("| Server 2");
            Console.WriteLine("| Received URL: " + ctx.Request.Url.Full);
            if (!String.IsNullOrEmpty(ctx.Request.Query.Querystring))
                Console.WriteLine("| Querystring: " + ctx.Request.Query.Querystring);
            if (ctx.Request.Headers.Count > 0)
            {
                Console.WriteLine("| Headers:");
                foreach (string key in ctx.Request.Headers.AllKeys)
                {
                    Console.WriteLine("  | " + key + ": " + ctx.Request.Headers.Get(key));
                }
            }
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send("Hello from server 2: " + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery);
            return;
        }

        private static async Task Server3Route(HttpContextBase ctx)
        {
            Console.WriteLine("| Server 3");
            Console.WriteLine("| Received URL: " + ctx.Request.Url.Full);
            if (!String.IsNullOrEmpty(ctx.Request.Query.Querystring))
                Console.WriteLine("| Querystring: " + ctx.Request.Query.Querystring);
            if (ctx.Request.Headers.Count > 0)
            {
                Console.WriteLine("| Headers:");
                foreach (string key in ctx.Request.Headers.AllKeys)
                {
                    Console.WriteLine("  | " + key + ": " + ctx.Request.Headers.Get(key));
                }
            }
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send("Hello from server 3: " + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery);
            return;
        }

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}