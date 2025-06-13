using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    public static class Sync
    {
        public static string CreateSync(Dictionary<string, string> d, AngelDB.DB db)
        {
            try
            {
                if (db.accountType == ACCOUNT_TYPE.DATABASE_USER)
                {
                    return $"Error: You do not have permissions to create Sync";
                }

                SqliteTools sqlite = new SqliteTools(db.sqliteConnectionString);

                if (db.account == "") return "Error: No account selected";
                if (db.database == "") return "Error: No database selected";
                if (db.IsReadOnly == true) return "Error: Your account is read only";

                DataTable t = sqlite.SQLTable($"SELECT * FROM table_sync WHERE name = '{d["create_sync"]}'");

                sqlite.Reset();

                if (t.Rows.Count == 0)
                {
                    sqlite.CreateInsert("table_sync");
                }
                else
                {
                    sqlite.CreateUpdate("table_sync", $"name = '{d["create_sync"]}'");
                }

                sqlite.AddField("name", d["create_sync"]);
                sqlite.AddField("from_table", d["from_table"]);
                sqlite.AddField("from_partitions", d["from_partitions"]);
                sqlite.AddField("from_connection", d["from_connection"]);
                sqlite.AddField("from_account", d["from_account"]);
                sqlite.AddField("from_database", d["from_database"]);
                sqlite.AddField("to_connection", d["to_connection"]);
                sqlite.AddField("to_account", d["to_account"]);
                sqlite.AddField("to_database", d["to_database"]);
                sqlite.AddField("to_table", d["to_table"]);
                return sqlite.Exec();

            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }


        public static string GetSyncs(Dictionary<string, string> d, AngelDB.DB db)
        {
            try
            {
                SqliteTools sqlite = new SqliteTools(db.sqliteConnectionString);

                if (d["where"] == "null")
                {
                    d["where"] = "1 = 1";
                }

                DataTable t = sqlite.SQLTable($"SELECT * FROM table_sync WHERE {d["where"]}");
                return JsonConvert.SerializeObject(t, Formatting.Indented);

            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }

        }


        public static string DeleteSync(Dictionary<string, string> d, AngelDB.DB db)
        {
            try
            {
                SqliteTools sqlite = new SqliteTools(db.sqliteConnectionString);

                DataTable t = sqlite.SQLTable($"SELECT * FROM table_sync WHERE name = {d["delete_sync"]}");

                if (t.Rows.Count == 0)
                {
                    return "Error: Synchronization does not exist";
                }

                return sqlite.SQLExec($"DELETE FROM table_sync WHERE name = '{d["delete_sync"]}'");
            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }


        public static string SyncDatabase(Dictionary<string, string> d, AngelDB.DB db)
        {

            try
            {

                SqliteTools sqlite = new SqliteTools($"Data Source={db.BaseDirectory + db.os_directory_separator + "config.db"}");
                DataTable t = sqlite.SQLTable($"SELECT * FROM table_sync");

                if (d["rows_per_cycle"] == "null")
                {
                    d["rows_per_cycle"] = "1000";
                }

                if (d["partitions_to_process"] == "null")
                {
                    d["partitions_to_process"] = "1000";
                }

                string show_log = "";

                if (d["show_log"] == "true")
                {
                    show_log = "SHOW LOG";
                }

                string result = "";

                StringBuilder sb = new StringBuilder();

                foreach (DataRow row in t.Rows)
                {
                    if (row["name"].ToString().StartsWith("system_"))
                    {

                        if (show_log == "SHOW LOG")
                        {
                            Console.WriteLine("SYNC :" + row["name"]);
                        }

                        result = db.Prompt($"SYNC NOW {row["name"]} PARTITIONS TO PROCESS {d["partitions_to_process"]} ROWS {d["rows_per_cycle"]} {show_log} LOG FILE {d["log_file"]}");
                        if (result.StartsWith("Error:"))
                        {
                            sb.AppendLine(result);

                            if (d["log_file"] != "null")
                            {
                                System.IO.File.AppendAllText(d["log_file"], new String('=', 80) + "\n");
                                System.IO.File.AppendAllText(d["log_file"], DateTime.Now.ToString("yyyy-MM-dd HH:mm_ss") + " " + result + "\n");
                                System.IO.File.AppendAllText(d["log_file"], new String('=', 80) + "\n");
                            }

                            if (show_log == "SHOW LOG")
                            {
                                if (result.Contains("Error:"))
                                {
                                    Console.WriteLine(result);
                                }
                            }

                        }

                    }
                }

                result = sb.ToString();
                if (result.StartsWith("Error:")) return result;

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: Sync Database Global Errors: {e}";
            }


        }



        public static string CreateSyncDataBase(Dictionary<string, string> d, AngelDB.DB db)
        {
            try
            {
                DB source_db = new DB();
                string result = source_db.Prompt(d["source_connection"]);

                if (d["source_connection"].StartsWith("ANGEL"))
                {
                    source_db.Prompt("ALWAYS USE ANGELSQL");
                }

                if (result.StartsWith("Error:")) return result;
                if (string.IsNullOrEmpty(source_db.Prompt("ACCOUNT"))) return "Error: No account selected on source connection";
                if (string.IsNullOrEmpty(source_db.Prompt("DATABASE"))) return "Error: No database selected on source connection";

                DB target_db = new DB();
                result = target_db.Prompt(d["target_connection"]);

                if (d["target_connection"].StartsWith("ANGEL"))
                {
                    target_db.Prompt("ALWAYS USE ANGELSQL");
                }

                if (result.StartsWith("Error:")) return result;
                if (string.IsNullOrEmpty(target_db.Prompt("ACCOUNT"))) return "Error: No account selected on target connection";
                if (string.IsNullOrEmpty(target_db.Prompt("DATABASE"))) return "Error: No database selected on target connection";

                if (source_db.Prompt("ACCOUNT").Trim().ToLower() == target_db.Prompt("ACCOUNT").Trim().ToLower()
                    && source_db.Prompt("DATABASE").Trim().ToLower() == target_db.Prompt("DATABASE").Trim().ToLower()
                    && source_db.BaseDirectory.Trim().ToLower() == target_db.BaseDirectory.Trim().ToLower()
                    && source_db.angel_url.Trim().ToLower() == target_db.angel_url.Trim().ToLower())
                {
                    return "Error: Source and target connections are the same";
                }

                if (target_db.IsReadOnly == true) return "Error: Target connection is read only";

                SqliteTools sqlite = new SqliteTools(db.sqliteConnectionString);
                result = sqlite.SQLExec($"CREATE TABLE IF NOT EXISTS database_sync (name PRIMARY KEY, source_connection, source_account, source_database, target_connection, target_account, target_database)");
                if (result.StartsWith("Error:")) return result;

                DataTable t = sqlite.SQLTable($"SELECT * FROM database_sync WHERE name = '{d["create_sync_database"]}'");

                if (t.Rows.Count == 0)
                {
                    sqlite.CreateInsert("database_sync");
                }
                else
                {
                    sqlite.CreateUpdate("database_sync", $"name = '{d["create_sync_database"]}'");
                }

                sqlite.AddField("name", d["create_sync_database"]);
                sqlite.AddField("source_connection", d["source_connection"]);
                sqlite.AddField("source_account", source_db.Prompt("ACCOUNT"));
                sqlite.AddField("source_database", source_db.Prompt("DATABASE"));
                sqlite.AddField("target_connection", d["target_connection"]);
                sqlite.AddField("target_account", target_db.Prompt("ACCOUNT"));
                sqlite.AddField("target_database", target_db.Prompt("DATABASE"));
                result = sqlite.Exec();
                if (result.StartsWith("Error:")) return result;

                result = source_db.Prompt("GET TABLES");
                if (result.StartsWith("Error:")) return result;

                DataTable source_tables = JsonConvert.DeserializeObject<DataTable>(result);

                foreach (DataRow row in source_tables.Rows)
                {
                    result = db.Prompt($"CREATE SYNC system_{row["tablename"]} FROM TABLE {row["tablename"]} FROM CONNECTION {d["source_connection"]} FROM ACCOUNT {source_db.Prompt("ACCOUNT")} FROM DATABASE {source_db.Prompt("DATABASE")} TO CONNECTION {d["target_connection"]} TO ACCOUNT {target_db.Prompt("ACCOUNT")} TO DATABASE {target_db.Prompt("DATABASE")}");
                    if (result.StartsWith("Error:")) return result;
                }

                return "Ok.";


            }
            catch (Exception e)
            {

                return $"Error: {e}";
            }


        }

        public static void SyncAndRemovePartitions(DataTable partitions, DataTable partitionsTo)
        {
            // Crear un diccionario para acceso rápido a sync_timestamp por partition
            var toDict = partitionsTo.AsEnumerable()
                .Where(row => row["sync_timestamp"] != DBNull.Value)
                .ToDictionary(
                    row => row["partition"].ToString(),
                    row => Convert.ToDateTime(row["sync_timestamp"])
                );

            // Buscar filas en 'partitions' que deben eliminarse
            var rowsToDelete = partitions.AsEnumerable()
                .Where(row =>
                {
                    var partitionKey = row["partition"].ToString();
                    if (!toDict.ContainsKey(partitionKey) || row["timestamp"] == DBNull.Value)
                        return false;

                    var timestamp = Convert.ToDateTime(row["timestamp"]);
                    return toDict[partitionKey] >= timestamp;
                })
                .ToList(); // Materializar antes de modificar la colección original

            // Eliminar las filas marcadas
            foreach (var row in rowsToDelete)
            {
                partitions.Rows.Remove(row);
            }
        }


        public static string SyncNow(Dictionary<string, string> d, AngelDB.DB db)
        {

            int n = 0;

            try
            {

                if (!db.IsLogged)
                {
                    return $"Error: You have not indicated your username and password";
                }

                if (db.account == "") return "Error: No account selected";
                if (db.database == "") return "Error: No database selected";
                if (db.IsReadOnly == true) return "Error: Your account is read only";

                SqliteTools sqlite = new SqliteTools(db.sqliteConnectionString);
                DataTable t = sqlite.SQLTable($"SELECT * FROM table_sync WHERE name = '{d["sync_now"]}'");

                if (t.Rows.Count == 0)
                {
                    return "Error: Synchronization does not exist";
                }

                DataRow r = t.Rows[0];

                string result = "";
                DB from_db = new DB();
                result = from_db.Prompt(r["from_connection"].ToString(), true);
                if (result.StartsWith("Error:")) return result;

                if (r["from_connection"].ToString().StartsWith("ANGEL"))
                {
                    from_db.Prompt("ALWAYS USE ANGELSQL");
                }

                if (r["from_account"].ToString() != "null") result = from_db.Prompt("USE ACCOUNT " + r["from_account"].ToString());
                if (result.StartsWith("Error:")) return result;

                if (r["from_database"].ToString() != "null") result = from_db.Prompt("USE DATABASE " + r["from_database"].ToString());
                if (result.StartsWith("Error:")) return result;

                DB to_db = new DB();
                //to_db.speed_up = true;
                result = to_db.Prompt(r["to_connection"].ToString(), true);
                if (result.StartsWith("Error:")) return result;

                if (r["to_connection"].ToString().StartsWith("ANGEL"))
                {
                    to_db.Prompt("ALWAYS USE ANGELSQL");
                }

                if (r["to_account"].ToString() != "null") result = to_db.Prompt("USE ACCOUNT " + r["to_account"].ToString());
                if (result.StartsWith("Error:")) return result;

                if (r["to_database"].ToString() != "null") result = to_db.Prompt("USE DATABASE " + r["to_database"].ToString());
                if (result.StartsWith("Error:")) return result;

                result = from_db.Prompt($"GET TABLES WHERE tablename = '{r["from_table"]}'");
                if (result.StartsWith("Error:")) return result;

                if (result == "[]")
                {
                    return $"Error: Source table does not exist {r["from_table"]}";
                }

                DataTable table_source = JsonConvert.DeserializeObject<DataTable>(result);

                string field_list = table_source.Rows[0]["fieldlist"].ToString();

                field_list = field_list.Replace(", sync_timestamp INDEXED", "");
                field_list = field_list.Replace(", sync_timestamp", "");

                if (!field_list.Contains("sync_timestamp"))
                {
                    field_list = field_list + ", sync_timestamp INDEXED";
                }

                string table = "";

                if (string.IsNullOrEmpty(r["to_table"].ToString()))
                {
                    table = r["from_table"].ToString();
                }

                if (r["to_table"].ToString() == "null")
                {
                    table = r["from_table"].ToString();
                }

                result = to_db.Prompt($"GET TABLES WHERE tablename = '{table}'");

                if (result == "[]") 
                {
                    if (table_source.Rows[0]["tabletype"].ToString() == "search" || d["change_to_search"] == "true")
                    {
                        result = to_db.Prompt($"CREATE TABLE {table} FIELD LIST {field_list} TYPE SEARCH");
                        if (result.StartsWith("Error:")) return result;
                    }
                    else
                    {
                        result = to_db.Prompt($"CREATE TABLE {table} FIELD LIST {field_list}");
                        if (result.StartsWith("Error:")) return result;
                    }
                }

                if (d["show_log"] == "true")
                {
                    Console.WriteLine("Processing table: " + r["from_table"].ToString());
                }

                string result_to = "";

                if (d["partitions_condition"].ToString() == "null")
                {
                    result = from_db.Prompt($"GET PARTITIONS FROM TABLE {r["from_table"]}");
                    if (result.StartsWith("Error:")) return result + " --> From " + r["from_table"];

                    result_to = to_db.Prompt($"GET PARTITIONS FROM TABLE {table}");
                    if (result_to.StartsWith("Error:")) return result_to + " --> To " + table;

                }
                else
                {
                    result = from_db.Prompt($"GET PARTITIONS FROM TABLE {r["from_table"]} WHERE {d["partitions_condition"]}");
                    if (result.StartsWith("Error:")) return result;

                    result_to = to_db.Prompt($"GET PARTITIONS FROM TABLE {table} WHERE {d["partitions_condition"]}");
                    if (result_to.StartsWith("Error:")) return result_to;
                }

                DataTable partitions = JsonConvert.DeserializeObject<DataTable>(result);
                DataTable partitions_to = JsonConvert.DeserializeObject<DataTable>(result_to);

                SyncAndRemovePartitions(partitions, partitions_to);

                if (d["rows"] == "null")
                {
                    d["rows"] = "1000";
                }

                if (d["partitions_to_process"] == "null")
                {
                    d["partitions_to_process"] = "1000";
                }

                StringBuilder sb = new StringBuilder();

                foreach (DataRow partition in partitions.Rows)
                {

                    if (d["show_log"] == "true")
                    {
                        Console.WriteLine("Processing table: " + r["from_table"].ToString() + " " + partition["partition"]);
                    }

                    while (true) 
                    {
                        result = from_db.Prompt($"SELECT timestamp AS max_time FROM {table} PARTITION KEY {partition["partition"]} ORDER BY timestamp DESC LIMIT 1");

                        if (result.StartsWith("Error:")) return result;

                        string remote_timestamp = "1900-01-01 00:00:00.0000000";

                        if (result == "[]")
                        {
                            remote_timestamp = "1900-01-01 00:00:00.0000000";
                        }
                        else
                        {
                            DataTable data_remote_timestamp = JsonConvert.DeserializeObject<DataTable>(result);
                            remote_timestamp = data_remote_timestamp.Rows[0]["max_time"].ToString();
                        }

                        DataTable from_table = JsonConvert.DeserializeObject<DataTable>(result);

                        result = to_db.Prompt($"SELECT sync_timestamp AS max_time FROM {table} PARTITION KEY {partition["partition"]} ORDER BY sync_timestamp DESC LIMIT 1", true);
                        if (result.StartsWith("Error:")) return result;

                        string max_sycn_timestamp = "";

                        if (result == "[]")
                        {
                            max_sycn_timestamp = "1900-01-01 00:00:00.0000000";
                        }
                        else
                        {
                            DataTable data_max_sync_timestamp = JsonConvert.DeserializeObject<DataTable>(result);
                            max_sycn_timestamp = data_max_sync_timestamp.Rows[0]["max_time"].ToString();
                        }

                        DateTime dt_local = DateTime.ParseExact(max_sycn_timestamp, "yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
                        DateTime dt_remote = DateTime.ParseExact(remote_timestamp, "yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);

                        //if (d["show_log"] == "true")
                        //{
                        //    Console.WriteLine("Table: " + r["from_table"].ToString() + " partition " + partition["partition"]);
                        //}

                        if (dt_local >= dt_remote)
                        {
                            if (d["show_log"] == "true")
                            {
                                Console.WriteLine("No new data, Table: " + r["from_table"].ToString() + " partition " + partition["partition"]);
                            }

                            Tables.UpdateSyncTimeStampPartition(r["from_table"].ToString(), partition["partition"].ToString(), partition["timestamp"].ToString(), to_db);
                            break;
                        }

                        ++n;

                        if (d["show_log"] == "true")
                        {
                            Console.WriteLine($"--------------------------------------");
                            Console.WriteLine($"Start {n} {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Procesing: Table: {r["from_table"]} Partition: {partition["partition"]}");
                        }

                        string condition = "";
                        if (d["where"] != "null") condition = " AND " + d["where"].ToString();

                        result = from_db.Prompt($"SELECT * FROM {r["from_table"]} PARTITION KEY {partition["partition"]} WHERE {condition} timestamp > '{max_sycn_timestamp}' ORDER BY timestamp LIMIT {d["rows"]}");

                        if (result == "[]")
                        {
                            //Console.WriteLine("--->> No more Data from table: " + r["from_table"] + $" Partition {partition["partition"]} " + max_sycn_timestamp);
                            //Console.WriteLine($"--------------------------------------");

                            Tables.UpdateSyncTimeStampPartition(r["from_table"].ToString(), partition["partition"].ToString(), partition["timestamp"].ToString(), to_db);
                            break;
                        }

                        if (d["show_log"] == "true")
                        {
                            Console.WriteLine("--->>MaxTime: " + r["from_table"] + " " + max_sycn_timestamp);
                        }

                        if (result.StartsWith("Error:"))
                        {
                            if (d["show_log"] == "true")
                            {
                                Console.WriteLine(result);
                            }

                            sb.AppendLine(result);
                            break;
                        }

                        DataTable data1 = JsonConvert.DeserializeObject<DataTable>(result);

                        if (d["show_log"] == "true")
                        {
                            Console.WriteLine("--->>Rows: " + data1.Rows.Count.ToString());
                        }

                        data1.Columns.Add("sync_timestamp", typeof(string));

                        foreach (DataRow row in data1.Rows)
                        {
                            row["sync_timestamp"] = row["timestamp"].ToString();
                        }

                        result = to_db.Prompt($"UPSERT INTO {table} PARTITION KEY {partition["partition"]} EXCLUDE COLUMNS row_id VALUES '''" + db.GetJson(data1) + "'''");

                        if (result.StartsWith("Error:"))
                        {
                            Console.WriteLine(result);
                            System.IO.File.AppendAllText(d["log_file"], new String('=', 80) + "\n");
                            System.IO.File.AppendAllText(d["log_file"], DateTime.Now.ToString("yyyy-MM-dd HH:mm_ss") + " " + result + "\n");
                            System.IO.File.AppendAllText(d["log_file"], new String('=', 80) + "\n");
                            break;
                        }

                        if (d["show_log"] == "true")
                        {
                            Console.WriteLine($"End {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Procesing: Table: {r["from_table"]} Partition: {partition["partition"]}");
                            Console.WriteLine($"--------------------------------------");
                        }

                    }

                    if (n == int.Parse(d["partitions_to_process"]))
                    {
                        break;
                    }

                }

                if (r["from_connection"].ToString().StartsWith("ANGEL"))
                {
                    from_db.Prompt("ANGEL STOP");
                }

                if (r["to_connection"].ToString().StartsWith("ANGEL"))
                {
                    to_db.Prompt("ANGEL STOP");
                }

                result = sb.ToString();

                if (result.StartsWith("Error:")) return result;

                from_db = null;
                to_db = null;

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }



    }




}
