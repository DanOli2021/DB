using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    public static class ScriptCommands
    {

        public static Dictionary<string, string> Commands()
        {
            Dictionary<string, string> commands = new Dictionary<string, string>
            {
                { @"COMPILE", @"COMPILE#free;REFERENCES#freeoptional;TYPE#freeoptional" },
                { @"COMPILE ON", @"COMPILE ON#free;DEMAND#free;REFERENCES#freeoptional" },
                { @"RUN ON", @"RUN ON#free;DEMAND#free" },
                { @"RUN CODE", @"RUN CODE#free;REFERENCES#freeoptional" },
                { @"RUN FROM", @"RUN FROM#free;DATABASE#free" },
                { @"CREATE CATALOG", @"CREATE CATALOG#free" }
             };

             return commands;

        }


    }
}
