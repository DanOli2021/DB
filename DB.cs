using AngelDBTools;
using DB;
using MathNet.Numerics.Financial;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    public enum ACCOUNT_TYPE
    {
        MASTER,
        ACCOUNT_MASTER,
        DATABASE_USER
    }

    public class DB : IDisposable
    {

        private ACCOUNT_TYPE _accountType;

        public ACCOUNT_TYPE accountType
        {
            get
            {
                return _accountType;
            }
            set
            {
            }
        }

        // Propiedad estática que devuelve la configuración personalizada
        private static JsonSerializerSettings ConfiguracionSerializacion =>
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };


        public string account = "";
        public bool IsReadOnly = false;
        public string sqliteConnectionString = "";
        public bool IsLogged = false;

        public string user = "";
        public string database = "";
        private Dictionary<string, string> config = new Dictionary<string, string>();
        public Dictionary<string, object> vars = new Dictionary<string, object>();
        //public Dictionary<string, object> parameters = new Dictionary<string, object>();        
        public Dictionary<string, TableInfo> table_connections = new Dictionary<string, TableInfo>();
        public Dictionary<string, DB> partitionsrules = new Dictionary<string, DB>();

        public ConcurrentDictionary<string, object> Globals = new ConcurrentDictionary<string, object>();

        public string ChatName;
        public string apps_directory = "";
        public bool save_command = true;

        DbLanguage language = new DbLanguage();

        public Dictionary<string, DB> dbs = new Dictionary<string, DB>();

        private string m_tables = "";
        private readonly string m_ConnectionError = "";

        public string UserTables
        {
            get { return m_tables; }
        }

        public string BaseDirectory
        {
            get { return _baseDirectory; }
        }

        public string ConnectionError
        {
            get { return m_ConnectionError; }
        }

        private string _Error = "";

        public string Error
        {
            get { return _Error; }
        }

        public SqliteTools MainDatabase;

        private string password = "";
        private string _baseDirectory = "";

        public delegate void OnReceived(string message);
        public event OnReceived OnReceivedMessage;
        public Dictionary<string, TableArea> TablesArea = new Dictionary<string, TableArea>();
        public bool use_connected_server = false;
        public string os_directory_separator = "/";
        public string Command;
        public string rule;

        public ConcurrentDictionary<string, PartitionsInfo> partitions = new ConcurrentDictionary<string, PartitionsInfo>();
        public ConcurrentDictionary<string, QueryTools> SQLiteConnections = new ConcurrentDictionary<string, QueryTools>();

        public WebForms web = new WebForms();
        public Dictionary<string, AzureTable> Azure = new Dictionary<string, AzureTable>();
        public ConcurrentDictionary<string, SQLServerInfo> SQLServer = new ConcurrentDictionary<string, SQLServerInfo>();

        public MemoryDb Grid = new MemoryDb();
        public bool speed_up = false;
        public string angel_token = "";
        public string angel_url = "";
        public string angel_user = "";

        public bool always_use_AngelSQL = false;
        public string ScriptMessage = "";
        public string ScriptCommandMessage = "";
        public System.DateTime script_file_datetime;
        public string script_file;

        public delegate void SendMessage(string message, ref string result);
        public event SendMessage OnSendMessage = null;

        public bool Development = false;
        public bool NewDatabases = true;

        public MyScript Script = new MyScript();
        private bool disposedValue1;

        public bool CancelTransactions = false;

        public PythonExecutor PythonExecutor;
        public string LastPythonError;
        public string LastPythonResult;
        public string LastPythonWarning;

        public string LockCode = "";

        public Dictionary<string, StatisticsAnalisis> statistics = new Dictionary<string, StatisticsAnalisis>();


        //Chat GPT
        public OpenAIChatbot GPT = null;

        //Ollama
        public OllamaClient Ollama = null;

        // SignalR Connection
        public HubOperation hub = null;

        /// <summary>Initializes a new instance of the <see cref="T:AngelDB.DB" /> class.</summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="baseDirectory">The base directory.</param>
        public DB(string user, string password, string baseDirectory, bool path_validation = true)
        {
            language.db = this;
            _Error = StartDB(user, password, baseDirectory, path_validation);
        }

        public DB(string user, string password)
        {
            language.db = this;
            _Error = StartDB(user, password, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + os_directory_separator + "Data");
        }

        public DB()
        {
            //
            language.db = this;
        }

        ~DB()
        {
            this.Dispose();
        }

        public string StartDB(string user, string password, string baseDirectory, bool path_validation = true)
        {
            _Error = "";

            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);

            try
            {

                if (baseDirectory == "null" || string.IsNullOrEmpty(baseDirectory))
                {
                    baseDirectory = Environment.CurrentDirectory + "/Data";
                }

                string result = SetBaseDirectory(baseDirectory, path_validation);

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                InitDatabases(path_validation);

                result = Login(user, password);

                if (result != "Ok.")
                {
                    return result;
                }

                if (result != "Ok.")
                {
                    return result;
                }

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }

        }

        public void RaiseOnReceived(string message)
        {
            OnReceivedMessage?.Invoke(message);
        }

        public string SetBaseDirectory(string baseDirectory, bool path_validation = true)
        {

            this._baseDirectory = baseDirectory;

            if (!path_validation) return "Ok.";

            if (!Directory.Exists(this.BaseDirectory))
            {
                if (this.NewDatabases == false)
                {
                    return "Error: Directory " + this.BaseDirectory + " does not exist.";
                }

                Directory.CreateDirectory(this.BaseDirectory);
            }

            return "Ok.";

        }

        public string GetKey(string masterkey, string key)
        {
            if (masterkey != "eytrrr67weqmnsdammhjweuasda")
            {
                return "Error: Wrong master key";
            }

            if (!this.config.ContainsKey(key))
            {
                return "Error: Key not found";
            }

            return this.config[key];
        }

        public string ChangeKey(string masterkey, string key, string data)
        {

            if (masterkey != "eytrrr67weqmnsdammhjweuasda")
            {
                return "Error: Wrong master key";
            }

            if (!this.config.ContainsKey(key))
            {
                return "Error: Key not found";
            }

            this.config[key] = data;
            return "Ok.";
        }

        public string Login(string user, string password)
        {

            this.account = "";
            this.database = "";

            if (user == config["master_user"])
            {

                if (password.Trim() != config["master_password"].Trim())
                {
                    return "Error: Invalid user or password";
                }
                else
                {
                    this.user = user;
                    this.password = password;
                    this._accountType = ACCOUNT_TYPE.MASTER;
                    this.IsLogged = true;
                    return "Ok.";
                }
            }

            DataTable t = MainDatabase.SQLTable($"SELECT * FROM masteraccounts WHERE user = '{user}' AND deleted IS NULL");

            if (t.Rows.Count > 0)
            {
                if (password.Trim() != t.Rows[0]["password"].ToString().Trim())
                {
                    return "Error: Invalid user or password";
                }

                this.user = user;
                this.password = password;
                this.IsLogged = true;
                this.account = t.Rows[0]["account"].ToString();
                this._accountType = ACCOUNT_TYPE.ACCOUNT_MASTER;
                return "Ok.";
            }

            string[] complexAccount = user.Split('@');

            if (complexAccount.Length < 2)
            {
                return "Error: Invalid user or password";
            }

            DataTable tAccounts = MainDatabase.SQLTable($"SELECT * FROM masteraccounts WHERE account = '{complexAccount[1]}' AND deleted IS NULL");

            if (tAccounts.Rows.Count == 0)
            {
                return $"Error: Account does not exist: {complexAccount[1]}";
            }

            this.account = complexAccount[1];

            DataTable u = MainDatabase.SQLTable($"SELECT * FROM users WHERE user = '{user}' AND deleted = 'false'");

            if (u.Rows.Count == 0)
            {
                return "Error: Invalid user or password";
            }

            if (password != u.Rows[0]["password"].ToString().Trim())
            {
                return "Error: Invalid user or password";
            }

            this.user = user;

            if (u.Rows[0]["database"].ToString().Trim() != "null")
            {
                DataTable tDataBase = MainDatabase.SQLTable($"SELECT * FROM databases WHERE database = '{u.Rows[0]["database"].ToString().Trim()}' AND deleted IS NULL");

                if (tDataBase.Rows.Count == 0)
                {
                    return "Error: Database does not exist";
                }
            }

            this.user = user;
            this.password = password;
            this._accountType = ACCOUNT_TYPE.DATABASE_USER;
            this.IsLogged = true;
            this.database = "";

            if (u.Rows[0]["database"].ToString().Trim() != "null")
            {
                this.database = u.Rows[0]["database"].ToString();
            }

            if (u.Rows[0]["readonly"].ToString() == "true") this.IsReadOnly = true;
            this.m_tables = u.Rows[0]["tables"].ToString();
            return "Ok.";
        }

        public string ValidateLogin(string user, string password)
        {

            if (user == config["master_user"])
            {
                if (password.Trim() != config["master_password"].Trim())
                {
                    return "Error: Invalid user or password";
                }
                else
                {
                    return ACCOUNT_TYPE.MASTER.ToString();
                }
            }

            DataTable t = MainDatabase.SQLTable($"SELECT * FROM masteraccounts WHERE user = '{user}' AND deleted IS NULL");

            if (t.Rows.Count > 0)
            {
                if (password.Trim() != t.Rows[0]["password"].ToString().Trim())
                {
                    return "Error: Invalid user or password";
                }

                return ACCOUNT_TYPE.ACCOUNT_MASTER.ToString();
            }

            string[] complexAccount = user.Split('@');

            if (complexAccount.Length < 2)
            {
                return "Error: Invalid user or password";
            }

            DataTable tAccounts = MainDatabase.SQLTable($"SELECT * FROM masteraccounts WHERE account = '{complexAccount[1]}' AND deleted IS NULL");

            if (tAccounts.Rows.Count == 0)
            {
                return $"Error: Account does not exist: {complexAccount[1]}";
            }

            DataTable u = MainDatabase.SQLTable($"SELECT * FROM users WHERE user = '{user}' AND deleted = 'false'");

            if (u.Rows.Count == 0)
            {
                return "Error: Invalid user or password";
            }

            if (password != u.Rows[0]["password"].ToString().Trim())
            {
                return "Error: Invalid user or password";
            }

            if (u.Rows[0]["database"].ToString().Trim() != "null")
            {
                DataTable tDataBase = MainDatabase.SQLTable($"SELECT * FROM databases WHERE database = '{u.Rows[0]["database"].ToString().Trim()}' AND deleted IS NULL");

                if (tDataBase.Rows.Count == 0)
                {
                    return "Error: Database does not exist";
                }
            }

            return ACCOUNT_TYPE.DATABASE_USER.ToString();
        }


        public string SaveConfig()
        {
            try
            {
                File.WriteAllText(this.BaseDirectory + os_directory_separator + "db.webmidb", AngelDBTools.CryptoString.Encrypt(JsonConvert.SerializeObject(config, Formatting.Indented), "hbjklios", "iuybncsa"));
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }


        public Dictionary<string, string> NormalizeData(string normalize_level, string token1, string token2)
        {
            if (normalize_level.IndexOf("ddha()/%$%$$%232232244545") <= 0)
            {
                return null;
            }

            if (token1 != "904ffjj") return null;
            if (token2 != "234aslfdf") return null;

            Dictionary<string, string> config = new Dictionary<string, string>();
            config = JsonConvert.DeserializeObject<Dictionary<string, string>>(AngelDBTools.CryptoString.Decrypt(File.ReadAllText(Environment.CurrentDirectory + "/Data/db.webmidb"), "hbjklios", "iuybncsa"));
            return config;

        }

        public string InitDatabases(bool path_validation = true)
        {

            config.Clear();

            if (!File.Exists(this.BaseDirectory + os_directory_separator + "db.webmidb"))
            {
                config.Add("master_user", "db");
                config.Add("master_password", "db");
                config.Add("finger_print", System.Guid.NewGuid().ToString());
                config.Add("server", "localhost");
                config.Add("port", "11000");
                config.Add("secret", "");
                config.Add("autostart", "false");
                SaveConfig();
            }

            config = JsonConvert.DeserializeObject<Dictionary<string, string>>(AngelDBTools.CryptoString.Decrypt(File.ReadAllText(this.BaseDirectory + os_directory_separator + "db.webmidb"), "hbjklios", "iuybncsa"));

            string result;
            this.sqliteConnectionString = $"Data Source={this.BaseDirectory + os_directory_separator + "config.db"};";
            MainDatabase = new SqliteTools(this.sqliteConnectionString);

            if (path_validation)
            {
                result = Dataconfig.CreateInitTables(this);

                if (result != "Ok.")
                {
                    return result;
                }
            }

            return "Ok.";

            //return ResetPartitionRules();

        }

        //public string ResetPartitionRules()
        //{

        //    try
        //    {
        //        DataTable t = MainDatabase.SQLTable("SELECT * FROM partitionrules");

        //        string result = this.Grid.SQLExec("DROP TABLE IF EXISTS grid_partitionrules");
        //        result = this.Grid.SQLExec("CREATE TABLE IF NOT EXISTS grid_partitionrules ( account, database, table_name, partition, connection_string, connection_type, timestamp, PRIMARY KEY (account, database, table_name, partition) )");

        //        foreach (DataRow item in t.Rows)
        //        {
        //            this.Grid.Reset();
        //            this.Grid.CreateInsert("grid_partitionrules");
        //            this.Grid.AddField("account", item["account"].ToString());
        //            this.Grid.AddField("database", item["database"].ToString());
        //            this.Grid.AddField("table_name", item["table_name"].ToString());
        //            this.Grid.AddField("partition", item["partition"].ToString());
        //            this.Grid.AddField("connection_string", item["connection_string"].ToString());
        //            this.Grid.AddField("connection_type", item["connection_type"].ToString());
        //            this.Grid.AddField("timestamp", item["timestamp"].ToString());
        //            result = this.Grid.Exec();
        //        }

        //        return "Ok.";


        //    }
        //    catch (Exception e)
        //    {
        //        return $"Error {e}";
        //    }

        //}

        public DB Clone()
        {

            DB db = new DB(this.user, this.password, this.BaseDirectory);

            if (!string.IsNullOrEmpty(this.account))
            {
                db.Prompt("USE ACCOUNT " + this.account);
            }

            if (!string.IsNullOrEmpty(this.database))
            {
                db.Prompt("USE DATABASE " + this.database);
            }

            return db;

        }

        public DbLanguage SyntaxAnalizer()
        {
            return new DbLanguage();
        }


        public async Task<string> AsyncPrompt(string command, bool ThrowError = false, DB operational_db = null, object main_object = null)
        {
            string result;
            Task<string> task = Task.Run(() => Prompt(command, ThrowError, operational_db, main_object));
            result = await task;
            return result;
        }


        public string Prompt(string command, bool ThrowError = false, DB operational_db = null, object main_object = null, TaskState taskState = null)
        {

            string Command = command;

            if (this.save_command)
            {
                this.Command = command;
            }

            foreach (var item in vars)
            {
                if (command.IndexOf("@") == -1)
                {
                    break;
                }

                command = command.Replace("@" + item.Key, item.Value.ToString());

            }

            string result;

            if (always_use_AngelSQL)
            {

                if (command.Trim().StartsWith("BATCH ANGEL STOP"))
                {
                    this.always_use_AngelSQL = false;
                    return "Ok.";
                }

                if (command.Trim() == "ANGEL STOP")
                {
                    this.always_use_AngelSQL = false;
                    return "Ok.";
                }

                if (command.Trim() == "DISCONNECT ANGEL")
                {
                    this.always_use_AngelSQL = false;
                    return "Ok.";
                }


                result = AngelExecute($"COMMAND {command}");

                if (result.StartsWith("Error:"))
                {
                    if (ThrowError)
                    {
                        throw new Exception(result);
                    }

                }
                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                return result;

            }


            Dictionary<string, string> d = new Dictionary<string, string>();
            d = language.Interpreter(command);

            if (d == null)
            {
                if (ThrowError)
                {
                    result = language.errorString;
                    throw new Exception(result);
                }

                return language.errorString;
            }

            if (d.Count == 0)
            {
                if (ThrowError)
                {
                    result = "Error: not command found " + command; ;
                    throw new Exception(result);
                }

                return "Error: not command found " + command;

            }

            string commandkey = d.First().Key;

            if (commandkey == "run")
            {
                result = RunScript(d);
                return result;
            }

            switch (commandkey)
            {
                case "create_account":
                    result = Accounts.CreateAccount(d, this);
                    break;
                case "use_account":
                    result = Accounts.UseAccount(d, this);
                    break;
                case "delete_account":
                    result = Accounts.DeleteAccount(d, this);
                    break;
                case "undelete_account":
                    result = Accounts.UndeleteAccount(d, this);
                    break;
                case "close_account":
                    result = Accounts.CloseAccount(d, this);
                    break;
                case "account":
                    result = Accounts.ActiveAccount(this);
                    break;
                case "create_database":
                    result = Database.CreateDatabase(d, this, this.BaseDirectory);
                    break;
                case "use_database":
                    result = Database.UseDatabase(d, this);
                    break;
                case "use":

                    result = Database.Use(d, this);
                    break;

                case "delete_database":
                    result = Database.DeleteDatabase(d, this);
                    break;
                case "undelete_database":
                    result = Database.UnDeleteDatabase(d, this);
                    break;
                case "database":
                    result = Database.ActiveDatabase(this);
                    break;
                case "create_table":
                    result = Tables.CreateTable(d, this);
                    break;

                case "delete_table":
                    result = Tables.DeleteTable(d, this);
                    break;
                case "insert_into":
                    result = Tables.InsertInto(d, this);
                    break;
                case "upsert_into":
                    d["upsert"] = "true";
                    d.Add("insert_into", d["upsert_into"]);
                    result = Tables.InsertInto(d, this);
                    break;
                //result = Tables.CopyTo( d, this );
                case "update":
                    result = Tables.Update(d, this);
                    break;
                case "delete_from":
                    result = Tables.DeleteFrom(d, this);
                    break;
                case "vacuum_table":

                    result = Tables.Vacuum(d, this);
                    break;

                case "alter_table":

                    result = "Error: No Add Column Or Drop Column";

                    if (d["add_column"] != "null")
                    {
                        result = Tables.AlterTable(d, this);
                    }
                    else
                    {
                        if (d["drop_column"] != "null")
                        {
                            result = Tables.AlterTableDropColumn(d, this);
                        }
                    }

                    break;

                case "create_index":

                    result = Tables.CreateIndex(d, this);
                    break;

                case "select":
                    result = Tables.Select(d, this);
                    break;
                case "save_to":
                    result = SavePrompt(d);
                    break;
                case "get_tables":
                    result = Tables.GetTables(d, this);
                    break;
                case "get_structure":
                    result = Tables.GetStructure(d, this);
                    break;
                case "get_partitions":
                    result = Tables.GetPartitions(d, this);
                    break;
                case "delete_partitions":

                    result = Tables.DeletePartitions(d, this);
                    break;

                case "move_partition":
                    result = Tables.MovePartition(d, this);
                    break;

                case "compress_partition":

                    result = Tables.CompressPartition(d, this);
                    break;

                case "decompress_partition":

                    result = Tables.DeCompressPartition(d, this);
                    break;

                case "get_accounts":
                    result = Accounts.GetsAccounts(d, this);
                    break;
                case "get_databases":
                    result = Database.GetDatabases(d, this);
                    break;
                case "who_i":

                    var v = new { local_user = this.user };
                    result = this.user;

                    break;
                case "send_to":
                    result = FileStorage.SendTo(d, this);
                    break;
                case "copy_from":
                    result = FileStorage.CopyFrom(d, this);
                    break;
                case "get_file":
                    result = FileStorage.GetFileAsBase64(d, this);
                    break;
                case "delete_file":
                    result = FileStorage.DeleteFile(d, this);
                    break;
                case "create_login":
                    result = Database.CreateLogin(d, this);
                    break;
                case "delete_login":
                    result = Database.DeleteLogin(d, this);
                    break;
                case "change_master":
                    result = Accounts.ChangeMaster(d, this);
                    break;
                case "add_master":
                    result = Accounts.AddMaster(d, this);
                    break;
                case "get_master_accounts":
                    result = Accounts.GetMasterAccounts(d, this);
                    break;
                case "update_master_account":
                    result = Accounts.UpdateMasterAccount(d, this);
                    break;
                case "delete_master_account":
                    result = Accounts.DeleteMasterAccount(d, this);
                    break;
                case "get_users":
                    result = Database.GetUsers(d, this);
                    break;
                case "get_masters":
                    result = Database.GetMasters(d, this);
                    break;
                case "stop_using_server":
                    this.use_connected_server = false;
                    result = "Ok.";
                    break;
                case "always_use_angelsql":
                    this.always_use_AngelSQL = true;
                    result = "Ok.";
                    break;
                case "get_logins":
                    result = Database.GetUsers(d, this);
                    break;
                case "validate_login":
                    result = ValidateLogin(d["validate_login"], d["password"]);
                    break;
                case "console":
                    result = FileStorage.WriteToConsole(d, this);
                    break;
                case "write_results_from":
                    result = FileStorage.WriteToFile(d, this);
                    break;
                case "write_file":

                    try
                    {
                        System.IO.File.WriteAllText(d["write_file"], d["values"]);
                        result = "Ok.";

                    }
                    catch (Exception e)
                    {
                        result = "Error: Write File " + e.Message;
                    }

                    break;

                case "cmd":
                    result = RunCMD(d["cmd"]);
                    break;
                case "load_file":
                    result = RunScript(d);
                    break;
                case "business":
                    result = Business.BusinessPrompt(this, d["business"]);
                    break;
                case "var":
                    result = SetVar(d["var"], d["="]);
                    break;
                case "get_vars":

                    result = GetVars(vars);
                    break;

                case "run":

                    result = RunScript(d);
                    break;

                case "my_level":

                    result = "";

                    switch (this.accountType)
                    {
                        case ACCOUNT_TYPE.ACCOUNT_MASTER:
                            result = "ACCOUNT MASTER";
                            break;

                        case ACCOUNT_TYPE.DATABASE_USER:

                            result = "DATABASE USER";
                            break;

                        case ACCOUNT_TYPE.MASTER:

                            result = "MASTER";
                            break;

                        default:
                            break;
                    }

                    break;

                case "who_is":

                    result = "01001101 01101001 00100000 01110000 01100001 01110000 11100001 00100000 01100101 01110011 00100000 01000100 01100001 01101110 01101001 01100101 01101100 00100000 01001111 01101100 01101001 01110110 01100101 01110010 00100000 01010010 01101111 01101010 01100001 01110011 00101100 00100000 01111001 00100000 01101101 01101001 00100000 01101101 01100001 01101101 11100001 00100000 01100101 01110011 00100000 01010010 01101111 01110011 01100001 00100000 01001101 01100001 01110010 01101001 01100001 00100000 01010100 01110010 01100101 01110110 01101001 01101100 01101100 01100001 00100000 01000111 01100001 01110010 01100011 01101001 01100001 00101100 00100000 01101101 01101001 01110011 00100000 01101000 01100101 01110010 01101101 01100001 01101110 01101111 01110011 00100000 01110011 01101111 01101110 00100000 01001111 01101100 01101001 01110110 01100101 01110010 00100000 01000111 01110101 01101001 01101100 01101100 01100101 01110010 01101101 01101111 00100000 01010010 01101111 01101010 01100001 01110011 00100000 01010100 01110010 01100101 01110110 01101001 01101100 01101100 01100001 00100000 01100101 00100000 01001001 01100001 01101110 00100000 01010010 01100001 01111010 01101001 01100101 01101100 00100000 01010010 01101111 01101010 01100001 01110011 00100000 01010100 01110010 01100101 01110110 01101001 01101100 01101100 01100001";
                    break;

                case "db_user":

                    this.account = "";
                    this.database = "";
                    this.IsLogged = false;
                    result = this.StartDB(d["db_user"], d["password"], d["data_directory"]);

                    if (result.StartsWith("Error:"))
                    {
                        break;
                    }

                    if (d["account"] == "null")
                    {
                        break;
                    }
                    else
                    {
                        result = this.Prompt("USE ACCOUNT " + d["account"]);
                    }

                    if (d["database"] == "null")
                    {
                        break;
                    }
                    else
                    {
                        result = this.Prompt("USE DATABASE " + d["database"]);
                    }


                    break;

                case "close_db":

                    this.account = "";
                    this.database = "";
                    this.IsLogged = false;
                    this.user = "";
                    result = "Ok.";
                    break;

                case "use_table":

                    result = this.UseTable(d);
                    break;

                case "(":

                    result = AnalizeParentesis(d);
                    break;

                case "create_partition_rule":

                    result = Partitions.CreatePartitionRule(d, this);
                    break;

                case "delete_partition_rule":

                    result = Partitions.DeletePartitionRule(d, this);
                    break;

                case "get_partition_rules":

                    result = Partitions.GetPartitionRules(d, this);
                    break;

                case "create_db":

                    result = CreateDB(d["create_db"], d["connection_string"]);
                    break;

                case "prompt_db":

                    result = PromptDB(d["prompt_db"], d["command"]);
                    break;

                case "get_db_list":

                    result = JsonConvert.SerializeObject(this.dbs);
                    break;

                case "remove_db":

                    result = RemoveDB(d["remove_db"]);
                    break;

                case "web_form":

                    result = this.web.WebFormsPrompt(this, d["web_form"]);
                    break;

                case "azure":

                    result = AzureExecute(d["azure"]);
                    break;

                case "sql_server":

                    result = SQLServerExecute(d["sql_server"]);
                    break;

                case "=":

                    return Script.Eval(d["="], d["message"], this);

                case "script_file":

                    return Script.EvalFile(d, this, operational_db, main_object, null, taskState);

                case "set_script_message":
                    this.ScriptMessage = d["set_script_message"];
                    return "Ok.";

                case "get_script_message":

                    string message = this.ScriptMessage;
                    this.ScriptMessage = "";
                    return message;

                case "set_script_command":

                    this.ScriptCommandMessage = d["set_script_command"];
                    return "Ok.";

                case "get_script_command":

                    string script_command = this.ScriptCommandMessage;
                    this.ScriptCommandMessage = "";
                    return script_command;

                case "get_url":

                    return WebTools.ReadUrl(d["get_url"]);

                case "send_to_web":

                    if (d["source"] != "null")
                    {
                        return WebTools.SendJsonToUrl(d["send_to_web"], this.Prompt(d["source"]));
                    }

                    return WebTools.SendJsonToUrl(d["send_to_web"], d["context_data"]);

                case "save_to_grid":

                    return SaveToGrid(d);

                case "save_to_table":

                    if (d["json"] != "null")
                    {
                        result = SaveJsonToTable(d);
                        break;
                    }

                    result = SaveToTable(d);
                    break;

                case "grid":

                    return RunGrid(d);

                case "grid_insert_on":

                    return GridInsertOn(d);

                case "scripts_on_main":

                    this.apps_directory = Environment.CurrentDirectory;
                    return "Ok.";

                case "set_scripts_directory":

                    if (!Directory.Exists(d["set_scripts_directory"]))
                    {
                        return $"Error: directory does not exist {Directory.Exists(d["set_scripts_directory"])}";
                    }

                    this.apps_directory = d["set_scripts_directory"];
                    return "Ok.";

                case "get_scripts_directory":

                    return this.apps_directory;

                case "speed_up":

                    this.speed_up = true;
                    result = "Ok.";
                    break;

                case "angel":

                    result = AngelExecute(d["angel"]);
                    break;

                case "read_file":

                    try
                    {
                        result = File.ReadAllText(d["read_file"]);
                    }
                    catch (Exception e)
                    {
                        result = $"Error: {e}";
                    }

                    break;

                case "set_development":

                    if (d["set_development"] == "true")
                    {
                        this.Development = true;
                    }
                    else
                    {
                        this.Development = false;
                    }

                    result = "Ok.";
                    break;

                case "set_new_databases":

                    if (d["set_new_databases"] == "true")
                    {
                        this.Development = true;
                    }
                    else
                    {
                        this.Development = false;
                    }

                    result = "Ok.";
                    break;

                case "to_client":

                    return this.SendToClient(d["to_client"]);

                case "get_enviroment":

                    return EnviromentTools();

                case "prompt":

                    result = Monitor.Prompt(d["prompt"]);
                    break;

                case "prompt_password":

                    result = Monitor.ReadPassword(d["prompt_password"]);
                    break;

                case "batch":

                    bool show_in_console = false;

                    if (d["show_in_console"] == "true")
                    {
                        show_in_console = true;
                    }

                    result = DBBatch.RunCode(d["batch"], show_in_console, this);
                    break;

                case "batch_file":

                    bool show_file_in_console = false;

                    if (d["show_in_console"] == "true")
                    {
                        show_file_in_console = true;
                    }

                    result = DBBatch.RunBatch(d["batch_file"], show_file_in_console, this);
                    break;

                case "create_sync":

                    result = Sync.CreateSync(d, this);
                    break;

                case "get_syncs":

                    result = Sync.GetSyncs(d, this);
                    break;

                case "sync_now":

                    result = Sync.SyncNow(d, this);
                    break;

                //case "compile":

                //    if (d["assembly_name"] == "null") d["assembly_name"] = "";
                //    result = script.CompileFileForBlazor(d["compile"], this, d["assembly_name"]);
                //    break;

                case "read_excel":

                    bool header = true;
                    if (d["first_row_as_header"] == "false") header = false;

                    if (d["on_memory"] == "true")
                    {
                        result = OpenDocuments.ReadExcelAsJsonOnMemory(d["read_excel"], this, d["as_table"], header, d["sheet"]);
                    }
                    else
                    {
                        result = OpenDocuments.ReadExcelAsJson(d["read_excel"], this, d["as_table"], header, d["sheet"]);
                    }

                    break;

                case "create_excel":

                    result = OpenDocuments.CreateExcelFromJson(d["create_excel"], this, d["json_values"]);
                    break;

                case "create_sync_database":

                    result = Sync.CreateSyncDataBase(d, this);
                    break;

                case "sync_database":

                    result = Sync.SyncDatabase(d, this);
                    break;

                case "get_max_sync_time":

                    result = Tables.GetMaxSyncTimeStampFromTable(d, this);
                    break;

                case "update_partition":

                    result = Tables.UpdatePartitionTimeStamp(d, this);
                    break;

                case "statistics":

                    result = DBStatistics.StatisticsCommand(this, d["statistics"]);
                    break;

                case "read_csv":

                    bool csv_header = false;

                    if (d["read_csv"] == "true")
                    {
                        csv_header = true;
                    }
                    else
                    {
                        csv_header = false;
                    }

                    result = AngelDBTools.StringFunctions.ReadCSV(d["read_csv"], csv_header, d["value_separator"], d["columns_as_numbers"]);
                    break;

                case "ollama":

                    if (d["url"] == "null")
                    {
                        d["url"] = "http://localhost:11434/api/chat";
                    }

                    if (d["model"] == "null")
                    {
                        d["model"] = "llama3.2";
                    }

                    bool stream = false;

                    if (d["stream"] == "true")
                    {
                        stream = true;
                    }

                    try
                    {
                        this.Ollama = new OllamaClient(d["model"]);

                        if (d["url"].ToLower().Contains("localhost"))
                        {
                            if (this.Ollama.IsOllamaInstalled() == false)
                            {
                                result = "Error: Ollama not installed. Please install Ollama from https://ollama.com/";
                                this.Ollama = null;
                                break;
                            }
                        }

                        this.Ollama.BaseUrl = d["url"];
                        this.Ollama.Stream = stream;

                        result = "Ok.";

                    }
                    catch (Exception e)
                    {
                        result = $"Error: {e.ToString()}";
                    }

                    break;

                case "ollama_load_model":

                    if (this.Ollama is null)
                    {
                        result = "Error: Ollama not initialized";
                        break;
                    }

                    this.Ollama.Model = d["ollama_load_model"];

                    if (this.Ollama.WarmUpModelAsync().GetAwaiter().GetResult())
                    {
                        result = "Ok.";
                    }
                    else
                    {
                        result = "Error: Model not loaded";
                    }

                    break;

                case "ollama_unload_model":

                    if (this.Ollama is null)
                    {
                        result = "Error: Ollama not initialized";
                        break;
                    }

                    this.Ollama.Model = d["ollama_unload_model"];

                    if (this.Ollama.PullModelAsync().GetAwaiter().GetResult())
                    {
                        result = "Ok.";
                    }
                    else
                    {
                        result = "Error: Model not unloaded";
                    }
                    break;

                case "ollama_add_system_message":

                    if (this.Ollama is null)
                    {
                        result = "Error: Ollama not initialized";
                        break;
                    }

                    this.Ollama.AddSystemMessage(d["ollama_add_system_message"]);

                    result = "Ok.";
                    break;

                case "ollama_add_assitant_message":

                    if (this.Ollama is null)
                    {
                        result = "Error: Ollama not initialized";
                        break;
                    }
                    this.Ollama.AddAssistantMessage(d["ollama_add_assitant_message"]);
                    result = "Ok.";
                    break;

                case "ollama_prompt":

                    if (this.Ollama is null)
                    {
                        result = "Error: Ollama not initialized";
                        break;
                    }
                    this.Ollama.AddUserMessage(d["ollama_prompt"]);
                    result = this.Ollama.ChatAsync().GetAwaiter().GetResult();
                    break;

                case "ollama_clear":

                    if (this.Ollama is null)
                    {
                        result = "Error: Ollama not initialized";
                        break;
                    }

                    this.Ollama.ClearHistory();

                    result = "Ok.";
                    break;

                case "gpt":

                    if (this.GPT is null)
                    {

                        this.GPT = new OpenAIChatbot(this);

                        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANGELSQL_GPT_KEY")))
                        {
                            this.GPT.SetApiKey(Environment.GetEnvironmentVariable("ANGELSQL_GPT_KEY"));
                            Dictionary<string, string> gpt = new Dictionary<string, string>();
                            gpt.Add("start_chat", "");
                            this.GPT.StartChat(gpt);
                        }


                    }

                    result = this.GPT.ProcessCommand(d["gpt"]);
                    break;

                case "post":

                    result = WebTools.SendJsonToUrl(d["post"], d["message"]);
                    break;

                case "post_api":

                    AngelDB.AngelPOST api = new AngelDB.AngelPOST();
                    api.api = d["post_api"];
                    api.message = d["message"];
                    api.language = d["language"];

                    if (d["account"] == "null")
                    {
                        d["account"] = "";
                    }

                    api.account = d["account"];

                    Console.WriteLine("POST: " + JsonConvert.SerializeObject(api, Formatting.Indented));
                    result = WebTools.SendJsonToUrl(d["post"], JsonConvert.SerializeObject(api, Formatting.Indented));
                    break;


                case "lock_table":

                    result = Tables.LockTable(d, this);
                    break;

                case "unlock_table":

                    result = Tables.UnLockTable(d, this);
                    break;

                case "version":
                    result = "01.00.00 2024-07-10";
                    break;

                case "hub":

                    if (hub is null)
                    {
                        this.hub = new HubOperation(this);
                    }

                    result = hub.HubExecute(d["hub"]).GetAwaiter().GetResult();
                    break;

                case "help":

                    var help = new
                    {
                        service = "help",
                        command = d["help"],
                    };

                    result = this.Prompt($"GET URL https://angelsql.net/AngelAPI?data={JsonConvert.SerializeObject(help)}");

                    if (!result.StartsWith("Error:"))
                    {
                        Dictionary<string, string> help_result = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
                        StringBuilder sb = new StringBuilder();

                        foreach (var item in help_result)
                        {
                            sb.AppendLine();
                            sb.AppendLine(item.Key + ": " + item.Value);
                            sb.AppendLine();
                        }

                        result = sb.ToString();

                    }

                    break;

                case "python_file":

                    if (this.PythonExecutor is null)
                    {
                        this.PythonExecutor = new PythonExecutor();
                    }

                    result = PythonExecutor.ExecutePythonScript(d["python_file"], d["message"], this, d["path"]);
                    break;

                case "python_set_path":
                    Environment.SetEnvironmentVariable("PYTHON_PATH", d["python_set_path"]);
                    result = "Ok";
                    break;
                case "python_get_last_error":

                    if (this.PythonExecutor is null)
                    {
                        result = "";
                    }
                    else
                    {
                        result = this.LastPythonError;
                    }
                    break;

                case "python_get_last_warning":

                    if (this.PythonExecutor is null)
                    {
                        result = "";
                    }
                    else
                    {
                        result = this.LastPythonWarning;
                    }
                    break;

                case "python_get_last_result":

                    if (this.PythonExecutor is null)
                    {
                        result = "";
                    }
                    else
                    {
                        result = this.LastPythonResult;
                    }
                    break;

                case "import_scripts_directory":

                    result = Script.ImportDirectory(d["import_scripts_directory"], d["type"], this);
                    break;

                case "import_script_file":

                    result = Script.ImportCodeFromFile(d["import_script"], this);
                    break;

                case "db_script":

                    result = Script.EvalDB(d, this, operational_db, main_object);
                    break;

                case "compile_to_dll":

                    if (d["to_file"] == "null")
                    {
                        d["to_file"] = Path.GetDirectoryName(d["compile_to_dll"]) + "/" + Path.GetFileNameWithoutExtension(d["compile_to_dll"]) + ".dll";
                    }

                    result = Script.CompileCsxToDll(d["compile_to_dll"], d["to_file"]);
                    break;

                case "remote_desktop":

                    result = RdpConnect(d);
                    break;

                case "system_info":

                    SystemMonitor systemMonitor = new SystemMonitor();

                    result = systemMonitor.GetSystemMetricsAsJsonAsync().GetAwaiter().GetResult();
                    break;

                case "connection_info":

                    var connectionInfo = new
                    {
                        Account = this.account,
                        Database = this.database,
                        User = this.user,
                        Use_connected_server = this.use_connected_server,
                        Always_use_AngelSQL = this.always_use_AngelSQL,
                    };

                    return JsonConvert.SerializeObject(connectionInfo, Formatting.Indented);

                default:

                    result = "Error: No command given: " + commandkey;
                    break;
            }


            if (result.StartsWith("Error:"))
            {
                if (ThrowError)
                {
                    throw new Exception(result);
                }

            }

            return result;
        }


        public string RdpConnect(Dictionary<string, string> d)
        {
            try
            {
                int port = 3389;
                bool full_screen = false;
                int width = 800;
                int height = 600;
                bool admin = false;
                bool redirect_disk = false;
                bool redirect_printer = false;
                bool redirect_clipboard = false;

                if (d["port"] != "null")
                {
                    port = Convert.ToInt32(d["port"]);
                }

                if (d["full_screen"] == "true")
                {
                    full_screen = true;
                }

                if (d["width"] != "null")
                {
                    width = Convert.ToInt32(d["width"]);
                }

                if (d["height"] != "null")
                {
                    height = Convert.ToInt32(d["height"]);
                }

                if (d["admin"] == "true")
                {
                    admin = true;
                }

                if (d["redirect_disks"] == "true")
                {
                    redirect_disk = true;
                }

                if (d["redirect_printers"] == "true")
                {
                    redirect_printer = true;
                }

                if (d["redirect_clipboard"] == "true")
                {
                    redirect_clipboard = true;
                }

                return RemoteDesktopHelper.StartRdpSessionWithResources(d["remote_desktop"], d["user"], d["password"], port, full_screen, width, height, admin, redirect_disk, redirect_printer, redirect_clipboard);

            }
            catch (Exception e)
            {
                return $"Error: RdpConnect: {e}";
            }
        }


        public string CreateTable(object o, string table_name = "", bool search_table = false, string discard_indices = "", bool ThrowError = false, bool RebuildTable = false)
        {
            string result = "";

            try
            {
                string[] discard = discard_indices.Split(',');

                if (string.IsNullOrEmpty(table_name)) table_name = o.GetType().Name;

                string sql = "CREATE TABLE " + table_name + " FIELD LIST ";

                foreach (var prop in o.GetType().GetProperties())
                {
                    // Ignorar propiedades específicas como 'id', 'partitionkey', 'timestamp'
                    if (prop.Name.Trim().ToLower() == "id" ||
                        prop.Name.Trim().ToLower() == "partitionkey" ||
                        prop.Name.Trim().ToLower() == "timestamp")
                    {
                        continue;
                    }

                    string indexed = !discard.Contains(prop.Name) ? " INDEXED" : "";

                    if (search_table == false)
                    {
                        switch (prop.PropertyType.Name)
                        {
                            case "String":
                                sql += prop.Name + " TEXT" + indexed + ", ";
                                break;
                            case "Int32":
                                sql += prop.Name + " INTEGER" + indexed + ", ";
                                break;
                            case "Decimal":
                                sql += prop.Name + " NUMERIC" + indexed + ", ";
                                break;
                            case "Boolean":
                                sql += prop.Name + " BOOLEAN" + indexed + ", ";
                                break;
                            case "DateTime":
                                sql += prop.Name + " TEXT" + indexed + ", ";
                                break;
                            default:
                                sql += prop.Name + " " + indexed + ", ";
                                break;
                        }
                    }
                    else
                    {
                        sql += prop.Name + ", ";
                    }
                }

                sql = sql.Substring(0, sql.Length - 2);

                if (search_table)
                {
                    sql += " TYPE SEARCH";
                }

                result = this.Prompt(sql);

                if (result.StartsWith("Error:"))
                {
                    Console.WriteLine(result);
                }
            }
            catch (System.Exception e)
            {
                result = $"Error: CreateTable: {e}";
            }

            if (ThrowError)
            {
                if (result.StartsWith("Error:"))
                {
                    throw new Exception(result);
                }
            }

            return result;
        }


        public string CreateTableFromJSon(string json)
        {
            try
            {
                var o = JsonConvert.DeserializeObject(json);
                string table_name = o.GetType().Name;
                string sql = "CREATE TABLE " + table_name + " FIELD LIST ";
                foreach (var prop in o.GetType().GetProperties())
                {

                    if (prop.Name.Trim().ToLower() == "id")
                    {
                        continue;
                    }

                    if (prop.Name.Trim().ToLower() == "partitionkey")
                    {
                        continue;
                    }

                    if (prop.Name.Trim().ToLower() == "timestamp")
                    {
                        continue;
                    }


                    switch (prop.PropertyType.Name)
                    {
                        case "String":
                            sql += prop.Name + " TEXT, ";
                            break;
                        case ("Int32"):
                            sql += prop.Name + " INTEGER, ";
                            break;
                        case ("Decimal"):
                            sql += prop.Name + " NUMERIC, ";
                            break;
                        case ("Boolean"):
                            sql += prop.Name + " BOOLEAN, ";
                            break;
                        case ("DateTime"):
                            sql += prop.Name + " TEXT, ";
                            break;
                        default:
                            sql += prop.Name + ", ";
                            break;
                    }
                }

                sql = sql.Substring(0, sql.Length - 2);
                return this.Prompt(sql);

            }
            catch (System.Exception e)
            {
                return $"Error: CreateTable: {e}";
            }

        }

        string EnviromentTools()
        {
            string str;
            string nl = Environment.NewLine;
            //
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-- Environment members --");

            //  Invoke this sample with an arbitrary set of command line arguments.
            sb.AppendLine($"CommandLine: {Environment.CommandLine}");

            string[] arguments = Environment.GetCommandLineArgs();
            sb.AppendLine($"GetCommandLineArgs: {String.Join(", ", arguments)}");

            //  <-- Keep this information secure! -->
            sb.AppendLine($"CurrentDirectory: {Environment.CurrentDirectory}");

            sb.AppendLine("ExitCode: {Environment.ExitCode}");

            sb.AppendLine($"HasShutdownStarted: {Environment.HasShutdownStarted}");

            //  <-- Keep this information secure! -->
            sb.AppendLine($"MachineName: {Environment.MachineName}");

            sb.AppendLine($"OSVersion: {Environment.OSVersion.ToString()}");

            sb.AppendLine($"StackTrace: '{Environment.StackTrace}'");

            //  <-- Keep this information secure! -->
            sb.AppendLine($"SystemDirectory: {Environment.SystemDirectory}");

            sb.AppendLine($"TickCount: {Environment.TickCount}");

            //  <-- Keep this information secure! -->
            sb.AppendLine($"UserDomainName: {Environment.UserDomainName}");

            sb.AppendLine($"UserInteractive: {Environment.UserInteractive}");

            //  <-- Keep this information secure! -->
            sb.AppendLine($"UserName: {Environment.UserName}");

            sb.AppendLine($"Version: {Environment.Version.ToString()}");

            sb.AppendLine($"WorkingSet: {Environment.WorkingSet}");

            //  No example for Exit(exitCode) because doing so would terminate this example.

            //  <-- Keep this information secure! -->
            string query = "My system drive is %SystemDrive% and my system root is %SystemRoot%";
            str = Environment.ExpandEnvironmentVariables(query);
            sb.AppendLine($"ExpandEnvironmentVariables: {nl}  {str}");

            sb.AppendLine($"GetEnvironmentVariable: {nl}  My temporary directory is {Environment.GetEnvironmentVariable("TEMP")}.");

            sb.AppendLine($"GetEnvironmentVariables: ");
            IDictionary environmentVariables = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry de in environmentVariables)
            {
                sb.AppendLine($"  {de.Key} = {de.Value}");
            }

            sb.AppendLine($"GetFolderPath: {Environment.GetFolderPath(Environment.SpecialFolder.System)}");

            string[] drives = Environment.GetLogicalDrives();
            sb.AppendLine($"GetLogicalDrives: {String.Join(", ", drives)}");

            return sb.ToString();
        }


        string RunGrid(Dictionary<string, string> d)
        {
            try
            {
                if (d["grid"].Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {

                    DataTable dataTable = new DataTable();

                    try
                    {
                        dataTable = Grid.SQLTable(d["grid"]);

                        if (d["as_json"] == "true")
                        {
                            return JsonConvert.SerializeObject(dataTable, Formatting.Indented);
                        }
                        else
                        {
                            return AngelDBTools.StringFunctions.ToCSVString(dataTable);
                        }

                    }
                    catch (Exception e)
                    {
                        return $"Error: Grid: {e}";
                    }

                }


                if (d["grid"].Trim().StartsWith("RESET", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        this.Grid.Close();
                        this.Grid = new MemoryDb();
                        return "Ok.";
                    }
                    catch (Exception e)
                    {
                        return $"Error: Grid: {e}";
                    }
                }

                return Grid.SQLExec(d["grid"]);

            }
            catch (Exception e)
            {
                return $"Error: Grid {e}";
            }
        }


        string GridInsertOn(Dictionary<string, string> d)
        {

            try
            {
                DataTable t = JsonConvert.DeserializeObject<DataTable>(d["values"]);

                string ColumnList = "";

                foreach (DataColumn c in t.Columns)
                {
                    ColumnList += c.ColumnName + ",";
                }

                ColumnList = ColumnList.Substring(0, ColumnList.Length - 1);

                string result = this.Prompt("GRID CREATE TABLE IF NOT EXISTS " + d["grid_insert_on"] + " (" + ColumnList + ")");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                if (d["id"] != "null")
                {
                    result = this.Prompt("GRID CREATE TABLE IF NOT EXISTS index_" + d["grid_insert_on"] + " ON " + d["table"] + " (" + d["id"] + ") ");

                    if (result.StartsWith("Error:"))
                    {
                        return result;
                    }

                }

                foreach (DataRow row in t.Rows)
                {

                    Grid.Reset();

                    if (d["id"] != "null")
                    {
                        result = this.Prompt("GRID SELECT * FROM " + d["grid_insert_on"] + " WHERE " + d["id"] + " = " + row[d["id"]]);

                        if (result.StartsWith("Error:"))
                        {
                            return result;
                        }

                        if (result == "[]")
                        {
                            Grid.CreateInsert(d["grid_insert_on"]);
                        }
                        else
                        {
                            Grid.CreateUpdate(d["grid_insert_on"], " WHERE " + d["id"] + " = " + row[d["id"]]);
                        }

                    }
                    else
                    {
                        Grid.CreateInsert(d["grid_insert_on"]);
                    }

                    foreach (DataColumn c in t.Columns)
                    {
                        Grid.AddField(c.ColumnName, row[c.ColumnName]);
                    }

                    result = Grid.Exec();

                    if (result.StartsWith("Error:"))
                    {
                        return result;
                    }

                }

                return result;

            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }

        }


        string SaveJsonToTable(Dictionary<string, string> d)
        {
            try
            {
                string result = "";
                DataTable t = JsonConvert.DeserializeObject<DataTable>(d["json"]);

                if (t.Columns.Contains("PartitionKey"))
                {
                    t.Columns["PartitionKey"].ColumnName = "source_partitionkey";
                }

                if (t.Columns.Contains("partitionkey"))
                {
                    t.Columns["PartitionKey"].ColumnName = "source_partitionkey";
                }

                if (t.Columns.Contains("timestamp"))
                {
                    t.Columns["timestamp"].ColumnName = "source_timestamp"; ;
                }

                if (t.Columns.Contains("odata.etag"))
                {
                    t.Columns.Remove("odata.etag");
                }

                if (t.Columns.Contains("id"))
                {
                    t.Columns["id"].ColumnName = "source_id"; ;
                }


                StringBuilder sb = new StringBuilder();

                foreach (DataColumn item in t.Columns)
                {
                    if (d["id_column"] != "null")
                    {
                        if (item.ColumnName.Trim().ToLower() == d["id_column"].Trim().ToLower())
                        {
                            t.Columns[item.ColumnName].ColumnName = "id";
                        }
                    }

                    if (item.ColumnName.Trim().ToLower() == "union")
                    {
                        t.Columns[item.ColumnName].ColumnName = "_union";
                        sb.Append("_union" + ",");
                    }
                    else if (item.ColumnName.Trim().ToLower() != "id")
                    {
                        sb.Append(item.ColumnName.Replace("-", "_") + ",");
                    }
                }

                string field_list = sb.ToString();
                field_list = field_list.Remove(field_list.Length - 1, 1);

                string local_result = this.Prompt("CREATE TABLE " + d["save_to_table"] + " FIELD LIST " + field_list);

                if (local_result.StartsWith("Error:"))
                {
                    return local_result + "  --> " + "CREATE TABLE " + d["save_to_table"] + " FIELD LIST " + field_list;
                }

                result = this.Prompt($"UPSERT INTO {d["save_to_table"]} PARTITION KEY {d["partition_key"]} VALUES {JsonConvert.SerializeObject(t)}");

                if (result.StartsWith("Error:"))
                {
                    return "Error: SaveToTable: " + result;
                }

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: SaveToGrid {e}";
            }
        }


        string SaveToTable(Dictionary<string, string> d)
        {
            try
            {
                string result = this.Prompt(d["source"]);

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                if (result == "[]")
                {
                    return "Error: There is no data to process";
                }

                DataTable t = JsonConvert.DeserializeObject<DataTable>(result);

                StringBuilder sb = new StringBuilder();

                foreach (DataColumn item in t.Columns)
                {

                    t.Columns[item.ColumnName].ColumnName = StringFunctions.ConvertStringToDbColumn(t.Columns[item.ColumnName].ColumnName);

                    if (d["id_column"] != "null")
                    {
                        if (item.ColumnName.Trim().ToLower() == d["id_column"].Trim().ToLower())
                        {
                            t.Columns[item.ColumnName].ColumnName = "id";
                        }
                    }

                    if (item.ColumnName.Trim().ToLower() == "union")
                    {
                        t.Columns[item.ColumnName].ColumnName = "_union";
                        sb.Append("_union" + ",");
                    }
                    else if (item.ColumnName.Trim().ToLower() != "id")
                    {
                        sb.Append(item.ColumnName + ",");
                    }

                }

                string field_list = sb.ToString();
                field_list = field_list.Remove(field_list.Length - 1, 1);

                string local_result = this.Prompt("CREATE TABLE " + d["save_to_table"] + " FIELD LIST " + field_list);

                if (local_result.StartsWith("Error:"))
                {
                    return "Error: SaveToTable: " + result;
                }

                result = this.Prompt($"UPSERT INTO {d["save_to_table"]} VALUES {JsonConvert.SerializeObject(t)}");

                if (result.StartsWith("Error:"))
                {
                    return "Error: SaveToTable: " + result;
                }

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: SaveToGrid {e}";
            }
        }



        string SaveToGrid(Dictionary<string, string> d)
        {
            try
            {

                string local_result;

                if (d["json"] != "null")
                {
                    local_result = d["json"];
                }
                else
                {
                    local_result = this.Prompt(d["save_to_grid"]);
                }


                if (local_result.StartsWith("Error:"))
                {
                    return "Error: SaveToGrid" + local_result.Replace("Error:", "");
                }

                if (local_result == "[]")
                {
                    return "[]";
                }

                DataTable t = JsonConvert.DeserializeObject<DataTable>(local_result);

                StringBuilder sb = new StringBuilder();

                if (t.Columns.Contains("odata.etag")) t.Columns.Remove("odata.etag");

                foreach (DataColumn item in t.Columns)
                {
                    if (item.ColumnName == "union")
                    {
                        t.Columns[item.ColumnName].ColumnName = "_union";
                        sb.Append("_union" + ",");
                    }
                    else
                    {
                        sb.Append(item.ColumnName + ",");
                    }
                }

                string field_list = sb.ToString();
                field_list = field_list.Remove(field_list.Length - 1, 1);

                if (d["merge_data"] == "false")
                {
                    local_result = Grid.SQLExec("DROP TABLE IF EXISTS " + d["as_table"]);
                }

                local_result = Grid.SQLExec("CREATE TABLE IF NOT EXISTS " + d["as_table"] + "(row_id, " + field_list + ")");

                if (local_result.StartsWith("Error:"))
                {
                    return "Error: create_table " + local_result.Replace("Error:", "");
                }

                local_result = Grid.SQLExec($"CREATE INDEX IF NOT EXISTS index_{d["as_table"]}_row_id ON {d["as_table"]} (row_id)");

                if (local_result.StartsWith("Error:"))
                {
                    return "Error: create_index " + local_result.Replace("Error:", "");
                }

                long max_row = 0;

                if (d["merge_data"] == "true")
                {
                    DataTable t1 = Grid.SQLTable($"SELECT MAX( row_id ) AS max FROM {d["as_table"]}");

                    max_row = 0;

                    if (t1.Rows[0]["max"] != DBNull.Value)
                    {
                        max_row = (long)t1.Rows[0]["max"];
                    }

                    DataTable t2 = Grid.SQLTable($"SELECT * FROM {d["as_table"]} LIMIT 1");

                    foreach (DataColumn item in t.Columns)
                    {
                        if (!t2.Columns.Contains(item.ColumnName))
                        {
                            local_result = Grid.SQLExec($"ALTER TABLE {d["as_table"]} ADD COLUMN {item.ColumnName}");

                            if (local_result.StartsWith("Error:"))
                            {
                                return local_result;
                            }
                        }
                    }
                }

                foreach (DataColumn item in t.Columns)
                {
                    local_result = Grid.SQLExec($"CREATE INDEX IF NOT exists index_{d["as_table"]}_{item.ColumnName} ON {d["as_table"]} ({item.ColumnName})");

                    if (local_result.StartsWith("Error:"))
                    {
                        return local_result;
                    }

                }

                long n = 0;
                long i = max_row;

                foreach (DataRow r in t.Rows)
                {

                    ++n;
                    ++i;

                    Grid.Reset();
                    Grid.CreateInsert(d["as_table"]);
                    Grid.AddField("row_id", i);

                    foreach (DataColumn c in t.Columns)
                    {
                        Grid.AddField(c.ColumnName, r[c.ColumnName]);
                    }

                    local_result = Grid.Exec();

                    if (local_result.StartsWith("Error:"))
                    {
                        return local_result;
                    }
                }

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: SaveToGrid {e}";
            }
        }




        string AngelExecute(string command)
        {
            DbLanguage l = new DbLanguage();
            l.SetCommands(AngelSQL_Commands.Commands());
            Dictionary<string, string> d = l.Interpreter(command);

            if (!string.IsNullOrEmpty(l.errorString)) return l.errorString;

            switch (d.First().Key)
            {
                case "connect":

                    return Angel.Connect(d, this);

                case "command":

                    return Angel.Query(d, this);

                case "stop":

                    Angel.Disconnect(d, this);
                    this.always_use_AngelSQL = false;
                    this.angel_url = "";
                    this.angel_token = "";
                    return "Ok.";

                case "get_token":

                    return this.angel_url + "," + this.angel_token;

                case "set_token":

                    return Settoken(d["set_token"]);

                case "server":

                    return Angel.ServerCommand(d["server"], this);

                default:
                    break;

            }

            return $"Error: Not AngelSQL Command found {command}";

        }


        string Settoken(string token)
        {

            string[] parts = token.Split(',');

            if (parts.Length < 2) return "Error: Url and token are needed";

            this.angel_url = parts[0];
            this.angel_token = parts[1];

            return "Ok.";
        }




        string AzureExecute(string command)
        {

            DbLanguage l = new DbLanguage();
            l.SetCommands(AzureCommands.Commands());
            Dictionary<string, string> d = l.Interpreter(command);

            if (!string.IsNullOrEmpty(l.errorString)) return l.errorString;

            switch (d.First().Key)
            {
                case "connect":

                    if (!this.Azure.ContainsKey(d["alias"]))
                    {
                        try
                        {
                            this.Azure.Add(d["alias"], new AzureTable());
                            string result_local = this.Azure[d["alias"]].TableServiceClient(d["connect"]);

                            if (result_local.StartsWith("Error:"))
                            {
                                return result_local;
                            }
                        }
                        catch (Exception e)
                        {
                            return $"Error: {e.ToString()}";
                        }

                    }

                    return "Ok.";

                case "select":

                    if (!this.Azure.ContainsKey(d["connection_alias"]))
                    {
                        return "Error: It is necessary to first start the Azure connection use the command CONNECT <connection_string> ALIAS <alias_name>";
                    }

                    d["where"] = CreateAzureQuery(d["where"]);

                    this.Azure[d["connection_alias"]].CreateTable(d["from"]);

                    string result = this.Azure[d["connection_alias"]].Query(d["where"]);

                    if (result.StartsWith("Error:"))
                    {
                        return result;
                    }

                    return result;

                case "get_results":

                    int page_size = 1000;
                    int.TryParse(d["page_size"], out page_size);

                    if (page_size == 0) page_size = 1000;

                    return this.Azure[d["connection_alias"]].GetQueryResults(page_size);

                case "save_accounts_to":

                    return SaveAzureAccountsTo(d);

                case "restore_accounts_from":

                    return RestorAzureAccounts(d);

                case "show_connections":

                    return ShowAzureConnections();

                case "get_container_from":

                    return AngelDBTools.StringFunctions.GetAccount(d["get_container_from"]);

                case "upsert_into":

                    if (!this.Azure.ContainsKey(d["connection_alias"]))
                    {
                        return "Error: It is necessary to first start the Azure connection use the command CONNECT <connection_string> ALIAS <alias_name>";
                    }

                    this.Azure[d["connection_alias"]].CreateTable(d["upsert_into"]);
                    result = this.Azure[d["connection_alias"]].InsertValues(d["values"]);

                    if (result.StartsWith("Error:"))
                    {
                        return result;
                    }

                    return result;

                case "clear_connectios":

                    Azure.Clear();
                    return "Ok.";

                default:
                    break;

            }

            return $"Error: Not Azure Command found {command}";
        }

        string SQLServerExecute(string command)
        {

            try
            {
                DbLanguage l = new DbLanguage();
                l.SetCommands(SQLServerCommands.Commands());
                Dictionary<string, string> d = l.Interpreter(command);

                if (!string.IsNullOrEmpty(l.errorString)) return l.errorString;

                switch (d.First().Key)
                {
                    case "connect":

                        try
                        {
                            if (this.SQLServer.ContainsKey(d["alias"]))
                            {
                                SQLServerInfo sQLServerInfo = this.SQLServer[d["alias"]];
                                this.SQLServer.TryRemove(d["alias"], out sQLServerInfo);
                            }

                            SQLServerInfo sql = new SQLServerInfo();
                            sql.ConnectionString = d["connect"];
                            sql.SQLTools = new SQLServerTools(sql.ConnectionString);

                            sql.SQLTools.Connection = new SqlConnection(d["connect"]);
                            sql.SQLTools.Connection.Open();
                            sql.SQLTools.SQLCommand = sql.SQLTools.Connection.CreateCommand();
                            this.SQLServer.TryAdd(d["alias"], sql);

                        }
                        catch (Exception e)
                        {
                            return $"Error: SQL Server Connect {e.ToString()}";
                        }

                        return "Ok.";

                    case "query":

                        if (!this.SQLServer.ContainsKey(d["connection_alias"]))
                        {
                            return "Error: It is necessary to first start the SQL SERVER connection use the command CONNECT <connection_string> ALIAS <alias_name>";
                        }

                        return SQLServerQuery(d["query"], d["connection_alias"]);

                    case "save_accounts_to":

                        return SaveSQLServerAccountsTo(d);

                    case "restore_accounts_from":

                        return RestoreSQLServerAccounts(d);

                    case "show_connections":

                        return ShowSQLServerConnections();

                    case "insert_into":

                        return SQLServerInsert(d);

                    case "update":

                        return SQLServerUpdate(d);

                    case "begin_transaction":

                        return SQLServerBenginTransaction(d);

                    case "commit_transaction":

                        return SQLServerCommitTransaction(d);

                    case "rollback_transaction":

                        return SQLServerRollbackTransaction(d);

                    case "exec":

                        if (!this.SQLServer.ContainsKey(d["connection_alias"]))
                        {
                            return "Error: It is necessary to first start the SQL SERVER connection use the command CONNECT <connection_string> ALIAS <alias_name>";
                        }

                        return this.SQLServer[d["connection_alias"]].SQLTools.DirectExec(d["exec"]);


                    default:
                        break;

                }

                return $"Error: Not SQL Server Command found {command}";

            }
            catch (Exception e)
            {
                return $"Error: SQL Server {e}";
            }

        }

        public string SQLServerInsert(Dictionary<string, string> d)
        {

            try
            {
                if (!this.SQLServer.ContainsKey(d["connection_alias"]))
                {
                    return "Error: It is necessary to first start the SQL Server connection use the command CONNECT <connection_string> ALIAS <alias_name>";
                }

                if (this.SQLServer[d["connection_alias"]].SQLTools is null)
                {
                    this.SQLServer[d["connection_alias"]].SQLTools = new SQLServerTools(this.SQLServer[d["connection_alias"]].ConnectionString);
                }

                return this.SQLServer[d["connection_alias"]].SQLTools.Insert(d);

            }
            catch (Exception e)
            {
                return $"Error: {e.ToString()}";
            }

        }

        public string SQLServerUpdate(Dictionary<string, string> d)
        {

            try
            {
                if (!this.SQLServer.ContainsKey(d["connection_alias"]))
                {
                    return "Error: It is necessary to first start the SQL Server connection use the command CONNECT <connection_string> ALIAS <alias_name>";
                }

                if (this.SQLServer[d["connection_alias"]].SQLTools is null)
                {
                    this.SQLServer[d["connection_alias"]].SQLTools = new SQLServerTools(this.SQLServer[d["connection_alias"]].ConnectionString);
                }

                return this.SQLServer[d["connection_alias"]].SQLTools.Update(d);

            }
            catch (Exception e)
            {
                return $"Error: {e.ToString()}";
            }

        }

        public string SQLServerBenginTransaction(Dictionary<string, string> d)
        {
            try
            {
                if (!this.SQLServer.ContainsKey(d["connection_alias"]))
                {
                    return "Error: It is necessary to first start the SQL Server connection use the command CONNECT <connection_string> ALIAS <alias_name>";
                }

                if (!(this.SQLServer[d["connection_alias"]].SQLTools.Connection.State == ConnectionState.Open))
                {
                    this.SQLServer[d["connection_alias"]].SQLTools.Connection.Open();
                }

                this.SQLServer[d["connection_alias"]].SQLTools.SQLCommand = this.SQLServer[d["connection_alias"]].SQLTools.Connection.CreateCommand();
                this.SQLServer[d["connection_alias"]].SQLTools.transaction = this.SQLServer[d["connection_alias"]].SQLTools.Connection.BeginTransaction();
                this.SQLServer[d["connection_alias"]].SQLTools.SQLCommand.Transaction = this.SQLServer[d["connection_alias"]].SQLTools.transaction;

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: {e.ToString()}";
            }

        }


        public string SQLServerCommitTransaction(Dictionary<string, string> d)
        {
            try
            {
                if (!this.SQLServer.ContainsKey(d["connection_alias"]))
                {
                    return "Error: It is necessary to first start the SQL Server connection use the command CONNECT <connection_string> ALIAS <alias_name>";
                }

                if (!(this.SQLServer[d["connection_alias"]].SQLTools.Connection.State == ConnectionState.Open))
                {
                    return "Error: The connection to the database is not open, there is no transaction that can be committed";
                }

                this.SQLServer[d["connection_alias"]].SQLTools.transaction.Commit();

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: {e.ToString()}";
            }

        }

        public string SQLServerRollbackTransaction(Dictionary<string, string> d)
        {
            try
            {
                if (!this.SQLServer.ContainsKey(d["connection_alias"]))
                {
                    return "Error: It is necessary to first start the SQL Server connection use the command CONNECT <connection_string> ALIAS <alias_name>";
                }

                if (!(this.SQLServer[d["connection_alias"]].SQLTools.Connection.State == ConnectionState.Open))
                {
                    return "Error: The connection to the database is not open, there is no transaction that can be RollOut";
                }

                this.SQLServer[d["connection_alias"]].SQLTools.transaction.Rollback();

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: {e.ToString()}";
            }

        }


        public string SQLServerQuery(string command, string alias)
        {

            try
            {
                if (!this.SQLServer.ContainsKey(alias))
                {
                    return "Error: It is necessary to first start the SQL Server connection use the command CONNECT <connection_string> ALIAS <alias_name>";
                }

                if (command.Trim().ToUpper().StartsWith("SELECT"))
                {
                    DataTable t = this.SQLServer[alias].SQLTools.SQLDataTable(command);
                    return JsonConvert.SerializeObject(t, Formatting.Indented);
                }

                string result = this.SQLServer[alias].SQLTools.DirectExec(command);

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: {e.ToString()}";
            }

        }


        public string SaveAzureAccountsTo(Dictionary<string, string> d)
        {
            try
            {

                Dictionary<string, string> dic = new Dictionary<string, string>();

                foreach (string item in this.Azure.Keys)
                {
                    dic.Add(item, this.Azure[item].ConnectionString);
                }

                string json = JsonConvert.SerializeObject(dic);
                return AngelDBTools.StringFunctions.SaveEncriptedFile(d["save_accounts_to"], json, d["password"]);
            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }

        public string RestorAzureAccounts(Dictionary<string, string> d)
        {
            try
            {

                if (!File.Exists(d["restore_accounts_from"]))
                {
                    return $"Error: The file does not exists {d["restore_accounts_from"]}";
                }

                string json = AngelDBTools.StringFunctions.RestoreEncriptedFile(d["restore_accounts_from"], d["password"]);

                Dictionary<string, string> dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                foreach (string item in dic.Keys)
                {
                    string result = this.Prompt($"AZURE CONNECT {dic[item]} ALIAS {item}");

                    if (result.StartsWith("Error:"))
                    {
                        return result;
                    }
                }

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }


        public string ShowAzureConnections()
        {

            List<string> d = new List<string>();

            foreach (string item in this.Azure.Keys)
            {
                d.Add(item);
            }

            return JsonConvert.SerializeObject(d, Formatting.Indented);
        }


        public string SaveSQLServerAccountsTo(Dictionary<string, string> d)
        {
            try
            {
                return AngelDBTools.StringFunctions.SaveEncriptedFile(d["save_accounts_to"], JsonConvert.SerializeObject(this.SQLServer), d["password"]);
            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }


        public string RestoreSQLServerAccounts(Dictionary<string, string> d)
        {
            try
            {
                string json = AngelDBTools.StringFunctions.RestoreEncriptedFile(d["restore_accounts_from"], d["password"]);
                this.SQLServer = JsonConvert.DeserializeObject<ConcurrentDictionary<string, SQLServerInfo>>(json);
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }

        public string ShowSQLServerConnections()
        {

            List<string> d = new List<string>();

            foreach (string item in this.SQLServer.Keys)
            {
                d.Add(item);
            }

            return JsonConvert.SerializeObject(d, Formatting.Indented);
        }



        string CreateAzureQuery(string query)
        {
            query = query.Replace(" = ", " eq ");
            query = query.Replace(" > ", " gt ");
            query = query.Replace(" >= ", " ge ");
            query = query.Replace(" < ", " lt ");
            query = query.Replace(" <= ", " le ");
            query = query.Replace(" <> ", " ne ");
            query = query.Replace(" AND ", " and ");
            query = query.Replace(" OR ", " or ");

            return query;

        }

        string SavePrompt(Dictionary<string, string> d)
        {
            try
            {
                string local_result = this.Prompt(d["source"]);

                if (local_result.StartsWith("Error:"))
                {
                    return local_result;
                }

                if (d["as_csv"] == "false")
                {
                    File.WriteAllText(d["save_to"], local_result, Encoding.UTF8);
                }
                else
                {
                    DataTable dt = JsonConvert.DeserializeObject<DataTable>(local_result);
                    AngelDBTools.StringFunctions.ToCSV(dt, d["save_to"], d["string_delimiter"]);
                }

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: {e.ToString()}";
            }
        }


        string CreateDB(string dbname, string ConnectionString)
        {
            if (!dbs.ContainsKey(dbname))
            {
                dbs.Add(dbname, new DB());
            }

            return dbs[dbname].Prompt(ConnectionString);

        }

        string PromptDB(string dbname, string Command)
        {
            if (!dbs.ContainsKey(dbname))
            {
                return $"Error: Database not exists; use CREAT DB command first {dbname}";
            }

            return dbs[dbname].Prompt(Command);

        }

        string RemoveDB(string dbname)
        {
            if (!dbs.ContainsKey(dbname))
            {
                return $"Error: Database not exists: {dbname}";
            }

            dbs.Remove(dbname);
            return "Ok.";
        }


        public void Close()
        {

        }

        public string AnalizeParentesis(Dictionary<string, string> d)
        {

            if (d["->"] != "null")
            {
                if (!this.TablesArea.ContainsKey(d["("]))
                {
                    return $"Error: There is no area: {d["("]}";
                }

                TableArea ta = this.TablesArea[d["("]];

                switch (d["->"].Trim().ToLower())
                {
                    case "eof()":

                        return ta.EOF();

                    case "next()":

                        return ta.Next();

                    case "update()":

                        return ta.UpdateData();

                    case "new()":

                        return ta.NewRow();

                    default:

                        return ta.Field(d["->"].Trim(), d["="].Trim());
                }

            }

            return "Ok.";

        }

        public string UseTable(Dictionary<string, string> d)
        {

            if (!this.TablesArea.ContainsKey(d["use_table"]))
            {
                this.TablesArea.Add(d["use_table"], null);
            }

            TableArea ta = new TableArea();
            string result = ta.UseTable(d, this);
            this.TablesArea[d["use_table"]] = ta;

            return result;
        }


        public DataTable GetDataTable(string Data)
        {
            try
            {
                return JsonConvert.DeserializeObject<DataTable>(Data);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: GetDataTable: " + e.ToString());
                return null;
            }

        }

        public string GetJson(object o, bool NullSerialize = true)
        {

            if (NullSerialize)
            {
                return JsonConvert.SerializeObject(o, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
            }

            return JsonConvert.SerializeObject(o, Formatting.Indented);
        }

        public DataRow GetDataRow(string Data)
        {
            DataTable dt = JsonConvert.DeserializeObject<DataTable>(Data);
            return dt.Rows[0];
        }

        public T DeserializeDBResult<T>(string json) where T : new()
        {
            var jObject = JObject.Parse(json);
            var result = new T();
            var props = typeof(T).GetProperties();

            foreach (var prop in props)
            {
                JToken token;

                if (!jObject.TryGetValue(prop.Name, StringComparison.OrdinalIgnoreCase, out token))
                    continue;

                try
                {
                    if (prop.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType.IsGenericType)
                    {
                        // Si el valor es un string, intentar deserializar como JSON
                        if (token.Type == JTokenType.String)
                        {
                            var nestedJson = token.ToString();
                            var listValue = JsonConvert.DeserializeObject(nestedJson, prop.PropertyType);
                            prop.SetValue(result, listValue);
                        }
                        else
                        {
                            // Ya viene como array normal
                            var listValue = token.ToObject(prop.PropertyType);
                            prop.SetValue(result, listValue);
                        }
                    }
                    else
                    {
                        // Valor simple
                        var value = token.ToObject(prop.PropertyType);
                        prop.SetValue(result, value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializando propiedad '{prop.Name}': {ex.Message}");
                }
            }

            return result;
        }


        public static dynamic DataRowToDynamic(string Data)
        {
            var dynamicObject = new ExpandoObject() as IDictionary<string, object>;

            DataTable dt = JsonConvert.DeserializeObject<DataTable>(Data);
            DataRow row = dt.Rows[0];

            foreach (DataColumn column in row.Table.Columns)
            {
                dynamicObject[column.ColumnName] = row[column];
            }

            return dynamicObject;
        }


        public string GetVars(Dictionary<string, object> d)
        {
            return JsonConvert.SerializeObject(d, Formatting.Indented);
        }

        public string RunCMD(string command)
        {

            var processInfo = new ProcessStartInfo("cmd.exe", command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = this.BaseDirectory + "scripts"
            };

            StringBuilder sb = new();
            Process p = Process.Start(processInfo);
            p.OutputDataReceived += (sender, args) => sb.AppendLine(args.Data);
            p.BeginOutputReadLine();
            p.WaitForExit(1000);
            return sb.ToString();
        }


        public string RunScript(Dictionary<string, string> d)
        {

            if (!File.Exists(d["run"]))
            {
                return "Error: The file does not exists";
            }

            return ProcessFile(d["run"], d["verbose"]);

        }

        public string ProcessFile(string fileName, string verbose)
        {

            string result = "";
            StringBuilder sb = new StringBuilder();

            if (File.Exists(fileName))
            {
                int counter = 0;
                string line;

                StreamReader file = new StreamReader(fileName);

                string command = "";

                while ((line = file.ReadLine()) != null)
                {

                    ++counter;

                    if (line.Trim().StartsWith(@"//")) continue;
                    if (line.Trim() == "") continue;

                    command += line;

                    if (!line.Trim().EndsWith(";"))
                    {
                        continue;
                    }

                    command = command.Trim();
                    command = command.Substring(0, command.Length - 1);

                    if (command.Trim().StartsWith("IGNORE ERROR"))
                    {
                        result = Prompt(command.Trim()[12..]);
                        sb.AppendLine(result);
                        command = "";
                        continue;
                    }

                    if (command.Trim().StartsWith("QUIT"))
                    {
                        file.Close();
                        return sb.ToString();
                    }

                    if (verbose == "true")
                    {
                        Monitor.ShowLine(command, ConsoleColor.Yellow);
                    }

                    result = Prompt(command);
                    sb.AppendLine(result);
                    command = "";

                    if (result.StartsWith("Error:"))
                    {
                        Monitor.ShowError(result);
                        Monitor.ShowError($"In line {counter}");
                        file.Close();
                        return sb.ToString();
                    }
                }

                file.Close();
            }
            else
            {
                Monitor.ShowError($"File does not exists {fileName}");
            }

            return "Ok.";
        }

        public string SetVar(string var_name, string value)
        {

            try
            {

                value = value.Trim();

                if (value.StartsWith("'") && value.EndsWith("'"))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                if (!vars.ContainsKey(var_name) && value == "null")
                {
                    return "";
                }
                else if (vars.ContainsKey(var_name) && value == "null")
                {
                    return vars[var_name].ToString();
                }
                else if (vars.ContainsKey(var_name) && value != "null")
                {
                    vars[var_name] = value;
                    return vars[var_name].ToString();
                }
                else
                {
                    vars.Add(var_name, value);
                    return vars[var_name].ToString();
                }
            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }


        public string SendToClient(string message)
        {
            if (this.OnSendMessage is null) return "";
            string result = "";
            this.OnSendMessage(message, ref result);
            return result;
        }


        public string InsertInto(string tablename, object values, string partitionkey = "null", string exclude_columns = "null")
        {

            // Si el objeto es nulo, retorna inmediatamente
            if (values == null) return "Error: UpsertInto There are no values ​​to insert or update";

            var configuracion = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            //Object clone = ObjectConverter.CreateDictionaryOrListFromObject(values);

            string result = this.Prompt($"INSERT INTO {tablename} PARTITION KEY {partitionkey} EXCLUDE COLUMNS {exclude_columns} VALUES '''{JsonConvert.SerializeObject(values, configuracion)}'''");
            return result;

        }

        public string UpsertInto(string tablename, object values, string partitionkey = "null", string exclude_columns = "null")
        {
            // Si el objeto es nulo, retorna inmediatamente
            if (values == null) return "Error: UpsertInto There are no values ​​to insert or update";

            var configuracion = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            //Object clone = ObjectConverter.CreateDictionaryOrListFromObject(values);

            string result = this.Prompt($"UPSERT INTO {tablename} PARTITION KEY {partitionkey} EXCLUDE COLUMNS {exclude_columns} VALUES '''{JsonConvert.SerializeObject(values, configuracion)}'''");
            return result;
        }



        public virtual void Dispose()
        {
            try
            {

                if (!string.IsNullOrEmpty(this.angel_url))
                {
                    this.Prompt("ANGEL STOP");
                }

                this.CancelTransactions = true;

                foreach (string key in this.dbs.Keys)
                {
                    this.dbs[key].CancelTransactions = true;
                }

                config = null;
                vars = null;
                //parameters = null;
                partitionsrules = null;
                language = null;
                dbs = null;
                MainDatabase = null;
                TablesArea = null;
                partitions = null;
                SQLiteConnections = null;
                web = null;
                Azure = null;
                SQLServer = null;
                Grid = null;
                Script = null;
            }
            catch (Exception)
            {
            }

        }

        public string jSonSerialize(object o)
        {
            return JsonConvert.SerializeObject(o, Formatting.Indented);
        }

        public T jSonDeserialize<T>(string jSon)
        {
            return JsonConvert.DeserializeObject<T>(jSon);
        }

        public string GetTimeStamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
        }

        public string GetWords(decimal number, string language, string currency_symbol, string currency_leyend)
        {

            number = Math.Round(number, 6);
            double decimal_part = (float)(number - Math.Truncate(number));
            decimal_part = Math.Round(decimal_part, 2);

            if (decimal_part == 1)
            {
                number++;
                decimal_part = 0;
            }

            string words = AngelDBTools.StringFunctions.NumberToWords((long)number, language);
            words = words + " " + currency_leyend + " " + decimal_part.ToString(".00").Replace(".", "") + "/100 " + currency_symbol;

            return words.ToUpper();
        }

        // // TODO: reemplazar el finalizador solo si "Dispose(bool disposing)" tiene código para liberar los recursos no administrados
        // ~DB()
        // {
        //     // No cambie este código. Coloque el código de limpieza en el método "Dispose(bool disposing)".
        //     Dispose(disposing: false);
        // }

    }

    public class PartitionsInfo
    {
        public string account_database_table_partition { get; set; } = "";
        public string ConnectionString { get; set; }
        public string table_type = "";
        public string partition_name { get; set; }
        public string file_name { get; set; }
        public QueryTools sqlite { get; set; } = null;

        public string UpdateTimeStamp()
        {
            try
            {
                return sqlite.ExecSQLDirect($"UPDATE partitions SET timestamp = '{System.DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff")}' WHERE partition = '{this.partition_name}'");
            }
            catch (Exception e)
            {
                return $"Error: UpdateTimeStamp {e}";
            }
        }

    }

    public static class Cloner
    {
        public static T DeepClone<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }


    public class TableInfo
    {
        public string ConnectionString { get; set; }
        public string table_directory { get; set; }
        public string table_type { get; set; }
    }


    public class SQLServerInfo
    {
        public string ConnectionString { get; set; }
        public SQLServerTools SQLTools { get; set; } = null;


    }

}
