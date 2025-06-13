using System;
using System.Data;
using System.Data.SQLite;
using System.Collections.Generic;

namespace AngelDB {
    public class MemoryDb
    {

        public string ConnectionString = "";
        public Dictionary<string, object> fieldsList;
        public string activetable = "";
        public string operationType = "";
        public string condition = "";
        public string SQL = "";
        readonly SQLiteConnection m_connection;

        public MemoryDb()
        {
            this.ConnectionString = "Data Source=:memory:";
            fieldsList = new Dictionary<string, object>();
            operationType = "INSERT";

            this.m_connection = new SQLiteConnection
            {
                ConnectionString = this.ConnectionString
            };
            this.m_connection.Open();
        }

        public SQLiteConnection GetConnection()
        {
            return m_connection;
        }

        public void Close()
        {
            this.m_connection.Close();
            this.m_connection.Dispose();
        }


        public string SQLExec(string SQL)
        {
            try
            {
                var m_command = m_connection.CreateCommand();
                m_command.CommandText = SQL;
                m_command.ExecuteNonQuery();
                m_command.Dispose();
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

        public DataTable SQLTable(string SQL )
        {
            using var da = new SQLiteDataAdapter(SQL, this.m_connection);
            var ds = new DataSet();
            da.SelectCommand.CommandTimeout = 120000;
            da.Fill(ds);
            return ds.Tables[0];
        }

        public string Exec()
        {
            string result = CreateQuery();
            if (result.StartsWith("Error")) return result;
            return SQLExecWithParameters(this.SQL);
        }

        public string SQLExecWithParameters(string SQL)
        {
            try
            {

                if (this.fieldsList.Count == 0) return "Error: No fields have been indicated to perform the operation";

                var m_command = m_connection.CreateCommand();
                m_command.CommandText = SQL;

                foreach (string item in this.fieldsList.Keys)
                {
                    m_command.Parameters.AddWithValue(item, fieldsList[item]);
                }

                m_command.ExecuteNonQuery();
                m_command.Dispose();
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error:{e.Message}";
            }
        }

    }
}
