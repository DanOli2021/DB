using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;

namespace AngelDB {

    public static class FileStorage
    {
        public static string SendTo(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            if (!File.Exists(d["file"]))
            {
                return $"Error: The file does not exists {d["file"]}";
            }

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db" };";
            SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
            DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["send_to"]}'");

            if (t.Rows.Count == 0) return $"Error: Table does not exist {d["send_to"]}";

            string tableDirectory = t.Rows[0]["storage"].ToString();

            if (string.IsNullOrEmpty(d["partition_key"])) return "Error: No partitionkey given";
            if (string.IsNullOrEmpty(d["id"])) return "Error: No id given";

            string partitionFile = tableDirectory + mainClass.os_directory_separator + d["send_to"] + "_" + d["partition_key"].Trim().ToLower() + ".db";
            string tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["send_to"].Trim().ToLower() + ".db" };";
            SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

            DataTable p = sqliteTable.SQLTable($"SELECT * FROM partitions WHERE partition = '{d["partition_key"]}'");

            string result;

            if (p.Rows.Count == 0)
            {
                return $"Error: partitions does not exists {d["partition_key"]}";
            }

            string ConnectionString = $"Data Source={partitionFile };";
            SqliteTools sqlite = new SqliteTools(ConnectionString);


            if (!File.Exists(partitionFile))
            {
                return $"Error: partition file does not exists {d["partition_key"]}";
            }

            t = sqlite.SQLTable($"SELECT id FROM {d["send_to"]} WHERE partitionkey = '{d["partition_key"]}' AND {d["where"]}");


            if (t.Rows.Count == 0)
            {
                return $"Error: data does not exists, you need to add a record first {d["partition_key"]}";
            }

            foreach (DataRow r in t.Rows)
            {
                sqlite.Reset();
                sqlite.CreateUpdate(d["send_to"], $"partitionkey = '{d["partition_key"]}' AND id = '{r["id"]}'");
                sqlite.AddField("file", Path.GetFileName(d["file"]));
                result = sqlite.Exec();

                if (result != "Ok.")
                {
                    return $"Error: We couldn't add the record ID {r["id"]} ({result}) ";
                }
            }

            string zipFile = tableDirectory + mainClass.os_directory_separator + d["send_to"] + "_" + d["partition_key"].Trim().ToLower() + ".zip";

            ZipArchive zip;

            if (!File.Exists(zipFile))
            {
                zip = ZipFile.Open(zipFile, ZipArchiveMode.Create);
            }
            else
            {
                zip = ZipFile.Open(zipFile, ZipArchiveMode.Update);
            }


            string fileName = Path.GetFileName(d["file"]);

            ZipArchiveEntry entry = zip.GetEntry(fileName);

            if (zip.Entries.Contains(entry))
            {
                entry.Delete();
            }

            zip.CreateEntryFromFile(d["file"], fileName);

            zip.Dispose();
            return "Ok.";

        }

        public static string GetFileAsBase64(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db" };";
            SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
            DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["from"]}'");

            if (t.Rows.Count == 0) return $"Error: Table does not exist {d["from"]}";

            string tableDirectory = t.Rows[0]["storage"].ToString();

            if (string.IsNullOrEmpty(d["partition_key"])) return "Error: No partitionkey given";

            string partitionFile = tableDirectory + mainClass.os_directory_separator + d["from"] + "_" + d["partition_key"].Trim().ToLower() + ".db";
            string tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["from"].Trim().ToLower() + ".db" };";
            SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

            DataTable p = sqliteTable.SQLTable($"SELECT * FROM partitions WHERE partition = '{d["partition_key"]}'");

            if (p.Rows.Count == 0)
            {
                return $"Error: partitions does not exists {d["partition_key"]}";
            }

            string ConnectionString = $"Data Source={partitionFile };";
            SqliteTools sqlite = new SqliteTools(ConnectionString);

            if (!File.Exists(partitionFile))
            {
                return $"Error: partition file does not exists {d["partition_key"]}";
            }

            t = sqlite.SQLTable($"SELECT file FROM {d["from"]} WHERE partitionkey = '{d["partition_key"]}' AND {d["where"]}");

            if (t.Rows.Count == 0)
            {
                return $"Error: data does not exists, you need to add a record first {d["partition_key"]}";
            }

            string zipFile = tableDirectory + mainClass.os_directory_separator + d["from"] + "_" + d["partition_key"].Trim().ToLower() + ".zip";
            ZipArchive zip;

            if (!File.Exists(zipFile))
            {
                return $"Error: Zip file does not exists: {zipFile}";
            }

            zip = ZipFile.Open(zipFile, ZipArchiveMode.Read);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            string tempDirectory = mainClass.BaseDirectory + "temp";

            if (!Directory.Exists(tempDirectory)) {
                Directory.CreateDirectory(tempDirectory);
            }

            Dictionary<string, string> results = new Dictionary<string, string>();

            t.Columns.Add("base64");

            foreach (DataRow r in t.Rows)
            {
                string fileName = r["file"].ToString();
                ZipArchiveEntry entry = zip.GetEntry(fileName);

                if (zip.Entries.Contains(entry))
                {
                    try
                    {
                        if (!results.ContainsKey("fileName")) {

                            if (File.Exists(tempDirectory + mainClass.os_directory_separator + fileName)) {
                                File.Delete(tempDirectory + mainClass.os_directory_separator + fileName);
                            }

                            entry.ExtractToFile(tempDirectory + mainClass.os_directory_separator + fileName);
                            r["base64"] = Convert.ToBase64String(File.ReadAllBytes(tempDirectory + mainClass.os_directory_separator + fileName));
                        }
                    }
                    catch (System.Exception)
                    {
                        sb.AppendLine("Extract file: " + r["file"]);
                    }
                }
            }

            string result = sb.ToString();

            if (result != "")
            {
                return "Error: " + result;
            }

            zip.Dispose();
            return JsonConvert.SerializeObject(t);
    }


        public static string DeleteFile(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db" };";
            SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
            DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["from"]}'");

            if (t.Rows.Count == 0) return $"Error: Table does not exist {d["from"]}";

            string tableDirectory = t.Rows[0]["storage"].ToString();

            if (string.IsNullOrEmpty(d["partition_key"])) return "Error: No partitionkey given";

            string partitionFile = tableDirectory + mainClass.os_directory_separator + d["from"] + "_" + d["partition_key"].Trim().ToLower() + ".db";
            string tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["from"].Trim().ToLower() + ".db" };";
            SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

            DataTable p = sqliteTable.SQLTable($"SELECT * FROM partitions WHERE partition = '{d["partition_key"]}'");

            if (p.Rows.Count == 0)
            {
                return $"Error: partitions does not exists {d["partition_key"]}";
            }

            string ConnectionString = $"Data Source={partitionFile };";
            SqliteTools sqlite = new SqliteTools(ConnectionString);

            if (!File.Exists(partitionFile))
            {
                return $"Error: partition file does not exists {d["partition_key"]}";
            }

            t = sqlite.SQLTable($"SELECT file FROM {d["from"]} WHERE partitionkey = '{d["partition_key"]}' AND {d["where"]}");

            if (t.Rows.Count == 0)
            {
                return $"Error: data does not exists, you need to add a record first {d["partition_key"]}";
            }

            string zipFile = tableDirectory + mainClass.os_directory_separator + d["from"] + "_" + d["partition_key"].Trim().ToLower() + ".zip";
            ZipArchive zip;

            if (!File.Exists(zipFile))
            {
                return $"Error: Zip file does not exists: {zipFile}";
            }

            zip = ZipFile.Open(zipFile, ZipArchiveMode.Read);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (DataRow r in t.Rows)
            {
                string fileName = r["file"].ToString();
                ZipArchiveEntry entry = zip.GetEntry(fileName);

                if (zip.Entries.Contains(entry))
                {
                    try
                    {
                        entry.Delete();
                    }
                    catch (System.Exception)
                    {
                        sb.AppendLine("Deleye file: " + r["file"]);
                    }
                }
            }

            string result = sb.ToString();

            if (result != "")
            {
                return "Error: " + result;
            }

            zip.Dispose();
            return "Ok.";
        }


        public static string CopyFrom(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            string databaseDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + mainClass.database;
            string databaseConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db" };";
            SqliteTools sqliteDatabase = new SqliteTools(databaseConnectionString);
            DataTable t = sqliteDatabase.SQLTable($"SELECT * FROM tables WHERE tablename = '{d["copy_from"]}'");

            if (t.Rows.Count == 0) return $"Error: Table does not exist {d["copy_from"]}";

            string tableDirectory = t.Rows[0]["storage"].ToString();

            if (string.IsNullOrEmpty(d["partition_key"])) return "Error: No partitionkey given";

            string partitionFile = tableDirectory + mainClass.os_directory_separator + d["copy_from"] + "_" + d["partition_key"].Trim().ToLower() + ".db";
            string tableConnectionString = $"Data Source={tableDirectory + mainClass.os_directory_separator + d["copy_from"].Trim().ToLower() + ".db" };";
            SqliteTools sqliteTable = new SqliteTools(tableConnectionString);

            DataTable p = sqliteTable.SQLTable($"SELECT * FROM partitions WHERE partition = '{d["partition_key"]}'");

            if (p.Rows.Count == 0)
            {
                return $"Error: partitions does not exists {d["partition_key"]}";
            }

            string ConnectionString = $"Data Source={partitionFile };";
            SqliteTools sqlite = new SqliteTools(ConnectionString);


            if (!File.Exists(partitionFile))
            {
                return $"Error: partition file does not exists {d["partition_key"]}";
            }

            t = sqlite.SQLTable($"SELECT file FROM {d["copy_from"]} WHERE partitionkey = '{d["partition_key"]}' AND {d["where"]}");


            if (t.Rows.Count == 0)
            {
                return $"Error: data does not exists, you need to add a record first {d["partition_key"]}";
            }

            string zipFile = tableDirectory + mainClass.os_directory_separator + d["copy_from"] + "_" + d["partition_key"].Trim().ToLower() + ".zip";
            ZipArchive zip;

            if (!File.Exists(zipFile))
            {
                return $"Error: Zip file does not exists: {zipFile}";
            }

            zip = ZipFile.Open(zipFile, ZipArchiveMode.Read);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (DataRow r in t.Rows)
            {
                string fileName = r["file"].ToString();
                ZipArchiveEntry entry = zip.GetEntry(fileName);

                if (zip.Entries.Contains(entry))
                {
                    try
                    {
                        if (d["overwrite"] == "true")
                        {
                            if (File.Exists(d["directory"] + mainClass.os_directory_separator + fileName))
                            {
                                File.Delete(d["directory"] + mainClass.os_directory_separator + fileName);
                            }
                        }
                        else
                        {
                            if (File.Exists(d["directory"] + mainClass.os_directory_separator + fileName))
                            {
                                sb.AppendLine("File already exists: " + d["directory"] + mainClass.os_directory_separator + fileName + " --> " + fileName);
                                continue;
                            }
                        }

                        entry.ExtractToFile(d["directory"] + mainClass.os_directory_separator + fileName);

                    }
                    catch (System.Exception)
                    {
                        sb.AppendLine("Extract file: " + d["directory"] + mainClass.os_directory_separator + fileName + " --> " + fileName);
                    }
                }
            }

            string result = sb.ToString();

            if (result != "")
            {
                return "Error: " + result;
            }

            zip.Dispose();
            return "Ok.";

        }


        public static string WriteToConsole(Dictionary<string, string> d, DB mainClass) {


            string result = mainClass.Prompt(d["console"]);

            if (result.StartsWith("Error:"))
            {
                Monitor.ShowLine(result, ConsoleColor.Red);
                return result;
            }
            else 
            {
                Monitor.ShowLine(result, ConsoleColor.Green);
                return result;
            }                

        }

        public static string WriteToFile(Dictionary<string, string> d, DB mainClass)
        {
            string result = mainClass.Prompt(d["write_results_from"]);

            try
            {
                File.WriteAllText(d["to_file"], result);
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: Write to file: {e}";
            }

        }




    }

}
