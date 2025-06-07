namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using RestWrapper;
    using SerializationHelper;
    using OllamaFlow.Core;
    using SyslogLogging;
    using Timestamps;
    using UrlMatcher;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using System.Threading;
    using System.Net.Http;

    /// <summary>
    /// Health check service.
    /// </summary>
    public class HealthCheckService : IDisposable
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[HealthCheckService] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private Random _Random = new Random(Guid.NewGuid().GetHashCode());
        private bool _IsDisposed = false;

        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private Dictionary<string, Task> _HealthCheckTasks = new Dictionary<string, Task>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Health check service.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        public HealthCheckService(
            OllamaFlowSettings settings,
            LoggingModule logging,
            Serializer serializer)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            foreach (OllamaBackend backend in _Settings.Backends)
            {
                _HealthCheckTasks.Add(
                    backend.Identifier,
                    Task.Run(() => HealthCheckTask(backend, _TokenSource.Token), _TokenSource.Token));
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    _Random = null;
                    _Serializer = null;
                    _Logging = null;
                    _Settings = null;
                }

                _IsDisposed = true;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        private async Task HealthCheckTask(OllamaBackend backend, CancellationToken token = default)
        {
            bool firstRun = true;

            _Logging.Debug(
                _Header +
                "starting healthcheck task for backend " +
                backend.Identifier + " " + backend.Name + " " + backend.Hostname + ":" + backend.Port);

            string healthCheckUrl = (backend.Ssl ? "https://" : "http://") + backend.Hostname + ":" + backend.Port + backend.HealthCheckUrl;

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(5);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (!firstRun) await Task.Delay(backend.HealthCheckIntervalMs, token);
                        else firstRun = false;

                        HttpRequestMessage request = new HttpRequestMessage(backend.HealthCheckMethod, healthCheckUrl);
                        HttpResponseMessage response = await client.SendAsync(request, token);

                        if (response.IsSuccessStatusCode)
                        {
                            lock (backend.Lock)
                            {
                                if (backend.HealthCheckSuccess < 99) backend.HealthCheckSuccess++;
                                backend.HealthCheckFailure = 0;

                                if (!backend.Healthy && backend.HealthCheckSuccess >= backend.HealthyThreshold)
                                {
                                    backend.Healthy = true;
                                    _Logging.Info(_Header + "backend " + backend.Identifier + " is now healthy");
                                }
                            }

                            _Logging.Debug(_Header + "health check succeeded for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl);
                        }
                        else
                        {
                            lock (backend.Lock)
                            {
                                if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                                backend.HealthCheckSuccess = 0;

                                if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                                {
                                    backend.Healthy = false;
                                    _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (HTTP " + (int)response.StatusCode + ")");
                                }
                            }

                            _Logging.Debug(_Header + "health check failed for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl + " with status " + (int)response.StatusCode);
                        }
                    }
                    catch (HttpRequestException hre)
                    {
                        lock (backend.Lock)
                        {
                            if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                            backend.HealthCheckSuccess = 0;

                            if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                            {
                                backend.Healthy = false;
                                _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (timeout)");
                            }
                        }

                        _Logging.Debug(_Header + "health check failed for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl + ": " + hre.Message);
                    }
                    catch (HttpIOException ioe)
                    {
                        lock (backend.Lock)
                        {
                            if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                            backend.HealthCheckSuccess = 0;

                            if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                            {
                                backend.Healthy = false;
                                _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (timeout)");
                            }
                        }

                        _Logging.Debug(_Header + "health check failed for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl + ": " + ioe.Message);
                    }
                    catch (SocketException se)
                    {
                        lock (backend.Lock)
                        {
                            if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                            backend.HealthCheckSuccess = 0;

                            if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                            {
                                backend.Healthy = false;
                                _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (timeout)");
                            }
                        }

                        _Logging.Debug(_Header + "health check failed for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl + ": " + se.Message);
                    }
                    catch (TaskCanceledException)
                    {
                        // Expected when cancellation is requested or timeout occurs
                        if (!token.IsCancellationRequested)
                        {
                            // This was a timeout, not a cancellation
                            lock (backend.Lock)
                            {
                                if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                                backend.HealthCheckSuccess = 0;

                                if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                                {
                                    backend.Healthy = false;
                                    _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (timeout)");
                                }
                            }

                            _Logging.Debug(_Header + "health check timeout for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancellation is requested or timeout occurs
                        if (!token.IsCancellationRequested)
                        {
                            // This was a timeout, not a cancellation
                            lock (backend.Lock)
                            {
                                if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                                backend.HealthCheckSuccess = 0;

                                if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                                {
                                    backend.Healthy = false;
                                    _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (timeout)");
                                }
                            }

                            _Logging.Debug(_Header + "health check timeout for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl);
                        }
                    }
                    catch (Exception e)
                    {
                        lock (backend.Lock)
                        {
                            if (backend.HealthCheckFailure < 99) backend.HealthCheckFailure++;
                            backend.HealthCheckSuccess = 0;

                            if (backend.Healthy && backend.HealthCheckFailure >= backend.UnhealthyThreshold)
                            {
                                backend.Healthy = false;
                                _Logging.Warn(_Header + "backend " + backend.Identifier + " is now unhealthy (" + e.GetType().Name + ")");
                            }
                        }

                        _Logging.Debug(_Header + "health check exception for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl + Environment.NewLine + e.ToString());
                    }
                }
            }

            _Logging.Debug(_Header + "stopping healthcheck task for backend " + backend.Identifier + " " + backend.Name + " " + healthCheckUrl);
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
