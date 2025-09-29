namespace OllamaFlow.Core.Database.Sqlite
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using SyslogLogging;
    using Microsoft.Data.Sqlite;
    using OllamaFlow.Core.Database.Interfaces;
    using OllamaFlow.Core.Database.Sqlite.Implementations;
    using OllamaFlow.Core.Database.Sqlite.Queries;
    using OllamaFlow.Core.Serialization;

    /// <summary>
    /// Sqlite database driver.
    /// </summary>
    public class SqliteDatabaseDriver : DatabaseDriverBase, IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Sqlite database filename.
        /// </summary>
        public string Filename
        {
            get
            {
                return _Filename;
            }
        }

        /// <summary>
        /// Maximum supported statement length.
        /// Default for Sqlite is 1,000,000,000 (see https://www.sqlite.org/limits.html).
        /// </summary>
        public int MaxStatementLength
        {
            get
            {
                return _MaxStatementLength;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxStatementLength));
                _MaxStatementLength = value;
            }
        }

        /// <summary>
        /// Number of records to retrieve for object list retrieval.
        /// </summary>
        public int SelectBatchSize
        {
            get
            {
                return _SelectBatchSize;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(SelectBatchSize));
                _SelectBatchSize = value;
            }
        }

        /// <summary>
        /// Timestamp format.
        /// </summary>
        public string TimestampFormat
        {
            get
            {
                return _TimestampFormat;
            }
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(TimestampFormat));
                string test = DateTime.UtcNow.ToString(value);
                _TimestampFormat = value;
            }
        }

        /// <summary>
        /// Frontend methods.
        /// </summary>
        public override IFrontendMethods Frontend { get; }

        /// <summary>
        /// Backend methods.
        /// </summary>
        public override IBackendMethods Backend { get; }

        #endregion

        #region Private-Members

        private OllamaFlowSettings _Settings = null;
        private string _Filename = Constants.DatabaseFilename;
        private string _ConnectionString = "Data Source=" + Constants.DatabaseFilename + ";Pooling=false";
        private SqliteConnection _SqliteConnection = null;
        private readonly object _QueryLock = new object();
        private bool _Disposed = false;

        private int _SelectBatchSize = 100;
        private int _MaxStatementLength = 1000000000; // https://www.sqlite.org/limits.html
        private string _TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Sqlite database driver.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="filename">Database filename.</param>
        public SqliteDatabaseDriver(OllamaFlowSettings settings, LoggingModule logging, Serializer serializer, string filename = Constants.DatabaseFilename) : base(logging, serializer)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            _Header = "[Sqlite] ";

            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Filename = filename;
            _ConnectionString = "Data Source=" + _Filename + ";Pooling=false";
            _SqliteConnection = new SqliteConnection(_ConnectionString);
            _SqliteConnection.Open();

            ApplyPerformanceSettings(_SqliteConnection);

            Frontend = new FrontendMethods(this);
            Backend = new BackendMethods(this);

            _Logging.Debug(_Header + "initialized");
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override void InitializeRepository()
        {
            ThrowIfDisposed();
            ExecuteQuery(SetupQueries.CreateTablesAndIndices());
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected-Methods

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources
                lock (_QueryLock)
                {
                    try
                    {
                        if (_SqliteConnection != null)
                        {
                            if (_SqliteConnection.State != ConnectionState.Closed)
                            {
                                _SqliteConnection.Close();
                            }
                            _SqliteConnection.Dispose();
                            _SqliteConnection = null;
                        }

                        _Logging?.Debug(_Header + "disposed");
                    }
                    catch (Exception ex)
                    {
                        _Logging?.Warn(_Header + "error during disposal: " + ex.Message);
                    }
                }
            }

            _Disposed = true;
        }

        #endregion

        #region Private-Methods

        private void ApplyPerformanceSettings(SqliteConnection conn)
        {
            using (SqliteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    // "PRAGMA journal_mode = WAL; " +
                    "PRAGMA synchronous = NORMAL; " +
                    "PRAGMA cache_size = -128000; " +
                    "PRAGMA temp_store = MEMORY; " +
                    "PRAGMA mmap_size = 536870912; ";
                cmd.ExecuteNonQuery();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_Disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        #endregion

        #region Internal-Methods

        internal DataTable ExecuteQuery(string query, bool isTransaction = false)
        {
            ThrowIfDisposed();

            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));
            if (query.Length > MaxStatementLength) throw new ArgumentException("Query exceeds maximum statement length of " + MaxStatementLength + " characters.");

            DataTable result = new DataTable();

            if (isTransaction)
            {
                query = query.Trim();
                query = "BEGIN TRANSACTION; " + query + " END TRANSACTION;";
            }

            if (_Settings.Logging.LogQueries) _Logging.Debug(_Header + "query: " + query);

            lock (_QueryLock)
            {
                using (SqliteConnection conn = new SqliteConnection(_ConnectionString))
                {
                    try
                    {
                        conn.Open();

                        using (SqliteCommand cmd = new SqliteCommand(query, conn))
                        {
                            using (SqliteDataReader rdr = cmd.ExecuteReader())
                            {
                                result.Load(rdr);
                            }
                        }

                        conn.Close();
                    }
                    catch (Exception e)
                    {
                        if (isTransaction)
                        {
                            using (SqliteCommand cmd = new SqliteCommand("ROLLBACK;", conn))
                                cmd.ExecuteNonQuery();
                        }

                        e.Data.Add("IsTransaction", isTransaction);
                        e.Data.Add("Query", query);
                        throw;
                    }
                }
            }

            if (_Settings.Logging.LogResults) _Logging.Debug(_Header + "result: " + query + ": " + (result != null ? result.Rows.Count + " rows" : "(null)"));
            return result;
        }

        internal DataTable ExecuteQueries(IEnumerable<string> queries, bool isTransaction = false)
        {
            ThrowIfDisposed();

            if (queries == null || !queries.Any()) throw new ArgumentNullException(nameof(queries));

            DataTable result = new DataTable();

            lock (_QueryLock)
            {
                using (SqliteConnection conn = new SqliteConnection(_ConnectionString))
                {
                    conn.Open();
                    SqliteTransaction transaction = null;

                    try
                    {
                        if (isTransaction)
                        {
                            transaction = conn.BeginTransaction();
                        }

                        DataTable lastResult = null;

                        foreach (string query in queries.Where(q => !string.IsNullOrEmpty(q)))
                        {
                            if (query.Length > MaxStatementLength)
                                throw new ArgumentException($"Query exceeds maximum statement length of {MaxStatementLength} characters.");

                            if (_Settings.Logging.LogQueries) _Logging.Debug(_Header + "query: " + query);

                            using (SqliteCommand cmd = new SqliteCommand(query, conn))
                            {
                                if (transaction != null)
                                {
                                    cmd.Transaction = transaction;
                                }

                                using (SqliteDataReader rdr = cmd.ExecuteReader())
                                {
                                    lastResult = new DataTable();
                                    lastResult.Load(rdr);
                                }

                                // We'll return the result of the last query that returns data
                                if (lastResult != null && lastResult.Rows.Count > 0)
                                {
                                    result = lastResult;
                                }
                            }
                        }

                        // Commit the transaction if we're using one
                        transaction?.Commit();
                    }
                    catch (Exception e)
                    {
                        // Roll back the transaction if an error occurs
                        transaction?.Rollback();

                        e.Data.Add("IsTransaction", isTransaction);
                        e.Data.Add("Queries", string.Join("; ", queries));
                        throw;
                    }
                    finally
                    {
                        transaction?.Dispose();
                        conn.Close();
                    }
                }
            }

            if (_Settings.Logging.LogResults) _Logging.Debug(_Header + "result: " + (result != null ? result.Rows.Count + " rows" : "(null)"));
            return result;
        }

        #endregion

        #region Destructor

        /// <summary>
        /// Finalizer for SqliteDatabaseDriver.
        /// </summary>
        ~SqliteDatabaseDriver()
        {
            Dispose(false);
        }

        #endregion
    }
}