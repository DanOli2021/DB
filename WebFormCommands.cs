using System;
using System.Collections.Generic;

namespace AngelDB
{    public static class WebFormCommands
    {
        public static Dictionary<string, string> Commands()
        {
            Dictionary<string, string> commands = new Dictionary<string, string>
            {
                { @"CONTROL", @"CONTROL#free;TYPE#free;INDEX#freeoptional;VALUE#freeoptional;ROWS#freeoptional;COLS#freeoptional;CAPTION#freeoptional;RENDER STRING#freeoptional;CLASS#freeoptional;STYLE#freeoptional;LABEL STYLE#freeoptional;PLACE HOLDER#freeoptional;DISABLED#freeoptional;VISIBLE#freeoptional;ROW SPACE#freeoptional;COL SPACE#freeoptional;PARAMETERS#freeoptional;IMAGE#freeoptional;GROUP#freeoptional;COMMAND#freeoptional;ON LOST FOCUS#freeoptional;ON KEY DOWN#freeoptional;ON CHANGE#freeoptional" },
                { @"EVENT", @"EVENT#free;COMMAND#freeoptional" },
                { @"REMOVE CONTROL", @"REMOVE CONTROL#free" },
                { @"GROUP", @"GROUP#free" },
                { @"END GROUP", @"END GROUP#free" },
                { @"DATA", @"DATA#free;GRID DATA#freeoptional;CONTROL#freeoptional" },
                { @"GRID DATA", @"GRID DATA#free" },
                { @"GET CONTROLS", @"GET CONTROLS#free" },
                { @"CHANGE PROPERTY", @"CHANGE PROPERTY#free;VALUE#free;OF CONTROL#free" },
                { @"CLEAR", @"CLEAR#free" }
            };

            return commands;

        }

    }
}
