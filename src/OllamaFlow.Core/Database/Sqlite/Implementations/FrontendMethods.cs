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
    using OllamaFlow.Core.Enums;

    /// <summary>
    /// Frontend methods.
    /// </summary>
    public class FrontendMethods : IFrontendMethods
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
        /// Frontend methods.
        /// </summary>
        /// <param name="repo">Database driver.</param>
        public FrontendMethods(SqliteDatabaseDriver repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public Frontend Create(Frontend obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            string query = FrontendQueries.Insert(obj);
            DataTable result = _Repo.ExecuteQuery(query, true);
            return Converters.FrontendFromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public List<Frontend> CreateMany(List<Frontend> objs)
        {
            if (objs == null || objs.Count == 0) throw new ArgumentNullException(nameof(objs));

            List<string> queries = new List<string>();
            foreach (Frontend obj in objs)
            {
                queries.Add(FrontendQueries.Insert(obj));
            }

            // Execute all queries in a single transaction
            DataTable result = _Repo.ExecuteQueries(queries, true);

            // Since we're using RETURNING *, we need to get all created objects
            // We'll need to retrieve them individually
            List<Frontend> created = new List<Frontend>();
            foreach (Frontend obj in objs)
            {
                string selectQuery = FrontendQueries.SelectByIdentifier(obj.Identifier);
                DataTable selectResult = _Repo.ExecuteQuery(selectQuery);
                if (selectResult != null && selectResult.Rows.Count > 0)
                {
                    created.Add(Converters.FrontendFromDataRow(selectResult.Rows[0]));
                }
            }

            return created;
        }

        /// <inheritdoc />
        public IEnumerable<Frontend> ReadAll(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            int skip = 0;
            bool moreData = true;

            while (moreData)
            {
                string query = FrontendQueries.SelectMany(_Repo.SelectBatchSize, skip, order);
                DataTable result = _Repo.ExecuteQuery(query);

                if (result == null || result.Rows.Count == 0)
                {
                    moreData = false;
                    yield break;
                }

                List<Frontend> frontends = Converters.FrontendsFromDataTable(result);
                if (frontends != null)
                {
                    foreach (Frontend frontend in frontends)
                    {
                        yield return frontend;
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
        public IEnumerable<Frontend> ReadByIdentifiers(List<string> ids)
        {
            if (ids == null || ids.Count == 0) yield break;

            List<string> identifiers = ids;

            // Process in batches to avoid exceeding query limits
            const int batchSize = 100;
            for (int i = 0; i < identifiers.Count; i += batchSize)
            {
                List<string> batch = identifiers.Skip(i).Take(batchSize).ToList();
                string query = FrontendQueries.SelectByIdentifiers(batch);
                DataTable result = _Repo.ExecuteQuery(query);

                if (result != null && result.Rows.Count > 0)
                {
                    List<Frontend> frontends = Converters.FrontendsFromDataTable(result);
                    if (frontends != null)
                    {
                        foreach (Frontend frontend in frontends)
                        {
                            yield return frontend;
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public EnumerationResult<Frontend> Enumerate(EnumerationRequest query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            EnumerationResult<Frontend> result = new EnumerationResult<Frontend>();
            result.Objects = new List<Frontend>();
            result.MaxResults = query.MaxResults;

            // Parse continuation token to get the marker
            Frontend marker = null;
            if (!string.IsNullOrEmpty(query.ContinuationToken))
            {
                string markerQuery = FrontendQueries.SelectByIdentifier(query.ContinuationToken);
                DataTable markerResult = _Repo.ExecuteQuery(markerQuery);
                if (markerResult != null && markerResult.Rows.Count > 0)
                {
                    marker = Converters.FrontendFromDataRow(markerResult.Rows[0]);
                }
            }

            // Get the page of records
            string pageQuery = FrontendQueries.GetRecordPage(
                query.MaxResults,
                query.Skip,
                query.Ordering,
                marker);

            DataTable pageResult = _Repo.ExecuteQuery(pageQuery);

            if (pageResult != null && pageResult.Rows.Count > 0)
            {
                result.Objects = Converters.FrontendsFromDataTable(pageResult);

                // Set continuation token if there might be more records
                if (result.Objects.Count == query.MaxResults)
                {
                    Frontend lastRecord = result.Objects.Last();
                    result.ContinuationToken = lastRecord.Identifier;
                    result.EndOfResults = false;
                }
                else
                {
                    result.EndOfResults = true;
                }
            }

            // Get total count
            string countQuery = FrontendQueries.GetRecordCount(query.Ordering, null);
            DataTable countResult = _Repo.ExecuteQuery(countQuery);
            if (countResult != null && countResult.Rows.Count > 0)
            {
                result.TotalRecords = Convert.ToInt64(countResult.Rows[0]["record_count"]);

                // Calculate records remaining
                if (!string.IsNullOrEmpty(query.ContinuationToken))
                {
                    string remainingQuery = FrontendQueries.GetRecordCount(query.Ordering, marker);
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
            Frontend marker = null;
            if (!string.IsNullOrEmpty(continuationToken))
            {
                string markerQuery = FrontendQueries.SelectByIdentifier(continuationToken);
                DataTable markerResult = _Repo.ExecuteQuery(markerQuery);
                if (markerResult != null && markerResult.Rows.Count > 0)
                {
                    marker = Converters.FrontendFromDataRow(markerResult.Rows[0]);
                }
            }

            string query = FrontendQueries.GetRecordCount(order, marker);
            DataTable result = _Repo.ExecuteQuery(query);

            if (result != null && result.Rows.Count > 0)
            {
                return Convert.ToInt32(result.Rows[0]["record_count"]);
            }

            return 0;
        }

        /// <inheritdoc />
        public Frontend Update(Frontend obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            string query = FrontendQueries.Update(obj);
            DataTable result = _Repo.ExecuteQuery(query, true);
            return Converters.FrontendFromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public void DeleteByGuid(string id)
        {
            string query = FrontendQueries.Delete(id);
            _Repo.ExecuteQuery(query, true);
        }

        /// <inheritdoc />
        public void DeleteMany(List<string> ids)
        {
            if (ids == null || ids.Count == 0) return;

            List<string> queries = new List<string>();
            foreach (string id in ids)
            {
                queries.Add(FrontendQueries.Delete(id));
            }

            _Repo.ExecuteQueries(queries, true);
        }

        /// <inheritdoc />
        public void DeleteAll()
        {
            _Repo.ExecuteQuery(FrontendQueries.DeleteAll(), true);
        }

        /// <inheritdoc />
        public bool ExistsByIdentifier(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            string query = FrontendQueries.SelectByIdentifier(id);
            DataTable result = _Repo.ExecuteQuery(query);

            return result != null && result.Rows.Count > 0;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}