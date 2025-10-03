namespace OllamaFlow.Core.Database.Interfaces
{
    using OllamaFlow.Core.Enums;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for backend methods.
    /// </summary>
    public interface IBackendMethods
    {
        /// <summary>
        /// Create.
        /// </summary>
        /// <param name="obj">Record.</param>
        /// <returns>Record.</returns>
        Backend Create(Backend obj);

        /// <summary>
        /// Create multiple records.
        /// </summary>
        /// <param name="objs">Records.</param>
        /// <returns>Records.</returns>
        List<Backend> CreateMany(List<Backend> objs);

        /// <summary>
        /// Read all records.
        /// </summary>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Records.</returns>
        IEnumerable<Backend> ReadAll(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending);

        /// <summary>
        /// Read objects by identifiers.
        /// </summary>
        /// <param name="ids">Identifiers.</param>
        /// <returns>Objects.</returns>
        IEnumerable<Backend> ReadByIdentifiers(List<string> ids);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result containing a page of objects.</returns>
        EnumerationResult<Backend> Enumerate(EnumerationRequest query);

        /// <summary>
        /// Get the record count.  Optionally supply a continuation token to indicate that only records from that continuation token (identifier) should be counted.
        /// </summary>
        /// <param name="order">Enumeration order.</param>
        /// <param name="continuationToken">Continuation token.</param>
        /// <returns>Number of records.</returns>
        int GetRecordCount(
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            string continuationToken = null);

        /// <summary>
        /// Update a record.
        /// </summary>
        /// <param name="obj">Record.</param>
        /// <returns>Record.</returns>
        Backend Update(Backend obj);

        /// <summary>
        /// Delete a record.
        /// </summary>
        /// <param name="id">Identifier.</param>
        void DeleteByIdentifier(string id);

        /// <summary>
        /// Delete records.
        /// </summary>
        /// <param name="ids">Identifiers.</param>
        void DeleteMany(List<string> ids);

        /// <summary>
        /// Delete all records.  Do not use this if you are not absolutely sure you want to delete all records!
        /// </summary>
        void DeleteAll();

        /// <summary>
        /// Check if a record exists by identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>True if exists.</returns>
        bool ExistsByGuidIdentifier(string id);

        /// <summary>
        /// Check if a record is linked to another record.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>True if linked.</returns>
        bool IsLinked(string id);
    }
}
