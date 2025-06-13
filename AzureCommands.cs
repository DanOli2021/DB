using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    public static class AzureCommands
    {
        public static Dictionary<string, string> Commands()
        {
            Dictionary<string, string> commands = new Dictionary<string, string>
            {
                { @"CONNECT", @"CONNECT#free;ALIAS#free" },
                { @"SELECT", @"SELECT#free;FROM#free;WHERE#free;LIMIT#freeoptional;CONNECTION ALIAS#free;USE ROWKEY#optional;EXCLUDE COUNTER#optional" },
                { @"GET RESULTS", @"GET RESULTS#free;PAGE SIZE#freeoptional;CONNECTION ALIAS#free" },
                { @"RESET TABLE", @"RESET TABLE#free;CONNECTION ALIAS#free" },
                { @"SAVE ACCOUNTS TO", @"SAVE ACCOUNTS TO#free;PASSWORD#password" },
                { @"RESTORE ACCOUNTS FROM", @"RESTORE ACCOUNTS FROM#free;PASSWORD#password" },
                { @"SHOW CONNECTIONS", @"SHOW CONNECTIONS#free" },
                { @"GET CONTAINER FROM", @"GET CONTAINER FROM#free" },
                { @"UPSERT INTO", @"UPSERT INTO#free;CONNECTION ALIAS#free;VALUES#free" },
                { @"CLEAR CONNECTIONS", @"CLEAR CONNECTIONS#free" }
            };

            return commands;

        }
    }
}
