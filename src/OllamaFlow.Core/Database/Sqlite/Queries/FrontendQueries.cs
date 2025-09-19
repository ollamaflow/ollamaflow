namespace OllamaFlow.Core.Database.Sqlite.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ExpressionTree;
    using OllamaFlow.Core.Serialization;

    internal static class FrontendQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Serializer Serializer = new Serializer();

        internal static string Insert(OllamaFrontend obj)
        {
            string ret =
                "INSERT INTO 'frontends' "
                + "(identifier, name, hostname, timeoutms, loadbalancing, blockhttp10, "
                + "maxrequestbodysize, backends, requiredmodels, logrequestfull, "
                + "logrequestbody, logresponsebody, usestickysessions, stickysessionexpirationms, "
                + "active, createdutc, lastupdateutc) "
                + "VALUES ("
                + "'" + Sanitizer.Sanitize(obj.Identifier) + "',"
                + "'" + Sanitizer.Sanitize(obj.Name) + "',"
                + "'" + Sanitizer.Sanitize(obj.Hostname) + "',"
                + obj.TimeoutMs + ","
                + "'" + Sanitizer.Sanitize(obj.LoadBalancing.ToString()) + "',"
                + (obj.BlockHttp10 ? "1" : "0") + ","
                + obj.MaxRequestBodySize + ","
                + "'" + Sanitizer.Sanitize(obj.BackendsString) + "',"
                + "'" + Sanitizer.Sanitize(obj.RequiredModelsString) + "',"
                + (obj.LogRequestFull ? "1" : "0") + ","
                + (obj.LogRequestBody ? "1" : "0") + ","
                + (obj.LogResponseBody ? "1" : "0") + ","
                + (obj.UseStickySessions ? "1" : "0") + ","
                + obj.StickySessionExpirationMs + ","
                + (obj.Active ? "1" : "0") + ","
                + "'" + Sanitizer.Sanitize(obj.CreatedUtc.ToString(TimestampFormat)) + "',"
                + "'" + Sanitizer.Sanitize(obj.LastUpdateUtc.ToString(TimestampFormat)) + "'"
                + ") "
                + "RETURNING *;";

            return ret;
        }

        internal static string SelectByName(string name)
        {
            return "SELECT * FROM 'frontends' WHERE name = '" + Sanitizer.Sanitize(name) + "';";
        }

        internal static string SelectByIdentifier(string identifier)
        {
            return "SELECT * FROM 'frontends' WHERE identifier = '" + Sanitizer.Sanitize(identifier) + "';";
        }

        internal static string SelectByIdentifiers(List<string> identifiers)
        {
            return
                "SELECT * FROM 'frontends' " +
                "WHERE identifier IN (" +
                string.Join(", ", identifiers.Select(i => "'" + Sanitizer.Sanitize(i) + "'")) +
                ");";
        }

        internal static string SelectByHostname(string hostname)
        {
            return "SELECT * FROM 'frontends' WHERE hostname = '" + Sanitizer.Sanitize(hostname) + "';";
        }

        internal static string SelectMany(
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'frontends' WHERE identifier IS NOT NULL "
                + "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string GetRecordPage(
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            OllamaFrontend marker = null)
        {
            string ret = "SELECT * FROM 'frontends' WHERE identifier IS NOT NULL ";

            if (marker != null)
            {
                ret += "AND " + ContinuationTokenWhereClause(order, marker);
            }

            ret += OrderByClause(order);
            ret += "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string GetRecordCount(
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            OllamaFrontend marker = null)
        {
            string ret = "SELECT COUNT(*) AS record_count FROM 'frontends' WHERE identifier IS NOT NULL ";

            if (marker != null)
            {
                ret += "AND " + ContinuationTokenWhereClause(order, marker);
            }

            return ret;
        }

        internal static string Update(OllamaFrontend obj)
        {
            string query =
                "UPDATE 'frontends' SET "
                + "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "',"
                + "name = '" + Sanitizer.Sanitize(obj.Name) + "',"
                + "hostname = '" + Sanitizer.Sanitize(obj.Hostname) + "',"
                + "timeoutms = " + obj.TimeoutMs + ","
                + "loadbalancing = '" + Sanitizer.Sanitize(obj.LoadBalancing.ToString()) + "',"
                + "blockhttp10 = " + (obj.BlockHttp10 ? "1" : "0") + ","
                + "maxrequestbodysize = " + obj.MaxRequestBodySize + ","
                + "backends = '" + Sanitizer.Sanitize(obj.BackendsString) + "',"
                + "requiredmodels = '" + Sanitizer.Sanitize(obj.RequiredModelsString) + "',"
                + "logrequestfull = " + (obj.LogRequestFull ? "1" : "0") + ","
                + "logrequestbody = " + (obj.LogRequestBody ? "1": "0") + ","
                + "logresponsebody = " + (obj.LogResponseBody ? "1" : "0") + ","
                + "usestickysessions = " + (obj.UseStickySessions ? "1" : "0") + ","
                + "stickysessionexpirationms = " + obj.StickySessionExpirationMs + ","
                + "active = " + (obj.Active ? "1" : "0") + " "
                + "WHERE identifier = '" + Sanitizer.Sanitize(obj.Identifier) + "' "
                + "RETURNING *;"; 
            return query;
        }

        internal static string Delete(string identifier)
        {
            return "DELETE FROM 'frontends' WHERE identifier = '" + Sanitizer.Sanitize(identifier) + "';";
        }

        private static string OrderByClause(EnumerationOrderEnum order)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CreatedDescending:
                    return "ORDER BY createdutc DESC ";
                case EnumerationOrderEnum.CreatedAscending:
                    return "ORDER BY createdutc ASC ";
                case EnumerationOrderEnum.IdentifierAscending:
                    return "ORDER BY identifier ASC ";
                case EnumerationOrderEnum.IdentifierDescending:
                    return "ORDER BY identifier DESC ";
                default:
                    return "ORDER BY createdutc DESC ";
            }
        }

        private static string ContinuationTokenWhereClause(EnumerationOrderEnum order, OllamaFrontend obj)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CreatedAscending:
                    return "createdutc > '" + obj.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.CreatedDescending:
                    return "createdutc < '" + obj.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.IdentifierAscending:
                    return "identifier > '" + Sanitizer.Sanitize(obj.Identifier) + "' ";
                case EnumerationOrderEnum.IdentifierDescending:
                    return "identifier < '" + Sanitizer.Sanitize(obj.Identifier) + "' ";
                default:
                    return "identifier IS NOT NULL ";
            }
        }
    }
}