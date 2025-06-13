using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    public static class MathCommands
    {
        public static Dictionary<string, string> Commands()
        {
            Dictionary<string, string> commands = new Dictionary<string, string>
            {
                { @"GAUSS JORDAN", @"GAUSS JORDAN#free" }
            };

            return commands;

        }

    }
}
