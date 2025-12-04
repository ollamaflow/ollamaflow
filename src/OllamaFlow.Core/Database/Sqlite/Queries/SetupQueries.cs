namespace OllamaFlow.Core.Database.Sqlite.Queries
{
    using System.Collections.Generic;
    using System.Text;

    internal static class SetupQueries
    {
        internal static string CreateTablesAndIndices()
        {
            StringBuilder sql = new StringBuilder();

            #region Frontends

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'frontends' ("
                + "identifier VARCHAR(128) NOT NULL UNIQUE, "
                + "name VARCHAR(128), "
                + "hostname VARCHAR(128) NOT NULL, "
                + "timeoutms INT NOT NULL, "
                + "loadbalancing VARCHAR(32) NOT NULL, "
                + "blockhttp10 INT NOT NULL, "
                + "maxrequestbodysize INT NOT NULL, "
                + "backends VARCHAR(2048) NOT NULL, "
                + "requiredmodels VARCHAR(2048) NOT NULL, "
                + "logrequestfull INT NOT NULL, "
                + "logrequestbody INT NOT NULL, "
                + "logresponsebody INT NOT NULL, "
                + "usestickysessions INT NOT NULL DEFAULT 0, "
                + "stickysessionexpirationms INT NOT NULL DEFAULT 1800000, "
                + "pinnedembeddingsprops VARCHAR(2048) NOT NULL, "
                + "pinnedcompletionsprops VARCHAR(2048) NOT NULL, "
                + "allowretries INT NOT NULL DEFAULT 1, "
                + "allowembeddings INT NOT NULL DEFAULT 1, "
                + "allowcompletions INT NOT NULL DEFAULT 1, "
                + "active INT NOT NULL, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_frontends_identifier' ON 'frontends' (identifier ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_frontends_name' ON 'frontends' ('name' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_frontends_hostname' ON 'frontends' ('hostname' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_frontends_active' ON 'frontends' ('active' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_frontends_allowembeddings' ON 'frontends' ('allowembeddings' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_frontends_allowcompletions' ON 'frontends' ('allowcompletions' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_frontends_createdutc' ON 'frontends' ('createdutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_frontends_lastupdateutc' ON 'frontends' ('lastupdateutc' ASC);");

            #endregion

            #region Backends

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'backends' ("
                + "identifier VARCHAR(128) NOT NULL UNIQUE, "
                + "name VARCHAR(128), "
                + "hostname VARCHAR(128), "
                + "port INT NOT NULL, "
                + "ssl INT NOT NULL, "
                + "unhealthythreshold INT NOT NULL, "
                + "healthythreshold INT NOT NULL, "
                + "healthcheckmethod VARCHAR(16) NOT NULL, "
                + "healthcheckurl VARCHAR(256) NOT NULL, "
                + "maxparallelrequests INT NOT NULL, "
                + "ratelimitthreshold INT NOT NULL, "
                + "logrequestfull INT NOT NULL, "
                + "logrequestbody INT NOT NULL, "
                + "logresponsebody INT NOT NULL, "
                + "apiformat VARCHAR(32) DEFAULT 'Ollama', "
                + "labels VARCHAR(512) NOT NULL, "
                + "pinnedembeddingsprops VARCHAR(2048) NOT NULL, "
                + "pinnedcompletionsprops VARCHAR(2048) NOT NULL, "
                + "bearertoken VARCHAR(512), "
                + "querystring VARCHAR(2048), "
                + "headers VARCHAR(4096) NOT NULL DEFAULT '{}', "
                + "allowembeddings INT NOT NULL DEFAULT 1, "
                + "allowcompletions INT NOT NULL DEFAULT 1, "
                + "active INT NOT NULL, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_identifier' ON 'backends' ('identifier' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_name' ON 'backends' ('name' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_hostname' ON 'backends' ('hostname' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_active' ON 'backends' ('active' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_allowembeddings' ON 'backends' ('allowembeddings' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_allowcompletions' ON 'backends' ('allowcompletions' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_createdutc' ON 'backends' ('createdutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_lastupdateutc' ON 'backends' ('lastupdateutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_hostname_port' ON 'backends' ('hostname' ASC, 'port' ASC);");

            #endregion

            return sql.ToString();
        }

        #region Updates

        /// <summary>
        /// Returns a list of ALTER TABLE statements to add columns that may not exist in older databases.
        /// Each statement should be executed individually with error handling to ignore "duplicate column name" errors.
        /// </summary>
        /// <returns>List of ALTER TABLE SQL statements.</returns>
        internal static List<string> GetBackendColumnMigrations()
        {
            return new List<string>
            {
                "ALTER TABLE 'backends' ADD COLUMN bearertoken VARCHAR(512);",
                "ALTER TABLE 'backends' ADD COLUMN querystring VARCHAR(2048);",
                "ALTER TABLE 'backends' ADD COLUMN headers VARCHAR(4096) NOT NULL DEFAULT '{}';"
            };
        }

        #endregion
    }
}
