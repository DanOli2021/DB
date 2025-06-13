using System.Collections.Generic;

namespace AngelDB
{
    public class AngelServerCommands
    {
        public static Dictionary<string, string> DbCommands()
        {
            Dictionary<string, string> commands = new Dictionary<string, string>
            {
                { @"ADD APP", @"ADD APP#free;DIRECTORY#freeoptional;DOMAIN#freeoptional" },
                { @"DELETE APP", @"DELETE APP#free" },
                { @"SHOW APPS", @"SHOW APPS#free;WHERE#freeoptional" },
                { @"ADD ROUTE", @"ADD ROUTE#free;APP NAME#free" },
                { @"DELETE ROUTE", @"DELETE ROUTE#free" },
                { @"SHOW ROUTES", @"SHOW ROUTES#free;WHERE#freeoptional" },
                { @"SET CORS", @"SET CORS#free" },
                { @"SET HOST", @"SET HOST#free" },
                { @"SET CERTIFICATE", @"SET CERTIFICATE#free;PASSWORD#free;LISTEN IP#free; PORT#free" },
                { @"DELETE CERTIFICATE", @"DELETE CERTIFICATE#free" },
                { @"SHOW CERTIFICATES", @"SHOW CERTIFICATES#free" },
                { @"SHOW PARAMS", @"SHOW PARAMS#free" },
                { @"INIT DATABASE", @"INIT DATABASE#free" }
            };

            return commands;

        }
    }
}