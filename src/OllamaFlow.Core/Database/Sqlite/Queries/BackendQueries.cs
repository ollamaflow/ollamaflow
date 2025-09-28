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

    internal static class BackendQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Serializer Serializer = new Serializer();

        internal static string Insert(Backend obj)
        {
            string ret =
                "INSERT INTO 'backends' "
                + "(identifier, name, hostname, port, ssl, unhealthythreshold, "
                + "healthythreshold, healthcheckmethod, healthcheckurl, maxparallelrequests, "
                + "ratelimitthreshold, logrequestfull, logrequestbody, logresponsebody, apiformat, active, createdutc, lastupdateutc) "
                + "VALUES ("
                + "'" + Sanitizer.Sanitize(obj.Identifier) + "',"
                + "'" + Sanitizer.Sanitize(obj.Name) + "',"
                + "'" + Sanitizer.Sanitize(obj.Hostname) + "',"
                + obj.Port + ","
                + (obj.Ssl ? "1" : "0") + ","
                + obj.UnhealthyThreshold + ","
                + obj.HealthyThreshold + ","
                + "'" + Sanitizer.Sanitize(obj.HealthCheckMethod.Method) + "',"
                + "'" + Sanitizer.Sanitize(obj.HealthCheckUrl) + "',"
                + obj.MaxParallelRequests + ","
                + obj.RateLimitRequestsThreshold + ","
                + (obj.LogRequestFull ? "1" : "0") + ","
                + (obj.LogRequestBody ? "1" : "0") + ","
                + (obj.LogResponseBody ? "1" : "0") + ","
                + "'" + Sanitizer.Sanitize(obj.ApiFormat.ToString()) + "',"
                + (obj.Active ? "1" : "0") + ","
                + "'" + Sanitizer.Sanitize(obj.CreatedUtc.ToString(TimestampFormat)) + "',"
                + "'" + Sanitizer.Sanitize(obj.LastUpdateUtc.ToString(TimestampFormat)) + "'"
                + ") "
                + "RETURNING *;";

            return ret;
        }

        internal static string SelectByName(string name)
        {
            return "SELECT * FROM 'backends' WHERE name = '" + Sanitizer.Sanitize(name) + "';";
        }

        internal static string SelectByIdentifier(string identifier)
        {
            return "SELECT * FROM 'backends' WHERE identifier = '" + Sanitizer.Sanitize(identifier) + "';";
        }

        internal static string SelectByIdentifiers(List<string> identifiers)
        {
            return
                "SELECT * FROM 'backends' " +
                "WHERE identifier IN (" +
                string.Join(", ", identifiers.Select(i => "'" + Sanitizer.Sanitize(i) + "'")) +
                ");";
        }

        internal static string SelectByHostname(string hostname)
        {
            return "SELECT * FROM 'backends' WHERE hostname = '" + Sanitizer.Sanitize(hostname) + "';";
        }

        internal static string SelectByHostnameAndPort(string hostname, int port)
        {
            return "SELECT * FROM 'backends' WHERE hostname = '" + Sanitizer.Sanitize(hostname) + "' AND port = " + port + ";";
        }

        internal static string SelectMany(
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'backends' WHERE identifier IS NOT NULL "
                + "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string GetRecordPage(
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Backend marker = null)
        {
            string ret = "SELECT * FROM 'backends' WHERE identifier IS NOT NULL ";

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
            Backend marker = null)
        {
            string ret = "SELECT COUNT(*) AS record_count FROM 'backends' WHERE identifier IS NOT NULL ";

            if (marker != null)
            {
                ret += "AND " + ContinuationTokenWhereClause(order, marker);
            }

            return ret;
        }

        internal static string Update(Backend obj)
        {
            return
                "UPDATE 'backends' SET "
                + "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "',"
                + "name = '" + Sanitizer.Sanitize(obj.Name) + "',"
                + "hostname = '" + Sanitizer.Sanitize(obj.Hostname) + "',"
                + "port = " + obj.Port + ","
                + "ssl = " + (obj.Ssl ? "1" : "0") + ","
                + "unhealthythreshold = " + obj.UnhealthyThreshold + ","
                + "healthythreshold = " + obj.HealthyThreshold + ","
                + "healthcheckmethod = '" + Sanitizer.Sanitize(obj.HealthCheckMethod.Method) + "',"
                + "healthcheckurl = '" + Sanitizer.Sanitize(obj.HealthCheckUrl) + "',"
                + "maxparallelrequests = " + obj.MaxParallelRequests + ","
                + "ratelimitthreshold = " + obj.RateLimitRequestsThreshold + ","
                + "logrequestfull = " + (obj.LogRequestFull ? "1" : "0") + ","
                + "logrequestbody = " + (obj.LogRequestBody ? "1" : "0") + ","
                + "logresponsebody = " + (obj.LogResponseBody ? "1" : "0") + ","
                + "apiformat = '" + Sanitizer.Sanitize(obj.ApiFormat.ToString()) + "',"
                + "active = " + (obj.Active ? "1" : "0") + " "
                + "WHERE identifier = '" + Sanitizer.Sanitize(obj.Identifier) + "' "
                + "RETURNING *;";
        }

        internal static string Delete(string identifier)
        {
            return "DELETE FROM 'backends' WHERE identifier = '" + Sanitizer.Sanitize(identifier) + "';";
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

        private static string ContinuationTokenWhereClause(EnumerationOrderEnum order, Backend obj)
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