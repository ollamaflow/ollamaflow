﻿namespace OllamaFlow.Core.Database.Interfaces
{
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
        OllamaFrontend Create(OllamaFrontend obj);

        /// <summary>
        /// Create multiple records.
        /// </summary>
        /// <param name="objs">Records.</param>
        /// <returns>Records.</returns>
        List<OllamaFrontend> CreateMany(List<OllamaFrontend> objs);

        /// <summary>
        /// Read all records.
        /// </summary>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Records.</returns>
        IEnumerable<OllamaFrontend> ReadAll(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending);

        /// <summary>
        /// Read objects by identifiers.
        /// </summary>
        /// <param name="ids">Identifiers.</param>
        /// <returns>Objects.</returns>
        IEnumerable<OllamaFrontend> ReadByIdentifiers(List<string> ids);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result containing a page of objects.</returns>
        EnumerationResult<OllamaFrontend> Enumerate(EnumerationRequest query);

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
        OllamaFrontend Update(OllamaFrontend obj);

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
        /// Check if a record exists by identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>True if exists.</returns>
        bool ExistsByIdentifier(string id);
    }
}
