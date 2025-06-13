using System;
using System.Data;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace AngelDB
{

    /// <summary>
    ///   <br />
    /// </summary>
    public class SqliteTools
    {

        public string ConnectionString = "";
        public Dictionary<string, object> fieldsList;
        public string activetable = "";
        public string operationType = "";
        public string condition = "";
        public string SQL = "";
        public string ExtensionsPath;

        public SqliteTools(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
            fieldsList = new Dictionary<string, object>();
            operationType = "INSERT";

            string s = "/";
            ExtensionsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
             + s + "runtimes" + s + OSTools.OSName() + s + "native" + s + "SQLite.Interop.dll";

        }

        public string SQLExec(string SQL, string table_type = "normal")
        {
            try
            {
                var m_connection = new SQLiteConnection
                {
                    ConnectionString = this.ConnectionString
                };

                m_connection.Open();

                if (table_type != "normal")
                {
                    m_connection.EnableExtensions(true);
                    m_connection.LoadExtension(ExtensionsPath, "sqlite3_fts5_init");
                }

                var m_command = m_connection.CreateCommand();
                m_command.CommandTimeout = 30;
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

        public string SQLSearchExec(string SQL)
        {
            try
            {
                var m_connection = new SQLiteConnection
                {
                    ConnectionString = this.ConnectionString
                };

                m_connection.Open();

                m_connection.EnableExtensions(true);
                m_connection.LoadExtension(ExtensionsPath, "sqlite3_fts5_init");

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

        public bool ColumnExists(string tableName, string columnName)
        {
            string SQL = $"PRAGMA table_info({tableName.ToLower()})";
            DataTable dt = SQLTable(SQL);
            foreach (DataRow row in dt.Rows)
            {
                if (row["name"].ToString().Trim().ToLower() == columnName.Trim().ToLower())
                {
                    return true;
                }
            }
            return false;
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

            using var da = new SQLiteDataAdapter(SQL, this.ConnectionString);
            var ds = new DataSet();
            da.SelectCommand.CommandTimeout = 120000;
            da.Fill(ds);
            da.SelectCommand.Connection.Close();
            da.SelectCommand.Dispose();
            return ds.Tables[0];
        }


        public DataTable SQLSearchTable(string SQL)
        {

            using var da = new SQLiteDataAdapter(SQL, this.ConnectionString);
            var ds = new DataSet();
            da.SelectCommand.Connection.Open();
            da.SelectCommand.CommandTimeout = 120000;
            da.SelectCommand.Connection.EnableExtensions(true);
            da.SelectCommand.Connection.LoadExtension(ExtensionsPath, "sqlite3_fts5_init");
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

        public string ExecSearch()
        {
            string result = CreateQuery();
            if (result.StartsWith("Error")) return result;
            return SQLExecSearchWithParameters(this.SQL);
        }



        public string SQLExecWithParameters(string SQL)
        {
            try
            {

                if (this.fieldsList.Count == 0) return "Error: No fields have been indicated to perform the operation";

                var m_connection = new SQLiteConnection
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

                m_command.CommandTimeout = 30;
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

                var m_connection = new SQLiteConnection
                {
                    ConnectionString = this.ConnectionString
                };
                m_connection.Open();
                var m_command = m_connection.CreateCommand();
                m_command.Connection.EnableExtensions(true);
                m_command.Connection.LoadExtension(ExtensionsPath, "sqlite3_fts5_init");
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


    }

}
