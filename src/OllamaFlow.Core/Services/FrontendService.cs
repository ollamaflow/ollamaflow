namespace OllamaFlow.Core.Services
{
    using OllamaFlow.Core;
    using OllamaFlow.Core.Database;
    using OllamaFlow.Core.Enums;
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Frontend service.
    /// </summary>
    public class FrontendService : IDisposable
    {
        private readonly string _Header = "[FrontendService] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private DatabaseDriverBase _Database = null;
        private ServiceContext _Services = null;
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private bool _Disposed = false;

        /// <summary>
        /// Frontend service.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="database">Database driver.</param>
        /// <param name="services">Service context.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public FrontendService(
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
        /// Create a new frontend.
        /// </summary>
        /// <param name="frontend">Frontend to create.</param>
        /// <returns>Created frontend.</returns>
        public Frontend Create(Frontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));
            if (string.IsNullOrEmpty(frontend.Identifier)) throw new ArgumentNullException(nameof(frontend.Identifier));

            if (Exists(frontend.Identifier))
            {
                _Logging.Warn(_Header + "frontend with identifier " + frontend.Identifier + " already exists");
                throw new DuplicateNameException("An object with identifier " + frontend.Identifier + " already exists.");
            }

            _Logging.Debug(_Header + "creating frontend " + frontend.Identifier);
            Frontend created = _Database.Frontend.Create(frontend);

            // Notify subordinate services
            _Services.HealthCheck.AddFrontend(created);
            _Services.ModelSynchronization.AddFrontend(created);
            return created;
        }

        /// <summary>
        /// Get a frontend by identifier.
        /// </summary>
        /// <param name="identifier">Frontend identifier.</param>
        /// <returns>Frontend if found, null otherwise.</returns>
        public Frontend GetByIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));
            return _Database.Frontend.ReadByIdentifiers(new List<string> { identifier }).FirstOrDefault();
        }

        /// <summary>
        /// Get multiple frontends by identifiers.
        /// </summary>
        /// <param name="identifiers">Frontend identifiers.</param>
        /// <returns>Frontends.</returns>
        public IEnumerable<Frontend> GetByIdentifiers(IEnumerable<string> identifiers)
        {
            if (identifiers == null) throw new ArgumentNullException(nameof(identifiers));
            List<string> idList = identifiers.ToList();
            if (!idList.Any()) return Enumerable.Empty<Frontend>();
            return _Database.Frontend.ReadByIdentifiers(idList);
        }

        /// <summary>
        /// Get all frontends.
        /// </summary>
        /// <param name="order">Sort order.</param>
        /// <returns>All frontends.</returns>
        public IEnumerable<Frontend> GetAll(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            return _Database.Frontend.ReadAll(order);
        }

        /// <summary>
        /// Get a page of frontends.
        /// </summary>
        /// <param name="request">Enumeration request.</param>
        /// <returns>Enumeration result.</returns>
        public EnumerationResult<Frontend> GetPage(EnumerationRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return _Database.Frontend.Enumerate(request);
        }

        /// <summary>
        /// Update a frontend.
        /// </summary>
        /// <param name="frontend">Frontend to update.</param>
        /// <returns>Updated frontend.</returns>
        public Frontend Update(Frontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));
            if (string.IsNullOrEmpty(frontend.Identifier)) throw new ArgumentException("Frontend identifier cannot be null or empty.");
            Frontend original = GetByIdentifier(frontend.Identifier);
            if (original == null) throw new KeyNotFoundException("The specified object could not be found by identifier " + frontend.Identifier + ".");
            frontend.LastUpdateUtc = DateTime.UtcNow;
            _Logging.Debug(_Header + "updating frontend " + frontend.Identifier);
            Frontend updated = _Database.Frontend.Update(frontend);

            // Notify subordinate services
            _Services.HealthCheck.UpdateFrontend(updated);
            _Services.ModelSynchronization.UpdateFrontend(updated);

            return updated;
        }

        /// <summary>
        /// Delete a frontend by identifier.
        /// </summary>
        /// <param name="identifier">Frontend identifier.</param>
        public void Delete(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));
            _Logging.Debug(_Header + "deleting frontend " + identifier);

            // Notify subordinate services
            _Services.HealthCheck.RemoveFrontend(identifier);
            _Services.ModelSynchronization.RemoveFrontend(identifier);
            _Database.Frontend.DeleteByGuid(identifier);
        }

        /// <summary>
        /// Check if a frontend exists.
        /// </summary>
        /// <param name="identifier">Frontend identifier.</param>
        /// <returns>True if exists.</returns>
        public bool Exists(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return false;
            return _Database.Frontend.ExistsByIdentifier(identifier);
        }

        /// <summary>
        /// Get the total count of frontends.
        /// </summary>
        /// <param name="order">Sort order.</param>
        /// <param name="continuationToken">Continuation token.</param>
        /// <returns>Total count.</returns>
        public int GetCount(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, string continuationToken = null)
        {
            return _Database.Frontend.GetRecordCount(order, continuationToken);
        }

        /// <summary>
        /// Get frontends by backend identifier.
        /// </summary>
        /// <param name="backendIdentifier">Backend identifier.</param>
        /// <returns>Frontends that reference the backend.</returns>
        public IEnumerable<Frontend> GetByBackendIdentifier(string backendIdentifier)
        {
            if (string.IsNullOrEmpty(backendIdentifier)) throw new ArgumentNullException(nameof(backendIdentifier));

            foreach (Frontend frontend in _Database.Frontend.ReadAll())
            {
                if (frontend.Backends != null && frontend.Backends.Contains(backendIdentifier))
                {
                    yield return frontend;
                }
            }
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