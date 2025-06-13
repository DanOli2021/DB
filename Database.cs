using System.Data;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AngelDB
{

    public static class Database
    {
        public static string ActiveDatabase(DB mainClass)
        {
            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            return mainClass.database;
        }

        public static string CreateDatabase(Dictionary<string, string> d, DB mainClass, string baseDirectory)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if ( mainClass.database.Trim().ToLower() ==  d["create_database"].Trim().ToLower() ) 
            {
                return "Ok.";
            }

            if (string.IsNullOrEmpty(mainClass.account)) return "Error: No acount selected";

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER) return "Error: You do not have permissions to create databases";

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            if (mainClass.accountType == ACCOUNT_TYPE.ACCOUNT_MASTER)
            {
                DataTable t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE user = '{ mainClass.user }'");
                if (t.Rows.Count == 0) return "Error: User does not exist";

                if (mainClass.account != t.Rows[0]["account"].ToString().Trim())
                {
                    return "Error: You cannot create databases in an account that is not yours";
                }
            }

            DataTable tDataTable = sqlite.SQLTable($"SELECT * FROM databases WHERE database = '{ d["create_database"] }' AND account = '{mainClass.account}'");

            if (tDataTable.Rows.Count > 0)
            {
                return "Ok.";
            }

            string databaseDirectory = baseDirectory + mainClass.os_directory_separator + mainClass.account + mainClass.os_directory_separator + d["create_database"];

            if (!Directory.Exists(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }

            string ConnectionString = $"Data Source={databaseDirectory + mainClass.os_directory_separator + "tables.db" };";
            string result = Dataconfig.CreateTablesDataBase(ConnectionString);

            if (result != "Ok.")
            {
                return result;
            }

            sqlite.Reset();
            sqlite.CreateInsert("databases");
            sqlite.AddField("database", d["create_database"].ToString().Trim().ToLower());
            sqlite.AddField("account", mainClass.account);
            sqlite.AddField("timestamp", System.DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            return sqlite.Exec();

        }

        public static string Use(Dictionary<string, string> d, DB mainClass)
        {
            string result;

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (!System.String.IsNullOrEmpty(mainClass.account) && d["database"] == "null")
            {
                return mainClass.Prompt($"USE DATABASE {d["use"]}");
            }

            result = mainClass.Prompt($"USE ACCOUNT {d["use"]}");

            if (d["database"] != "null")
            {
                result = mainClass.Prompt($"USE DATABASE {d["database"]}");
            }

            return result;

        }



        public static string CreateLogin(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (string.IsNullOrEmpty(mainClass.account)) return "Error: No acount selected";

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER) return "Error: You do not have permissions to create Login";

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            if (mainClass.accountType == ACCOUNT_TYPE.ACCOUNT_MASTER)
            {
                DataTable t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE user = '{ mainClass.user }'");
                if (t.Rows.Count == 0) return "Error: User does not exist";

                if (mainClass.account != t.Rows[0]["account"].ToString().Trim())
                {
                    return "Error: You cannot create databases in an account that is not yours";
                }
            }

            if(d["to_database"] != "null") {
                DataTable tDataTable = sqlite.SQLTable($"SELECT * FROM databases WHERE database = '{ d["to_database"] }' AND account = '{mainClass.account}'");
                if (tDataTable.Rows.Count == 0) return $"Error: Database not exists {d["to_database"]} on account {mainClass.account}";
            } 

            DataTable tUser = sqlite.SQLTable($"SELECT * FROM users WHERE user = '{d["create_login"] + "@" + mainClass.account}'");
            sqlite.Reset();

            if (tUser.Rows.Count == 0)
            {
                sqlite.CreateInsert("users");
            }
            else
            {
                sqlite.CreateUpdate("users", $"user = '{d["create_login"] + "@" + mainClass.account}'");
            }

            sqlite.AddField("account", mainClass.account);
            sqlite.AddField("user", d["create_login"] + "@" + mainClass.account);
            sqlite.AddField("password", d["password"]);
            sqlite.AddField("database", d["to_database"]);
            sqlite.AddField("name", d["name"]);
            sqlite.AddField("readonly", d["read_only"]);
            sqlite.AddField("deleted", "false");
            sqlite.AddField("tables", d["tables"]);
            sqlite.AddField("timestamp", System.DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            string result = sqlite.Exec();

            if (result != "Ok.")
            {
                return "Error: User could not be added: " + result;
            }

            return "Ok.";

        }

        public static string DeleteLogin(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (string.IsNullOrEmpty(mainClass.account)) return "Error: No acount selected";

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER) return "Error: You do not have permissions to create Login";

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            if (mainClass.accountType == ACCOUNT_TYPE.ACCOUNT_MASTER)
            {
                DataTable t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE user = '{ mainClass.user }'");
                if (t.Rows.Count == 0) return "Error: User does not exist";

                if (mainClass.account != t.Rows[0]["account"].ToString().Trim())
                {
                    return "Error: You cannot create databases in an account that is not yours";
                }
            }

            DataTable tUser = sqlite.SQLTable($"SELECT * FROM users WHERE user = '{d["delete_login"]}'");
            sqlite.Reset();

            if (tUser.Rows.Count > 0)
            {
                sqlite.CreateUpdate("users", $"user = '{d["delete_login"]}'");
                sqlite.AddField("deleted", "true");
                sqlite.AddField("timestamp", System.DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
                string result = sqlite.Exec();

                if (result != "Ok.")
                {
                    return $"Error: {result}";
                }
            }
            else 
            {
                return $"Error: The login does not exist {d["delete_login"]}";
            }

            return "Ok.";

        }

        public static string UnDeleteLogin(Dictionary<string, string> d, DB mainClass)
        {

            if (string.IsNullOrEmpty(mainClass.account)) return "Error: No acount selected";
            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER) return "Error: You do not have permissions to create Login";

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            DataTable tUser = sqlite.SQLTable($"SELECT * FROM users WHERE user = '{d["undelete_login"]}'");
            sqlite.Reset();

            if (tUser.Rows.Count > 0)
            {
                sqlite.CreateUpdate("users", $"user = '{d["undelete_login"]}'");
                sqlite.AddField("deleted", "false");
                sqlite.AddField("timestamp", System.DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
                string result = sqlite.Exec();

                if (result != "Ok.")
                {
                    return $"Error: {result}";
                }
            }
            else
            {
                return $"Error: The login does not exist {d["undelete_login"]}";
            }

            return "Ok.";

        }


        public static string ValidateLogin(Dictionary<string, string> d, DB mainClass)
        {

            if (string.IsNullOrEmpty(mainClass.account)) return "Error: No acount selected";

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER) return "Error: You do not have permissions to validate Login";
            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);
            DataTable tUser = sqlite.SQLTable($"SELECT * FROM users WHERE user = '{d["undelete_login"]}'");
            sqlite.Reset();

            if (tUser.Rows.Count > 0)
            {
                if (tUser.Rows[0]["password"].ToString().Trim() == d["password"].Trim().ToLower())
                {
                    return $"Error: The password is wrong";
                }
                else
                {
                    return "Ok.";
                }

            }
            else
            {
                return $"Error: The login does not exist {d["undelete_login"]}";
            }
        }


        public static string UseDatabase(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if ( mainClass.database.Trim().ToLower() ==  d["use_database"].Trim().ToLower() ) 
            {
                return "Ok.";
            }

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER) 
            {
                DataTable tu = sqlite.SQLTable($"SELECT * FROM users WHERE account = '{mainClass.account}' AND user = {mainClass.user} AND deleted = 'false'");

                if (tu.Rows.Count == 0) return "Error: You do not have permissions to use this database";

                if (!string.IsNullOrEmpty(tu.Rows[0]["database"].ToString())) 
                {
                    if (!(tu.Rows[0]["database"].ToString().Trim().ToLower().IndexOf(d["use_database"].Trim().ToLower()) >= 0)) 
                    {
                        return "Error: You do not have permissions to use this database";
                    }                    
                }                
            }            

            DataTable t = sqlite.SQLTable($"SELECT * FROM databases WHERE database = '{ d["use_database"].Trim().ToLower() }' AND account = '{mainClass.account}' AND deleted IS NULL");

            if (t.Rows.Count == 0)
            {
                return "Error: The database does not exist";
            }

            if (t.Rows[0]["deleted"].ToString() == "true")
            {
                return "Error: The database was deleted";
            }

            mainClass.database = d["use_database"].Trim().ToLower();
            return "Ok.";
        }

        public static string DeleteDatabase(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
            {
                return "Error: Only the account owner can delete the database";
            }

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);
            DataTable t = sqlite.SQLTable($"SELECT * FROM databases WHERE database = '{d["delete_database"]}' AND account = '{mainClass.account}'");

            if (t.Rows.Count == 0)
            {
                return "Error: The database does not exist";
            }

            if (t.Rows[0]["deleted"].ToString() == "false")
            {
                return "Error: The database has already been deleted";
            }

            mainClass.database = "";

            sqlite.Reset();
            sqlite.CreateUpdate("databases", $"database = '{d["delete_database"]}' AND account = '{mainClass.account}'");
            sqlite.AddField("database", d["delete_database"].ToString().Trim().ToLower());
            sqlite.AddField("account", mainClass.account);
            sqlite.AddField("deleted", "true");
            sqlite.AddField("timestamp", System.DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            return sqlite.Exec();

        }

        public static string GetUsers(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
            {
                return "Error: Only the account owner can get users";
            }

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);
            DataTable t;

            string order_by = "";

            if (d["order_by"] != "null") 
            {
                order_by = $" ORDER BY {d["order_by"]}";
            }

            if (mainClass.accountType == ACCOUNT_TYPE.ACCOUNT_MASTER)
            {
                if (d["where"] != "null")
                {
                    t = sqlite.SQLTable($"SELECT account, user, name, password, database, tables, readonly, deleted, timestamp FROM users WHERE account = '{mainClass.account}' AND {d["where"]} {order_by}");
                }
                else
                {
                    t = sqlite.SQLTable($"SELECT account, user, name, password, database, tables, readonly, deleted, timestamp FROM users WHERE account = '{mainClass.account}' {order_by}");
                }

            }
            else
            {
                if (d["where"] != "null")
                {
                    t = sqlite.SQLTable($"SELECT account, user, name, password, database, tables, readonly, deleted, timestamp FROM users WHERE {d["where"]} {order_by}");
                }
                else
                {
                    t = sqlite.SQLTable($"SELECT account, user, name, password, database, tables, readonly, deleted, timestamp FROM users {order_by}");
                }
            }

            return JsonConvert.SerializeObject(t, Formatting.Indented);

        }

        public static string GetMasters(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.accountType != ACCOUNT_TYPE.MASTER)
            {
                return "Error: Only the master can get master users";
            }

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);
            DataTable t;

            if (!string.IsNullOrEmpty(d["where"]))
            {
                t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE {d["where"]} ");
            }
            else
            {
                t = sqlite.SQLTable($"SELECT account, user, database, readonly, deleted, timestamp FROM users");
            }

            return JsonConvert.SerializeObject(t, Formatting.Indented);

        }

        public static string UnDeleteDatabase(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
            {
                return "Error: Only the account owner can undelete the database";
            }

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);
            DataTable t = sqlite.SQLTable($"SELECT * FROM databases WHERE database = '{ d["undelete_database"] }' AND account = '{mainClass.account}'");

            if (t.Rows.Count == 0)
            {
                return "Error: The database does not exist";
            }

            if (t.Rows[0]["deleted"].ToString() == "")
            {
                return "Error: The database has already been undeleted";
            }

            mainClass.database = "";
            return sqlite.SQLExec($"UPDATE databases SET deleted = NULL WHERE database = '{d["undelete_database"] }' AND account = '{mainClass.account}'");

        }

        public static string GetDatabases(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (string.IsNullOrEmpty(mainClass.account)) return "Error: No acount selected";

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER) return "Error: You do not have permissions to get databases";

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);
            DataTable tDataTable;

            if (d["where"] == "null")
            {
                tDataTable = sqlite.SQLTable($"SELECT database, timestamp FROM databases WHERE deleted IS NULL AND account = '{ mainClass.account }' ORDER BY database");
            }
            else
            {
                tDataTable = sqlite.SQLTable($"SELECT database, timestamp FROM databases WHERE deleted IS NULL AND account = '{ mainClass.account }' AND {d["where"]}");
            }

            return JsonConvert.SerializeObject(tDataTable, Formatting.Indented);

        }
    }
}
