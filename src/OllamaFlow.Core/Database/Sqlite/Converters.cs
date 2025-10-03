namespace OllamaFlow.Core.Database.Sqlite
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using ExpressionTree;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Serialization;
    using OllamaFlow.Core.Models;

    internal static class Converters
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Serializer Serializer = new Serializer();

        internal static bool HasColumn(DataTable table, string column)
        {
            return table.Columns.Contains(column);
        }

        internal static string GetDataRowStringValue(DataRow row, string column)
        {
            if (row.Table.Columns.Contains(column))
            {
                if (row[column] != null && row[column] != DBNull.Value)
                {
                    return row[column].ToString();
                }
            }
            return null;
        }

        internal static object GetDataRowJsonValue(DataRow row, string column)
        {
            if (row.Table.Columns.Contains(column))
            {
                if (row[column] != null && row[column] != DBNull.Value)
                {
                    return Serializer.DeserializeJson<object>(row[column].ToString());
                }
            }
            return null;
        }

        internal static int GetDataRowIntValue(DataRow row, string column)
        {
            if (row.Table.Columns.Contains(column))
            {
                if (row[column] != null && row[column] != DBNull.Value)
                {
                    if (int.TryParse(row[column].ToString(), out int val))
                        return val;
                }
            }
            return 0;
        }

        internal static int? GetDataRowNullableIntValue(DataRow row, string column)
        {
            if (row.Table.Columns.Contains(column))
            {
                if (row[column] != null && row[column] != DBNull.Value)
                {
                    if (int.TryParse(row[column].ToString(), out int val))
                        return val;
                }
            }
            return null;
        }

        internal static bool IsList(object o)
        {
            if (o == null) return false;
            return o is IList &&
                   o.GetType().IsGenericType &&
                   o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        internal static List<object> ObjectToList(object obj)
        {
            if (obj == null) return null;
            List<object> ret = new List<object>();
            IEnumerator enumerator = ((IEnumerable)obj).GetEnumerator();
            while (enumerator.MoveNext())
            {
                ret.Add(enumerator.Current);
            }
            return ret;
        }

        internal static string EnumerationOrderToClause(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CreatedAscending:
                    return "createdutc ASC";
                case EnumerationOrderEnum.CreatedDescending:
                    return "createdutc DESC";
                case EnumerationOrderEnum.IdentifierAscending:
                    return "identifier ASC";
                case EnumerationOrderEnum.IdentifierDescending:
                    return "identifier DESC";
                default:
                    throw new ArgumentException("Unsupported enumeration order '" + order.ToString() + "'.");
            }
        }

        internal static string ExpressionToWhereClause(string table, Expr expr)
        {
            if (expr == null) return null;
            if (expr.Left == null) return null;

            string clause = "(";

            if (expr.Left is Expr)
            {
                clause += ExpressionToWhereClause(table, (Expr)expr.Left) + " ";
            }
            else
            {
                if (!(expr.Left is string))
                {
                    throw new ArgumentException("Left term must be of type Expression or String");
                }

                clause += "json_extract(" + table + ".data, '$." + Sanitizer.Sanitize(expr.Left.ToString()) + "') ";
            }

            switch (expr.Operator)
            {
                #region Process-By-Operators

                case OperatorEnum.And:
                    #region And

                    if (expr.Right == null) return null;
                    clause += "AND ";

                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal || expr.Right is double || expr.Right is float)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += (bool)expr.Right ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.Or:
                    #region Or

                    if (expr.Right == null) return null;
                    clause += "OR ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal || expr.Right is double || expr.Right is float)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += (bool)expr.Right ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.Equals:
                    #region Equals

                    if (expr.Right == null) return null;
                    clause += "= ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal || expr.Right is double || expr.Right is float)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += (bool)expr.Right ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.NotEquals:
                    #region NotEquals

                    if (expr.Right == null) return null;
                    clause += "<> ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal || expr.Right is double || expr.Right is float)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += (bool)expr.Right ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.In:
                    #region In

                    if (expr.Right == null) return null;
                    int inAdded = 0;
                    if (!IsList(expr.Right)) return null;
                    List<object> inTempList = ObjectToList(expr.Right);
                    clause += "IN (";
                    foreach (object currObj in inTempList)
                    {
                        if (currObj == null) continue;
                        if (inAdded > 0) clause += ",";

                        if (currObj is DateTime || currObj is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(currObj).ToString(TimestampFormat) + "'";
                        }
                        else if (currObj is int || currObj is long || currObj is decimal || currObj is double || currObj is float)
                        {
                            clause += currObj.ToString();
                        }
                        else if (currObj is bool)
                        {
                            clause += (bool)currObj ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(currObj.ToString()) + "'";
                        }
                        inAdded++;
                    }
                    clause += ")";
                    break;

                #endregion

                case OperatorEnum.NotIn:
                    #region NotIn

                    if (expr.Right == null) return null;
                    int notInAdded = 0;
                    if (!IsList(expr.Right)) return null;
                    List<object> notInTempList = ObjectToList(expr.Right);
                    clause += "NOT IN (";
                    foreach (object currObj in notInTempList)
                    {
                        if (currObj == null) continue;
                        if (notInAdded > 0) clause += ",";
                        if (currObj is DateTime || currObj is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(currObj).ToString(TimestampFormat) + "'";
                        }
                        else if (currObj is int || currObj is long || currObj is decimal || currObj is double || currObj is float)
                        {
                            clause += currObj.ToString();
                        }
                        else if (currObj is bool)
                        {
                            clause += (bool)currObj ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(currObj.ToString()) + "'";
                        }
                        notInAdded++;
                    }
                    clause += ")";
                    break;

                #endregion

                case OperatorEnum.Contains:
                    #region Contains

                    if (expr.Right == null) return null;
                    if (expr.Right is string)
                    {
                        clause += "LIKE '%" + Sanitizer.Sanitize(expr.Right.ToString()) + "%'";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                #endregion

                case OperatorEnum.ContainsNot:
                    #region ContainsNot

                    if (expr.Right == null) return null;
                    if (expr.Right is string)
                    {
                        clause += "NOT LIKE '%" + Sanitizer.Sanitize(expr.Right.ToString()) + "%'";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                #endregion

                case OperatorEnum.StartsWith:
                    #region StartsWith

                    if (expr.Right == null) return null;
                    if (expr.Right is string)
                    {
                        clause += "LIKE '" + Sanitizer.Sanitize(expr.Right.ToString()) + "%'";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                #endregion

                case OperatorEnum.StartsWithNot:
                    #region StartsWithNot

                    if (expr.Right == null) return null;
                    if (expr.Right is string)
                    {
                        clause += "NOT LIKE '" + Sanitizer.Sanitize(expr.Right.ToString()) + "%'";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                #endregion

                case OperatorEnum.EndsWith:
                    #region EndsWith

                    if (expr.Right == null) return null;
                    if (expr.Right is string)
                    {
                        clause += "LIKE '%" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                #endregion

                case OperatorEnum.EndsWithNot:
                    #region EndsWith

                    if (expr.Right == null) return null;
                    if (expr.Right is string)
                    {
                        clause += "NOT LIKE '%" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                #endregion

                case OperatorEnum.GreaterThan:
                    #region GreaterThan

                    if (expr.Right == null) return null;
                    clause += "> ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal || expr.Right is double || expr.Right is float)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += (bool)expr.Right ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.GreaterThanOrEqualTo:
                    #region GreaterThanOrEqualTo

                    if (expr.Right == null) return null;
                    clause += ">= ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal || expr.Right is double || expr.Right is float)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += (bool)expr.Right ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.LessThan:
                    #region LessThan

                    if (expr.Right == null) return null;
                    clause += "< ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal || expr.Right is double || expr.Right is float)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += (bool)expr.Right ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.LessThanOrEqualTo:
                    #region LessThanOrEqualTo

                    if (expr.Right == null) return null;
                    clause += "<= ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal || expr.Right is double || expr.Right is float)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += (bool)expr.Right ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.IsNull:
                    #region IsNull

                    clause += "IS NULL";
                    break;

                #endregion

                case OperatorEnum.IsNotNull:
                    #region IsNotNull

                    clause += "IS NOT NULL";
                    break;

                    #endregion

                    #endregion
            }

            clause += ") ";

            return clause;
        }

        internal static Frontend FrontendFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Frontend
            {
                Identifier = GetDataRowStringValue(row, "identifier"),
                Name = GetDataRowStringValue(row, "name"),
                Hostname = GetDataRowStringValue(row, "hostname"),
                TimeoutMs = GetDataRowIntValue(row, "timeoutms"),
                LoadBalancing = (LoadBalancingMode)Enum.Parse(typeof(LoadBalancingMode), GetDataRowStringValue(row, "loadbalancing")),
                BlockHttp10 = GetDataRowIntValue(row, "blockhttp10") > 0,
                MaxRequestBodySize = GetDataRowIntValue(row, "maxrequestbodysize"),
                BackendsString = GetDataRowStringValue(row, "backends"),
                RequiredModelsString = GetDataRowStringValue(row, "requiredmodels"),
                LogRequestFull = GetDataRowIntValue(row, "logrequestfull") == 1,
                LogRequestBody = GetDataRowIntValue(row, "logrequestbody") == 1,
                LogResponseBody = GetDataRowIntValue(row, "logresponsebody") == 1,
                UseStickySessions = HasColumn(row.Table, "usestickysessions") ? GetDataRowIntValue(row, "usestickysessions") == 1 : false,
                StickySessionExpirationMs = HasColumn(row.Table, "stickysessionexpirationms") ? GetDataRowIntValue(row, "stickysessionexpirationms") : 1800000,
                PinnedEmbeddingsPropertiesString = GetDataRowStringValue(row, "pinnedembeddingsprops"),
                PinnedCompletionsPropertiesString = GetDataRowStringValue(row, "pinnedcompletionsprops"),
                AllowEmbeddings = HasColumn(row.Table, "allowembeddings") ? GetDataRowIntValue(row, "allowembeddings") == 1 : false,
                AllowCompletions = HasColumn(row.Table, "allowcompletions") ? GetDataRowIntValue(row, "allowcompletions") == 1 : false,
                AllowRetries = HasColumn(row.Table, "allowretries") ? GetDataRowIntValue(row, "allowretries") == 1 : false,
                Active = GetDataRowIntValue(row, "active") == 1,
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static List<Frontend> FrontendsFromDataTable(DataTable table)
        {
            if (table == null || table.Rows == null || table.Rows.Count < 1) return null;

            List<Frontend> ret = new List<Frontend>();

            foreach (DataRow row in table.Rows)
                ret.Add(FrontendFromDataRow(row));

            return ret;
        }

        internal static Backend BackendFromDataRow(DataRow row)
        {
            if (row == null) return null;

            string identifier = GetDataRowStringValue(row, "identifier");
            bool hasEmbeddingsCol = HasColumn(row.Table, "allowembeddings");
            bool hasCompletionsCol = HasColumn(row.Table, "allowcompletions");
            int embeddingsIntValue = hasEmbeddingsCol ? GetDataRowIntValue(row, "allowembeddings") : -1;
            int completionsIntValue = hasCompletionsCol ? GetDataRowIntValue(row, "allowcompletions") : -1;
            bool allowEmbeddings = hasEmbeddingsCol ? embeddingsIntValue == 1 : false;
            bool allowCompletions = hasCompletionsCol ? completionsIntValue == 1 : false;

            return new Backend
            {
                Identifier = identifier,
                Name = GetDataRowStringValue(row, "name"),
                Hostname = GetDataRowStringValue(row, "hostname"),
                Port = GetDataRowIntValue(row, "port"),
                Ssl = GetDataRowIntValue(row, "ssl") > 0,
                UnhealthyThreshold = GetDataRowIntValue(row, "unhealthythreshold"),
                HealthyThreshold = GetDataRowIntValue(row, "healthythreshold"),
                HealthCheckMethod = GetDataRowStringValue(row, "healthcheckmethod"),
                HealthCheckUrl = GetDataRowStringValue(row, "healthcheckurl"),
                MaxParallelRequests = GetDataRowIntValue(row, "maxparallelrequests"),
                RateLimitRequestsThreshold = GetDataRowIntValue(row, "ratelimitthreshold"),
                LogRequestFull = GetDataRowIntValue(row, "logrequestfull") == 1,
                LogRequestBody = GetDataRowIntValue(row, "logrequestbody") == 1,
                LogResponseBody = GetDataRowIntValue(row, "logresponsebody") == 1,
                ApiFormat = Enum.TryParse<ApiFormatEnum>(GetDataRowStringValue(row, "apiformat"), out ApiFormatEnum apiFormat) ? apiFormat : ApiFormatEnum.Ollama,
                PinnedEmbeddingsPropertiesString = GetDataRowStringValue(row, "pinnedembeddingsprops"),
                PinnedCompletionsPropertiesString = GetDataRowStringValue(row, "pinnedcompletionsprops"),
                AllowEmbeddings = allowEmbeddings,
                AllowCompletions = allowCompletions,
                Active = GetDataRowIntValue(row, "active") == 1,
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static List<Backend> BackendsFromDataTable(DataTable table)
        {
            if (table == null || table.Rows == null || table.Rows.Count < 1) return null;

            List<Backend> ret = new List<Backend>();

            foreach (DataRow row in table.Rows)
                ret.Add(BackendFromDataRow(row));

            return ret;
        }
    }
}
