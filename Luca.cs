using System.Linq;
using System.Data;
using Newtonsoft.Json;
using System.Globalization;
using System.Collections.Generic;

namespace AngelDB
{
    static class Luca
    {
        /// <summary>
        /// We create the structure of the accounting processes database (1100)
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static string CreateStructure(DB db)
        {

            string result;

            result = db.Prompt("CREATE TABLE accountingdata FIELD LIST ownername, invoice STORAGE main");

            if (result != "Ok.")
            {
                return result;
            }

            result = db.Prompt("CREATE TABLE accounts FIELD LIST businessaccount, description, level, masteraccount, accounttype, user STORAGE main TYPE SEARCH");

            if (result != "Ok.")
            {
                return result;
            }

            result = db.Prompt("CREATE TABLE accounting FIELD LIST account, operationdate, masteraccount, businessaccount, description, debit NUMERIC, credit NUMERIC, Balance NUMERIC, documentidenfier, user STORAGE main");

            if (result != "Ok.")
            {
                return result;
            }

            result = db.Prompt("CREATE TABLE balance FIELD LIST description, masteraccount, debit NUMERIC, credit NUMERIC, user STORAGE main");

            if (result != "Ok.")
            {
                return result;
            }

            result = db.Prompt("CREATE TABLE periods FIELD LIST active NUMERIC, closed NUMERIC, user STORAGE main");

            if (result != "Ok.")
            {
                return result;
            }

            return "Ok.";
        }

        public static string DeleteInfo(DB db)
        {

            string result;

            result = DeleteTable("accounts", db);

            if (result != "Ok.")
            {
                return result;
            }

            result = DeleteTable("accounting", db);

            if (result != "Ok.")
            {
                return result;
            }

            result = DeleteTable("balance", db);

            if (result != "Ok.")
            {
                return result;
            }

            result = DeleteTable("periods", db);

            if (result != "Ok.")
            {
                return result;
            }

            return "Ok.";

        }


        private static string DeleteTable(string TableName, DB db)
        {

            string result;

            result = db.Prompt($"GET PARTITIONS FROM {TableName}");

            if (result != "Ok.")
            {
                return result;
            }

            DataTable p = DataTools.JsonWebMiDictionaryToDataTable(result);

            foreach (DataRow item in p.Rows)
            {
                result = db.Prompt($"DELETE FROM {TableName} PARTITION KEY {item["partition"]}");

                if (result != "Ok.")
                {
                    return result;
                }
            }

            return "Ok.";

        }



        /// <summary>
        /// Insert or update account (1200)
        /// </summary>
        /// <param name="d"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static string CreateAccount(Dictionary<string, string> d, DB db)
        {

            string result;

            if (d["create_account"].Trim() == "")
            {
                return "Error: It is necessary to indicate the account";
            }

            if (d["description"].Trim() == "")
            {
                return "Error: You need to indicate the description of the account";
            }

            if (d["master_account"].Trim() != "")
            {
                result = db.Prompt($"SELECT SEARCH * FROM accounts PARTITION KEY accounts WHERE id = '{d["master_account"]}'");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                if (result == "{}")
                {
                    return $"Error: The master account does not exist {d["master_account"]}";
                }

                if (d["create_account"].Trim().ToUpper() == d["master_account"].Trim().ToUpper())
                {
                    return $"The account and the master account can not be the same";
                }

            }


            Dictionary<string, object> values = new Dictionary<string, object>();
            values.Add("id", d["create_account"]);
            values.Add("businessaccount", d["business_account"]);
            values.Add("description", d["description"]);
            values.Add("masteraccount", d["master_account"]);
            values.Add("accounttype", d["account_type"]);
            values.Add("user", db.user);
            result = db.Prompt($"INSERT INTO accounts PARTITION KEY accounts VALUES {JsonConvert.SerializeObject(values)}");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            result = AccountLevel(d["create_account"], db, 0);

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            int level = int.Parse(result.Substring(3));
            return db.Prompt($"UPDATE accounts PARTITION KEY accounts SET level = {level} WHERE id = '{d["create_account"]}'");

        }


        public static string DeleteAccount(Dictionary<string, string> d, DB db)
        {

            string result = db.Prompt($"SELECT * FROM accounting WHERE account = '{d["delete_account"]}' LIMIT 1");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result != "{}")
            {
                Dictionary<string, DataTable> webmidic = DataTools.JsonToWebMiDictionary(result);
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                foreach (string item in webmidic.Keys)
                {
                    sb.Append(webmidic[item].Rows[0]["period"] + ",");
                }

                result = sb.ToString();
                result = result.Substring(0, result.Length - 1);

                return $"Error: The account exists in the periods {result}";
            }

            return db.Prompt($"DELETE FROM accounts PARTITION KEY accounts WHERE id = '{d["delete_account"]}'");

        }

        /// <summary>
        /// Insert or update policy detail (1300)
        /// </summary>
        /// <param name="d"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static string InsertOrUpdateAccount(Dictionary<string, string> d, DB db)
        {

            string result;

            result = IsValidPeriod(d["period"]);

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            result = db.Prompt($"SELECT * FROM periods PARTITION KEY periods WHERE id = '{d["period"]}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "{}")
            {
                return $"Error: The period does not exist {d["period"]}";
            }

            DataRow r = DataTools.JsonWebMiDictionaryToDataRow(result);

            if (r["closed"].ToString() == "1")
            {
                return $"Error: The period is closed, Can't be insert or update account entry {d["period"]}";
            }

            result = db.Prompt($"SELECT SEARCH * FROM accounts PARTITION KEY accounts WHERE id = '{d["account"]}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "{}")
            {
                return $"Error: Account does not exist {d["account"]}";
            }


            Dictionary<string, DataTable> AccountData = DataTools.JsonToWebMiDictionary(result);
            decimal balance = GetTheLastBalance(d["period"], d["account"], d["operation_date"], db);

            /// The balance is calculated, which is the debit minus the credit, plus the previous balance. (1330)
            decimal Debit;
            decimal Credit;

            decimal.TryParse(d["debit"].ToString(), out Debit);
            decimal.TryParse(d["credit"].ToString(), out Credit);

            balance += (Debit - Credit);

            Dictionary<string, object> values = new Dictionary<string, object>();
            values.Add("id", d["guid"]);
            values.Add("operationdate", d["operation_date"]);
            values.Add("account", d["account"]);
            values.Add("businessaccount", (string)AccountData.First().Value.Rows[0]["businessaccount"]);
            values.Add("masteraccount", (string)AccountData.First().Value.Rows[0]["masteraccount"]);
            values.Add("description", d["description"]);
            values.Add("debit", Debit);
            values.Add("credit", Credit);
            values.Add("balance", balance);
            values.Add("documentidenfier", d["document"]);
            values.Add("user", db.user);
            result = db.Prompt($"INSERT INTO accounting PARTITION KEY {d["period"]} VALUES {JsonConvert.SerializeObject(values)}");

            if (result != "Ok.")
            {
                return result;
            }

            return result;

        }


        public static string DeleteAccountEntry(Dictionary<string, string> d, DB db)
        {

            string result;

            result = IsValidPeriod(d["period"]);

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            result = db.Prompt($"SELECT * FROM periods PARTITION KEY periodos WHERE period = '{d["period"]}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "{}")
            {
                return $"Error: The period does not exist {d["period"]}";
            }

            DataRow r = DataTools.JsonWebMiDictionaryToDataRow(result);

            if (r["closed"].ToString() == "1")
            {
                return $"Error: The period is closed, Can't be erased account entry {d["period"]}";
            }

            result = db.Prompt($"SELECT * FROM accounting PARTITION KEY {d["period"]} WHERE id = '{d["guid"]}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "{}")
            {
                return $"Error: Account entry does not exist {d["guid"]}";
            }

            result = db.Prompt($"DELETE FROM accounting PARTITION KEY {d["period"]} WHERE id = {d["guid"]}");

            if (result.StartsWith("Error:"))
            {
                return result;
            }


            return result;

        }



        /// <summary>
        /// The last movement of this account that is less than the date of the newly inserted record is searched and the last balance is obtained (1310)
        /// </summary>
        /// <param name="Period"></param>
        /// <param name="account"></param>
        /// <param name="operationdate"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static decimal GetTheLastBalance(string Period, string account, string operationdate, DB db)
        {
            string result = db.Prompt($"SELECT balance FROM accounting PARTITION KEY {Period} WHERE account = '{account}' AND operationdate < '{operationdate}' ORDER BY operationdate DESC LIMIT 1");

            decimal balance = 0;

            if (result != "{}")
            {
                Dictionary<string, DataTable> LastAccountBalance = DataTools.JsonToWebMiDictionary(result);
                decimal.TryParse(LastAccountBalance.First().Value.Rows[0]["balance"].ToString(), out balance);
            }

            return balance;

        }


        /// <summary>
        /// If by the date there are subsequent records, the new balance of the remaining movements is calculated (1410)
        /// </summary>
        /// <param name="Period"></param>
        /// <param name="account"></param>
        /// <param name="operationdate"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static string CarryBalances(Dictionary<string, string> d, DB db)
        {

            string result = db.Prompt($"SELECT * FROM accounting PARTITION KEY {d["period"]} WHERE account = '{d["account"]}' AND operationdate > '{d["operation_date"]}' ORDER BY operationdate");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result.StartsWith("{}"))
            {
                return "Ok.";
            }

            Dictionary<string, DataTable> Recalculate = DataTools.JsonToWebMiDictionary(result);
            DataTable accounting = Recalculate.First().Value;

            foreach (DataRow item in accounting.Rows)
            {

                #pragma warning disable CS8604 // Posible argumento de referencia nulo
                decimal balance = GetTheLastBalance(item["partitionkey"].ToString(), item["account"].ToString(), item["operationdate"].ToString(), db);
                #pragma warning restore CS8604 // Posible argumento de referencia nulo
                result = db.Prompt($"UPDATE accounting PARTITION KEY {d["period"]} SET balance = ( debit - credit ) + {balance.ToString(new CultureInfo("en-US"))} WHERE id = '{item["id"]}'");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

            }

            return "Ok.";

        }

        /// <summary>
        /// Insert or update policy detail (1300)
        /// </summary>
        /// <param name="db"></param>
        /// <param name="Period"></param>
        /// <param name="Account"></param>
        /// <returns></returns>
        public static string InserOrUpdateBalance(Dictionary<string, string> d, DB db)
        {

            string result;

            result = db.Prompt($"SELECT SEARCH * FROM accounts PARTITION KEY accounts WHERE id = '{d["account"]}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "{}")
            {
                return $"Error: Account does not exist {d["account"]}";
            }

            DataRow accountData = DataTools.JsonWebMiDictionaryToDataRow(result);

            string masteraccount = accountData["masteraccount"].ToString();
            string description = accountData["description"].ToString();

            result = db.Prompt($"SELECT SUM(debit) AS BalanceDebit, SUM(credit) AS BalanceCredit FROM accounting PARTITION KEY {d["period"]} WHERE account = '{d["account"]}'");

            decimal BalanceDebit = 0;
            decimal BalanceCredit = 0;

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result != "{}")
            {
                DataRow accountiong = DataTools.JsonWebMiDictionaryToDataRow(result);
                decimal.TryParse(accountiong["BalanceDebit"].ToString(), out BalanceDebit);
                decimal.TryParse(accountiong["BalanceCredit"].ToString(), out BalanceCredit);
            }

            Dictionary<string, object> values = new Dictionary<string, object>();
            values.Add("id", d["account"]);
            #pragma warning disable CS8604 // Posible argumento de referencia nulo
            values.Add("description", description);
            values.Add("masteraccount", masteraccount);
            #pragma warning restore CS8604 // Posible argumento de referencia nulo
            values.Add("debit", BalanceDebit);
            values.Add("credit", BalanceCredit);
            values.Add("user", db.user);
            result = db.Prompt($"INSERT INTO balance PARTITION KEY {d["period"]} VALUES {JsonConvert.SerializeObject(values)}");

            if (result != "Ok.")
            {
                return result;
            }

            return "Ok.";

        }


        public static string InserOrUpdateBalanceTable(Dictionary<string, string> d, DB db, bool IsMaster)
        {

            string result;

            result = db.Prompt($"SELECT SEARCH * FROM accounts PARTITION KEY accounts WHERE id = '{d["account"]}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "{}")
            {
                return $"Error: Account does not exist {d["account"]}";
            }

            DataRow accountData = DataTools.JsonWebMiDictionaryToDataRow(result);

            if (accountData["masteraccount"].ToString() == "null")
            {
                return "Ok.";
            }

            if (!IsMaster)
            {
                result = db.Prompt($"SELECT SUM(debit) AS BalanceDebit, SUM(credit) AS BalanceCredit FROM balance PARTITION KEY {d["period"]} WHERE id = '{d["account"]}'");
            }
            else
            {
                result = db.Prompt($"SELECT SUM(debit) AS BalanceDebit, SUM(credit) AS BalanceCredit FROM balance PARTITION KEY {d["period"]} WHERE masteraccount = '{d["account"]}'");
            }

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            decimal BalanceDebit = 0;
            decimal BalanceCredit = 0;

            if (result != "{}")
            {
                DataTable balanceSum = DataTools.JsonWebMiDictionaryToDataTable(result);
                decimal.TryParse(balanceSum.Rows[0]["BalanceDebit"].ToString(), out BalanceDebit);
                decimal.TryParse(balanceSum.Rows[0]["BalanceCredit"].ToString(), out BalanceCredit);
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("id", accountData["id"]);
            data.Add("description", accountData["description"]);
            data.Add("masteraccount", accountData["masteraccount"]);
            data.Add("debit", BalanceDebit);
            data.Add("credit", BalanceCredit);
            data.Add("user", db.user);
            result = db.Prompt($"INSERT INTO balance PARTITION KEY {d["period"]} VALUES {JsonConvert.SerializeObject(data)}");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (accountData["masteraccount"].ToString() == "null" || accountData["masteraccount"].ToString() == "")
            {
                return "Ok.";
            }

            #pragma warning disable CS8601 // Posible asignación de referencia nula
            d["account"] = accountData["masteraccount"].ToString();
            #pragma warning restore CS8601 // Posible asignación de referencia nula
            result = InserOrUpdateBalanceTable(d, db, true);
            return result;

        }


        public static string TransferBalances(Dictionary<string, string> d, DB db)
        {

            string result;

            if (d["account"] == "null")
            {
                result = db.Prompt($"SELECT account, SUM( debit ) AS 'debit', SUM( credit ) AS 'credit' FROM accounting PARTITION KEY {d["initial_period"]} GROUP BY account");
            }
            else
            {
                result = db.Prompt($"SELECT account, SUM( debit ) AS 'debit', SUM( credit ) AS 'credit' FROM accounting PARTITION KEY {d["initial_period"]} WHERE account = '{d["account"]}' GROUP BY account");
            }

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "{}")
            {
                return $"Ok.";
            }

            DataTable t = DataTools.JsonWebMiDictionaryToDataTable(result);

            foreach (DataRow item in t.Rows)
            {
                result = db.Prompt($"SELECT SEARCH * FROM accounts PARTITION KEY accounts WHERE id = '{item["account"]}'");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                if (result == "{}")
                {
                    return $"Error: The accounting account no longer exists in the catalog {item["account"]}";
                }

                DataRow r = DataTools.JsonWebMiDictionaryToDataRow(result);
                result = Business.BusinessPrompt(db, $"ACCOUNTING PERIOD {d["destination_period"]} GUID {item["account"]} ACCOUNT {item["account"]} OPERATION DATE 0002-00-00 00:00:00.0001 DESCRIPTION {r["description"]} DOCUMENT INITIAL DEBIT {item["debit"]} CREDIT {item["credit"]}");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                result = Business.BusinessPrompt(db, $"CARRY BALANCES PERIOD {d["destination_period"]} ACCOUNT {item["account"]} OPERATION DATE 0001-00-00 0:00:00.0000");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                result = Business.BusinessPrompt(db, $"UPDATE BALANCE PERIOD {d["destination_period"]} ACCOUNT {item["account"]}");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                result = Business.BusinessPrompt(db, $"UPDATE MASTER PERIOD {d["destination_period"]} ACCOUNT {item["account"]}");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

            }

            return "Ok.";
        }


        public static string GetActivePeriod(Dictionary<string, string> d, DB db)
        {

            string result = db.Prompt($"SELECT * FROM periods WHERE active = 1");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result.StartsWith("{}"))
            {
                return "Error: No active period has been indicated";
            }

            return result;

        }


        public static string IsActivePeriod(Dictionary<string, string> d, DB db)
        {

            string result = Business.BusinessPrompt(db, "GET ACTIVE PERIOD");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            DataRow r = DataTools.JsonWebMiDictionaryToDataRow(result);

            #pragma warning disable CS8602 // Desreferencia de una referencia posiblemente NULL.
            
            if (r["period"].ToString().Trim() != d["period"].Trim())
            {
                return "Error: The indicated period is not active";
            }
            
            #pragma warning restore CS8602 // Desreferencia de una referencia posiblemente NULL.

            return "Ok.";
        }


        public static string CreatePeriod(Dictionary<string, string> d, DB db)
        {

            string result = IsValidPeriod(d["create_period"]);

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            result = db.Prompt($"SELECT * FROM periods WHERE id = '{d["create_period"]}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("id", d["create_period"]);
            data.Add("active", "0");
            data.Add("closed", "0");
            data.Add("user", db.user);
            return db.Prompt($"INSERT INTO periods PARTITION KEY periods VALUES {JsonConvert.SerializeObject(data)}");

        }


        public static string SetPeriodActive(Dictionary<string, string> d, DB db)
        {

            string result = IsValidPeriod(d["set_period"]);

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            result = db.Prompt($"SELECT * FROM periods PARTITION KEY periods WHERE id = '{d["set_period"]}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "{}")
            {
                return $"Error: The accounting period does not exist {d["set_period"]}";
            }

            result = db.Prompt($"UPDATE periods PARTITION KEY periods SET active = 0 WHERE active = 1");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("id", d["set_period"]);
            data.Add("active", "1");
            data.Add("closed", "0");
            data.Add("user", db.user);
            result = db.Prompt($"INSERT INTO periods PARTITION KEY periods VALUES {JsonConvert.SerializeObject(data)}");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            return "Ok.";

        }


        public static string ClosePeriod(Dictionary<string, string> d, DB db)
        {

            string result = db.Prompt($"SELECT * FROM periods PARTITION KEY periods WHERE id = '{d["close_period"]}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "{}")
            {
                return $"Error: The period does not exist {d["close_period"]}";
            }

            DataRow r = DataTools.JsonWebMiDictionaryToDataRow(result);

            if (r["closed"].ToString() == "1")
            {
                return $"Error: The period is already closed {d["close_period"]}";
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("id", d["close_period"]);
            data.Add("active", "0");
            data.Add("closed", "1");
            data.Add("user", db.user);
            return db.Prompt($"INSERT INTO periods PARTITION KEY periods VALUES {JsonConvert.SerializeObject(data)}");

        }


        public static string DeletePeriod(Dictionary<string, string> d, DB db)
        {

            string result = db.Prompt($"SELECT * FROM periods PARTITION KEY periods WHERE id = '{d["delete_period"]}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result == "{}")
            {
                return $"Error: The period does not exist {d["delete_period"]}";
            }

            DataRow r = DataTools.JsonWebMiDictionaryToDataRow(result);

            if (r["closed"].ToString() == "1")
            {
                return $"Error: The period is closed, Can't be erased {d["delete_period"]}";
            }

            result = db.Prompt($"SELECT * FROM balance PARTITION KEY {d["delete_period"]} LIMIT 1");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            if (result != "{}")
            {
                return $"Error: There are movements in balance for the indicated period, it cannot be deleted {d["delete_period"]}";
            }

            return db.Prompt($"DELETE FROM periods PARTITION KEY periods WHERE id = '{d["delete_period"]}'");

        }

        public static string IsValidPeriod(string period)
        {
            if (period.Length != 6)
            {
                return $"Error: The period format is 4 digits for the year and 2 for the month: {period}";
            }

            if (!AngelDBTools.StringFunctions.IsStringNumber(period))
            {
                return $"Error: The period can only contain numbers: {period}";
            }

            return "Ok.";
        }


        public static string AccountLevel(string account, DB db, int level)
        {

            string result = db.Prompt($"SELECT SEARCH * FROM accounts PARTITION KEY accounts WHERE id = '{account}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }


            if (result == "{}") return "Ok." + level.ToString();

            DataTable t = DataTools.JsonWebMiDictionaryToDataTable(result);

            if (t.Rows.Count == 0)
            {
                return "Ok." + level.ToString();
            }

            if (t.Rows[0]["masteraccount"].ToString() != "")
            {
                #pragma warning disable CS8604 // Posible argumento de referencia nulo
                result = AccountLevel(t.Rows[0]["masteraccount"].ToString(), db, level);
                #pragma warning restore CS8604 // Posible argumento de referencia nulo

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                level += int.Parse(result.Substring(3)) + 1;
            }

            return "Ok." + level.ToString();

        }

        public static string GetOwner(Dictionary<string, string> d, DB db)
        {

            string result;

            if (d["get_owner"].Trim() != "")
            {
                result = db.Prompt($"SELECT * FROM accountingdata PARTITION KEY accountingdata WHERE id = '{d["get_owner"]}'");
            }
            else
            {
                result = db.Prompt($"SELECT * FROM accountingdata PARTITION KEY accountingdata");
            }

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            return result;

        }


        public static string SetOwner(Dictionary<string, string> d, DB db)
        {

            if (d["set_owner"].Trim() == "")
            {
                return "Error: It is necessary that you indicate a unique code that identifies your accounting";
            }

            if (d["name"].Trim() == "")
            {
                return "Error: It is necessary that you indicate the name that identifies your accounting";
            }

            if (d["invoice_identifier"].Trim() == "")
            {
                return "Error: It is necessary that you indicate the name that identifies your accounting";
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("id", d["set_owner"]);
            data.Add("ownername", d["name"]);
            data.Add("invoice", d["invoice_identifier"]);
            string result = db.Prompt($"INSERT INTO accountingdata PARTITION KEY accountingdata VALUES {JsonConvert.SerializeObject(data)}");
            return result;

        }


    }

}