using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using System.Data.SQLite;

namespace AngelDB
{

    /// <summary>
    ///   <br />
    /// </summary>
    public class SQLServerTools
    {

        public string ConnectionString = "";
        public Dictionary<string, object> fieldsList;
        public string activetable = "";
        public string operationType = "";
        public string condition = "";
        public string SQL = "";
        public string ExtensionsPath;

        public SqlConnection Connection = null;
        public SqlCommand SQLCommand = null;
        public SqlTransaction transaction = null;

        public SQLServerTools(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
            fieldsList = new Dictionary<string, object>();
            operationType = "INSERT";

            string s = "/";
            ExtensionsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
             + s + "runtimes" + s + OSTools.OSName() + s + "native" + s + "SQLite.Interop.dll";

        }

        public string SQLExec(string SQL)
        {
            try
            {
                var m_connection = new SqlConnection
                {
                    ConnectionString = this.ConnectionString
                };

                m_connection.Open();

                var m_command = m_connection.CreateCommand();
                m_command.CommandText = SQL;
                m_command.ExecuteNonQuery();
                m_command.Dispose();
                m_connection.Close();
                m_connection.Dispose();
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error:{e.Message}";
            }
        }

        public string DirectExec(string SQL)
        {
            try
            {
                this.SQLCommand.CommandText = SQL;
                this.SQLCommand.ExecuteNonQuery();
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error:{e.Message}";
            }
        }



        public void CreateInsert(string tableName)
        {
            this.activetable = tableName;
            operationType = "INSERT";
            this.condition = "";
        }

        public void CreateUpdate(string tableName, string condition)
        {
            this.activetable = tableName;
            operationType = "UPDATE";
            this.condition = condition;
        }


        public void AddField(string fieldName, object value)
        {
            if (value == null)
            {
                value = DBNull.Value;
            }

            fieldsList.Add(fieldName, value);
        }

        public void Reset()
        {
            this.condition = "";
            this.activetable = "";
            this.SQL = "";
            this.fieldsList.Clear();
        }

        public void ClearFieldList()
        {
            this.fieldsList.Clear();
        }




        public string CreateQuery()
        {

            if (this.operationType == "UPDATE" && System.String.IsNullOrEmpty(this.condition)) return "Error: No condition was indicated in the UPDATE context";
            if (this.fieldsList.Count == 0) return "Error: No fields have been indicated to perform the operation";

            var sb = new System.Text.StringBuilder();

            if (this.operationType == "INSERT")
            {
                sb.AppendLine($"INSERT INTO {this.activetable} (");
                int n = 0;

                foreach (string item in this.fieldsList.Keys)
                {
                    ++n;
                    string separator = ",";
                    if (n == this.fieldsList.Count) separator = "";

                    sb.AppendLine(item + separator);
                }

                sb.AppendLine(") VALUES (");
                n = 0;

                foreach (string item in this.fieldsList.Keys)
                {
                    ++n;
                    string separator = ",";
                    if (n == this.fieldsList.Count) separator = "";

                    sb.AppendLine(@"@" + item + separator);
                }

                sb.AppendLine(")");

            }
            else
            {
                sb.AppendLine($"UPDATE {this.activetable} SET ");
                int n = 0;

                foreach (string item in this.fieldsList.Keys)
                {
                    ++n;
                    string separator = ",";
                    if (n == this.fieldsList.Count) separator = "";

                    sb.AppendLine(item + @" = @" + item + separator);
                }

                sb.AppendLine("WHERE " + this.condition);

            }

            this.SQL = sb.ToString();

            return this.SQL;

        }

        public DataTable SQLTable(string SQL, string tabletype = "normal")
        {

            if (tabletype == "search")
            {
                return SQLSearchTable(SQL);
            }

            using var da = new SqlDataAdapter(SQL, this.ConnectionString);
            var ds = new DataSet();
            da.SelectCommand.CommandTimeout = 120000;
            da.Fill(ds);
            da.SelectCommand.Connection.Close();
            da.SelectCommand.Dispose();
            return ds.Tables[0];
        }

        public DataTable SQLDataTable(string SQL, string tabletype = "normal")
        {
            SQLCommand.CommandText = SQL;

            using var da = new SqlDataAdapter(SQLCommand);
            var ds = new DataSet();
            da.SelectCommand.CommandTimeout = 120000;
            da.Fill(ds);
            return ds.Tables[0];
        }


        public DataTable SQLSearchTable(string SQL)
        {

            using var da = new SqlDataAdapter(SQL, this.ConnectionString);
            var ds = new DataSet();
            da.SelectCommand.Connection.Open();
            da.SelectCommand.CommandTimeout = 120000;
            da.Fill(ds);

            da.SelectCommand.Connection.Close();
            da.SelectCommand.Dispose();

            return ds.Tables[0];
        }


        public string Exec(string tabletype = "normal")
        {
            string result = CreateQuery();
            if (result.StartsWith("Error")) return result;

            if (tabletype != "normal")
            {
                return SQLExecSearchWithParameters(this.SQL);
            }
            else
            {
                return SQLExecWithParameters(this.SQL);
            }

        }
        public string SQLExecWithParameters(string SQL)
        {
            try
            {

                if (this.fieldsList.Count == 0) return "Error: No fields have been indicated to perform the operation";

                var m_connection = new SqlConnection
                {
                    ConnectionString = this.ConnectionString
                };
                m_connection.Open();
                var m_command = m_connection.CreateCommand();
                m_command.CommandText = SQL;

                foreach (string item in this.fieldsList.Keys)
                {
                    m_command.Parameters.AddWithValue(item, this.fieldsList[item]);
                }

                m_command.ExecuteNonQuery();
                m_command.Dispose();
                m_connection.Close();
                m_connection.Dispose();
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error:{e.Message}";
            }
        }


        public string SQLExecSearchWithParameters(string SQL)
        {
            try
            {

                if (this.fieldsList.Count == 0) return "Error: No fields have been indicated to perform the operation";

                var m_connection = new SqlConnection();
                m_connection.ConnectionString = this.ConnectionString;
                m_connection.Open();
                var m_command = m_connection.CreateCommand();
                m_command.CommandText = SQL;

                foreach (string item in this.fieldsList.Keys)
                {
                    m_command.Parameters.AddWithValue(item, this.fieldsList[item]);
                }

                m_command.ExecuteNonQuery();
                m_command.Dispose();
                m_connection.Close();
                m_connection.Dispose();
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error:{e.Message}";
            }
        }


        public string StartTransaction(string ConnectionString)
        {
            try
            {
                this.Connection = new SqlConnection(ConnectionString);
                if (this.Connection.State == ConnectionState.Closed) this.Connection.Open();
                this.transaction = this.Connection.BeginTransaction();
                this.SQLCommand = this.Connection.CreateCommand();

                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error:{e.Message}";
            }
        }

        public string EndTransaction()
        {
            try
            {
                if (this.Connection.State == ConnectionState.Closed) this.Connection.Open();
                this.transaction = this.Connection.BeginTransaction();
                this.SQLCommand = this.Connection.CreateCommand();

                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error:{e.Message}";
            }
        }

        public string RollBackTransaction()
        {
            try
            {
                if (!(this.Connection.State == ConnectionState.Open)) return "Ok.";
                if (this.transaction == null) return "Ok.";

                this.transaction.Rollback();
                this.Connection.Close();

                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error:{e.Message}";
            }
        }


        public string Insert(Dictionary<string, string> d)
        {

            DataTable json_values = null;

            try
            {
                if (d["values"].StartsWith("["))
                {
                    json_values = JsonConvert.DeserializeObject<DataTable>(d["values"]);
                }
                else
                {
                    json_values = JsonConvert.DeserializeObject<DataTable>("[" + d["values"] + "]");
                }

                if (json_values.Rows.Count == 0)
                {
                    return "Error: No values have been indicated to perform the operation";
                }

                List<string> ColumnList = new List<string>();

                foreach (DataColumn item in json_values.Columns)
                {
                    ColumnList.Add(item.ColumnName);
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                sb.AppendLine($"INSERT INTO {d["insert_into"]} (");
                int n = 0;

                foreach (string item in ColumnList)
                {
                    ++n;
                    string separator = ",";
                    if (n == ColumnList.Count) separator = "";

                    sb.AppendLine(item + separator);
                }

                sb.AppendLine(") VALUES (");
                n = 0;

                foreach (string item in ColumnList)
                {
                    ++n;
                    string separator = ",";
                    if (n == ColumnList.Count) separator = "";

                    sb.AppendLine(@"@" + item + separator);
                }

                sb.AppendLine(")");
                string InsertQuery = sb.ToString();


                foreach (DataRow item in json_values.Rows)
                {

                    SQLCommand.Parameters.Clear();

                    foreach (string p in ColumnList)
                    {
                        SQLCommand.Parameters.AddWithValue(p, item[p]);
                    }

                    SQLCommand.CommandText = InsertQuery;
                    SQLCommand.ExecuteNonQuery();

                }

                return "Ok.";

            }
            catch (System.Exception e1)
            {
                return $"Error: Insert {d["insert_into"]} Json string contains errors, {e1} jSon Values -->> {d["values"]}";
            }
        }

        public string Update(Dictionary<string, string> d)
        {

            DataTable json_values = null;

            try
            {
                if (d["values"].StartsWith("["))
                {
                    json_values = JsonConvert.DeserializeObject<DataTable>(d["values"]);
                }
                else
                {
                    json_values = JsonConvert.DeserializeObject<DataTable>("[" + d["values"] + "]");
                }

                if (json_values.Rows.Count == 0)
                {
                    return "Error: No values have been indicated to perform the operation";
                }

                List<string> ColumnList = new List<string>();

                foreach (DataColumn item in json_values.Columns)
                {
                    ColumnList.Add(item.ColumnName);
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                sb.AppendLine($"UPDATE {d["update"]} SET ");
                int n = 0;

                foreach (string item in ColumnList)
                {
                    ++n;
                    string separator = ",";
                    if (n == ColumnList.Count) separator = "";

                    sb.AppendLine(item + @" = @" + item + separator);
                }

                sb.AppendLine("WHERE " + d["where"]);

                string UpdateQuery = sb.ToString();
                SQLCommand.CommandText = UpdateQuery;

                foreach (DataRow item in json_values.Rows)
                {

                    SQLCommand.Parameters.Clear();

                    //DataTable tableId = null;

                    foreach (string p in ColumnList)
                    {
                        SQLCommand.Parameters.AddWithValue(p, item[p]);
                    }

                    //tableId = SQLDataTable($"SELECT * FROM {d["update"]} WHERE {d["where"]}");
                    SQLCommand.ExecuteNonQuery();

                }

                return "Ok.";

            }
            catch (System.Exception e1)
            {
                return $"Error: Insert {d["update"]} Json string contains errors, {e1} jSon Values -->> {d["values"]}";
            }
        }

    }

    public class DataCopier
    {
        public static void CopyTableRecords(string sourceConnectionString, string destinationConnectionString, string tableName, string query)
        {
            // Abrimos la conexión al servidor origen
            using (SqlConnection sourceConnection = new SqlConnection(sourceConnectionString))
            {
                sourceConnection.Open();
                // Seleccionamos todos los registros de la tabla
                using (SqlCommand sourceCommand = new SqlCommand(query, sourceConnection))
                using (SqlDataReader reader = sourceCommand.ExecuteReader())
                {
                    // Abrimos la conexión al servidor destino
                    using (SqlConnection destinationConnection = new SqlConnection(destinationConnectionString))
                    {
                        destinationConnection.Open();
                        // Configuramos el BulkCopy para escribir en la tabla destino
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(destinationConnection))
                        {
                            bulkCopy.DestinationTableName = tableName;
                            bulkCopy.BulkCopyTimeout = 0;
                            bulkCopy.ColumnMappings.Clear();
                            bulkCopy.BatchSize = 1000;
                            bulkCopy.NotifyAfter = 1000;

                            bulkCopy.SqlRowsCopied += (sender, e) =>
                            {
                                Console.WriteLine($"{DateTime.Now.ToString()} Se han copiado {e.RowsCopied} registros.");
                            };

                            try
                            {
                                bulkCopy.WriteToServer(reader);
                                Console.WriteLine("Datos copiados correctamente.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error: copiando datos: " + ex.Message);
                            }
                        }
                    }
                }
            }
        }
    }
}
