using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;
using System.Linq;
using AngelDBTools;

namespace AngelDB
{
    public static class Tables
    {

        public static string CreateTable(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
            {
                if (mainClass.UserTables != "null")
                {
                    return "Error: You do not have permissions to create tables in this account";
                }
            }

            d["create_table"] = d["create_table"].Trim().ToLower();

            string tableDirectory;

            if (d["storage"].Trim().ToLower() == "null")
            {
                d["storage"] = "main";
            }

            if (d["storage"].Trim().ToLower() == "main")
            {
                tableDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database + mainClass.os_directory_separator + d["create_table"];
            }
            else
            {
                tableDirectory = d["storage"];
            }

            if (!Directory.Exists(tableDirectory))
            {
                Directory.CreateDirectory(tableDirectory);
            }

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string dataBaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
            SqliteTools sqlite = new SqliteTools(dataBaseConnectionString);
            DataTable t = sqlite.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["create_table"]}'");

            sqlite.Reset();

            if (t.Rows.Count == 0)
            {
                sqlite.CreateInsert("tables");
            }
            else
            {
                sqlite.CreateUpdate("tables", $"tablename = '{d["create_table"]}'");
            }

            sqlite.AddField("tablename", d["create_table"]);
            sqlite.AddField("storage", d["storage"]);
            sqlite.AddField("deleted", "false");
            sqlite.AddField("fieldlist", d["field_list"]);

            if (d["type_search"] == "true")
            {
                sqlite.AddField("tabletype", "search");
            }
            else
            {
                sqlite.AddField("tabletype", "normal");
            }

            sqlite.AddField("timestamp", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            string result = sqlite.Exec();

            if (result != "Ok.")
            {
                return "Error: creating db file " + result;
            }

            string ConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["create_table"] + ".db"}" + ";version = 3";
            sqlite = new SqliteTools(ConnectionString);

            // Search for indexed files
            string[] arrayFields = d["field_list"].Split(",");
            List<string> fields = new List<string>();

            for (int i = 0; i < arrayFields.Length; i++)
            {
                fields.Add(arrayFields[i].Replace(" INDEXED", ""));
            }

            string field_list = string.Join(",", fields);

            result = sqlite.SQLExec($"CREATE TABLE IF NOT EXISTS {d["create_table"]} ( PartitionKey, id, timestamp, {field_list}, PRIMARY KEY ( PartitionKey, id ) )");

            if (result != "Ok.")
            {
                return $"Error: creating table {result} ({$"CREATE TABLE IF NOT EXISTS {d["create_table"]} ( PartitionKey, id, timestamp, {field_list}, PRIMARY KEY ( PartitionKey, id ) )"})";
            }

            result = sqlite.SQLExec($"CREATE INDEX IF NOT EXISTS {d["create_table"]}_timestamp ON {d["create_table"]} (timestamp)");

            if (result != "Ok.")
            {
                return "Error: creating index timestamp " + result;
            }

            result = sqlite.SQLExec("CREATE TABLE IF NOT EXISTS partitions (partition, tablename, timestamp, sync_timestamp, compressed, PRIMARY KEY ( partition, tablename ) )");

            if (result != "Ok.")
            {
                return $"Error: creating table partitions {result}";
            }

            sqlite.SQLExec($"ALTER TABLE partitions ADD COLUMN compressed");

            result = sqlite.SQLExec($"CREATE INDEX IF NOT EXISTS {d["create_table"]}_timestamp ON partitions (timestamp)");

            if (result != "Ok.")
            {
                return $"Error: creating index timestamp {d["create_table"]}_timestamp" + result;
            }

            result = sqlite.SQLExec($"CREATE INDEX IF NOT EXISTS {d["create_table"]}_sync_timestamp ON partitions (sync_timestamp)");

            if (result != "Ok.")
            {
                return $"Error: creating index timestamp {d["create_table"]}_sync_timestamp" + result;
            }

            foreach (var field in arrayFields)
            {

                bool column_exists = sqlite.ColumnExists(d["create_table"], AngelDBTools.StringFunctions.GetFirstElement(field.Trim(), ' '));
                //result = sqlite.SQLExec($"CREATE INDEX IF NOT EXISTS {d["create_table"]}_{AngelDBTools.StringFunctions.GetFirstElement(field.Trim(), ' ')} ON {d["create_table"]} ({AngelDBTools.StringFunctions.GetFirstElement(field.Trim(), ' ')})");

                if (!column_exists)
                {
                    result = mainClass.Prompt($"ALTER TABLE {d["create_table"]} ADD COLUMN {AngelDBTools.StringFunctions.GetFirstElement(field.Trim(), ' ')}");

                    if (result.StartsWith("Error:"))
                    {
                        return "Error: alter table " + result;
                    }

                }
            }

            foreach (var field in arrayFields)
            {
                if (!field.Contains(" INDEXED"))
                {
                    continue;
                }

                if (field.Trim().ToLower().EndsWith("_blob"))
                {
                    continue;
                }

                string field_name = field.Replace(" INDEXED", "").Trim();

                result = sqlite.SQLExec($"CREATE INDEX IF NOT EXISTS {d["create_table"]}_{AngelDBTools.StringFunctions.GetFirstElement(field_name.Trim(), ' ')} ON {d["create_table"]} ({AngelDBTools.StringFunctions.GetFirstElement(field_name.Trim(), ' ')})");

                if (result != "Ok.")
                {
                    return "Error: creating index  1 " + result;
                }

                result = mainClass.Prompt($"CREATE INDEX {d["create_table"]}_{field_name.Trim()} ON TABLE {d["create_table"]} COLUMN {field_name.Trim()}");

                if (result != "Ok.")
                {
                    return "Error: creating index 2 " + result;
                }

            }

            return "Ok.";

        }

        public static string CopyTo(Dictionary<string, string> d, DB mainClass)
        {

            DbLanguage l = new DbLanguage();
            Dictionary<string, string> select = l.Interpreter(d["from"]);

            if (d == null)
            {
                return l.errorString;
            }

            string result = mainClass.Prompt($"GET TABLES WHERE tablename = '{select["from"]}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "[]")
            {
                return $"Error: Source table does not exist: {select["from"]}";
            }

            DataTable source_table = JsonConvert.DeserializeObject<DataTable>(result);
            ;
            string sql;

            if (d["copy_to"] == select["from"])
            {
                return "Error: The source table and the destination table cannot be the same";
            }

            if (d["type_search"] == "false")
            {
                sql = $"CREATE TABLE {d["copy_to"]} FIELD LIST {source_table.Rows[0]["fieldlist"]} STORAGE {d["storage"]}";
            }
            else
            {
                sql = $"CREATE TABLE {d["copy_to"]} FIELD LIST {source_table.Rows[0]["fieldlist"]} STORAGE {d["storage"]} TYPE SEARCH";
            }

            result = mainClass.Prompt(sql);

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            int rows_limit = 2000;
            int off_set = 0;

            while (true)
            {

                sql = "";

                if (off_set == 0)
                {
                    sql = d["from"] + " LIMIT " + rows_limit.ToString();
                }
                else
                {
                    sql = d["from"] + " LIMIT " + rows_limit.ToString() + " OFFSET " + off_set;
                }

                result = mainClass.Prompt(sql);

                off_set += rows_limit;

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                if (result == "[]")
                {
                    break;
                }

                if (result == "{}")
                {
                    break;
                }

                Dictionary<string, DataTable> ds = JsonConvert.DeserializeObject<Dictionary<string, DataTable>>(result);

                foreach (string item in ds.Keys)
                {
                    result = mainClass.Prompt($"INSERT INTO {d["copy_to"]} PARTITION KEY {item} VALUES {JsonConvert.SerializeObject(ds[item], Formatting.Indented)}");

                    if (result.StartsWith("Error:"))
                    {
                        return result;
                    }

                }

            }

            return "Ok.";
        }

        public static string validate_partition(string table_name, string partition_key, DB mainClass, out string table_type, out QueryTools queryTable)
        {

            //if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER) return "Error: You do not have permissions to save accounts";

            table_type = "";
            queryTable = null;

            try
            {
                partition_key = partition_key.Trim().ToLower();

                if (mainClass.account == "") return "Error: No account selected";
                if (mainClass.database == "") return "Error: No database selected";
                if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

                if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
                {
                    if (mainClass.UserTables != "null")
                    {
                        if (mainClass.UserTables.Contains(table_name))
                        {
                            return $"Error: You do not have permissions to insert in this table {table_name}";
                        }
                    };
                }

                table_name = table_name.Trim().ToLower();
                string database_directory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
                string database_connection_string = $"Data Source={database_directory + mainClass.os_directory_separator + "tables.db"};" + "version = 3";
                QueryTools query_database = new QueryTools(database_connection_string);
                string result = query_database.OpenConnection();
                if (result.StartsWith("Error:")) return "Error: On Open Database List:" + result;
                DataTable t = query_database.SQLTable($"SELECT * FROM tables WHERE tablename = '{table_name}'");
                if (t.Rows.Count == 0) return $"Error: Table does not exist {table_name}";
                query_database.CloseConnection();

                string table_directory;

                if (t.Rows[0]["storage"].ToString() == "main")
                {
                    table_directory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + table_name;
                }
                else
                {
                    table_directory = t.Rows[0]["storage"].ToString();
                }

                string tabletype = t.Rows[0]["tabletype"].ToString();
                table_type = tabletype;
                string partition_file = table_directory + mainClass.os_directory_separator + table_name + "_" + partition_key.Trim().ToLower() + ".db";

                string lockFile = Path.ChangeExtension(partition_file, "lock");

                result = IsTableLocked(lockFile, mainClass, table_name, partition_key);

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                string tableConnectionString = $"Data Source={table_directory + mainClass.os_directory_separator + table_name.Trim().ToLower() + ".db"}" + ";version = 3";

                QueryTools sqlite_table = new QueryTools(tableConnectionString);

                if (!sqlite_table.IsOpen())
                {
                    result = sqlite_table.OpenConnection();
                }

                if (result.StartsWith("Error:")) return "Error: On Open table: {result}";

                queryTable = sqlite_table;

                DataTable p = sqlite_table.SQLTable($"SELECT * FROM partitions WHERE partition = '{partition_key}'");

                if (p.Rows.Count == 0)
                {
                    sqlite_table.AddValue("partition", partition_key);
                    sqlite_table.AddValue("tablename", table_name);
                    sqlite_table.AddValue("timestamp", System.DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
                    sqlite_table.GenInsertQuery("partitions");
                    result = sqlite_table.Exec("insert");

                    if (result != "Ok.")
                    {
                        sqlite_table.CloseConnection();
                        return $"Error: Inserting on table partitions {result}";
                    }
                }
                else 
                {
                    if( p.Rows[0]["compressed"].ToString() == "1")
                    {
                        return $"Error: Partition {partition_key} from table {table_name} is compressed";
                    }
                }

                if (!File.Exists(partition_file))
                {

                    string ConnectionString = $"Data Source={partition_file}" + ";version = 3";
                    QueryTools sqlite_partition = new QueryTools(ConnectionString);

                    result = sqlite_partition.OpenConnection();

                    if (result.StartsWith("Error:")) return "Error: On Open Partition List:" + result;

                    switch (table_type)
                    {
                        case "normal":
                            DataTable tData = sqlite_table.SQLTable($"SELECT sql FROM sqlite_master WHERE name = '{table_name}' AND type = 'table'");

                            if (tData.Rows.Count > 0)
                            {
                                result = sqlite_partition.ExecSQLDirect(tData.Rows[0]["sql"].ToString());
                                if (result != "Ok.") return "Error: creating table partition";

                                tData = sqlite_table.SQLTable($"SELECT sql FROM sqlite_master WHERE tbl_name = '{table_name}' AND type = 'index'");

                                foreach (DataRow item in tData.Rows)
                                {
                                    if (item["sql"].ToString().Trim() != "") sqlite_partition.ExecSQLDirect(item["sql"].ToString());
                                }

                            }

                            break;

                        case "search":

                            string fieldList = t.Rows[0]["fieldlist"].ToString();
                            result = sqlite_partition.ExecSQLDirect($"CREATE VIRTUAL TABLE {table_name} USING FTS5(partitionkey, id, timestamp, {fieldList})");
                            if (result != "Ok.") return $"Error: creating table partition {result}";
                            break;
                    }

                    sqlite_partition.CloseConnection();

                }

                return table_directory;

            }
            catch (System.Exception e)
            {
                return $"Error: validate_partition: {e}";
            }

        }

        public static string InsertInto(Dictionary<string, string> d, DB mainClass)
        {
            
            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            QueryTools query = null;

            System.Data.SQLite.SQLiteTransaction transaction = null;

            try
            {

                d["partition_key"] = d["partition_key"].Trim().ToLower();

                if (d["partition_key"] == "null")
                {
                    d["partition_key"] = "main";
                }

                d["partition_key"] = d["partition_key"].Trim().ToLower();

                string result = "";
                string account_database_table_partition = mainClass.account + "-" + mainClass.database + "-" + d["insert_into"] + "-" + d["partition_key"];
                string table_type = "";

                if (mainClass.partitions.ContainsKey(account_database_table_partition))
                {
                    string result_update = mainClass.partitions[account_database_table_partition].UpdateTimeStamp();

                    if (result.StartsWith("Error:"))
                    {
                        DataTable dataTable = mainClass.partitions[account_database_table_partition].sqlite.SQLTable($"SELECT * FROM partitions WHERE partition = '{d["partition_key"]}' AND tablename = '{d["insert_into"]}'");

                        return $"Error: UpdateTimeStamp {result_update}";
                    }

                    result = mainClass.partitions[account_database_table_partition].account_database_table_partition;
                }
                else
                {

                    QueryTools queryTable = null;

                    result = validate_partition(d["insert_into"], d["partition_key"], mainClass, out table_type, out queryTable);

                    if (result.StartsWith("Error:"))
                    {
                        return $"Error: {result}";
                    }

                    PartitionsInfo p = new PartitionsInfo();
                    p.account_database_table_partition = account_database_table_partition;
                    p.table_type = table_type;
                    p.partition_name = d["partition_key"];
                    p.sqlite = queryTable;

                    string result_update = p.UpdateTimeStamp();

                    if (result.StartsWith("Error:"))
                    {
                        return $"Error: UpdateTimeStamp {result_update}";
                    }

                    mainClass.partitions.TryAdd(account_database_table_partition, p);
                }

                string table_directory = result;

                if (mainClass.SQLiteConnections.ContainsKey(account_database_table_partition))
                {
                    query = new QueryTools(mainClass.SQLiteConnections[account_database_table_partition].ConnectionString);
                }
                else
                {
                    query = new QueryTools($"Data Source={table_directory + mainClass.os_directory_separator + d["insert_into"].Trim().ToLower() + "_" + d["partition_key"].Trim().ToLower() + ".db"}" + ";version = 3");
                    mainClass.SQLiteConnections.TryAdd(account_database_table_partition, query);
                }

                if (!query.IsOpen())
                {
                    result = query.OpenConnection();

                    if (result.StartsWith("Error:"))                     
                    {

                        string result_partition = mainClass.Prompt($"GET PARTITIONS FROM TABLE {d["insert_into"]} WHERE partition = '{d["partition_key"]}'");

                        DataTable partitions = JsonConvert.DeserializeObject<DataTable>(result_partition);

                        if (partitions.Rows.Count > 0)
                        {
                            if (partitions.Rows[0]["compressed"].ToString() == "1")
                            {
                                return $"Error: Partition {d["partition_key"]} from table {d["insert_into"]} is compressed";
                            }
                        }

                        return $"Error: {result}";
                    }
                }

                if (result.StartsWith("Error:"))
                {
                    return $"Error: On open partition table: {result}";
                }

                if (mainClass.speed_up)
                {
                    query.ExecSQLDirect("PRAGMA synchronous = OFF");
                    query.ExecSQLDirect("PRAGMA journal_mode = MEMORY");
                }

                result = RunPartitionRule(d["insert_into"], d["partition_key"], mainClass, d);

                if (result.StartsWith("Error:")) return $"Error: On partition rule {d["partition_key"]}: {result.Replace("Error:", "")}";
                if (!result.StartsWith("CONTINUE:")) return result;

                DataTable json_values;

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
                }
                catch (System.Exception e1)
                {
                    return $"Error: Insert {d["insert_into"]} Json string contains errors, {e1} jSon Values -->> {d["values"]}";
                }

                foreach (DataColumn column in json_values.Columns)
                {
                    column.ColumnName = column.ColumnName.ToLower();
                }


                if (d["exclude_columns"] != "null")
                {
                    string[] exclude_columns = d["exclude_columns"].Split(",");

                    foreach (string item in exclude_columns)
                    {
                        if (json_values.Columns.Contains(item.Trim()))
                        {
                            json_values.Columns.Remove(item.Trim());
                        }
                    }

                }

                query.ColumnList.Clear();

                foreach (DataColumn item in json_values.Columns)
                {
                    if (item.ColumnName.Trim().ToLower() == "partitionkey")
                    {
                        continue;
                    }

                    query.ColumnList.Add(item.ColumnName);
                }

                if (!query.ColumnList.Contains("id"))
                {
                    query.ColumnList.Add("id");
                }

                if (!query.ColumnList.Contains("partitionkey"))
                {
                    query.ColumnList.Add("partitionkey");
                }

                if (!query.ColumnList.Contains("timestamp"))
                {
                    query.ColumnList.Add("timestamp");
                }

                //DataTable partition_table = query.SQLTable($"SELECT * FROM {d["insert_into"]} LIMIT 1");

                //foreach (DataColumn c in partition_table.Columns)
                //{
                //    if (c.ColumnName.StartsWith("node_"))
                //    {
                //        if (!query.ColumnList.Contains(c.ColumnName))
                //        {
                //            query.ColumnList.Add(c.ColumnName);
                //        }
                //    }
                //}

                transaction = query.SQLConnection.BeginTransaction();
                bool autoid = false;

                if (!json_values.Columns.Contains("id"))
                {
                    autoid = true;
                }

                if (mainClass.partitions[account_database_table_partition].table_type == "search")
                {

                    result = query.ExecSQLDirect($"CREATE TABLE IF NOT EXISTS ids ( PartitionKey, id, requires_insert, PRIMARY KEY ( PartitionKey, id ) )");

                    if (!autoid)
                    {
                        foreach (DataRow json_row in json_values.Rows)
                        {
                            query.SQLCommand.Parameters.Clear();
                            query.SQLCommand.Parameters.AddWithValue("partitionkey", d["partition_key"]);
                            query.SQLCommand.Parameters.AddWithValue("@id", json_row["id"].ToString());
                            query.SQLCommand.Parameters.AddWithValue("@partitionkey1", d["partition_key"]);
                            query.SQLCommand.Parameters.AddWithValue("@id1", json_row["id"]);
                            result = query.ExecSQLDirect("INSERT INTO ids (PartitionKey, id, requires_insert) VALUES (@partitionkey, @id, 1) ON CONFLICT(PartitionKey, id) DO UPDATE SET PartitionKey = @partitionkey1, id = @id1, requires_insert = 2");
                        }
                    }
                }

                if (d["upsert"] == "true")
                {
                    query.GenUpsertQuery(d["insert_into"], $"partitionkey = @partitionkey1 AND id = @id1");
                }
                else
                {
                    query.GenInsertQuery(d["insert_into"]);
                }

                if (mainClass.partitions[account_database_table_partition].table_type == "search")
                {
                    query.GenUpdateQuery(d["insert_into"], $"partitionkey = @partitionkey1 AND id = @id1");
                    query.GenInsertQuery(d["insert_into"]);
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                foreach (DataRow json_row in json_values.Rows)
                {

                    if (mainClass.CancelTransactions)
                    {
                        try
                        {
                            transaction.Rollback();
                            return "Error: Cancelled";
                        }
                        catch (Exception)
                        {
                        }
                    }

                    string id;

                    query.SQLCommand.Parameters.Clear();

                    foreach (DataColumn column in json_values.Columns)
                    {
                        if (json_row[column.ColumnName] is null)
                        {
                            query.SQLCommand.Parameters.AddWithValue(column.ColumnName, DBNull.Value);
                        }
                        else
                        {
                            query.SQLCommand.Parameters.AddWithValue(column.ColumnName, json_row[column.ColumnName]);
                        }

                    }

                    if (autoid)
                    {
                        id = Guid.NewGuid().ToString();
                    }
                    else
                    {
                        id = json_row["id"].ToString();
                    }

                    query.SQLCommand.Parameters.AddWithValue("id", id);
                    query.SQLCommand.Parameters.AddWithValue("partitionkey", d["partition_key"]);
                    query.SQLCommand.Parameters.AddWithValue("timestamp", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
                    query.SQLCommand.Parameters.AddWithValue("id1", id);
                    query.SQLCommand.Parameters.AddWithValue("partitionkey1", d["partition_key"]);

                    if (mainClass.partitions[account_database_table_partition].table_type != "search")
                    {
                        if (d["upsert"] == "true")
                            result = query.Exec("upsert");
                        else
                        {
                            result = query.Exec("insert");
                        }
                    }
                    else
                    {
                        if (d["upsert"] == "true")
                        {
                            if (!autoid)
                            {
                                DataTable tSearch = query.SQLTable($"SELECT requires_insert FROM ids WHERE partitionkey = '{d["partition_key"]}' AND id = '{id}' LIMIT 1");

                                if (tSearch.Rows.Count == 0)
                                {
                                    result = query.Exec("insert");
                                }
                                else
                                {
                                    if (tSearch.Rows[0]["requires_insert"].ToString() == "1")
                                    {
                                        result = query.Exec("insert");
                                    }
                                    else
                                    {
                                        result = query.Exec("update");
                                    }
                                }
                            }
                            else
                            {
                                result = query.Exec("insert");
                            }
                        }
                        else
                        {
                            result = query.Exec("insert");
                        }

                        if (result.StartsWith("Error:"))
                        {
                            if (transaction != null)
                            {
                                try
                                {
                                    transaction.Rollback();
                                }
                                catch (Exception)
                                {
                                }
                            }

                            return ($"Error: {result}");
                        }
                    }

                    if (result.StartsWith("Error:"))
                    {
                        if (transaction != null)
                        {
                            try
                            {
                                transaction.Rollback();

                                //if (mainClass.SQLiteConnections.ContainsKey(account_database_table_partition))
                                //{
                                //    mainClass.SQLiteConnections[account_database_table_partition].CloseConnection();
                                //    mainClass.SQLiteConnections.TryRemove(account_database_table_partition, out _);
                                //}
                            }
                            catch (Exception)
                            {
                            }
                        }

                        return ($"Error: {result}");
                    }

                }

                transaction.Commit();
                //query.CloseConnection();

                result = sb.ToString();

                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }

                return "Ok.";

            }
            catch (System.Exception e)
            {

                string error = $"Error: Insert General: {e}";

                try
                {
                    if (transaction != null)
                    {
                        transaction.Rollback();
                    }

                    if (query != null)
                    {
                        if (query.SQLConnection.State == ConnectionState.Open)
                        {
                            query.SQLConnection.Close();
                        }
                    }

                }
                catch (Exception)
                {
                }

                return error;
            }
        }

        public static string GetStructure(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
            {
                if (mainClass.UserTables != "null")
                {
                    if (mainClass.UserTables.Contains(d["from"]))
                    {
                        return $"Error: You do not have permissions to insert in this table {d["insert_into"]}";
                    }
                };
            }

            d["from"] = d["from"].Trim().ToLower();

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"};" + "version = 3";
            SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
            DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["from"]}'");
            if (t.Rows.Count == 0) return $"Error: Table does not exist {d["from"]}";

            string tableDirectory;

            if (t.Rows[0]["storage"].ToString() == "main")
            {
                tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["from"];
            }
            else
            {
                tableDirectory = t.Rows[0]["storage"].ToString();
            }

            string tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["from"].Trim().ToLower() + ".db"}" + ";version = 3";
            SqliteTools sqliteTable = new SqliteTools(tableConnectionString);
            DataTable tData = sqliteTable.SQLTable($"SELECT sql FROM sqlite_master WHERE name = '{d["from"]}' AND type = 'table'");

            return JsonConvert.SerializeObject(tData, Formatting.Indented);

        }

        public static string DeleteTable(Dictionary<string, string> d, DB mainClass)
        {

            try
            {

                if (!mainClass.IsLogged)
                {
                    return $"Error: You have not indicated your username and password";
                }

                if (mainClass.account == "") return "Error: No account selected";
                if (mainClass.database == "") return "Error: No database selected";
                if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

                if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
                {
                    if (mainClass.UserTables != "null")
                    {
                        return "Error: You do not have permissions to delete tables in this account";
                    };
                }

                string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
                string ConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"};" + ";version = 3";
                SqliteTools sqlite = new SqliteTools(ConnectionString);
                DataTable t = sqlite.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["delete_table"]}'");

                if (t.Rows.Count == 0) return $"Error: Table does not exist {d["delete_table"]}";

                string result = sqlite.SQLExec($"DELETE FROM tables WHERE tablename = '{d["delete_table"]}'");

                if (result != "Ok.")
                {
                    return result;
                }

                string tableDirectory;

                if (t.Rows[0]["storage"].ToString() == "main")
                {
                    tableDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database + mainClass.os_directory_separator + d["delete_table"];
                }
                else
                {
                    tableDirectory = t.Rows[0]["storage"].ToString();
                }

                Directory.Move(tableDirectory, tableDirectory + "_del_" + byteTools.RandomString(8, true));
                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: Deleting table {d["delete_table"]} {e}";

            }

        }

        static string saveFile(Dictionary<string, string> d, DB mainClass, string tableDirectory)
        {

            ZipArchive zip = null;

            try
            {
                if (d["blob"] != "null")
                {
                    if (!Directory.Exists(mainClass.BaseDirectory + "temp"))
                    {
                        Directory.CreateDirectory(mainClass.BaseDirectory + "temp");
                    }

                    if (File.Exists(mainClass.BaseDirectory + "temp" + mainClass.os_directory_separator + d["blob_name"]))
                    {
                        File.Delete(mainClass.BaseDirectory + "temp" + mainClass.os_directory_separator + d["blob_name"]);
                    }

                    byte[] blob = Convert.FromBase64String(d["blob"]);
                    File.WriteAllBytes(mainClass.BaseDirectory + "temp" + mainClass.os_directory_separator + d["blob_name"], blob);

                    string zipFile = tableDirectory + mainClass.os_directory_separator + d["insert_into"] + "_" + d["partition_key"].Trim().ToLower() + ".zip";

                    bool newZip;

                    if (!File.Exists(zipFile))
                    {
                        zip = ZipFile.Open(zipFile, ZipArchiveMode.Create);
                        newZip = true;
                    }
                    else
                    {
                        zip = ZipFile.Open(zipFile, ZipArchiveMode.Update);
                        newZip = false;
                    }

                    if (!newZip)
                    {
                        ZipArchiveEntry entry = zip.GetEntry(d["blob_name"]);

                        if (zip.Entries.Contains(entry))
                        {
                            entry.Delete();
                        }
                    }

                    zip.CreateEntryFromFile(mainClass.BaseDirectory + "temp" + mainClass.os_directory_separator + d["blob_name"], d["blob_name"]);
                    zip.Dispose();

                    File.Delete(mainClass.BaseDirectory + "temp" + mainClass.os_directory_separator + d["blob_name"]);

                }

            }
            catch (Exception e)
            {
                if (zip != null)
                {
                    zip.Dispose();
                }

                return $"Error: Saving Blob {d["blob_name"]} {e}";
            }

            return "Ok.";

        }

        public static string Update(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            QueryTools query = null;

            try
            {

                d["partition_key"] = d["partition_key"].Trim().ToLower();

                if (d["partition_key"] == "null")
                {
                    d["partition_key"] = "main";
                }

                if (mainClass.account == "") return "Error: No account selected";
                if (mainClass.database == "") return "Error: No database selected";
                if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

                if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
                {
                    if (mainClass.UserTables != "null")
                    {
                        if (mainClass.UserTables.Contains(d["insert_into"]))
                        {
                            return $"Error: You do not have permissions to update this table {d["insert_into"]}";
                        }
                    };
                }

                string result = "";
                string account_database_table_partition = mainClass.account + "-" + mainClass.database + "-" + d["update"] + "-" + d["partition_key"];
                string table_type = "";

                if (mainClass.partitions.ContainsKey(account_database_table_partition))
                {
                    string result_update = mainClass.partitions[account_database_table_partition].UpdateTimeStamp();

                    if (result.StartsWith("Error:"))
                    {
                        return $"Error: UpdateTimeStamp {result_update}";
                    }

                    result = mainClass.partitions[account_database_table_partition].account_database_table_partition;
                }
                else
                {

                    QueryTools queryTable = null;

                    result = validate_partition(d["update"], d["partition_key"], mainClass, out table_type, out queryTable);

                    if (result.StartsWith("Error:"))
                    {
                        return $"Error: {result}";
                    }


                    PartitionsInfo p = new PartitionsInfo();
                    p.account_database_table_partition = account_database_table_partition;
                    p.table_type = table_type;
                    p.partition_name = d["partition_key"];
                    p.sqlite = queryTable;

                    string result_update = p.UpdateTimeStamp();

                    if (result.StartsWith("Error:"))
                    {
                        return $"Error: UpdateTimeStamp {result_update}";
                    }

                    mainClass.partitions.TryAdd(account_database_table_partition, p);
                }

                string table_directory = result;

                if (mainClass.SQLiteConnections.ContainsKey(account_database_table_partition))
                {
                    query = new QueryTools(mainClass.SQLiteConnections[account_database_table_partition].ConnectionString);
                }
                else
                {
                    query = new QueryTools($"Data Source={table_directory + mainClass.os_directory_separator + d["update"].Trim().ToLower() + "_" + d["partition_key"].Trim().ToLower() + ".db"}" + ";version = 3");
                    mainClass.SQLiteConnections.TryAdd(account_database_table_partition, query);
                }

                if (!query.IsOpen())
                {
                    result = query.OpenConnection();
                    if (result.StartsWith("Error:")) return $"Error: {result}";
                }

                if (mainClass.speed_up)
                {
                    query.ExecSQLDirect("PRAGMA synchronous = OFF");
                    query.ExecSQLDirect("PRAGMA journal_mode = MEMORY");
                }

                result = RunPartitionRule(d["update"], d["partition_key"], mainClass, d);

                if (result.StartsWith("Error:")) return $"Error: On partition rule {d["partition_key"]}: {result.Replace("Error:", "")}";
                if (!result.StartsWith("CONTINUE:")) return result;

                d["update"] = d["update"].Trim().ToLower();
                string sql = "";

                if (d["set"].StartsWith("[") || d["set"].StartsWith("{"))
                {
                    if (d["set"].StartsWith("{"))
                    {
                        d["set"] = "[" + d["set"] + "]";
                    }

                    DataTable t = JsonConvert.DeserializeObject<DataTable>(d["set"]);

                    if (t.Rows.Count > 0)
                    {
                        if (!t.Columns.Contains("id"))
                        {
                            query.SQLConnection.Close();
                            return "Error: The set parameter must contain the id column";
                        }
                    }

                    System.Data.SQLite.SQLiteTransaction transaction = query.SQLConnection.BeginTransaction();

                    foreach (DataRow r in t.Rows)
                    {

                        if (mainClass.CancelTransactions)
                        {
                            try
                            {
                                transaction.Rollback();
                                return "Error: Cancelled";
                            }
                            catch (Exception)
                            {
                            }
                        }

                        StringBuilder set = new StringBuilder();

                        foreach (DataColumn c in t.Columns)
                        {

                            if (r[c.ColumnName].GetType().Name == "String")
                            {
                                set.Append(c.ColumnName + " = '" + r[c.ColumnName].ToString() + "',");
                            }
                            else
                            {
                                set.Append(c.ColumnName + " = " + r[c.ColumnName].ToString() + ",");
                            }
                        }

                        d["set"] = set.ToString();

                        if (d["set"].EndsWith(","))
                        {
                            d["set"] = d["set"].Substring(0, d["set"].Length - 1);
                        }

                        sql = $"UPDATE {d["update"]} SET timestamp = '{System.DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff")}', {d["set"]} WHERE partitionkey = '{d["partition_key"]}' AND id = '{r["id"]}'";
                        result = query.ExecSQLDirect(sql);
                    }

                    if (result.StartsWith("Error:"))
                    {
                        if (query is not null)
                        {
                            query.CloseConnection();
                        }

                        return result;
                    }

                    transaction.Commit();

                }
                else
                {
                    if (string.IsNullOrEmpty(d["where"]))
                    {
                        sql = $"UPDATE {d["update"]} SET timestamp = '{System.DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff")}', {d["set"]} WHERE partitionkey = '{d["partition_key"]}'";
                    }
                    else
                    {
                        sql = $"UPDATE {d["update"]} SET timestamp = '{System.DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff")}', {d["set"]} WHERE partitionkey = '{d["partition_key"]}' AND {d["where"]}";
                    }
                    result = query.ExecConcurrentSQLDirect(sql);
                }


                //query.CloseConnection();
                return result;


            }
            catch (Exception e)
            {
                if (query is not null)
                {
                    query.CloseConnection();
                }

                return $"Error: Update 1: {e.ToString()}";
            }

        }

        public static string DeleteFrom(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            d["partition_key"] = d["partition_key"].Trim().ToLower();

            if (d["partition_key"] == "null")
            {
                d["partition_key"] = "main";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
            {
                if (mainClass.UserTables != "null")
                {
                    if (mainClass.UserTables.Contains(d["delete_from"]))
                    {
                        return $"Error: You do not have permissions to delete in this table {d["delete_from"]}";
                    }
                };
            }

            string result = RunPartitionRule(d["delete_from"], d["partition_key"].ToString(), mainClass, d);

            if (result.StartsWith("Error:")) return $"Error: On partition rule {d["partition_key"]}: {result.Replace("Error:", "")}";
            if (!result.StartsWith("CONTINUE:")) return result;

            d["delete_from"] = d["delete_from"].Trim().ToLower();

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
            SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
            DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["delete_from"]}'");

            if (t.Rows.Count == 0) return $"Error: Table does not exist {d["delete_from"]}";

            string table_type = t.Rows[0]["tabletype"].ToString();

            string tableDirectory;

            if (t.Rows[0]["storage"].ToString() == "main")
            {
                tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["delete_from"];
            }
            else
            {
                tableDirectory = t.Rows[0]["storage"].ToString();
            }


            if (string.IsNullOrEmpty(d["partition_key"])) return "Error: No partitionkey given";
            string partitionFile = tableDirectory + mainClass.os_directory_separator + d["delete_from"] + "_" + d["partition_key"].Trim().ToLower() + ".db";
            string tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["delete_from"].Trim().ToLower() + ".db"}" + ";version = 3";
            SqliteTools sqliteTable = new SqliteTools(tableConnectionString);
            DataTable p = sqliteTable.SQLTable($"SELECT * FROM partitions WHERE partition = '{d["partition_key"]}'");

            if (p.Rows.Count == 0)
            {
                return "Ok.";
            }

            string ConnectionString = $"Data Source={partitionFile}" + ";version = 3";

            if (!File.Exists(partitionFile))
            {
                if (t.Rows.Count == 0) return $"Error: Partition Table does not exist {partitionFile}";
            }


            System.Data.SQLite.SQLiteTransaction transaction = null;

            try
            {
                QueryTools query = new QueryTools(ConnectionString);

                result = query.OpenConnection();
                if (result.StartsWith("Error:")) return $"Error: {result}";

                transaction = query.SQLConnection.BeginTransaction();

                if (table_type == "search")
                {
                    DataTable tids = query.SQLTable($"SELECT partitionkey, id FROM {d["delete_from"]} WHERE {d["where"]}");

                    foreach (DataRow item in tids.Rows)
                    {
                        query.ExecSQLDirect($"DELETE FROM ids WHERE partitionkey = '{item["partitionkey"]}' AND id = '{item["id"]}'");
                    }
                }

                result = query.ExecSQLDirect($"DELETE FROM {d["delete_from"]} WHERE {d["where"]}");

                if (result.StartsWith("Error:"))
                {
                    transaction.Rollback();
                    query.CloseConnection();
                    return result;
                }

                transaction.Commit();
                query.CloseConnection();

                return result;

            }
            catch (Exception e)
            {

                if (transaction is not null)
                {
                    transaction.Rollback();
                }

                return "Error: Delete 1: " + e.ToString();
            }

        }

        public static string AlterTable(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
            SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
            DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["alter_table"]}'");

            if (t.Rows.Count == 0) return $"Error: Table does not exist {d["alter_table"]}";
            string table_type = t.Rows[0]["tabletype"].ToString();

            string tableDirectory;

            if (t.Rows[0]["storage"].ToString() == "main")
            {
                tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["alter_table"];
            }
            else
            {
                tableDirectory = t.Rows[0]["storage"].ToString();
            }

            string tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["alter_table"].Trim().ToLower() + ".db"}" + ";version = 3";
            SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

            string result = sqliteTable.SQLExec($"ALTER TABLE {d["alter_table"]} ADD COLUMN {d["add_column"]}");

            if (result != "Ok.")
            {
                 return $"Error: table {d["alter_table"]} opertion  {result}";
            }

            result = sqliteDatabase.SQLExec($"UPDATE tables SET fieldlist = '{t.Rows[0]["fieldlist"].ToString() + "," + d["add_column"]}' WHERE tablename = '{d["alter_table"]}'");

            DataTable p = sqliteTable.SQLTable($"SELECT * FROM partitions ORDER BY partition DESC");

            if (p.Rows.Count == 0)
            {
                if (t.Rows.Count == 0) return "Ok.";
            }

            foreach (DataRow r in p.Rows)
            {

                result = RunPartitionRule(d["alter_table"], r["partition"].ToString(), mainClass, d);

                if (result.StartsWith("Error:")) return $"Error: On partition rule {r["partition"]}: {result.Replace("Error:", "")}";
                if (!result.StartsWith("CONTINUE:")) continue;

                string partitionFile = tableDirectory + mainClass.os_directory_separator + d["alter_table"] + "_" + r["partition"].ToString().Trim().ToLower() + ".db";
                string ConnectionString = $"Data Source={partitionFile + ";version = 3"};";
                SqliteTools sqlite = new SqliteTools(ConnectionString);

                if (!File.Exists(partitionFile))
                {
                    if (t.Rows.Count == 0) return $"Error: Partition Table does not exist {partitionFile}";
                }

                if (d["add_column"] != "null")
                {
                    result = sqlite.SQLExec($"ALTER TABLE {d["alter_table"]} ADD COLUMN {d["add_column"]}", table_type);

                    if (result != "Ok.")
                    {
                        return $"Error: alter partition table {d["alter_table"]}.{r["partition"]} opertion  {result}";
                    }
                }

            }

            return "Ok.";

        }



        public static string AlterTableDropColumn(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
            SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
            DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["alter_table"]}'");

            if (t.Rows.Count == 0) return $"Error: Table does not exist {d["alter_table"]}";
            string table_type = t.Rows[0]["tabletype"].ToString();

            string tableDirectory;

            if (t.Rows[0]["storage"].ToString() == "main")
            {
                tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["alter_table"];
            }
            else
            {
                tableDirectory = t.Rows[0]["storage"].ToString();
            }

            string tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["alter_table"].Trim().ToLower() + ".db"}" + ";version = 3";
            SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

            string result = sqliteTable.SQLExec($"ALTER TABLE {d["alter_table"]} DROP COLUMN {d["drop_column"]}");

            if (result != "Ok.")
            {
                return $"Error: table {d["alter_table"]} opertion  {result}";
            }

            string[] columns = t.Rows[0]["fieldlist"].ToString().Split(",");
            string newcolumns = "";

            foreach (string item in columns)
            {

                string column = item.Replace(" INDEXED", "").ToLower().Trim();

                if (d["drop_column"].Trim().ToLower() != column)
                {
                    newcolumns += item + ",";
                }
            }

            newcolumns = newcolumns.Substring(0, newcolumns.Length - 1);
            result = sqliteDatabase.SQLExec($"UPDATE tables SET fieldlist = '{newcolumns}' WHERE tablename = '{d["alter_table"]}'");

            //DataTable main_columns = sqliteTable.SQLTable($"SELECT * FROM {d["drop_column"]}");


            DataTable p = sqliteTable.SQLTable($"SELECT * FROM partitions ORDER BY partition DESC");

            if (p.Rows.Count == 0)
            {
                if (t.Rows.Count == 0) return "Ok.";
            }

            foreach (DataRow r in p.Rows)
            {

                result = RunPartitionRule(d["alter_table"], r["partition"].ToString(), mainClass, d);

                if (result.StartsWith("Error:")) return $"Error: On partition rule {r["partition"]}: {result.Replace("Error:", "")}";
                if (!result.StartsWith("CONTINUE:")) continue;

                string partitionFile = tableDirectory + mainClass.os_directory_separator + d["alter_table"] + "_" + r["partition"].ToString().Trim().ToLower() + ".db";
                string ConnectionString = $"Data Source={partitionFile + ";version = 3"};";
                SqliteTools sqlite = new SqliteTools(ConnectionString);

                if (!File.Exists(partitionFile))
                {
                    if (t.Rows.Count == 0) return $"Error: Partition Table does not exist {partitionFile}";
                }

                if (d["drop_column"] != "null")
                {
                    result = sqlite.SQLExec($"ALTER TABLE {d["alter_table"]} DROP COLUMN {d["drop_column"]}", table_type);

                    if (result != "Ok.")
                    {
                        return $"Error: alter partition table {d["alter_table"]}.{r["partition"]} opertion  {result}";
                    }
                }

            }

            return "Ok.";

        }


        public static string CreateIndex(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
            SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
            DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["on_table"]}'");

            if (t.Rows.Count == 0) return $"Error: Table does not exist {d["on_table"]}";
            string table_type = t.Rows[0]["tabletype"].ToString();

            if(table_type == "search")
            {
                return "Error: Search tables do not support indexes";
            }

            string tableDirectory;

            if (t.Rows[0]["storage"].ToString() == "main")
            {
                tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["on_table"];
            }
            else
            {
                tableDirectory = t.Rows[0]["storage"].ToString();
            }

            string tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["on_table"].Trim().ToLower() + ".db"}" + ";version = 3";
            SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

            DataTable p = sqliteTable.SQLTable($"SELECT * FROM partitions WHERE compressed <> 1 ORDER BY partition DESC");

            if (p.Rows.Count == 0)
            {
                if (t.Rows.Count == 0) return "Ok.";
            }

            string result = "";

            foreach (DataRow r in p.Rows)
            {

                result = RunPartitionRule(d["on_table"], r["partition"].ToString(), mainClass, d);

                if (result.StartsWith("Error:")) return $"Error: On partition rule {r["partition"]}: {result.Replace("Error:", "")}";
                if (!result.StartsWith("CONTINUE:")) continue;

                string partitionFile = tableDirectory + mainClass.os_directory_separator + d["on_table"] + "_" + r["partition"].ToString().Trim().ToLower() + ".db";

                if (!File.Exists(partitionFile))
                {
                    if (t.Rows.Count == 0) return $"Error: Partition Table does not exist {partitionFile}";
                }

                string ConnectionString = $"Data Source={partitionFile + ";version = 3"};";
                SqliteTools sqlite = new SqliteTools(ConnectionString);


                if (d["column"] != "null")
                {
                    result = sqlite.SQLExec($"CREATE INDEX IF NOT EXISTS {d["create_index"]} ON {d["on_table"]} ({d["column"]})");

                    Console.WriteLine($"CREATE INDEX IF NOT EXISTS {d["create_index"]} ON {d["on_table"]} ({d["column"]})");

                    if (result != "Ok.")
                    {
                        return $"Error: Create index partition table {d["on_table"]}.{r["partition"]} {result}";
                    }
                }

            }

            return "Ok.";

        }


        public static string GetColumnNames(DataTable main_columns)
        {
            return string.Join(",", main_columns.Columns.Cast<DataColumn>().Select(col => col.ColumnName));
        }

        public static string Vacuum(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
            SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
            DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["vacuum_table"]}'");

            if (t.Rows.Count == 0) return $"Error: Table does not exist {d["vacuum_table"]}";
            string table_type = t.Rows[0]["tabletype"].ToString();

            string tableDirectory;

            if (t.Rows[0]["storage"].ToString() == "main")
            {
                tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["vacuum_table"];
            }
            else
            {
                tableDirectory = t.Rows[0]["storage"].ToString();
            }

            string tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["vacuum_table"].Trim().ToLower() + ".db"}" + ";version = 3";
            SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

            string result = sqliteTable.SQLExec($"VACUUM");

            if (result != "Ok.")
            {
                 return $"Error: table {d["alter_table"]} opertion  {result}";
            }

            DataTable p;

            if (d["partition"] != "null")
            {
                p = sqliteTable.SQLTable($"SELECT * FROM partitions WHERE partition = '{d["partition"]}'");
            }
            else 
            {
                p = sqliteTable.SQLTable($"SELECT * FROM partitions ORDER BY partition DESC");
            }

            if (p.Rows.Count == 0)
            {
                if (t.Rows.Count == 0) return "Ok.";
            }

            foreach (DataRow r in p.Rows)
            {

                result = RunPartitionRule(d["vacuum_table"], r["partition"].ToString(), mainClass, d);

                if (result.StartsWith("Error:")) return $"Error: On partition rule {r["partition"]}: {result.Replace("Error:", "")}";
                if (!result.StartsWith("CONTINUE:")) continue;

                string partitionFile = tableDirectory + mainClass.os_directory_separator + d["vacuum_table"] + "_" + r["partition"].ToString().Trim().ToLower() + ".db";
                string ConnectionString = $"Data Source={partitionFile + ";version = 3"};";
                SqliteTools sqlite = new SqliteTools(ConnectionString);

                if (!File.Exists(partitionFile))
                {
                    if (t.Rows.Count == 0) return $"Error: Partition Table does not exist {partitionFile}";
                }

                result = sqlite.SQLExec($"VACUUM", table_type);

                if (result != "Ok.")
                {
                    return $"Error: VACUUM table {d["vacuum_table"]}.{r["partition"]} operation  {result}";
                }

            }

            return "Ok.";

        }

        public static string Select(Dictionary<string, string> d, DB mainClass)
        {
            try
            {

                if (!mainClass.IsLogged)
                {
                    return $"Error: You have not indicated your username and password";
                }

                string partitionFile = "";

                if (mainClass.account == "") return "Error: No account selected";
                if (mainClass.database == "") return "Error: No database selected";

                if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
                {
                    if (mainClass.UserTables != "null")
                    {
                        if (!mainClass.UserTables.Contains(d["from"]))
                        {
                            return $"Error: You do not have permissions to read this table {d["from"]}";
                        }
                    };
                }

                SqliteTools sqliteTable;

                d["from"] = d["from"].Trim().ToLower();

                string key = mainClass.account + "_" + mainClass.database + "_" + d["from"];

                if (!mainClass.table_connections.ContainsKey(key))
                {
                    string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
                    string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
                    SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
                    DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["from"]}'");
                    if (t.Rows.Count == 0) return $"Error: Table does not exist: {d["from"]}";
                    string table_type = t.Rows[0]["tabletype"].ToString();
                    string tableDirectory;

                    if (t.Rows[0]["storage"].ToString() == "main")
                    {
                        tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["from"];
                    }
                    else
                    {
                        tableDirectory = t.Rows[0]["storage"].ToString();
                    }

                    d["partition_key"] = d["partition_key"].ToLower();

                    if (string.IsNullOrEmpty(d["partition_key"])) return "Error: No partitionkey given";
                    string tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["from"].Trim().ToLower() + ".db"}" + ";version = 3";
                    sqliteTable = new SqliteTools(tableConnectionString);

                    TableInfo ti = new TableInfo();
                    ti.ConnectionString = tableConnectionString;
                    ti.table_directory = tableDirectory;
                    ti.table_type = table_type;

                    mainClass.table_connections.TryAdd(key, ti);

                }
                else
                {
                    sqliteTable = new SqliteTools(mainClass.table_connections[key].ConnectionString);
                }

                DataTable p;

                if (d["partition_key"] == "null")
                {
                    p = sqliteTable.SQLTable($"SELECT * FROM partitions ORDER BY partition");
                }
                else
                {
                    if (d["partition_key"].Trim().Split(' ').Length > 1)
                    {
                        p = sqliteTable.SQLTable($"SELECT * FROM partitions WHERE {d["partition_key"]}");
                    }
                    else
                    {
                        p = sqliteTable.SQLTable($"SELECT * FROM partitions WHERE partition = '{d["partition_key"]}'");
                    }
                }

                Dictionary<string, DataTable> ds = new Dictionary<string, DataTable>();

                DataTable data_merge = null;
                FileResult fileResult = null;

                if (d["as_csv_file"] != "null")
                {
                    fileResult = new FileResult();
                    fileResult.Start = DateTime.Now;
                    fileResult.FileName = d["as_csv_file"];
                    fileResult.Query = mainClass.Command;

                    if (File.Exists(d["as_csv_file"]))
                    {
                        File.Delete(d["as_csv_file"]);
                    }
                }

                if (d["as_arff_file"] != "null")
                {
                    fileResult = new FileResult();
                    fileResult.Start = DateTime.Now;
                    fileResult.FileName = d["as_arff_file"];
                    fileResult.Query = mainClass.Command;

                    if (File.Exists(d["as_arff_file"]))
                    {
                        File.Delete(d["as_arff_file"]);
                    }

                }


                foreach (DataRow r in p.Rows)
                {

                    string result = "";

                    result = RunPartitionRule(d["from"], r["partition"].ToString().Trim().ToLower(), mainClass, d);

                    if (result.StartsWith("Error:"))
                    {
                        return result + $" --> Partition Rule {r["partition"]}";
                    }

                    if (!result.StartsWith("CONTINUE:"))
                    {

                        DataTable tPartition = JsonConvert.DeserializeObject<DataTable>(result);

                        if (d["verbose"] == "true")
                        {
                            Console.WriteLine("Partition rule applied: " + r["partition"].ToString().Trim().ToLower());
                        }

                        if (d["as_csv_file"] != "null")
                        {

                            if (fileResult.PartitionsIncluded == 0)
                            {
                                File.AppendAllText(d["as_csv_file"], StringFunctions.ToCSVString(tPartition, true), Encoding.UTF8);
                            }
                            else
                            {
                                File.AppendAllText(d["as_csv_file"], StringFunctions.ToCSVString(tPartition, false), Encoding.UTF8);
                            }

                            fileResult.PartitionsIncluded += 1;
                            fileResult.RecordsReturnet += tPartition.Rows.Count;
                            continue;
                        }

                        if (d["as_arff_file"] != "null")
                        {

                            if (fileResult.PartitionsIncluded == 0)
                            {
                                File.AppendAllText(d["as_arff_file"], StringFunctions.ToArffString(tPartition, d["from"], d["class"], true), Encoding.UTF8);
                            }
                            else
                            {
                                File.AppendAllText(d["as_arff_file"], StringFunctions.ToArffString(tPartition, d["from"], d["class"], false), Encoding.UTF8);
                            }

                            fileResult.PartitionsIncluded += 1;
                            fileResult.RecordsReturnet += tPartition.Rows.Count;
                            continue;
                        }

                        if (data_merge is null)
                        {
                            data_merge = tPartition;
                        }
                        else
                        {
                            data_merge.Merge(tPartition, true, MissingSchemaAction.Ignore);
                        }

                        continue;
                    };

                    if( r.Table.Columns.Contains("compressed") )
                    {
                        if (r["compressed"].ToString() == "1")
                        {
                            continue;
                        }
                    }

                    partitionFile = mainClass.table_connections[key].table_directory + mainClass.os_directory_separator + d["from"] + "_" + r["partition"].ToString().Trim().ToLower() + ".db";

                    if (!File.Exists(partitionFile))
                    {
                        continue;
                    }

                    string lockFile = Path.ChangeExtension(partitionFile, "lock");

                    if (d["read_locked"] != "true") 
                    {
                        result = IsTableLocked(lockFile, mainClass, d["from"], d["partition_key"]);
                    }

                    if (result.StartsWith("Error:"))
                    {
                        return "Error: Select File is locked: " + d["from"] + "_" + r["partition"].ToString().Trim().ToLower();
                    }

                    string ConnectionString = $"Data Source={partitionFile}" + ";version = 3";
                    SqliteTools sqlite = new SqliteTools(ConnectionString);

                    string where = "";

                    if (d["where"] != "null")
                    {
                        where = " WHERE " + d["where"];
                    }

                    string group_by = "";

                    if (d["group_by"] != "null")
                    {
                        group_by = " GROUP BY " + d["group_by"];
                    }

                    string order_by = "";

                    if (d["order_by"] != "null")
                    {
                        order_by = " ORDER BY " + d["order_by"];
                    }

                    string limit = "";

                    if (d["limit"] != "null")
                    {
                        limit = " LIMIT " + d["limit"];
                    }

                    DataTable tData;

                    try
                    {

                        tData = sqlite.SQLTable($"SELECT {d["select"]} FROM {d["from"]}{where}{group_by}{order_by}{limit}", mainClass.table_connections[key].table_type);

                        if (d["verbose"] == "true")
                        {
                            Console.WriteLine("Partition added: " + key);
                        }

                        if (d["as_csv_file"] != "null")
                        {

                            if (fileResult.PartitionsIncluded == 0)
                            {
                                File.AppendAllText(d["as_csv_file"], StringFunctions.ToCSVString(tData, true), Encoding.UTF8);
                            }
                            else
                            {
                                File.AppendAllText(d["as_csv_file"], StringFunctions.ToCSVString(tData, false), Encoding.UTF8);
                            }

                            fileResult.PartitionsIncluded += 1;
                            fileResult.RecordsReturnet += tData.Rows.Count;
                            continue;
                        }

                        if (d["as_arff_file"] != "null")
                        {

                            if (fileResult.PartitionsIncluded == 0)
                            {
                                File.AppendAllText(d["as_arff_file"], StringFunctions.ToArffString(tData, d["from"], d["class"], true), Encoding.UTF8);
                            }
                            else
                            {
                                File.AppendAllText(d["as_arff_file"], StringFunctions.ToArffString(tData, d["from"], d["class"], false), Encoding.UTF8);
                            }

                            fileResult.PartitionsIncluded += 1;
                            fileResult.RecordsReturnet += tData.Rows.Count;
                            continue;
                        }


                        if (data_merge is null)
                        {
                            data_merge = tData;
                        }
                        else
                        {
                            data_merge.Merge(tData, true, MissingSchemaAction.Ignore);
                        }

                    }
                    catch (System.Exception e)
                    {
                        return $"Error: Select {d["from"]} Partition file {partitionFile} " + e.Message;
                    }

                }

                if (fileResult is not null)
                {
                    fileResult.End = DateTime.Now;
                    TimeSpan end = fileResult.End - fileResult.Start;
                    fileResult.TimeElapsed = end.ToString();
                    return JsonConvert.SerializeObject(fileResult, Formatting.Indented);
                }

                if (data_merge is null)
                {
                    return "[]";
                }

                return JsonConvert.SerializeObject(data_merge, Formatting.Indented);

            }
            catch (Exception e)
            {
                return "Error: (Select 1)" + e.ToString();
            }

        }

        public static string GetTables(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
            SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);

            DataTable t;

            if (d["where"] == "null")
            {
                t = sqliteDatabase.SQLTable($"SELECT * FROM tables ORDER BY tablename");
            }
            else
            {
                t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE {d["where"]} ORDER BY tablename");
            }

            return JsonConvert.SerializeObject(t, Formatting.Indented);

        }

        public static string GetPartitions(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string tableConnectionString = "";

            try
            {
                string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
                SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
                DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["from_table"]}'");

                if (t.Rows.Count == 0) return $"Error: Table does not exist {d["from_table"]}";

                string tableDirectory;

                if (t.Rows[0]["storage"].ToString() == "main")
                {
                    tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["from_table"];
                }
                else
                {
                    tableDirectory = t.Rows[0]["storage"].ToString();
                }

                tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["from_table"].Trim().ToLower() + ".db"}" + ";version = 3";
                SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

                DataTable p;

                if (d["where"] == "null")
                {
                    p = sqliteTable.SQLTable($"SELECT * FROM partitions ORDER BY partition");
                }
                else
                {
                    p = sqliteTable.SQLTable($"SELECT * FROM partitions WHERE {d["where"]} ORDER BY partition");
                }

                return JsonConvert.SerializeObject(p, Formatting.Indented);

            }
            catch (Exception e)
            {
                return $"Error: Partitions {tableConnectionString} {e}";
            }
        }


        public static string DeletePartitions(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string tableConnectionString = "";


            try
            {
                string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
                SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
                DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["from_table"]}'");

                if (t.Rows.Count == 0) return $"Error: Table does not exist {d["from_table"]}";

                string tableDirectory;

                if (t.Rows[0]["storage"].ToString() == "main")
                {
                    tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["from_table"];
                }
                else
                {
                    tableDirectory = t.Rows[0]["storage"].ToString();
                }

                tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["from_table"].Trim().ToLower() + ".db"}" + ";version = 3";
                SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

                DataTable p;

                if (d["where"] == "null")
                {
                    return "Error: The condition is required (WHERE)";
                }
                else
                {
                    p = sqliteTable.SQLTable($"SELECT * FROM partitions WHERE {d["where"]} ORDER BY partition");
                }

                foreach (DataRow row in p.Rows)
                {

                    string result = RunPartitionRule(d["from_table"], row["partition"].ToString(), mainClass, d);
                    if (result.StartsWith("Error:")) return $"Error: On partition rule {row["partition"].ToString()}: {result.Replace("Error:", "")}";
                    if (!result.StartsWith("CONTINUE:")) return result;

                    string partitionFile = tableDirectory + mainClass.os_directory_separator + d["from_table"] + "_" + row["partition"].ToString().ToString().Trim().ToLower() + ".db";
                    string lockFile = Path.ChangeExtension(partitionFile, "lock");
                    string account_database_table_partition = mainClass.account + "-" + mainClass.database + "-" + d["from_table"] + "-" + row["partition"].ToString().Trim().ToLower();

                    if( mainClass.SQLiteConnections.ContainsKey(account_database_table_partition))
                    {
                        mainClass.SQLiteConnections[account_database_table_partition].CloseConnection();
                        mainClass.SQLiteConnections.TryRemove(account_database_table_partition, out _);
                    }

                    if (File.Exists(partitionFile))
                    {
                        File.Delete(partitionFile);
                    }

                    if (File.Exists(lockFile))
                    {
                        File.Delete(lockFile);
                    }
                }

                SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

                if (d["only_files"] != "true") 
                {
                    sqlite.SQLExec($"DELETE FROM partitionrules WHERE {d["where"]}");
                    sqliteTable.SQLExec($"DELETE FROM partitions WHERE {d["where"]}");
                }

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: Partitions {tableConnectionString} {e}";
            }
        }


        public static string UpdateSyncTimeStampPartition(string tableName, string partiton, string timestamp, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string tableConnectionString = "";

            try
            {
                string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
                SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
                DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{tableName}'");

                if (t.Rows.Count == 0) return $"Error: Table does not exist {tableName}";

                string tableDirectory;

                if (t.Rows[0]["storage"].ToString() == "main")
                {
                    tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + tableName;
                }
                else
                {
                    tableDirectory = t.Rows[0]["storage"].ToString();
                }

                tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + tableName.Trim().ToLower() + ".db"}" + ";version = 3";
                SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

                DataTable p;

                p = sqliteTable.SQLTable($"SELECT * FROM partitions WHERE partition = '{partiton}' ORDER BY partition");

                if (p.Rows.Count == 0)
                {
                    return $"Error: Partition {partiton} does not exist";
                }

                string result = sqliteTable.SQLExec($"UPDATE partitions SET sync_timestamp = '{timestamp}' WHERE partition = '{partiton}'");

                return result;

            }
            catch (Exception e)
            {
                return $"Error: Partitions {tableConnectionString} {e}";
            }
        }


        public static string CompressPartition(Dictionary<string, string> d, DB mainClass) 
        {
            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
            {
                if (mainClass.UserTables != "null")
                {
                    if (!mainClass.UserTables.Contains(d["from_table"]))
                    {
                        return $"Error: You do not have permissions to read this table {d["from_table"]}";
                    }
                };
            }

            string result = RunPartitionRule(d["from_table"], d["compress_partition"], mainClass, d);
            if (result.StartsWith("Error:")) return $"Error: On partition rule {d["compress_partition"]}: {result.Replace("Error:", "")}";
            if (!result.StartsWith("CONTINUE:")) return result;

            result = mainClass.Prompt("GET PARTITIONS FROM TABLE " + d["from_table"] + " WHERE partition = '" + d["compress_partition"] + "'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "[]")
            {
                return $"Error: There is no partition {d["compress_partition"]} on table {d["from_table"]}";
            }

            DataTable dt = mainClass.GetDataTable(result);

            if (dt.Rows[0]["compressed"].ToString() == "1") 
            {
                return "Ok.";
            }

            string partitionFile = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database + mainClass.os_directory_separator + d["from_table"] + mainClass.os_directory_separator + d["from_table"] + "_" + d["compress_partition"] + ".db";

            if (!File.Exists(partitionFile))
            {
                return $"Error: Partition file does not exist {partitionFile}";
            }

            string tableFile = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database + mainClass.os_directory_separator + d["from_table"] + mainClass.os_directory_separator + d["from_table"] + ".db";

            if (!File.Exists(tableFile))
            {
                return $"Error: Master Table file does not exist {tableFile}";
            }

            string lockFile = Path.ChangeExtension(partitionFile, "lock");

            result = IsTableLocked(lockFile, mainClass, d["from_table"], d["compress_partition"]);

            if (result.StartsWith("Error:"))
            {
                return "Error: Compress Partition is locked: " + d["from_table"] + "_" + d["compress_partition"];
            }

            string account_database_table_partition = mainClass.account + "-" + mainClass.database + "-" + d["from_table"] + "-" + d["compress_partition"].Trim().ToLower();

            if (mainClass.SQLiteConnections.ContainsKey(account_database_table_partition))
            {
                mainClass.SQLiteConnections[account_database_table_partition].CloseConnection();
                mainClass.SQLiteConnections.TryRemove(account_database_table_partition, out _);
            }

            string zipPath = Path.ChangeExtension(partitionFile, ".zip");

            result = mainClass.Prompt("LOCK TABLE " + d["from_table"] + " PARTITION KEY " + d["compress_partition"]);

            if (result.StartsWith("Error:"))
            {
                return $"Error: Locking table {d["from_table"]} partition {d["compress_partition"]}: " + result;
            }

            try
            {
                CompressFile(partitionFile, zipPath);
            }
            catch (Exception e)
            {
                mainClass.Prompt("UNLOCK TABLE " + d["from_table"] + " PARTITION KEY " + d["compress_partition"]);
                return $"Error: Compressing partition {partitionFile} to {zipPath}: " + e.Message;
            }

            if (File.Exists(zipPath))
            {
                File.Delete(partitionFile);
            }

            SqliteTools sqliteTools = new SqliteTools("Data Source = " + tableFile + ";version = 3");
            result = sqliteTools.SQLExec($"UPDATE partitions SET compressed = '1' WHERE partition = '{d["compress_partition"]}' AND tablename = '{d["from_table"]}'");

            if(result != "Ok.")
            {
                return $"Error: Updating partition {d["from_table"]} {d["compress_partition"]} {result}";
            }

            result = mainClass.Prompt("UNLOCK TABLE " + d["from_table"] + " PARTITION KEY " + d["compress_partition"]);

            if (result.StartsWith("Error:"))
            {
                return $"Error: Unlocking table {d["from_table"]} partition {d["compress_partition"]}: " + result;
            }

            mainClass.partitions.Clear();
            mainClass.SQLiteConnections.Clear();

            return "Ok.";

        }

        public static string DeCompressPartition(Dictionary<string, string> d, DB mainClass) 
        {
            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
            {
                if (mainClass.UserTables != "null")
                {
                    if (!mainClass.UserTables.Contains(d["from_table"]))
                    {
                        return $"Error: You do not have permissions to read this table {d["from_table"]}";
                    }
                };
            }

            string result = RunPartitionRule(d["from_table"], d["decompress_partition"], mainClass, d);
            if (result.StartsWith("Error:")) return $"Error: On partition rule {d["decompress_partition"]}: {result.Replace("Error:", "")}";
            if (!result.StartsWith("CONTINUE:")) return result;

            result = mainClass.Prompt("GET PARTITIONS FROM TABLE " + d["from_table"] + " WHERE partition = '" + d["decompress_partition"] + "'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "[]")
            {
                return $"Error: There is no partition {d["decompress_partition"]} on table {d["from_table"]}";
            }

            DataTable dt = mainClass.GetDataTable(result);

            if (dt.Rows[0]["compressed"].ToString() != "1")
            {
                return $"Error: The partition is not compressed, {d["from_table"]} partition {d["decompress_partition"]}";
            }

            string table = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database + mainClass.os_directory_separator + d["from_table"] + mainClass.os_directory_separator + d["from_table"].Trim().ToLower() + ".db";
            string zipFile = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database + mainClass.os_directory_separator + d["from_table"] + mainClass.os_directory_separator + d["from_table"] + "_" + d["decompress_partition"] + ".zip";

            if (!File.Exists(zipFile))
            {
                return $"Error: Partition file does not exist {zipFile}";
            }

            string lockFile = Path.ChangeExtension(zipFile, "lock");
            result = IsTableLocked(lockFile, mainClass, d["from_table"], d["decompress_partition"]);

            if (result.StartsWith("Error:"))
            {
                return "Error: Compress Partition is locked: " + d["from_table"] + "_" + d["decompress_partition"] + " " + result;
            }

            result = mainClass.Prompt("LOCK TABLE " + d["from_table"] + " PARTITION KEY " + d["decompress_partition"]);

            if (result.StartsWith("Error:"))
            {
                return $"Error: Locking table {d["from_table"]} partition {d["decompress_partition"]}: " + result;
            }

            string PartitionFile = Path.ChangeExtension(zipFile, ".db");


            try
            {
                DecompressFile(zipFile, Path.GetDirectoryName( PartitionFile ) );
            }
            catch (Exception e)
            {
                mainClass.Prompt("UNLOCK TABLE " + d["from_table"] + " PARTITION KEY " + d["decompress_partition"]);
                return $"Error: Uncompressing partition {zipFile} to {PartitionFile}: " + e.Message;
            }

            if (File.Exists(zipFile))
            {
                File.Delete(zipFile);
            }

            SqliteTools sqliteTools = new SqliteTools($"Data Source = {table};version = 3");
            result = sqliteTools.SQLExec($"UPDATE partitions SET compressed = '0' WHERE partition = '{d["decompress_partition"]}' AND tablename = '{d["from_table"]}'");

            if (result.StartsWith("Error:")) 
            {
                return $"Error: Updating partition {d["from_table"]} {d["decompress_partition"]} {result}";
            }

            result = mainClass.Prompt("UNLOCK TABLE " + d["from_table"] + " PARTITION KEY " + d["decompress_partition"]);

            if (result.StartsWith("Error:"))
            {
                return $"Error: Unlocking table {d["from_table"]} partition {d["decompress_partition"]}: " + result;
            }

            mainClass.partitions.Clear();
            mainClass.SQLiteConnections.Clear();

            return "Ok.";

        }

        public static void CompressFile(string filePath, string zipPath)
        {
            using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath), CompressionLevel.Optimal);
                }
            }
        }

        public static void DecompressFile(string zipPath, string extractPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destinationPath = Path.Combine(extractPath, entry.FullName);
                    entry.ExtractToFile(destinationPath, true);
                }
            }
        }


        public static string MovePartition(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
            {
                if (mainClass.UserTables != "null")
                {
                    if (!mainClass.UserTables.Contains(d["from_table"]))
                    {
                        return $"Error: You do not have permissions to read this table {d["from_table"]}";
                    }
                };
            }

            string result = RunPartitionRule(d["from_table"], d["move_partition"], mainClass, d);
            if (result.StartsWith("Error:")) return $"Error: On partition rule {d["move_partition"]}: {result.Replace("Error:", "")}";
            if (!result.StartsWith("CONTINUE:")) return result;

            try
            {
                result = mainClass.Prompt("GET PARTITIONS FROM TABLE " + d["from_table"] + " WHERE partition = '" + d["move_partition"] + "'");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                if (result == "[]")
                {
                    return $"Error: There is no partition {d["move_partition"]} on table {d["from_table"]}";
                }

                DB db_newpartition = new DB();

                result = db_newpartition.Prompt(d["to_connection"]);

                if (result.StartsWith("Error:"))
                {
                    return "Error: connecting to the target connection: " + result.Replace("Error:", "");
                }

                if (d["to_connection"].StartsWith("ANGEL"))
                {
                    db_newpartition.Prompt("ALWAYS USE ANGELSQL");
                }

                result = mainClass.Prompt($"GET TABLES WHERE tablename = '{d["from_table"]}'");

                if (result.StartsWith("Error:"))
                {
                    return "Error: getting table information: " + result.Replace("Error:", "");
                }

                if (result == "[]")
                {
                    return "Error:" + result.Replace("Error:", "");
                }

                DataTable dt = db_newpartition.GetDataTable(result);

                result = db_newpartition.Prompt($"GET TABLES WHERE tablename = '{d["from_table"]}'");

                if (result.StartsWith("Error:"))
                {
                    return "Error: getting table information: " + result.Replace("Error:", "");
                }

                if (result == "[]")
                {
                    result = db_newpartition.Prompt($"CREATE TABLE {d["from_table"]} FIELD LIST {dt.Rows[0]["fieldlist"]}");

                    if (result.StartsWith("Error:"))
                    {
                        return "Error: Creating partition: " + result.Replace("Error:", "");
                    }
                }
                else
                {

                    DataTable dt2 = db_newpartition.GetDataTable(result);

                    if (dt.Rows[0]["fieldlist"].ToString() != dt2.Rows[0]["fieldlist"].ToString())
                    {
                        result = db_newpartition.Prompt($"CREATE TABLE {d["from_table"]} FIELD LIST {dt.Rows[0]["fieldlist"]}");

                        if (result.StartsWith("Error:"))
                        {
                            return "Error: Creating partition: " + result.Replace("Error:", "");
                        }
                    }
                }

                string timestamp = "0000-00-00 00:00:00.0000000";
                int records = 0;

                mainClass.Prompt("LOCK TABLE " + d["from_table"] + " PARTITION KEY " + d["move_partition"]);

                while (true)
                {
                    result = mainClass.Prompt($"SELECT * FROM {d["from_table"]} PARTITION KEY {d["move_partition"]} WHERE timestamp > '{timestamp}' ORDER BY timestamp LIMIT 1000 READ LOCKED");

                    if (result.StartsWith("Error:"))
                    {
                        mainClass.Prompt("UNLOCK TABLE " + d["from_table"] + " PARTITION KEY " + d["move_partition"]);
                        return $"Error: Retrieving information from the table {d["from_table"]}: " + result.Replace("Error:", "");
                    }

                    if (result == "[]")
                    {
                        break;
                    }

                    DataTable dt2 = db_newpartition.GetDataTable(result);
                    timestamp = dt2.Rows[dt2.Rows.Count - 1]["timestamp"].ToString();

                    result = db_newpartition.Prompt($"UPSERT INTO {d["from_table"]} PARTITION KEY {d["move_partition"]} VALUES '''" + result + "'''");

                    if (result.StartsWith("Error:"))
                    {
                        mainClass.Prompt("UNLOCK TABLE " + d["from_table"] + " PARTITION KEY " + d["move_partition"]);
                        return $"Error: Update information on destination from the table {d["from_table"]} {d["move_partition"]}: " + result.Replace("Error:", "");
                    }

                    records += dt2.Rows.Count;

                    if (d["verbose"] == "true")
                    {
                        Console.WriteLine("Moving partition: " + d["move_partition"] + "->" + records.ToString());
                    }


                }

                result = mainClass.Prompt($"DELETE PARTITIONS FROM TABLE {d["from_table"]} WHERE partition = '{d["move_partition"]}' ONLY FILES");

                if (result.StartsWith("Error:"))
                {
                    mainClass.Prompt("UNLOCK TABLE " + d["from_table"] + " PARTITION KEY " + d["move_partition"]);
                    return $"Error: Deleting partition: " + result.Replace("Error:", "");
                }

                result = mainClass.Prompt($"CREATE PARTITION RULE TABLE {d["from_table"]} PARTITION KEY {d["move_partition"]} CONNECTION {d["to_connection"]}");

                if (result.StartsWith("Error:"))
                {
                    mainClass.Prompt("UNLOCK TABLE " + d["from_table"] + " PARTITION KEY " + d["move_partition"]);
                    return $"Error: Creating partition rule: " + result.Replace("Error:", "");
                }

                mainClass.Prompt("UNLOCK TABLE " + d["from_table"] + " PARTITION KEY " + d["move_partition"]);

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: Move partition {d["move_partition"]} on table {d["from_table"]} {e}";
            }
        }



        public static string UpdatePartitionTimeStamp(Dictionary<string, string> d, DB mainClass)
        {
            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string tableConnectionString = "";

            try
            {
                string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
                SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
                DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["from_table"]}'");

                if (t.Rows.Count == 0) return $"Error: Table does not exist {d["from_table"]}";

                string tableDirectory;

                if (t.Rows[0]["storage"].ToString() == "main")
                {
                    tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["from_table"];
                }
                else
                {
                    tableDirectory = t.Rows[0]["storage"].ToString();
                }

                tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["from_table"].Trim().ToLower() + ".db"}" + ";version = 3";
                SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

                //UPDATE PARTITION

                DataTable p = sqliteTable.SQLTable($"SELECT partition FROM partitions WHERE partition = '{d["update_partition"]}'");

                sqliteTable.Reset();
                sqliteTable.CreateUpdate("partitions", $"partition = '{d["update_partition"]}'");
                sqliteTable.AddField("partition", d["update_partition"]);
                sqliteTable.AddField("tablename", d["from_table"].Trim().ToLower());
                sqliteTable.AddField("sync_timestamp", d["time_stamp"]);
                return sqliteTable.Exec();

            }
            catch (Exception e)
            {
                return $"Error: Partitions {tableConnectionString} {e}";
            }
        }

        public static string GetMaxSyncTimeStampFromTable(Dictionary<string, string> d, DB mainClass)
        {
            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string tableConnectionString = "";

            try
            {
                string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
                SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
                DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["from_table"]}'");

                if (t.Rows.Count == 0) return $"Error: Table does not exist {d["from_table"]}";

                string tableDirectory;

                if (t.Rows[0]["storage"].ToString() == "main")
                {
                    tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["from_table"];
                }
                else
                {
                    tableDirectory = t.Rows[0]["storage"].ToString();
                }

                tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["from_table"].Trim().ToLower() + ".db"}" + ";version = 3";
                SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

                sqliteTable.SQLExec("ALTER TABLE partitions ADD COLUMN sync_timestamp");
                sqliteTable.SQLExec($"CREATE INDEX IF NOT EXISTS {d["from_table"]}_timestamp ON partitions (timestamp)");
                sqliteTable.SQLExec($"CREATE INDEX IF NOT EXISTS {d["from_table"]}_sync_timestamp ON partitions (sync_timestamp)");

                DataTable p;

                if (d["where"] == "null")
                {
                    p = sqliteTable.SQLTable($"SELECT partition, max(sync_timestamp) as 'maxtime' FROM partitions ORDER BY partition");
                }
                else
                {
                    p = sqliteTable.SQLTable($"SELECT partition, max(sync_timestamp) as 'maxtime' FROM partitions WHERE {d["where"]} ORDER BY partition");
                }

                if (p.Rows.Count == 0)
                {
                    return "0000-00-00 00:00:00.0000000";
                }

                if (string.IsNullOrEmpty(p.Rows[0]["maxtime"].ToString()))
                {
                    return "0000-00-00 00:00:00.0000000";
                }

                return p.Rows[0]["maxtime"].ToString();

            }
            catch (Exception e)
            {
                return $"Error: Partitions {tableConnectionString} {e}";
            }
        }


        public static string VerifyAccountAndDatabase(DB db)
        {
            if (db.account == "") return "Error: No account selected";
            if (db.database == "") return "Error: No database selected";
            if (db.IsReadOnly == true) return "Error: Your account is read only";

            return "Ok.";
        }

        public static string RunPartitionRule(string table_name, string partition, DB mainClass, Dictionary<string, string> d)
        {
            try
            {
                SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);
                DataTable t = mainClass.MainDatabase.SQLTable($"SELECT * FROM partitionrules WHERE account = '{mainClass.account}' AND database = '{mainClass.database}' AND table_name = '{table_name}' AND partition = '{partition}'");

                if (t.Rows.Count == 0)
                {
                    return "CONTINUE:";
                }

                DataRow r = t.Rows[0];

                string new_command = UpdatePartitionKey(mainClass.Command, partition);

                return RemotePrompt(table_name, partition, r["account"].ToString(), r["database"].ToString(), r["connection_string"].ToString(), mainClass, new_command);

            }
            catch (Exception e)
            {
                return $"Error: RunPartition {e}";
            }
        }


        static string RemotePrompt(string table_name, string partition, string account, string database, string connection_string, DB mainClass, string command = "")
        {

            DB localdb;
            string result;
            string key = account + "-" + database + "-" + table_name + "-" + partition;

            if (!mainClass.partitionsrules.ContainsKey(key))
            {
                localdb = new DB();
                result = localdb.Prompt(connection_string);

                if (result.StartsWith("Error:")) return result;

                if (connection_string.Trim().StartsWith("DB USER"))
                {
                    result = localdb.Prompt("ANGEL STOP");
                }

                if (connection_string.Trim().StartsWith("ANGEL"))
                {
                    result = localdb.Prompt("ALWAYS USE ANGELSQL");
                }

                //result = localdb.Prompt("SELECT * FROM test1");
                result = localdb.Prompt($"GET TABLES WHERE tablename = '{table_name}'");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                mainClass.save_command = false;
                result = mainClass.Prompt($"GET TABLES WHERE tablename = '{table_name}'");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                mainClass.save_command = true;
                if (result == "[]") return $"Error: The indicated table does not exist: {table_name}";

                //DataRow r = JsonConvert.DeserializeObject<DataTable>(result).Rows[0];
                //result = localdb.Prompt($"CREATE TABLE {table_name} FIELD LIST {r["fieldlist"]} TYPE {r["tabletype"].ToString().ToLower()}");

                //if (result.StartsWith("Error:"))
                //{
                //    return result;
                //}

                mainClass.partitionsrules.Add(key, localdb);

            }

            if( string.IsNullOrEmpty(command))
            {
                return mainClass.partitionsrules[key].Prompt(mainClass.Command);
            }
            else
            {
                return mainClass.partitionsrules[key].Prompt(command);
            }

        }


        public static string UpdatePartitionKey(string query, string newPartition)
        {
            string partitionKeyword = "PARTITION KEY ";
            int partitionIndex = query.IndexOf(partitionKeyword, StringComparison.Ordinal); // Case-sensitive

            if (partitionIndex != -1)
            {
                // Encuentra el final del nombre de la partición actual
                int start = partitionIndex + partitionKeyword.Length;
                int end = query.IndexOf(' ', start);
                end = (end == -1) ? query.Length : end;

                // Reemplaza la partición actual por la nueva
                return query.Substring(0, start) + newPartition + query.Substring(end);
            }
            else
            {
                // Busca "FROM nombre_tabla" y solo agrega PARTITION KEY en la primera ocurrencia
                string fromKeyword = "FROM ";
                int fromIndex = query.IndexOf(fromKeyword, StringComparison.Ordinal); // Case-sensitive

                if (fromIndex != -1)
                {
                    int tableNameEnd = query.IndexOf(' ', fromIndex + fromKeyword.Length);
                    tableNameEnd = (tableNameEnd == -1) ? query.Length : tableNameEnd;

                    // Inserta "PARTITION KEY newPartition" después del nombre de la tabla
                    return query.Insert(tableNameEnd, " " + partitionKeyword + newPartition);
                }
            }

            // Si no encontró FROM (query mal formada), devuelve la misma query sin cambios
            return query;
        }


        public static string LockTable(Dictionary<string, string> d, DB mainClass)
        {
            try
            {
                if (!mainClass.IsLogged)
                {
                    return $"Error: You have not indicated your username and password";
                }

                if (mainClass.account == "") return "Error: No account selected";
                if (mainClass.database == "") return "Error: No database selected";

                if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
                {
                    if (mainClass.UserTables != "null")
                    {
                        if (!mainClass.UserTables.Contains(d["lock_table"]))
                        {
                            return $"Error: You do not have permissions to read this table {d["lock_table"]}";
                        }
                    };
                }

                if (d["partition_key"] == "null")
                {
                    d["partition_key"] = "main";
                }

                d["lock_table"] = d["lock_table"].Trim().ToLower();

                string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
                string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
                SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
                DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["lock_table"]}'");

                if (t.Rows.Count == 0) return $"Error: Table does not exist: {d["lock_table"]}";

                string tableDirectory;

                if (t.Rows[0]["storage"].ToString() == "main")
                {
                    tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["lock_table"];
                }
                else
                {
                    tableDirectory = t.Rows[0]["storage"].ToString();
                }

                string lockFile = tableDirectory + mainClass.os_directory_separator + d["lock_table"] + "_" + d["partition_key"] + ".lock";

                string result = IsTableLocked(lockFile, mainClass, d["lock_table"], d["partition_key"]);

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                if (File.Exists(lockFile))
                {
                    File.Delete(lockFile);
                }

                if (mainClass.LockCode == "") 
                {
                    mainClass.LockCode = Guid.NewGuid().ToString();
                }

                File.WriteAllText(lockFile, mainClass.LockCode);
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: LockTable {e}";
            }

        }

        public static string UnLockTable(Dictionary<string, string> d, DB mainClass)
        {
            try
            {
                if (!mainClass.IsLogged)
                {
                    return $"Error: You have not indicated your username and password";
                }

                if (mainClass.account == "") return "Error: No account selected";
                if (mainClass.database == "") return "Error: No database selected";

                if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
                {
                    if (mainClass.UserTables != "null")
                    {
                        if (!mainClass.UserTables.Contains(d["unlock_table"]))
                        {
                            return $"Error: You do not have permissions to read this table {d["unlock_table"]}";
                        }
                    };
                }

                if (d["partition_key"] == "null")
                {
                    d["partition_key"] = "main";
                }

                d["unlock_table"] = d["unlock_table"].Trim().ToLower();

                string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
                string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db"}" + ";version = 3";
                SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
                DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["unlock_table"]}'");

                if (t.Rows.Count == 0) return $"Error: Table does not exist: {d["unlock_table"]}";

                string tableDirectory;

                if (t.Rows[0]["storage"].ToString() == "main")
                {
                    tableDirectory = mainClass.BaseDirectory + "/" + mainClass.account + "/" + mainClass.database + "/" + d["unlock_table"];
                }
                else
                {
                    tableDirectory = t.Rows[0]["storage"].ToString();
                }

                string lockFile = tableDirectory + mainClass.os_directory_separator + d["unlock_table"] + "_" + d["partition_key"] + ".lock";

                string result = IsTableLocked(lockFile, mainClass, d["unlock_table"], d["partition_key"]);

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                if (File.Exists(lockFile))                
                {

                    if(File.ReadAllText(lockFile) != mainClass.LockCode)
                    {
                        return "Error: Lock code does not match.";
                    }

                    File.Delete(lockFile);
                }

                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: LockTable {e}";
            }

        }


        public static string IsTableLocked(string file, AngelDB.DB mainClass, string table, string partition)
        {

            if (File.Exists(file))
            {
                string lockCode = File.ReadAllText(file);

                if (lockCode == mainClass.LockCode)
                {
                    return "Ok.";
                }

                for (int i = 0; i < 100; i++)
                {
                    System.Threading.Thread.Sleep(100);
                    if (!File.Exists(file))
                    {
                        return "Ok.";
                    }
                }

                return $"Error: Table {table} partition {partition} is already locked";
            }
            else 
            {
                return "Ok.";
            }

        }

        private class FileResult
        {
            public string FileName { get; set; } = "";
            public int RecordsReturnet { get; set; } = 0;
            public int PartitionsIncluded { get; set; } = 0;
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string TimeElapsed { get; set; } = "";
            public string Query { get; set; } = "";
            public string Error { get; set; } = "";
        }


    }

}
