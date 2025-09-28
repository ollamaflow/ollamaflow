namespace OllamaFlow.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Database;
    using SyslogLogging;

    /// <summary>
    /// Backend service.
    /// </summary>
    public class BackendService
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[BackendService] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private DatabaseDriverBase _Database = null;
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Backend service.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="database">Database driver.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public BackendService(OllamaFlowSettings settings, LoggingModule logging, DatabaseDriverBase database, CancellationTokenSource tokenSource = default)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _TokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Create a new backend.
        /// </summary>
        /// <param name="backend">Backend to create.</param>
        /// <returns>Created backend.</returns>
        public Backend Create(Backend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            if (string.IsNullOrEmpty(backend.Identifier)) throw new ArgumentNullException(nameof(backend.Identifier));
            _Logging.Debug(_Header + "creating backend " + backend.Identifier);
            return _Database.Backend.Create(backend);
        }

        /// <summary>
        /// Create multiple backends.
        /// </summary>
        /// <param name="backends">Backends to create.</param>
        /// <returns>Created backends.</returns>
        public IEnumerable<Backend> CreateMany(IEnumerable<Backend> backends)
        {
            if (backends == null) throw new ArgumentNullException(nameof(backends));

            List<Backend> backendList = backends.ToList();
            if (!backendList.Any()) return Enumerable.Empty<Backend>();

            foreach (Backend backend in backendList)
            {
                if (string.IsNullOrEmpty(backend.Identifier)) throw new ArgumentNullException(nameof(backend.Identifier));
            }

            _Logging.Debug(_Header + "creating " + backendList.Count + " backends");
            return _Database.Backend.CreateMany(backendList);
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
            return _Database.Backend.Update(backend);
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
            _Database.Backend.DeleteByIdentifier(identifier);
            return true;
        }

        /// <summary>
        /// Delete multiple backends by identifiers.
        /// </summary>
        /// <param name="identifiers">Backend identifiers.</param>
        /// <param name="force">Force deletion even if linked to frontends.</param>
        /// <returns>Identifiers that were successfully deleted.</returns>
        public IEnumerable<string> DeleteMany(IEnumerable<string> identifiers, bool force = false)
        {
            if (identifiers == null) throw new ArgumentNullException(nameof(identifiers));

            List<string> idList = identifiers.ToList();
            if (!idList.Any()) return Enumerable.Empty<string>();

            List<string> toDelete = new List<string>();
            List<string> linked = new List<string>();

            // Check which backends can be deleted
            foreach (string id in idList)
            {
                if (force || !_Database.Backend.IsLinked(id))
                {
                    toDelete.Add(id);
                }
                else
                {
                    linked.Add(id);
                }
            }

            if (linked.Any())
            {
                _Logging.Warn(_Header + linked.Count + " backends are linked to frontends and will not be deleted");
            }

            if (toDelete.Any())
            {
                _Logging.Debug(_Header + "deleting " + toDelete.Count + " backends");
                _Database.Backend.DeleteMany(toDelete);
            }

            return toDelete;
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

        #endregion

        #region Private-Methods

        #endregion
    }
}