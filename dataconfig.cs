namespace AngelDB
{

    public static class Dataconfig
    {

        public static string CreateInitTables(DB mainClass)
        {
            string result;
            SqliteTools sqlite;
            mainClass.sqliteConnectionString = $"Data Source={mainClass.BaseDirectory + mainClass.os_directory_separator + "config.db" };";
            sqlite = new SqliteTools(mainClass.sqliteConnectionString);

            result = sqlite.SQLExec("CREATE TABLE IF NOT EXISTS masteraccounts (user PRIMARY KEY, name, password, account, deleted, timestamp)");

            if (result != "Ok.")
            {
                return $"Error: Cannot create table (users): {result}";
            }

            result = sqlite.SQLExec("CREATE INDEX IF NOT EXISTS masteraccounts_x1 ON masteraccounts (account)");

            if (result != "Ok.")
            {
                return $"Error: Cannot create index (masteraccounts_x1): {result}";
            }

            result = sqlite.SQLExec("CREATE INDEX IF NOT EXISTS masteraccounts_x2 ON masteraccounts (timestamp)");

            if (result != "Ok.")
            {
                return $"Error: Cannot create index (masteraccounts_x2): {result}";
            }

            result = sqlite.SQLExec("CREATE INDEX IF NOT EXISTS masteraccounts_x3 ON masteraccounts (deleted)");

            if (result != "Ok.")
            {
                return $"Error: Cannot create index (masteraccounts_x3): {result}";
            }

            result = sqlite.SQLExec("CREATE TABLE IF NOT EXISTS databases (database, account, deleted, timestamp, PRIMARY KEY ( database, account))");

            if (result != "Ok.")
            {
                return $"Error: Cannot create table (database): {result}";
            }

            result = sqlite.SQLExec("CREATE INDEX IF NOT EXISTS databases_x1 ON databases (timestamp)");

            if (result != "Ok.")
            {
                return $"Error: Cannot create index (databases_x1): {result}";
            }

            result = sqlite.SQLExec("CREATE INDEX IF NOT EXISTS databases_x2 ON databases (deleted)");

            if (result != "Ok.")
            {
                return $"Error: Cannot create index (databases_x2): {result}";
            }

            result = sqlite.SQLExec("CREATE TABLE IF NOT EXISTS users ( account, user, name, password, database, tables, readonly, deleted, timestamp, PRIMARY KEY (account, user) )");

            if (result != "Ok.")
            {
                return $"Error: Cannot create table (users): {result}";
            }

            result = sqlite.SQLExec("CREATE INDEX IF NOT EXISTS users_x1 ON users (timestamp)");

            if (result != "Ok.")
            {
                return $"Error: Cannot create index (users_x1): {result}";
            }

            result = sqlite.SQLExec("CREATE TABLE IF NOT EXISTS partitionrules ( account, database, table_name, partition, connection_string, connection_type, timestamp, PRIMARY KEY (account, database, table_name, partition) )");

            if (result != "Ok.")
            {
                return $"Error: Cannot create table (partitionrules): {result}";
            }

            result = sqlite.SQLExec("CREATE TABLE IF NOT EXISTS azure_connections ( alias PRIMARY KEY, connection_string )");

            if (result != "Ok.")
            {
                return $"Error: Cannot create table (partitionrules): {result}";
            }

            result = sqlite.SQLExec("CREATE TABLE IF NOT EXISTS table_sync ( name PRIMARY KEY, from_table, from_partitions, from_connection, from_account, from_database, to_connection, to_account, to_database, to_table )");

            if (result != "Ok.")
            {
                return $"Error: Cannot create table (table_sync): {result}";
            }

            return "Ok.";


        }

        public static string CreateTablesDataBase(string ConnectionString)
        {
            SqliteTools sqlite = new SqliteTools(ConnectionString);
            string result;
            result = sqlite.SQLExec("CREATE TABLE tables (tablename PRIMARY KEY, fieldlist, storage, deleted, tabletype, timestamp )");

            if (result != "Ok.")
            {
                return $"Error: creating table {result}";
            }

            return "Ok.";

        }

    }

}
