using System.Collections.Generic;

namespace AngelDB
{
    public static class GPTCommands
    {
        public static Dictionary<string, string> Commands()
        {
            Dictionary<string, string> commands = new Dictionary<string, string>
            {
                { @"SET API KEY", @"SET API KEY#free" },
                { @"SET END POINT", @"SET END POINT#free" },
                { @"SET MODEL", @"SET MODEL#free" },
                { @"SAVE ACCOUNT TO", @"SAVE ACCOUNT TO#free;PASSWORD#password" },
                { @"RESTORE ACCOUNT FROM", @"RESTORE ACCOUNT FROM#free;PASSWORD#password" },
                { @"PROMPT", @"PROMPT#free" },
                { @"PROMPT PREVIEW", @"PROMPT PREVIEW#free" },
                { @"START CHAT", @"START CHAT#free" },
                { @"GET CHAT", @"GET CHAT#free" },
                { @"CLEAR CHAT", @"CLEAR CHAT#free" },
                { @"READ FILES", @"READ FILES#free;FROM DIRECTORY#free;INCLUDE FILE NAME#optional" },
                { @"READ FILE", @"READ FILE#free;INCLUDE FILE NAME#optional" },
                { @"ADD CONTEXT", @"ADD CONTEXT#free" }
            };

            return commands;

        }
    }
}
