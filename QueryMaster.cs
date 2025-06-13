using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

namespace AngelDB
{

    public class QueryMaster
    {

        public MemoryDb mdb = new();
        Dictionary<string, ConnectionsData> sqlConnections = null;
        DbLanguage l = new();

        public delegate void OnReceived(string source, string message);
        public event OnReceived OnMessage;

        public delegate void OnWorking(bool Working);
        public event OnWorking OnAppWorking;


        public void RaiseOnReceived(string source, string message)
        {
            OnMessage?.Invoke(source, message);
        }

        public void RaiseOnWorking(bool working)
        {
            OnAppWorking?.Invoke(working);
        }

        public string GetConnections(string jSonConnections) {

            try
            {
                this.sqlConnections = JsonConvert.DeserializeObject<Dictionary<string, ConnectionsData>>(jSonConnections);
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: GetConnections {e.Message}";
            }            

        }

        public string SuperQuery(string tablename, string query) {

            StringBuilder sb = new();

            if (sqlConnections == null) {
                return "Error: First get Connections with GET CONNECTIONS commnd";
            }


            //foreach (var item in sqlConnections.Keys)
            //{
            //    try
            //    {
            //        //Dictionary<string, string> d = l.Interpreter(query);
            //        DataTable t = SQLTable(query, sqlConnections[item].Connection);
            //        sqlConnections[item].Table = t;
            //        RaiseOnReceived("CONNECTION " + item, $"Ok. Task complete table {sqlConnections[item].Name} {sqlConnections[item].Description}");

            //    }
            //    catch (Exception e)
            //    {
            //        RaiseOnReceived("CONNECTION " + item, $"Error: {item} {e.Message}");
            //        sb.AppendLine($"Error: {item} {e.Message}");
            //    }

            //}


            RaiseOnWorking(true);

            Parallel.ForEach(sqlConnections.Keys, item =>
            {
                try
                {
                    DataTable t = SQLTable(query, sqlConnections[item].Connection);

                    if (t is not null) 
                    {
                        sqlConnections[item].Table = t;
                        RaiseOnReceived("CONNECTION " + item, $"Ok. Task complete table {sqlConnections[item].Name} {sqlConnections[item].Description}");
                    }

                }
                catch (Exception e)
                {
                    RaiseOnReceived("CONNECTION " + item, $"Error: {item} {e.Message}");
                    sb.AppendLine($"Error: {item} {e.Message}");
                }
            });

            RaiseOnReceived("CONNECTION", "Process finished");

            RaiseOnWorking(false);
            string result = "";
            //string result = sb.ToString();

            //if (!string.IsNullOrEmpty(result)) {
            //    return result;            
            //}

            this.mdb.SQLExec($"DROP TABLE {tablename}");

            foreach (string key in sqlConnections.Keys)
            {
                result = InsertIntoMemoryTableFromDataTable(tablename, sqlConnections[key]);
            }

            if (result != "Ok.") {
                RaiseOnReceived("TABLE " + tablename, $"Error: Insert on memory {result}");
                sb.AppendLine($"Error: {result}");
            }

            result = sb.ToString();

            if (string.IsNullOrEmpty(result)) {
                return result;
            }

            return "Ok";

        }


        public string CreateTableFromDataTable(string tablename, DataTable t, DB db) {

            string fields = "";

            foreach (var column in t.Columns)
            {
                fields += column + ",";
            }

            fields = fields[0..^1];

            string result;
            result = db.Prompt($"CREATE TABLE {tablename} FIELD LIST {fields} STORAGE main");
            return result;

        }

        public string CreateMemoryTableFromDataTable(string tablename, ConnectionsData cd)
        {

            string fields = "connection_name, connection_description, connection_timestamp,";

            if (cd.Table == null) {
                return $"Error: no data in connection: {cd.Name}"; 
            }

            foreach (var column in cd.Table.Columns)
            {
                fields += column + ",";
            }

            fields = fields[0..^1];

            string result;
            result = mdb.SQLExec($"CREATE TABLE IF NOT EXISTS {tablename} ({fields})");

            if (result != "Ok.") {
                return result;
            }

            result = mdb.SQLExec($"CREATE INDEX IF NOT EXISTS {tablename.Trim() + "_connection_name"} ON {tablename} (connection_name)"); ;

            if (result != "Ok.")
            {
                return $"Error: Creating index {tablename.Trim() + "_connection_name"} " + result;
            }

            result = mdb.SQLExec($"CREATE INDEX IF NOT EXISTS {tablename.Trim() + "_connection_description"} ON {tablename} (connection_description)"); ;

            if (result != "Ok.")
            {
                return $"Error: Creating index {tablename.Trim() + "_connection_description"} " + result;
            }

            result = mdb.SQLExec($"CREATE INDEX IF NOT EXISTS {tablename.Trim() + "_connection_timestamp"} ON {tablename} (connection_timestamp)"); ;

            if (result != "Ok.")
            {
                return $"Error: Creating index {tablename.Trim() + "_connection_timestamp"} " + result;
            }

            foreach (var column in cd.Table.Columns)
            {                
                result = mdb.SQLExec($"CREATE INDEX IF NOT EXISTS {tablename.Trim() + "_" + column.ToString()} ON {tablename} ({column})"); ;

                if (result != "Ok.")
                {
                    return $"Error: Creating index {tablename.Trim() + "_" + column.ToString()} " + result;
                }

            }

            return result;

        }

        public string InsertIntoMemoryTableFromDataTable(string tablename, ConnectionsData cd) {

            string result = CreateMemoryTableFromDataTable(tablename, cd);

            if (result != "Ok.")
            {
                return result;
            }

            try
            {
                string fields = "connection_name, connection_description, connection_timestamp,";
                string values = "@connection_name, @connection_description, @connection_timestamp,";

                foreach (var column in cd.Table.Columns)
                {
                    fields += column + ",";
                    values += "@" + column + ",";
                }

                fields = fields[0..^1];
                values = values[0..^1];

                Parallel.ForEach<DataRow>(cd.Table.AsEnumerable(), item =>
                {
                    string query = $"INSERT INTO {tablename} ({fields}) VALUES ({values})";

                    var m_command = mdb.GetConnection().CreateCommand();
                    m_command.CommandText = query;

                    m_command.Parameters.AddWithValue("connection_name", cd.Name);
                    m_command.Parameters.AddWithValue("connection_description", cd.Description);
                    m_command.Parameters.AddWithValue("connection_timestamp", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));

                    foreach (DataColumn c in cd.Table.Columns)
                    {
                        m_command.Parameters.AddWithValue(c.ColumnName, item[c.ColumnName]);
                    }

                    m_command.ExecuteNonQuery();
                    m_command.Dispose();

                });


                //foreach (DataRow item in cd.Table.Rows)
                //{

                //    string query = $"INSERT INTO {tablename} ({fields}) VALUES ({values})";

                //    var m_command = mdb.GetConnection().CreateCommand();
                //    m_command.CommandText = query;

                //    m_command.Parameters.AddWithValue("connection_name", cd.Name);
                //    m_command.Parameters.AddWithValue("connection_description", cd.Description);
                //    m_command.Parameters.AddWithValue("connection_timestamp", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));

                //    foreach (DataColumn c in cd.Table.Columns)
                //    {
                //        m_command.Parameters.AddWithValue(c.ColumnName, item[c.ColumnName]);
                //    }

                //    m_command.ExecuteNonQuery();
                //    m_command.Dispose();

                //}

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: {e.Message}";
            }

        }


        public string InsertToTableFromDataTable(string tablename, string partitionkey, DataTable t, DB db)
        {
            return db.Prompt($"INSERT INTO {tablename} PARTITION KEY {partitionkey} VALUES {JsonConvert.SerializeObject(t)}");
        }


        public DataTable SQLTable(string sSQL, string sConnection)
        {
            using (var da = new SqlDataAdapter(sSQL, sConnection))
            {
                var ds = new DataSet();
                //da.SelectCommand.CommandTimeout = 30;
                da.Fill(ds);
                return ds.Tables[0];
            }
        }

    }


    public class ConnectionsData {

        public DataTable Table { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Connection { get; set; }

    }



}

