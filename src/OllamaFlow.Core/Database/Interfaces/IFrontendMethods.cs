namespace OllamaFlow.Core.Database.Interfaces
{
    using OllamaFlow.Core.Enums;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for frontend methods.
    /// </summary>
    public interface IFrontendMethods
    {
        /// <summary>
        /// Create.
        /// </summary>
        /// <param name="obj">Record.</param>
        /// <returns>Record.</returns>
        Frontend Create(Frontend obj);

        /// <summary>
        /// Create multiple records.
        /// </summary>
        /// <param name="objs">Records.</param>
        /// <returns>Records.</returns>
        List<Frontend> CreateMany(List<Frontend> objs);

        /// <summary>
        /// Read all records.
        /// </summary>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Records.</returns>
        IEnumerable<Frontend> ReadAll(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending);

        /// <summary>
        /// Read objects by identifiers.
        /// </summary>
        /// <param name="ids">Identifiers.</param>
        /// <returns>Objects.</returns>
        IEnumerable<Frontend> ReadByIdentifiers(List<string> ids);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result containing a page of objects.</returns>
        EnumerationResult<Frontend> Enumerate(EnumerationRequest query);

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
        Frontend Update(Frontend obj);

        /// <summary>
        /// Delete a record.
        /// </summary>
        /// <param name="id">Identifier.</param>
        void DeleteByGuid(string id);

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
        bool ExistsByIdentifier(string id);
    }
}
