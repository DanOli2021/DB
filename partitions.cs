using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Newtonsoft.Json;

namespace AngelDB
{
    public static class Partitions
    {
        public static string CreatePartitionRule(Dictionary<string, string> d, DB mainClass) 
        {

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);
            DataTable t = sqlite.SQLTable($"SELECT * FROM partitionrules WHERE account = '{mainClass.account}' AND database = '{mainClass.database}' AND table_name = '{d["table"]}' AND partition = '{d["partition_key"]}'");

            if (t.Rows.Count == 0)
            {
                sqlite.CreateInsert("partitionrules");
            }
            else 
            {
                sqlite.CreateUpdate("partitionrules", $"account = '{mainClass.account}' AND database = '{mainClass.database}' AND table_name = '{d["table"]}' AND partition = '{d["partition_key"]}'");
            }

            //id PRIMARY KEY, table_name, partition, rule, timestamp

            sqlite.AddField("table_name", d["table"]);
            sqlite.AddField("partition", d["partition_key"]);
            sqlite.AddField("connection_string", d["connection"]);
            sqlite.AddField("account", mainClass.account);
            sqlite.AddField("database", mainClass.database);
            sqlite.AddField("timestamp", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"));

            string result = sqlite.Exec();  

            if( result.StartsWith("Error:")) 
            {
                return "Error: CreatePartitionRule: " +result;
            }

            //mainClass.ResetPartitionRules();

            return result;
        }

        
        public static string DeletePartitionRule(Dictionary<string, string> d, DB mainClass)
        {
            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            string result = sqlite.SQLExec($"DELETE FROM partitionrules WHERE account = '{mainClass.account}' AND database = '{mainClass.database}' AND table_name = '{d["table"]}' AND partition = '{d["partition_key"]}'");

            if( result.StartsWith("Error:")) 
            {
                return "Error: DeletePartitionRule: " +result;
            }

            //mainClass.ResetPartitionRules();

            return result;
        }


        public static string GetPartitionRules(Dictionary<string, string> d, DB mainClass)
        {

            if (mainClass.account == "") return "Error: No account selected";
            if (mainClass.database == "") return "Error: No database selected";
            if (mainClass.IsReadOnly == true) return "Error: Your account is read only";

            if (mainClass.accountType == ACCOUNT_TYPE.DATABASE_USER) return "Error: You cannot see the partition rules with your access level";

            SqliteTools sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            DataTable t;

            if (d["where"] == "null")
            {
                t = sqlite.SQLTable($"SELECT * FROM partitionrules WHERE account = '{mainClass.account}' ORDER BY account, database, table_name");
            }
            else 
            {
                t = sqlite.SQLTable($"SELECT * FROM partitionrules WHERE account = '{mainClass.account}' AND {d["where"]}");
            }

            return JsonConvert.SerializeObject(t, Formatting.Indented);

        }
    }
}
