using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    public static class Business
    {

        public static string BusinessPrompt(DB db, string command) {

            DbLanguage l = new DbLanguage();
            l.SetCommands( BusinessCommands.Commands() );
            Dictionary<string, string> d;

            d = l.Interpreter(command);

            if (d == null)
            {
                return l.errorString;
            }

            if (d.Count == 0)
            {
                return "Error: not command found";
            }

            string result = "";

            switch (d.First().Key)
            {
                case "create_catalog":
                    result = Luca.CreateStructure(db);
                    break;
                case "create_account":
                    result = Luca.CreateAccount(d, db);
                    break;
                case "delete_account":
                    result = Luca.DeleteAccount(d, db);
                    break;
                case "accounting":
                    result = Luca.InsertOrUpdateAccount(d, db);
                    break;
                case "carry_balances":
                    result = Luca.CarryBalances(d, db);
                    break;
                case "db":
                    result = db.Prompt(d["db"]);
                    break;
                case "update_balance":
                    result = Luca.InserOrUpdateBalance(d, db);
                    break;
                 case "update_master":
                    result = Luca.InserOrUpdateBalanceTable(d, db, false);
                    break;
                case "transfer_balances":
                    result = Luca.TransferBalances(d, db);
                    break;
                case "get_active":
                    result = Luca.GetActivePeriod(d, db);
                    break;
                case "is_active":
                    result = Luca.IsActivePeriod(d, db);
                    break;
                case "create_period":
                    result = Luca.CreatePeriod(d, db);
                    break;
                case "set_period":
                    result = Luca.SetPeriodActive(d, db);
                    break;
                case "close_period":
                    result = Luca.ClosePeriod(d, db);
                    break;
                case "delete_period":
                    result = Luca.DeletePeriod(d, db);
                    break;
                case "delete_all":
                    result = Luca.DeleteInfo(db);
                    break;
                case "get_level":
                    result = Luca.AccountLevel(d["from_account"], db, 1);
                    break;
                case "get_owner":
                    result = Luca.GetOwner(d, db);
                    break;
                case "set_owner":
                    result = Luca.SetOwner(d, db);
                    break;
                default:
                    return JsonConvert.SerializeObject(d, Formatting.Indented);
            }

            return result;

        }
    }
}

