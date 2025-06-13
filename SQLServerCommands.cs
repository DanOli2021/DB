using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    internal class SQLServerCommands
    {
        public static Dictionary<string, string> Commands()
        {
            Dictionary<string, string> commands = new Dictionary<string, string>
            {
                { @"CONNECT", @"CONNECT#free;ALIAS#free" },
                { @"QUERY", @"QUERY#free;CONNECTION ALIAS#free" },
                { @"SAVE ACCOUNTS TO", @"SAVE ACCOUNTS TO#free;PASSWORD#password" },
                { @"RESTORE ACCOUNTS FROM", @"RESTORE ACCOUNTS FROM#free;PASSWORD#password" },
                { @"SHOW CONNECTIONS", @"SHOW CONNECTIONS#free" },
                { @"INSERT INTO", @"INSERT INTO#free;CONNECTION ALIAS#free;VALUES#free" },
                { @"UPDATE", @"UPDATE#free;WHERE#free;CONNECTION ALIAS#free;VALUES#free" },
                { @"EXEC", @"EXEC#free;CONNECTION ALIAS#free" },
                { @"BEGIN TRANSACTION", "BEGIN TRANSACTION#free;CONNECTION ALIAS#free" },
                { @"COMMIT TRANSACTION", "COMMIT TRANSACTION#free;CONNECTION ALIAS#free" },
                { @"ROLLBACK TRANSACTION", "ROLLBACK TRANSACTION#free;CONNECTION ALIAS#free" },
            };

            return commands;

        }
    }
}
