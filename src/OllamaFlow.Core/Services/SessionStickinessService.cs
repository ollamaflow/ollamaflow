namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core.Models;
    using SyslogLogging;

    /// <summary>
    /// Service for managing sticky sessions between clients and backends.
    /// </summary>
    public class SessionStickinessService : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Interval in milliseconds for cleaning up expired sessions.
        /// Default is 300000 milliseconds (5 minutes).
        /// Minimum is 10000 milliseconds (10 seconds).
        /// Maximum is 3600000 milliseconds (1 hour).
        /// </summary>
        public int CleanupIntervalMs
        {
            get
            {
                return _CleanupIntervalMs;
            }
            set
            {
                if (value < 10000) throw new ArgumentOutOfRangeException(nameof(CleanupIntervalMs), "Minimum value is 10000 milliseconds (10 seconds)");
                if (value > 3600000) throw new ArgumentOutOfRangeException(nameof(CleanupIntervalMs), "Maximum value is 3600000 milliseconds (1 hour)");
                _CleanupIntervalMs = value;
            }
        }

        /// <summary>
        /// Get the total number of active sessions.
        /// </summary>
        public int ActiveSessionCount
        {
            get
            {
                return _Sessions.Count;
            }
        }

        #endregion

        #region Private-Members

        private readonly string _Header = "[SessionStickinessService] ";
        private LoggingModule _Logging = null;
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private bool _IsDisposed = false;

        private ConcurrentDictionary<string, StickySession> _Sessions = new ConcurrentDictionary<string, StickySession>();
        private Task _CleanupTask = null;
        private int _CleanupIntervalMs = 300000; // 5 minutes

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="logging">Logging module.</param>
        /// <exception cref="ArgumentNullException">Thrown when logging is null.</exception>
        public SessionStickinessService(LoggingModule logging)
        {
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));

            _CleanupTask = Task.Run(CleanupWorker, _TokenSource.Token);
            _Logging.Debug(_Header + "initialized with cleanup interval " + _CleanupIntervalMs + "ms");
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
                    _TokenSource?.Cancel();
                    try
                    {
                        _CleanupTask?.Wait(5000);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelling
                    }
                    _TokenSource?.Dispose();
                    _Sessions?.Clear();
                    _Logging = null;
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

        /// <summary>
        /// Get the sticky backend for a client and frontend combination.
        /// </summary>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="frontendId">Frontend identifier.</param>
        /// <returns>Backend identifier if a valid sticky session exists, null otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when clientId or frontendId is null or empty.</exception>
        public string GetStickyBackend(string clientId, string frontendId)
        {
            if (String.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));
            if (String.IsNullOrEmpty(frontendId)) throw new ArgumentNullException(nameof(frontendId));

            string sessionKey = $"{clientId}:{frontendId}";

            if (_Sessions.TryGetValue(sessionKey, out StickySession session))
            {
                if (session.IsExpired())
                {
                    _Sessions.TryRemove(sessionKey, out _);
                    _Logging.Debug(_Header + "expired session removed for client " + clientId + " frontend " + frontendId + " backend " + session.BackendId);
                    return null;
                }

                _Logging.Debug(_Header + "found sticky session for client " + clientId + " frontend " + frontendId + " backend " + session.BackendId);
                return session.BackendId;
            }

            return null;
        }

        /// <summary>
        /// Set a sticky backend for a client and frontend combination.
        /// </summary>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="frontendId">Frontend identifier.</param>
        /// <param name="backendId">Backend identifier.</param>
        /// <param name="expirationMs">Session expiration in milliseconds.</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when expirationMs is less than or equal to zero.</exception>
        public void SetStickyBackend(string clientId, string frontendId, string backendId, int expirationMs)
        {
            if (String.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));
            if (String.IsNullOrEmpty(frontendId)) throw new ArgumentNullException(nameof(frontendId));
            if (String.IsNullOrEmpty(backendId)) throw new ArgumentNullException(nameof(backendId));
            if (expirationMs <= 0) throw new ArgumentOutOfRangeException(nameof(expirationMs));

            string sessionKey = $"{clientId}:{frontendId}";
            StickySession session = new StickySession(clientId, frontendId, backendId, expirationMs);

            _Sessions.AddOrUpdate(sessionKey, session, (key, existing) =>
            {
                if (existing.BackendId != backendId)
                {
                    _Logging.Info(_Header + "backend switch for client " + clientId + " frontend " + frontendId + " from " + existing.BackendId + " to " + backendId);
                }
                return session;
            });

            _Logging.Debug(_Header + "created sticky session for client " + clientId + " frontend " + frontendId + " backend " + backendId + " expires " + session.ExpiresUtc.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
        }

        /// <summary>
        /// Update the last access time for an existing session and extend its expiration.
        /// </summary>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="frontendId">Frontend identifier.</param>
        /// <param name="expirationMs">New session expiration in milliseconds.</param>
        /// <returns>True if the session was found and updated, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when clientId or frontendId is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when expirationMs is less than or equal to zero.</exception>
        public bool TouchSession(string clientId, string frontendId, int expirationMs)
        {
            if (String.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));
            if (String.IsNullOrEmpty(frontendId)) throw new ArgumentNullException(nameof(frontendId));
            if (expirationMs <= 0) throw new ArgumentOutOfRangeException(nameof(expirationMs));

            string sessionKey = $"{clientId}:{frontendId}";

            if (_Sessions.TryGetValue(sessionKey, out StickySession session))
            {
                session.Touch(expirationMs);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove a specific sticky session.
        /// </summary>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="frontendId">Frontend identifier.</param>
        /// <returns>True if the session was found and removed, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when clientId or frontendId is null or empty.</exception>
        public bool RemoveSession(string clientId, string frontendId)
        {
            if (String.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));
            if (String.IsNullOrEmpty(frontendId)) throw new ArgumentNullException(nameof(frontendId));

            string sessionKey = $"{clientId}:{frontendId}";

            if (_Sessions.TryRemove(sessionKey, out StickySession session))
            {
                _Logging.Debug(_Header + "removed session for client " + clientId + " frontend " + frontendId + " backend " + session.BackendId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove all sessions for a specific client.
        /// </summary>
        /// <param name="clientId">Client identifier.</param>
        /// <returns>Number of sessions removed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when clientId is null or empty.</exception>
        public int RemoveClientSessions(string clientId)
        {
            if (String.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));

            int removedCount = 0;
            List<string> keysToRemove = new List<string>();

            foreach (KeyValuePair<string, StickySession> kvp in _Sessions)
            {
                if (kvp.Value.ClientId.Equals(clientId))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (string key in keysToRemove)
            {
                if (_Sessions.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                _Logging.Debug(_Header + "removed " + removedCount + " sessions for client " + clientId);
            }

            return removedCount;
        }

        /// <summary>
        /// Remove all sessions associated with a specific frontend.
        /// </summary>
        /// <param name="frontendId">Frontend identifier.</param>
        /// <returns>Number of sessions removed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when frontendId is null or empty.</exception>
        public int RemoveFrontendSessions(string frontendId)
        {
            if (String.IsNullOrEmpty(frontendId)) throw new ArgumentNullException(nameof(frontendId));

            int removedCount = 0;
            List<string> keysToRemove = new List<string>();

            foreach (KeyValuePair<string, StickySession> kvp in _Sessions)
            {
                if (kvp.Value.FrontendId.Equals(frontendId))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (string key in keysToRemove)
            {
                if (_Sessions.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                _Logging.Info(_Header + "removed " + removedCount + " sessions for deleted frontend " + frontendId);
            }

            return removedCount;
        }

        /// <summary>
        /// Remove all sessions associated with a specific backend.
        /// </summary>
        /// <param name="backendId">Backend identifier.</param>
        /// <returns>Number of sessions removed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when backendId is null or empty.</exception>
        public int RemoveBackendSessions(string backendId)
        {
            if (String.IsNullOrEmpty(backendId)) throw new ArgumentNullException(nameof(backendId));

            int removedCount = 0;
            List<string> keysToRemove = new List<string>();

            foreach (KeyValuePair<string, StickySession> kvp in _Sessions)
            {
                if (kvp.Value.BackendId.Equals(backendId))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (string key in keysToRemove)
            {
                if (_Sessions.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                _Logging.Info(_Header + "removed " + removedCount + " sessions for unavailable backend " + backendId);
            }

            return removedCount;
        }

        /// <summary>
        /// Get all active sessions.
        /// </summary>
        /// <returns>List of all active sticky sessions.</returns>
        public List<StickySession> GetAllSessions()
        {
            return _Sessions.Values.ToList();
        }

        /// <summary>
        /// Get all sessions for a specific client.
        /// </summary>
        /// <param name="clientId">Client identifier.</param>
        /// <returns>List of sessions for the specified client.</returns>
        /// <exception cref="ArgumentNullException">Thrown when clientId is null or empty.</exception>
        public List<StickySession> GetClientSessions(string clientId)
        {
            if (String.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));

            return _Sessions.Values.Where(s => s.ClientId.Equals(clientId)).ToList();
        }

        /// <summary>
        /// Clear all sessions.
        /// </summary>
        /// <returns>Number of sessions removed.</returns>
        public int ClearAllSessions()
        {
            int count = _Sessions.Count;
            _Sessions.Clear();
            _Logging.Info(_Header + "cleared all " + count + " sessions");
            return count;
        }

        #endregion

        #region Private-Methods

        private async Task CleanupWorker()
        {
            _Logging.Debug(_Header + "cleanup worker started");

            while (!_TokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_CleanupIntervalMs, _TokenSource.Token).ConfigureAwait(false);

                    int expiredCount = 0;
                    List<string> keysToRemove = new List<string>();

                    foreach (KeyValuePair<string, StickySession> kvp in _Sessions)
                    {
                        if (kvp.Value.IsExpired())
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }

                    foreach (string key in keysToRemove)
                    {
                        if (_Sessions.TryRemove(key, out StickySession removedSession))
                        {
                            expiredCount++;
                            _Logging.Debug(_Header + "expired session removed for client " + removedSession.ClientId + " frontend " + removedSession.FrontendId + " backend " + removedSession.BackendId);
                        }
                    }

                    if (expiredCount > 0)
                    {
                        _Logging.Debug(_Header + "cleanup removed " + expiredCount + " expired sessions, " + _Sessions.Count + " active sessions remaining");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "exception in cleanup worker: " + e.Message);
                }
            }

            _Logging.Debug(_Header + "cleanup worker stopped");
        }

        #endregion
    }
}