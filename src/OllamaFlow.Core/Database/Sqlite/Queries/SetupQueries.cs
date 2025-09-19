namespace OllamaFlow.Core.Database.Sqlite.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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
                + "active INT NOT NULL, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_frontends_identifier' ON 'frontends' (identifier ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_frontends_name' ON 'frontends' ('name' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_frontends_hostname' ON 'frontends' ('hostname' ASC);");
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
                + "active INT NOT NULL, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_identifier' ON 'backends' ('identifier' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_name' ON 'backends' ('name' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_hostname' ON 'backends' ('hostname' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_createdutc' ON 'backends' ('createdutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_lastupdateutc' ON 'backends' ('lastupdateutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_backends_hostname_port' ON 'backends' ('hostname' ASC, 'port' ASC);");

            #endregion

            return sql.ToString();
        }
    }
}
