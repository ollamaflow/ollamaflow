namespace OllamaFlow.Core.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Database.Interfaces;
    using OllamaFlow.Core.Database.Sqlite;
    using OllamaFlow.Core.Database.Sqlite.Queries;

    /// <summary>
    /// Backend methods.
    /// </summary>
    public class BackendMethods : IBackendMethods
    {
        #region Public-Members

        /// <summary>
        /// Frontend methods.
        /// </summary>
        public IFrontendMethods Frontend { get; }

        /// <summary>
        /// Backend methods.
        /// </summary>
        public IBackendMethods Backend { get; }

        #endregion

        #region Private-Members

        private SqliteDatabaseDriver _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Backend methods.
        /// </summary>
        /// <param name="repo">Database driver.</param>
        public BackendMethods(SqliteDatabaseDriver repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public OllamaBackend Create(OllamaBackend obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            string query = BackendQueries.Insert(obj);
            DataTable result = _Repo.ExecuteQuery(query, true);
            return Converters.BackendFromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public List<OllamaBackend> CreateMany(List<OllamaBackend> objs)
        {
            if (objs == null || objs.Count == 0) throw new ArgumentNullException(nameof(objs));

            List<string> queries = new List<string>();
            foreach (var obj in objs)
            {
                queries.Add(BackendQueries.Insert(obj));
            }

            // Execute all queries in a single transaction
            DataTable result = _Repo.ExecuteQueries(queries, true);

            // Since we're using RETURNING *, we need to get all created objects
            // We'll need to retrieve them individually
            List<OllamaBackend> created = new List<OllamaBackend>();
            foreach (var obj in objs)
            {
                string selectQuery = BackendQueries.SelectByIdentifier(obj.Identifier);
                DataTable selectResult = _Repo.ExecuteQuery(selectQuery);
                if (selectResult != null && selectResult.Rows.Count > 0)
                {
                    created.Add(Converters.BackendFromDataRow(selectResult.Rows[0]));
                }
            }

            return created;
        }

        /// <inheritdoc />
        public IEnumerable<OllamaBackend> ReadAll(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            int skip = 0;
            bool moreData = true;

            while (moreData)
            {
                string query = BackendQueries.SelectMany(_Repo.SelectBatchSize, skip, order);
                DataTable result = _Repo.ExecuteQuery(query);

                if (result == null || result.Rows.Count == 0)
                {
                    moreData = false;
                    yield break;
                }

                List<OllamaBackend> backends = Converters.BackendsFromDataTable(result);
                if (backends != null)
                {
                    foreach (var backend in backends)
                    {
                        yield return backend;
                    }
                }

                if (result.Rows.Count < _Repo.SelectBatchSize)
                {
                    moreData = false;
                }
                else
                {
                    skip += _Repo.SelectBatchSize;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<OllamaBackend> ReadByIdentifiers(List<string> ids)
        {
            if (ids == null || ids.Count == 0) yield break;

            // Process in batches to avoid exceeding query limits
            const int batchSize = 100;
            for (int i = 0; i < ids.Count; i += batchSize)
            {
                List<string> batch = ids.Skip(i).Take(batchSize).ToList();
                string query = BackendQueries.SelectByIdentifiers(batch);
                DataTable result = _Repo.ExecuteQuery(query);

                if (result != null && result.Rows.Count > 0)
                {
                    List<OllamaBackend> backends = Converters.BackendsFromDataTable(result);
                    if (backends != null)
                    {
                        foreach (var backend in backends)
                        {
                            yield return backend;
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public EnumerationResult<OllamaBackend> Enumerate(EnumerationRequest query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            EnumerationResult<OllamaBackend> result = new EnumerationResult<OllamaBackend>();
            result.Objects = new List<OllamaBackend>();
            result.MaxResults = query.MaxResults;

            // Parse continuation token to get the marker
            OllamaBackend marker = null;
            if (!string.IsNullOrEmpty(query.ContinuationToken))
            {
                string markerQuery = BackendQueries.SelectByIdentifier(query.ContinuationToken);
                DataTable markerResult = _Repo.ExecuteQuery(markerQuery);
                if (markerResult != null && markerResult.Rows.Count > 0)
                {
                    marker = Converters.BackendFromDataRow(markerResult.Rows[0]);
                }
            }

            // Get the page of records
            string pageQuery = BackendQueries.GetRecordPage(
                query.MaxResults,
                query.Skip,
                query.Ordering,
                marker);

            DataTable pageResult = _Repo.ExecuteQuery(pageQuery);

            if (pageResult != null && pageResult.Rows.Count > 0)
            {
                result.Objects = Converters.BackendsFromDataTable(pageResult);

                // Set continuation token if there might be more records
                if (result.Objects.Count == query.MaxResults)
                {
                    var lastRecord = result.Objects.Last();
                    result.ContinuationToken = lastRecord.Identifier;
                    result.EndOfResults = false;
                }
                else
                {
                    result.EndOfResults = true;
                }
            }

            // Get total count
            string countQuery = BackendQueries.GetRecordCount(query.Ordering, null);
            DataTable countResult = _Repo.ExecuteQuery(countQuery);
            if (countResult != null && countResult.Rows.Count > 0)
            {
                result.TotalRecords = Convert.ToInt64(countResult.Rows[0]["record_count"]);

                // Calculate records remaining
                if (!string.IsNullOrEmpty(query.ContinuationToken))
                {
                    string remainingQuery = BackendQueries.GetRecordCount(query.Ordering, marker);
                    DataTable remainingResult = _Repo.ExecuteQuery(remainingQuery);
                    if (remainingResult != null && remainingResult.Rows.Count > 0)
                    {
                        result.RecordsRemaining = Convert.ToInt64(remainingResult.Rows[0]["record_count"]);
                    }
                }
                else
                {
                    result.RecordsRemaining = result.TotalRecords - result.Objects.Count;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public int GetRecordCount(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, string continuationToken = null)
        {
            OllamaBackend marker = null;
            if (!string.IsNullOrEmpty(continuationToken))
            {
                string markerQuery = BackendQueries.SelectByIdentifier(continuationToken);
                DataTable markerResult = _Repo.ExecuteQuery(markerQuery);
                if (markerResult != null && markerResult.Rows.Count > 0)
                {
                    marker = Converters.BackendFromDataRow(markerResult.Rows[0]);
                }
            }

            string query = BackendQueries.GetRecordCount(order, marker);
            DataTable result = _Repo.ExecuteQuery(query);

            if (result != null && result.Rows.Count > 0)
            {
                return Convert.ToInt32(result.Rows[0]["record_count"]);
            }

            return 0;
        }

        /// <inheritdoc />
        public OllamaBackend Update(OllamaBackend obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            string query = BackendQueries.Update(obj);
            DataTable result = _Repo.ExecuteQuery(query, true);
            return Converters.BackendFromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public void DeleteByIdentifier(string id)
        {
            string query = BackendQueries.Delete(id);
            _Repo.ExecuteQuery(query, true);
        }

        /// <inheritdoc />
        public void DeleteMany(List<string> ids)
        {
            if (ids == null || ids.Count == 0) return;

            List<string> queries = new List<string>();
            foreach (var id in ids)
            {
                queries.Add(BackendQueries.Delete(id));
            }

            _Repo.ExecuteQueries(queries, true);
        }

        /// <inheritdoc />
        public bool ExistsByGuidIdentifier(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            string query = BackendQueries.SelectByIdentifier(id);
            DataTable result = _Repo.ExecuteQuery(query);

            return result != null && result.Rows.Count > 0;
        }

        /// <inheritdoc />
        public bool IsLinked(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            // Query to check if this backend identifier appears in any frontend's backends list
            string query = @"
                SELECT COUNT(*) as count 
                FROM frontends 
                WHERE backends LIKE '%""" + Sanitizer.Sanitize(id) + @"""%'";

            DataTable result = _Repo.ExecuteQuery(query);

            if (result != null && result.Rows.Count > 0)
            {
                int count = Convert.ToInt32(result.Rows[0]["count"]);
                return count > 0;
            }

            return false;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}