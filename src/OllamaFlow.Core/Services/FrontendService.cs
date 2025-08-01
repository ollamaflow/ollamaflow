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
    /// Frontend service.
    /// </summary>
    public class FrontendService
    {
        #region Public-Members
        #endregion

        #region Private-Members

        private readonly string _Header = "[FrontendService] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private DatabaseDriverBase _Database = null;
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Frontend service.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="database">Database driver.</param>
        /// <param name="tokenSource">Cancellation token source.</param>
        public FrontendService(OllamaFlowSettings settings, LoggingModule logging, DatabaseDriverBase database, CancellationTokenSource tokenSource = default)
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
        /// Create a new frontend.
        /// </summary>
        /// <param name="frontend">Frontend to create.</param>
        /// <returns>Created frontend.</returns>
        public OllamaFrontend Create(OllamaFrontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));
            if (string.IsNullOrEmpty(frontend.Identifier)) throw new ArgumentNullException(nameof(frontend.Identifier));
            _Logging.Debug(_Header + "creating frontend " + frontend.Identifier);
            return _Database.Frontend.Create(frontend);
        }

        /// <summary>
        /// Create multiple frontends.
        /// </summary>
        /// <param name="frontends">Frontends to create.</param>
        /// <returns>Created frontends.</returns>
        public IEnumerable<OllamaFrontend> CreateMany(IEnumerable<OllamaFrontend> frontends)
        {
            if (frontends == null) throw new ArgumentNullException(nameof(frontends));

            var frontendList = frontends.ToList();
            if (!frontendList.Any()) return Enumerable.Empty<OllamaFrontend>();

            foreach (var frontend in frontendList)
            {
                if (string.IsNullOrEmpty(frontend.Identifier)) throw new ArgumentNullException(nameof(frontend.Identifier));
            }

            _Logging.Debug(_Header + "creating " + frontendList.Count + " frontends");
            return _Database.Frontend.CreateMany(frontendList);
        }

        /// <summary>
        /// Get a frontend by identifier.
        /// </summary>
        /// <param name="identifier">Frontend identifier.</param>
        /// <returns>Frontend if found, null otherwise.</returns>
        public OllamaFrontend GetByIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));
            return _Database.Frontend.ReadByIdentifiers(new List<string> { identifier }).FirstOrDefault();
        }

        /// <summary>
        /// Get multiple frontends by identifiers.
        /// </summary>
        /// <param name="identifiers">Frontend identifiers.</param>
        /// <returns>Frontends.</returns>
        public IEnumerable<OllamaFrontend> GetByIdentifiers(IEnumerable<string> identifiers)
        {
            if (identifiers == null) throw new ArgumentNullException(nameof(identifiers));
            List<string> idList = identifiers.ToList();
            if (!idList.Any()) return Enumerable.Empty<OllamaFrontend>();
            return _Database.Frontend.ReadByIdentifiers(idList);
        }

        /// <summary>
        /// Get all frontends.
        /// </summary>
        /// <param name="order">Sort order.</param>
        /// <returns>All frontends.</returns>
        public IEnumerable<OllamaFrontend> GetAll(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            return _Database.Frontend.ReadAll(order);
        }

        /// <summary>
        /// Get a page of frontends.
        /// </summary>
        /// <param name="request">Enumeration request.</param>
        /// <returns>Enumeration result.</returns>
        public EnumerationResult<OllamaFrontend> GetPage(EnumerationRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return _Database.Frontend.Enumerate(request);
        }

        /// <summary>
        /// Update a frontend.
        /// </summary>
        /// <param name="frontend">Frontend to update.</param>
        /// <returns>Updated frontend.</returns>
        public OllamaFrontend Update(OllamaFrontend frontend)
        {
            if (frontend == null) throw new ArgumentNullException(nameof(frontend));
            if (string.IsNullOrEmpty(frontend.Identifier)) throw new ArgumentException("Frontend identifier cannot be null or empty.");
            OllamaFrontend original = GetByIdentifier(frontend.Identifier);
            if (original == null) throw new KeyNotFoundException("The specified object could not be found by identifier " + frontend.Identifier + ".");
            frontend.LastUpdateUtc = DateTime.UtcNow;
            _Logging.Debug(_Header + "updating frontend " + frontend.Identifier);
            return _Database.Frontend.Update(frontend);
        }

        /// <summary>
        /// Delete a frontend by identifier.
        /// </summary>
        /// <param name="identifier">Frontend identifier.</param>
        public void Delete(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));
            _Logging.Debug(_Header + "deleting frontend " + identifier);
            _Database.Frontend.DeleteByGuid(identifier);
        }

        /// <summary>
        /// Delete multiple frontends by identifiers.
        /// </summary>
        /// <param name="identifiers">Frontend identifiers.</param>
        public void DeleteMany(IEnumerable<string> identifiers)
        {
            if (identifiers == null) throw new ArgumentNullException(nameof(identifiers));

            List<string> idList = identifiers.ToList();
            if (!idList.Any()) return;

            _Logging.Debug(_Header + "deleting " + idList.Count + " frontends");
            _Database.Frontend.DeleteMany(idList);
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
        public IEnumerable<OllamaFrontend> GetByBackendIdentifier(string backendIdentifier)
        {
            if (string.IsNullOrEmpty(backendIdentifier)) throw new ArgumentNullException(nameof(backendIdentifier));

            foreach (var frontend in _Database.Frontend.ReadAll())
            {
                if (frontend.Backends != null && frontend.Backends.Contains(backendIdentifier))
                {
                    yield return frontend;
                }
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}