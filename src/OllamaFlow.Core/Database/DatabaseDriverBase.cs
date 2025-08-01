namespace OllamaFlow.Core.Database
{
    using System;
    using OllamaFlow.Core.Database.Interfaces;
    using OllamaFlow.Core.Serialization;
    using SyslogLogging;

    /// <summary>
    /// Base class for database driver.
    /// </summary>
    public abstract class DatabaseDriverBase
    {
        /// <summary>
        /// Logging header
        /// </summary>
        public string _Header = "[Database] ";

        /// <summary>
        /// Logging module.
        /// </summary>
        public LoggingModule _Logging = null;

        /// <summary>
        /// Serializer.
        /// </summary>
        public Serializer _Serializer = null;

        /// <summary>
        /// Frontend methods.
        /// </summary>
        public abstract IFrontendMethods Frontend { get; }

        /// <summary>
        /// Backend methods.
        /// </summary>
        public abstract IBackendMethods Backend { get; }

        /// <summary>
        /// Base class for database driver.
        /// </summary>
        /// <param name="logging">Logging module.</param>
        /// <param name="serializer">Serializer.</param>
        public DatabaseDriverBase(
            LoggingModule logging, 
            Serializer serializer)
        {
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// Initialize the repository.
        /// </summary>
        public abstract void InitializeRepository();
    }
}
