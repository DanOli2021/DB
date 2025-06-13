using System.Data;
using System.Data.SQLite;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using System.Threading;
using System.Data.SqlClient;

namespace AngelDB
{
    public class QueryTools
    {

        string ConnectionString;
        public SQLiteConnection SQLConnection = null;
        public SQLiteCommand SQLCommand = null;
        public List<string> ColumnList = new List<string>();
        public string InsertQuery = "";
        public string UpdateQuery = "";
        public string UpsertQuery = "";

        public QueryTools(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
        }

        public void AddValue(string column, object value)
        {
            this.ColumnList.Add(column);
            SQLCommand.Parameters.AddWithValue(column, value);
        }

        public string GenInsertQuery(string table)
        {

            if (ColumnList.Count == 0) return "Error: No fields have been indicated to perform the operation";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine($"INSERT INTO {table} (");
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
            this.InsertQuery = sb.ToString();

            return this.InsertQuery;

        }

        public string GenUpsertQuery(string table, string condition, bool removeTimeStamp = false)
        {

            if (ColumnList.Count == 0) return "Error: No fields have been indicated to perform the operation";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine($"INSERT INTO {table} (");
            int n = 0;

            if (removeTimeStamp)
            {
                ColumnList.Remove("timestamp");
            }

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
            this.UpsertQuery = sb.ToString() + " ON CONFLICT(partitionkey, id) DO " + GenUpdateQuery("", condition);

            return this.UpsertQuery;

        }


        public string GenUpdateQuery(string table, string condition)
        {

            if (System.String.IsNullOrEmpty(condition)) return "Error: No condition was indicated in the UPDATE context";
            if (ColumnList.Count == 0) return "Error: No fields have been indicated to perform the operation";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine($"UPDATE {table} SET ");
            int n = 0;

            foreach (string item in ColumnList)
            {
                ++n;
                string separator = ",";
                if (n == ColumnList.Count) separator = "";

                sb.AppendLine(item + @" = @" + item + separator);
            }

            sb.AppendLine("WHERE " + condition);

            this.UpdateQuery = sb.ToString();
            return this.UpdateQuery;

        }

        public string OpenConnection()
        {
            try
            {
                string s = "/";
                string interop_file = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                 + s + "runtimes" + s + OSTools.OSName() + s + "native" + s + "SQLite.Interop.dll";
                this.SQLConnection = new SQLiteConnection(this.ConnectionString);
                this.SQLConnection.Open();
                this.SQLConnection.EnableExtensions(true);
                this.SQLConnection.LoadExtension(interop_file, "sqlite3_fts5_init");
                this.SQLCommand = this.SQLConnection.CreateCommand();

            }
            catch (System.Exception e)
            {
                return $"Error: On Open Connection: {this.ConnectionString}->{e}";
            }

            return "Ok.";

        }


        public bool IsOpen() 
        {
            if (this.SQLConnection is null) 
            {
                return false;
            }
            return this.SQLConnection.State == ConnectionState.Open;
        }

        public string CloseConnection()
        {
            try
            {
                this.SQLCommand.Dispose();
                this.SQLCommand = null;
                this.SQLConnection.Close();
                this.SQLConnection.Dispose();
                this.SQLConnection = null;
            }
            catch (System.Exception e)
            {
                return $"Error: {e}";
            }

            return "Ok.";
        }

        public string Exec(string operation)
        {
            try
            {
                if (operation.Trim().ToLower() == "update") this.SQLCommand.CommandText = this.UpdateQuery;
                if (operation.Trim().ToLower() == "insert") this.SQLCommand.CommandText = this.InsertQuery;
                if (operation.Trim().ToLower() == "upsert") this.SQLCommand.CommandText = this.UpsertQuery;
                this.SQLCommand.CommandTimeout = 30;
                this.SQLCommand.ExecuteNonQuery();
                return "Ok.";
            }
            catch (System.Exception e)
            {
                return $"Error: {e}";
            }
        }

        private readonly object _lock = new object();

        public string ExecSQLDirect(string sql)
        {
            lock (_lock)

            try
            {
                this.SQLCommand.CommandText = sql;
                this.SQLCommand.ExecuteNonQuery();
                return "Ok.";
            }
            catch (System.Exception e)
            {
                return $"Error: {sql} {e}";
            }
        }

        public string ExecConcurrentSQLDirect(string sql)
        {
            using (var connection = new SQLiteConnection(this.ConnectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(sql, connection))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                        return "Ok.";
                    }
                    catch (System.Exception e)
                    {
                        return $"Error: {sql} {e}";
                    }
                }
            }
        }



        public DataTable SQLTable(string SQL)        
        {
            this.SQLCommand.CommandText = SQL;
            this.SQLCommand.CommandTimeout = 60;
            SQLiteDataReader reader = this.SQLCommand.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(reader);
            reader.Close();
            return dt;
        }


    }
}

