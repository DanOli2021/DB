using System;
using System.Collections.Generic;


namespace AngelDB
{
    public static class AngelSQL_Commands
    {
        public static Dictionary<string, string> Commands()
        {
            Dictionary<string, string> commands = new Dictionary<string, string>
            {
                { @"CONNECT", @"CONNECT#free;USER#free;PASSWORD#free;ACCOUNT#freeoptional;DATABASE#freeoptional;DATA DIRECTORY#freeoptional" },
                { @"COMMAND", @"COMMAND#free;URL#freeoptional;token#freeoptional" },
                { @"GET token", @"GET token#free" },
                { @"SET token", @"SET token#free" },
                { @"SERVER", @"SERVER#free" },
                { @"STOP", @"STOP#free" }
            };

            return commands;

        }

    }
}
