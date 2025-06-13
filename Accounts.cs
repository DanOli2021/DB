using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AngelDB
{
    public static class Accounts
    {
        public static string CreateAccount(Dictionary<string, string> d, DB mainClass)
        {

            if ( mainClass.account.Trim().ToLower() ==  d["create_account"].Trim().ToLower() ) 
            {
                return "Ok.";
            }

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            if (mainClass.accountType != ACCOUNT_TYPE.MASTER)
            {
                return "Error: You need a master account";
            }

            if (mainClass.IsLogged == false)
            {
                return "Error: User is not logged in";
            }

            DataTable t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE account = '{ d["create_account"].Trim().ToLower() }'");

            if (t.Rows.Count > 0)
            {
                return "Ok.";
            }

            t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE user = '{d["superuser"]}'");

            if (t.Rows.Count > 0) {
                return $"Error: The user  {d["superuser"]}  is already assigned to another account select another user ";
            }

            sqlite.Reset();
            sqlite.CreateInsert("masteraccounts");
            sqlite.AddField("user", d["superuser"]);
            sqlite.AddField("password", d["password"]);
            sqlite.AddField("name", d["name"]);
            sqlite.AddField("account", d["create_account"].Trim().ToLower());
            sqlite.AddField("timestamp", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            string result = sqlite.Exec();

            if (result != "Ok.")
            {
                return $"Error: Entering account information {result}";
            }

            DataTable tUser = sqlite.SQLTable($"SELECT * FROM users WHERE user = '{d["superuser"]}'");
            sqlite.Reset();

            if (tUser.Rows.Count == 0)
            {
                sqlite.CreateInsert("users");
            }
            else
            {
                sqlite.CreateUpdate("users", $"user = '{d["superuser"]}'");
            }

            sqlite.AddField("account", d["create_account"].Trim().ToLower());
            sqlite.AddField("user", d["superuser"]);
            sqlite.AddField("password", d["password"]);
            sqlite.AddField("database", "");
            sqlite.AddField("name", d["name"]);
            sqlite.AddField("readonly", "false");
            sqlite.AddField("deleted", "false");
            sqlite.AddField("tables", "null");
            sqlite.AddField("timestamp", System.DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            result = sqlite.Exec();

            if (result != "Ok.")
            {
                return $"Error: Entering user information {result}";
            }

            string accountDirectory = mainClass.BaseDirectory + mainClass.os_directory_separator + d["create_account"];

            if (!Directory.Exists(accountDirectory))
            {
                Directory.CreateDirectory(accountDirectory);
            }

            return "Ok.";
        }


        public static string UseAccount(Dictionary<string, string> d, DB mainClass)
        {

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            if (mainClass.IsLogged == false)
            {
                return "Error: User is not logged in";
            }

            if (mainClass.account.Trim().ToLower() == d["use_account"].Trim().ToLower())
            {
                return "Ok.";
            }

            DataTable t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE account = '{d["use_account"].Trim().ToLower()}' AND deleted IS NULL");

            if (t.Rows.Count == 0)
            {
                return "Error: Account does not exist";
            }

            if (t.Rows[0]["deleted"].ToString() == "true")
            {
                return "Error: The account was deleted";
            }

            if (mainClass.accountType != ACCOUNT_TYPE.MASTER)
            {
                if (mainClass.user != t.Rows[0]["user"].ToString().Trim())
                {
                    return "Error: You do not have permission to use this account";
                }
            }

            mainClass.database = "";
            mainClass.account = t.Rows[0]["account"].ToString().Trim().ToLower();
            return "Ok.";

        }


        public static string CloseAccount(Dictionary<string, string> d, DB mainClass)
        {
            mainClass.database = "";
            mainClass.account = "";
            return "Ok.";
        }

        public static string DeleteAccount(Dictionary<string, string> d, DB mainClass)
        {

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            if (mainClass.accountType != ACCOUNT_TYPE.MASTER)
            {
                return "Error: You need a master account";
            }

            if (mainClass.IsLogged == false)
            {
                return "Error: User is not logged in";
            }

            DataTable t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE account = '{ d["delete_account"] }'");

            if (t.Rows.Count == 0)
            {
                return "Error: Account does not exist";
            }

            if (t.Rows[0]["deleted"].ToString() == "true")
            {
                return "Error: The account is already deleted";
            }

            sqlite.Reset();
            sqlite.CreateUpdate("masteraccounts", $"account = '{ d["delete_account"] }'");
            sqlite.AddField("deleted", "true");
            sqlite.AddField("timestamp", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            string result = sqlite.Exec();

            if (result != "Ok.")
            {
                return $"Error: Entering account information {result}";
            }

            return "Ok.";
        }

        public static string UndeleteAccount(Dictionary<string, string> d, DB mainClass)
        {

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            if (mainClass.accountType != ACCOUNT_TYPE.MASTER)
            {
                return "Error: You need a master account";
            }

            if (mainClass.IsLogged == false)
            {
                return "Error: User is not logged in";
            }

            DataTable t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE account = '{ d["undelete_account"] }'");

            if (t.Rows.Count == 0)
            {
                return "Error: Account does not exist";
            }

            if (t.Rows[0]["deleted"].ToString() == "")
            {
                return "Error: The account is already undeleted";
            }

            return sqlite.SQLExec($"UPDATE masteraccounts SET deleted = NULL WHERE account = '{d["undelete_account"]}'");
        }

        public static string ActiveAccount(DB mainClass)
        {
            return mainClass.account;
        }

        public static string GetsAccounts(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            if (mainClass.accountType != ACCOUNT_TYPE.MASTER)
            {
                return "Error: You need a master account";
            }

            if (mainClass.IsLogged == false)
            {
                return "Error: User is not logged in";
            }

            DataTable t;

            if (d["where"] == "null")
            {
                t = sqlite.SQLTable($"SELECT account, timestamp FROM masteraccounts WHERE deleted IS NULL GROUP BY account ORDER BY account");
            }
            else
            {
                t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE deleted IS NULL AND { d["where"] } GROUP BY account ORDER BY account");
            }

            return JsonConvert.SerializeObject(t, Formatting.Indented);

        }

        public static string ChangeMaster(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.accountType != ACCOUNT_TYPE.MASTER)
            {
                return "Error: You need to be a master";
            }

            if (!AngelDBTools.StringFunctions.IsStringValidPassword(d["password"]))
            {
                return "Error: Password is invalid, At least 8 characters are required";
            }

            if (string.IsNullOrEmpty(d["to_user"]))
            {
                return "Error: The user has not been indicated";
            }

            if (!AngelDBTools.StringFunctions.IsStringAlphaNumberOrUndescore(d["to_user"]))
            {
                return "Error: User can only contain letters, numbers and underscores";
            }

            mainClass.ChangeKey( "eytrrr67weqmnsdammhjweuasda", "master_user", d["to_user"] );
            mainClass.ChangeKey( "eytrrr67weqmnsdammhjweuasda", "master_password", d["password"] );
            mainClass.user = d["to_user"];
            mainClass.SaveConfig(); 
            return "Ok.";

        }

        public static string AddMaster(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
            {
                return "Error: You need a master account";
            }

            if (!AngelDBTools.StringFunctions.IsStringValidPassword(d["password"]))
            {
                return "Error: Password is invalid, At least 8 characters are required";
            }

            if (string.IsNullOrEmpty(d["user"]))
            {
                return "Error: The user has not been indicated";
            }

            if (!AngelDBTools.StringFunctions.IsStringAlphaNumberOrUndescore(d["user"]))
            {
                return "Error: User can only contain letters, numbers and underscores";
            }

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);
            DataTable t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE account = '{d["to_account"].Trim().ToLower()}' AND deleted IS NULL");

            if (t.Rows.Count == 0)
            {
                return "Error: Account does not exist";
            }

            t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE user = '{d["user"]}' AND account = '{d["to_account"]}'");

            sqlite.Reset();

            if (t.Rows.Count == 0)
            {
                sqlite.CreateInsert("masteraccounts");
            }
            else
            {
                sqlite.CreateUpdate("masteraccounts", $"user = '{d["user"]}' AND account = '{d["to_account"]}'");
            }

            sqlite.AddField("user", d["user"]);
            sqlite.AddField("password", d["password"]);
            sqlite.AddField("account", d["to_account"]);
            sqlite.AddField("name", d["name"]);
            sqlite.AddField("timestamp", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            string result = sqlite.Exec();

            if (result != "Ok.")
            {
                return $"Error: Could not change the username and password of this account {result}";
            }

            return "Ok.";

        }

        public static string UpdateMasterAccount(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
            {
                return "Error: You need a master account";
            }

            if (!AngelDBTools.StringFunctions.IsStringValidPassword(d["password"]))
            {
                return "Password is invalid, At least 8 characters are required";
            }

            if (string.IsNullOrEmpty(d["update_master_account"]))
            {
                return "Error: The user has not been indicated";
            }

            if (!AngelDBTools.StringFunctions.IsStringAlphaNumberOrUndescore(d["update_master_account"]))
            {
                return "Error: User can only contain letters, numbers and underscores";
            }

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            DataTable t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE user = '{d["update_master_account"]}' AND account = '{d["from_account"]}'");

            sqlite.Reset();

            if (t.Rows.Count == 0)
            {
                return $"Error: UpdateMasterAccount: The user {d["update_master_account"]} for the account {d["from_account"]} does not exist"; ;
            }
            else
            {
                sqlite.CreateUpdate("masteraccounts", $"user = '{d["update_master_account"]}' AND account = '{d["from_account"]}'");

            }

            sqlite.AddField("user", d["update_master_account"]);
            sqlite.AddField("password", d["password"]);
            sqlite.AddField("account", d["from_account"]);
            sqlite.AddField("timestamp", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            string result = sqlite.Exec();

            if (result != "Ok.")
            {
                return $"Error: UpdateMasterAccount: Could not change the username and password of this account {result}";
            }

            return "Ok.";

        }


        public static string DeleteMasterAccount(Dictionary<string, string> d, DB mainClass)
        {

            try
            {

                if (!mainClass.IsLogged)
                {
                    return $"Error: You have not indicated your username and password";
                }

                if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER)
                {
                    return "Error: You need a master account";
                }

                if (string.IsNullOrEmpty(d["delete_master_account"]))
                {
                    return "Error: The user has not been indicated";
                }

                if (!AngelDBTools.StringFunctions.IsStringAlphaNumberOrUndescore(d["delete_master_account"]))
                {
                    return "Error: User can only contain letters, numbers and underscores";
                }

                SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

                return sqlite.SQLExec($"DELETE FROM masteraccounts WHERE user = '{d["delete_master_account"]}' AND account = '{d["from_account"]}'");

            }
            catch (Exception e)
            {
                return $"Error: DeleteMasterAccount: {e}";
            }

        }


        public static string GetMasterAccounts(Dictionary<string, string> d, DB mainClass)
        {

            if (!mainClass.IsLogged)
            {
                return $"Error: You have not indicated your username and password";
            }

            if (mainClass.accountType != ACCOUNT_TYPE.MASTER)
            {
                return "Error: You need a master account";
            }

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            DataTable t;
            
            if (d["where"] == "null")
            {
                t = sqlite.SQLTable($"SELECT * FROM masteraccounts");
            } 
            else 
            {
                t = sqlite.SQLTable($"SELECT * FROM masteraccounts WHERE {d["where"]}");
            }
            
            return JsonConvert.SerializeObject(t, Formatting.Indented);

        }


    }
}