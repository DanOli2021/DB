using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    public static class BusinessCommands
    {
        public static Dictionary<string, string> Commands()
        {
            Dictionary<string, string> commands = new Dictionary<string, string>
            {
                { @"CREATE CATALOG", @"CREATE CATALOG#free" },
                { @"CREATE ACCOUNT", @"CREATE ACCOUNT#free;BUSINESS ACCOUNT#freeoptional;MASTER ACCOUNT#freeoptional;DESCRIPTION#free;ACCOUNT TYPE#free" },
                { @"DELETE ACCOUNT", @"DELETE ACCOUNT#free" },
                { @"GET LEVEL", @"GET LEVEL#free;FROM ACCOUNT#free" },
                { @"ACCOUNTING", @"ACCOUNTING#free;PERIOD#free;GUID#free;ACCOUNT#free;OPERATION DATE#free;DESCRIPTION#free;DOCUMENT#free;DEBIT#free;CREDIT#free" },
                { @"DELETE ACCOUNTING", @"DELETE ACCOUNTING#free;PERIOD#free;GUID#free" },
                { @"CARRY BALANCES", @"CARRY BALANCES#free;PERIOD#free;ACCOUNT#free;OPERATION DATE#free" },
                { @"UPDATE BALANCE", @"UPDATE BALANCE#free;TABLE#freeoptional;PERIOD#free;ACCOUNT#free" },
                { @"UPDATE MASTER", @"UPDATE MASTER#free;PERIOD#free;ACCOUNT#free" },
                { @"TRANSFER BALANCES", @"TRANSFER BALANCES#free;INITIAL PERIOD#free;DESTINATION PERIOD#free;ACCOUNT#freeoptional" },
                { @"GET ACTIVE", @"GET ACTIVE#free;PERIOD#free" },
                { @"IS ACTIVE", @"IS ACTIVE#free;PERIOD#free" },
                { @"CREATE PERIOD", @"CREATE PERIOD#free" },
                { @"SET PERIOD", @"SET PERIOD#free;ACTIVE#free" },
                { @"CLOSE PERIOD", @"CLOSE PERIOD#free" },
                { @"DELETE PERIOD", @"DELETE PERIOD#free" },
                { @"VAR", @"VAR#Systemtoken;=#free" },
                { @"DB", @"DB#code" },
                { @"SET OWNER", @"SET OWNER#free;NAME#free;INVOICE IDENTIFIER#free" },
                { @"GET OWNER", @"GET OWNER#free" },
                { @"DELETE ALL", @"DELETE ALL#free;ACCOUNTING#free" }
            };

            return commands;

        }

    }
}
