namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Database;
    using OllamaFlow.Core.Enums;
    using SyslogLogging;

    /// <summary>
    /// Backend service.
    /// </summary>
    public class BackendService : IDisposable
    {
        private readonly string _Header = "[BackendService] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private DatabaseDriverBase _Database = null;
        private ServiceContext _Services = null;
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private bool _Disposed = false;

        /// <summary>
        /// Backend service.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="database">Database driver.</param>
        /// <param name="services">Service context.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public BackendService(
            OllamaFlowSettings settings, 
            LoggingModule logging, 
            DatabaseDriverBase database, 
            ServiceContext services,
            CancellationTokenSource tokenSource = default)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Services = services ?? throw new ArgumentNullException(nameof(services));
            _TokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        /// <summary>
        /// Initialize.
        /// </summary>
        public void Initialize()
        {
            _Logging.Debug(_Header + "initialized");
        }

        /// <summary>
        /// Create a new backend.
        /// </summary>
        /// <param name="backend">Backend to create.</param>
        /// <returns>Created backend.</returns>
        public Backend Create(Backend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            if (string.IsNullOrEmpty(backend.Identifier)) throw new ArgumentNullException(nameof(backend.Identifier));

            if (Exists(backend.Identifier))
            {
                _Logging.Warn(_Header + "backend with identifier " + backend.Identifier + " already exists");
                throw new DuplicateNameException("An object with identifier " + backend.Identifier + " already exists.");
            }

            _Logging.Debug(_Header + "creating backend " + backend.Identifier);
            Backend created = _Database.Backend.Create(backend);

            // Notify subordinate services
            _Services.HealthCheck.AddBackend(created);
            _Services.ModelSynchronization.AddBackend(created);

            return created;
        }

        /// <summary>
        /// Get a backend by identifier.
        /// </summary>
        /// <param name="identifier">Backend identifier.</param>
        /// <returns>Backend if found, null otherwise.</returns>
        public Backend GetByIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));
            return _Database.Backend.ReadByIdentifiers(new List<string> { identifier }).FirstOrDefault();
        }

        /// <summary>
        /// Get multiple backends by identifiers.
        /// </summary>
        /// <param name="identifiers">Backend identifiers.</param>
        /// <returns>Backends.</returns>
        public IEnumerable<Backend> GetByIdentifiers(IEnumerable<string> identifiers)
        {
            if (identifiers == null) throw new ArgumentNullException(nameof(identifiers));
            List<string> idList = identifiers.ToList();
            if (!idList.Any()) return Enumerable.Empty<Backend>();
            return _Database.Backend.ReadByIdentifiers(idList);
        }

        /// <summary>
        /// Get all backends.
        /// </summary>
        /// <param name="order">Sort order.</param>
        /// <returns>All backends.</returns>
        public IEnumerable<Backend> GetAll(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            return _Database.Backend.ReadAll(order);
        }

        /// <summary>
        /// Get a page of backends.
        /// </summary>
        /// <param name="request">Enumeration request.</param>
        /// <returns>Enumeration result.</returns>
        public EnumerationResult<Backend> GetPage(EnumerationRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return _Database.Backend.Enumerate(request);
        }

        /// <summary>
        /// Update a backend.
        /// </summary>
        /// <param name="backend">Backend to update.</param>
        /// <returns>Updated backend.</returns>
        public Backend Update(Backend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            if (string.IsNullOrEmpty(backend.Identifier)) throw new ArgumentException("Backend identifier cannot be null or empty.");
            Backend original = GetByIdentifier(backend.Identifier);
            if (original == null) throw new KeyNotFoundException("The specified object could not be found by identifier " + backend.Identifier + ".");
            backend.LastUpdateUtc = DateTime.UtcNow;
            Backend updated = _Database.Backend.Update(backend);

            // Notify subordinate services
            _Services.HealthCheck.UpdateBackend(updated);
            _Services.ModelSynchronization.UpdateBackend(updated);
            return updated;
        }

        /// <summary>
        /// Delete a backend by identifier.
        /// </summary>
        /// <param name="identifier">Backend identifier.</param>
        /// <param name="force">Force deletion even if linked to frontends.</param>
        /// <returns>True if deleted, false if linked and not forced.</returns>
        public bool Delete(string identifier, bool force = false)
        {
            if (string.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));

            if (!force && _Database.Backend.IsLinked(identifier))
            {
                _Logging.Warn(_Header + "backend " + identifier + " is linked to one or more frontends, cannot delete");
                return false;
            }

            _Logging.Debug(_Header + "deleting backend " + identifier);

            // Notify subordinate services
            _Services.HealthCheck.RemoveBackend(identifier);
            _Services.ModelSynchronization.RemoveBackend(identifier);

            _Database.Backend.DeleteByIdentifier(identifier);
            return true;
        }

        /// <summary>
        /// Check if a backend exists.
        /// </summary>
        /// <param name="identifier">Backend identifier.</param>
        /// <returns>True if exists.</returns>
        public bool Exists(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return false;
            return _Database.Backend.ExistsByGuidIdentifier(identifier);
        }

        /// <summary>
        /// Check if a backend is linked to any frontends.
        /// </summary>
        /// <param name="identifier">Backend identifier.</param>
        /// <returns>True if linked.</returns>
        public bool IsLinked(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return false;
            return _Database.Backend.IsLinked(identifier);
        }

        /// <summary>
        /// Get the total count of backends.
        /// </summary>
        /// <param name="order">Sort order.</param>
        /// <param name="continuationToken">Continuation token.</param>
        /// <returns>Total count.</returns>
        public int GetCount(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, string continuationToken = null)
        {
            return _Database.Backend.GetRecordCount(order, continuationToken);
        }

        /// <summary>
        /// Get backends for a specific frontend.
        /// </summary>
        /// <param name="frontendIdentifier">Frontend identifier.</param>
        /// <returns>Backends associated with the frontend.</returns>
        public IEnumerable<Backend> GetByFrontendIdentifier(string frontendIdentifier)
        {
            if (string.IsNullOrEmpty(frontendIdentifier)) throw new ArgumentNullException(nameof(frontendIdentifier));
            Frontend frontend = _Database.Frontend.ReadByIdentifiers(new List<string> { frontendIdentifier }).FirstOrDefault();
            if (frontend == null || frontend.Backends == null || !frontend.Backends.Any()) return Enumerable.Empty<Backend>();
            return _Database.Backend.ReadByIdentifiers(frontend.Backends);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _Disposed = true;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}