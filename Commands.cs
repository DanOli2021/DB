using System.Collections.Generic;

namespace AngelDB {
    
    public static class Commands
    {
        public static Dictionary<string, string> DbCommands()
        {
            Dictionary<string, string> commands = new Dictionary<string, string>
            {

                { @"USE", @"USE#Systemtoken;DATABASE#freeoptional" },
                { @"ACCOUNT", @"ACCOUNT#optional" },
                { @"CLOSE ACCOUNT", @"CLOSE ACCOUNT#freeoptional" },
                { @"CREATE ACCOUNT", @"CREATE ACCOUNT#Systemtoken; SUPERUSER#Systemtoken; PASSWORD#password; NAME#freeoptional" },
                { @"USE ACCOUNT", @"USE ACCOUNT#Systemtoken" },
                { @"DELETE ACCOUNT", @"DELETE ACCOUNT#Systemtoken" },
                { @"UNDELETE ACCOUNT", @"UNDELETE ACCOUNT#Systemtoken" },
                { @"GET ACCOUNTS", @"GET ACCOUNTS#free;WHERE#freeoptional" },
                { @"CHANGE MASTER", @"CHANGE MASTER#free;TO USER#Systemtoken;PASSWORD#password" },
                { @"ADD MASTER", @"ADD MASTER#free;TO ACCOUNT#Systemtoken;USER#Systemtoken;PASSWORD#password;NAME#freeoptional;UNIQUE#optional" },
                { @"UPDATE MASTER ACCOUNT", @"UPDATE MASTER ACCOUNT#free;FROM ACCOUNT#Systemtoken;PASSWORD#password" },
                { @"DELETE MASTER ACCOUNT", @"DELETE MASTER ACCOUNT#free;FROM ACCOUNT#Systemtoken" },
                { @"GET MASTER ACCOUNTS", @"GET MASTER ACCOUNTS#free;WHERE#freeoptional" },                
                { @"MY LEVEL", @"MY LEVEL#free" },
                { @"DATABASE", @"DATABASE#optional" },
                { @"CREATE DATABASE", @"CREATE DATABASE#Systemtoken" },
                { @"USE DATABASE", @"USE DATABASE#Systemtoken USE DATABASE#Systemtoken" },
                { @"DELETE DATABASE", @"DELETE DATABASE#Systemtoken USE DATABASE#Systemtoken" },
                { @"UNDELETE DATABASE", @"UNDELETE DATABASE#Systemtoken USE DATABASE#Systemtoken" },
                { @"GET DATABASES", @"GET DATABASES#free;WHERE#freeoptional" },
                { @"CREATE LOGIN", @"CREATE LOGIN#Systemtoken;TO DATABASE#freeoptional;PASSWORD#password;NAME#freeoptional;TABLES#freeoptional;READ ONLY#optional" },
                { @"VALIDATE LOGIN", @"VALIDATE LOGIN#Systemtoken;PASSWORD#password" },
                { @"DELETE LOGIN", @"DELETE LOGIN#Systemtoken" },
                { @"UNDELETE LOGIN", @"DELETE LOGIN#Systemtoken" },
                { @"GET LOGINS", @"GET LOGINS#free;WHERE#freeoptional;ORDER BY#freeoptional" },
                { @"GET USERS", @"GET USERS#free;WHERE#freeoptional;ORDER BY#freeoptional" },
                { @"CREATE TABLE", @"CREATE TABLE#Systemtoken;FIELD LIST#free;STORAGE#freeoptional;TYPE SEARCH#optional" },
                { @"CREATE PARTITION RULE", @"CREATE PARTITION RULE#free;TABLE#free;PARTITION KEY#free;CONNECTION#free" },
                { @"DELETE PARTITION RULE", @"DELETE PARTITION RULE#free;ACCOUNT#free;DATABASE#free;TABLE#free;PARTITION KEY#free" },                
                { @"GET PARTITION RULES", @"GET PARTITION RULES#free;WHERE#freeoptional" },
                { @"DELETE TABLE", @"DELETE TABLE#Systemtoken" },
                { @"ALTER TABLE", @"ALTER TABLE#free;ADD COLUMN#freeoptional;DROP COLUMN#freeoptional;REDEFINE COLUMN#freeoptional"},
                { @"CREATE INDEX", @"CREATE INDEX#free;ON TABLE#free;COLUMN#free"},
                { @"VACUUM TABLE", @"VACUUM TABLE#free;PARTITION#freeoptional"},
                { @"INSERT INTO", @"INSERT INTO#Systemtoken;PARTITION KEY#freeoptional;UPSERT#optional;EXCLUDE COLUMNS#freeoptional;SPEED UP#optional;VALUES#free" },
                { @"UPSERT INTO", @"UPSERT INTO#Systemtoken;PARTITION KEY#freeoptional;EXCLUDE COLUMNS#freeoptional;SPEED UP#optional;VALUES#free" },
                { @"COPY TO", @"COPY TO#Systemtoken;FROM#code;STORAGE#freeoptional;TYPE SEARCH#optional" },
                { @"IMPORT FROM", @"IMPORT FROM#free;FILE TYPE#free;TO#Systemtoken;TYPE SEARCH#optional;STORAGE#freeoptional" },
                { @"EXPORT TO", @"EXPORT TO#free;FROM#code;TO FILE#free;TYPE#free" },
                { @"UPDATE", @"UPDATE#free;PARTITION KEY#freeoptional; SET#free; WHERE#free;SYNC#optional" },                                
                { @"DELETE FROM", @"DELETE FROM#Systemtoken; PARTITION KEY#free; WHERE#free" },
                { @"SELECT", @"SELECT#free; FROM#Systemtoken; PARTITION KEY#freeoptional;WHERE#freeoptional;GROUP BY#freeoptional;ORDER BY#freeoptional;LIMIT#freeoptional;AS CSV FILE#freeoptional;AS ARFF FILE#freeoptional;CLASS#freeoptional;VERBOSE#optional;NO RULES#optional;READ LOCKED#optional" },
                { @"SAVE TO", @"SAVE TO#free;SOURCE#code;AS CSV#optional;STRING DELIMITER#freeoptional" },
                { @"READ CSV", @"READ CSV#free;VALUE SEPARATOR#freeoptional;COLUMNS AS NUMBERS#freeoptional;FIRST AS HEADER#optional" },
                { @"GET TABLES", @"GET TABLES#free;WHERE#freeoptional" },
                { @"GET STRUCTURE", @"GET STRUCTURE#free;FROM#free" },
                
                { @"GET PARTITIONS", @"GET PARTITIONS#free;FROM TABLE#Systemtoken;WHERE#freeoptional" },
                { @"DELETE PARTITIONS", @"DELETE PARTITIONS#free;FROM TABLE#Systemtoken;WHERE#free;ONLY FILES#optional" },
                { @"MOVE PARTITION", @"MOVE PARTITION#free;FROM TABLE#free;TO CONNECTION#free;VERBOSE#optional" },
                { @"COMPRESS PARTITION", @"COMPRESS PARTITION#free;FROM TABLE#free" },
                { @"DECOMPRESS PARTITION", @"DECOMPRESS PARTITION#free;FROM TABLE#free" },


                { @"GET MASTERS", @"GET MASTERS#free;WHERE#freeoptional" },
                { @"WHO I AM", @"WHO I#free;AM#free" },
                { @"GET FILE", @"GET FILE#free;BASE 64#free;FROM#Systemtoken;PARTITION KEY#free;WHERE#freeoptional" },
                { @"DELETE FILE", @"DELETE FILE#free;FROM#Systemtoken;PARTITION KEY#free;WHERE#free" },
                { @"ALWAYS USE ANGELSQL", @"ALWAYS USE ANGELSQL#free" },
                { @"DB USER", @"DB USER#Systemtoken;PASSWORD#password;ACCOUNT#freeoptional;DATABASE#freeoptional;DATA DIRECTORY#freeoptional;LOAD FILE#freeoptional" },
                { @"CLOSE DB", @"CLOSE DB#free" },
                { @"CONSOLE", @"CONSOLE#code" },
                { @"WRITE RESULTS FROM", @"WRITE RESULTS FROM#code;TO FILE#free" },
                { @"WRITE FILE", @"WRITE FILE#free;VALUES#free" },
                { @"READ FILE", @"READ FILE#free" },
                { @"SQL SERVER", @"SQL SERVER#free" },
                { @"BUSINESS", @"BUSINESS#code" },
                { @"USE TABLE", @"USE TABLE#Systemtoken;PARTITION KEY#freeoptional;WHERE#freeoptional;ORDER BY#freeoptional" },
                { @"VAR", @"VAR#Systemtoken;=#freeoptional" },
                { @"GET VARS", @"GET VARS#free" },
                { @"AZURE", @"AZURE#free" },
                { @"CREATE DB", @"CREATE DB#free;CONNECTION STRING#free" },
                { @"PROMPT DB", @"PROMPT DB#free;COMMAND#free" },
                { @"GET DB LIST", @"GET DB LIST#free" },
                { @"REMOVE DB", @"REMOVE DB#free" },
                { @"WEB FORM", @"WEB FORM#free" },
                { @"=", @"=#free;DATA#freeoptional" },
                { @"SCRIPT FILE", @"SCRIPT FILE#free;REFERENCES#freeoptional;RECOMPILE#optional;ON MAIN DIRECTORY#optional;ON DATABASE#optional;ON APPLICATION DIRECTORY#optional;ON TABLE#freeoptional;MESSAGE#freeoptional;DATA#freeoptional" },
                { @"GET URL", @"GET URL#free" },
                { @"SEND TO WEB", @"SEND TO WEB#free;CONTEXT DATA#freeoptional;SOURCE#freeoptional" },
                { @"SAVE TO GRID", @"SAVE TO GRID#free;JSON#freeoptional;AS TABLE#free;MERGE DATA#optional" },
                { @"SAVE TO TABLE", @"SAVE TO TABLE#free;PARTITION KEY#freeoptional;SOURCE#freeoptional;JSON#freeoptional;ID COLUMN#freeoptional" },
                { @"GRID", @"GRID#free;AS JSON#optional" },
                { @"GRID INSERT ON", @"GRID INSERT ON#free;ID#freeoptional;VALUES#free" },
                { @"SPEED UP", @"SPEED UP#free" },
                { @"ANGEL", @"ANGEL#free" },
                { @"PROMPT", @"PROMPT#free" },
                { @"PROMPT PASSWORD", @"PROMPT PASSWORD#free" },
                { @"BATCH", @"BATCH#free;SHOW IN CONSOLE#optional" },
                { @"BATCH FILE", @"BATCH FILE#free;SHOW IN CONSOLE#optional" },

                { @"CREATE SYNC", @"CREATE SYNC#free;FROM TABLE#free;FROM PARTITIONS#freeoptional;FROM CONNECTION#free;FROM ACCOUNT#freeoptional;FROM DATABASE#freeoptional;TO CONNECTION#free;TO ACCOUNT#freeoptional;TO DATABASE#freeoptional;TO TABLE#freeoptional" },
                { @"GET SYNCS", @"GET SYNCS#free;WHERE#freeoptional" },
                { @"DELETE SYNC", @"DELETE SYNC#free" },
                { @"CREATE SYNC DATABASE", @"CREATE SYNC DATABASE#free;SOURCE CONNECTION#free;TARGET CONNECTION#free" },
                { @"SYNC DATABASE", @"SYNC DATABASE#free;ROWS PER CYCLE#freeoptional;EXCLUDE TABLES#freeptiopnal;LOG FILE#freeoptional;PARTITIONS TO PROCESS#freeoptional;SHOW LOG#optional" },
                { @"GET MAX SYNC TIME", @"GET MAX SYNC TIME#free;FROM TABLE#freeoptional;WHERE#freeoptional" },
                { @"UPDATE PARTITION", @"UPDATE PARTITION#free;FROM TABLE#free;TIME STAMP#free" },
                { @"SYNC NOW", @"SYNC NOW#free;PARTITIONS CONDITION#freeoptional;PARTITIONS TO PROCESS#freeoptional;COLUMNS#freeoptional;ROWS#freeoptional;SHOW LOG#optional;LOG FILE#freeoptional;CHANGE TO SEARCH#optional;WHERE#freeoptional" },

                { @"COMPILE", @"COMPILE#free;ASSEMBLY NAME#freeoptional" },
                { @"READ EXCEL", @"READ EXCEL#free;AS TABLE#freeoptional;SHEET#freeoptional;ON MEMORY#optional;FIRST ROW AS HEADER#optional" },
                { @"CREATE EXCEL", @"CREATE EXCEL#free;JSON VALUES#free" },
                { @"STATISTICS", @"STATISTICS#free" },
                { @"MYBUSINESS POS", @"MYBUSINESS POS#free" },
                { @"GPT", @"GPT#free" },

                { @"OLLAMA", @"OLLAMA#free;URL#freeoptional;MODEL#freeoptional;STREAM#optional" },
                { @"OLLAMA LOAD MODEL", @"OLLAMA LOAD MODEL#free" },
                { @"OLLAMA UNLOAD MODEL", @"OLLAMA UNLOAD MODEL#free" },
                { @"OLLAMA ADD SYSTEM MESSAGE", @"OLLAMA ADD SYSTEM MESSAGE#free" },
                { @"OLLAMA ADD ASSISTANT MESSAGE", @"OLLAMA ADD ASSISTANT MESSAGE#free" },
                { @"OLLAMA CLEAR", @"OLLAMA CLEAR#free" },
                { @"OLLAMA PROMPT", @"OLLAMA PROMPT#free" },

                { @"POST API", @"POST API#free;ACCOUNT#freeoptional;API#free;LANGUAGE#freeoptional;MESSAGE#free" },
                { @"POST", @"POST#free;MESSAGE#free" },

                { @"LOCK TABLE", @"LOCK TABLE#free;PARTITION KEY#freeoptional" },
                { @"UNLOCK TABLE", @"UNLOCK TABLE#free;PARTITION KEY#freeoptional" },

                { @"HUB", @"HUB#free" },
                { @"HELP", @"HELP#free" },
                { @"PYTHON FILE", @"PYTHON FILE#free;PATH#freeoptional;MESSAGE#freeoptional" },
                { @"PYTHON SET PATH", @"PYTHON SET PATH#free" },
                { @"PYTHON GET LAST ERROR", @"PYTHON GET LAST ERROR#free" },
                { @"PYTHON GET LAST WARNING", @"PYTHON GET LAST WARNING#free" },
                { @"PYTHON GET LAST RESULT", @"PYTHON GET LAST RESULT#free" },               


                { @"IMPORT SCRIPTS DIRECTORY", @"IMPORT SCRIPTS DIRECTORY#free;TYPE#free" },
                { @"IMPORT SCRIPT FILE", @"IMPORT SCRIPT FILE#free" },
                { @"DB SCRIPT", @"DB SCRIPT#free;MAIN DIRECTORY#freeoptional;REFERENCES#freeoptional;RECOMPILE#optional;MESSAGE#freeoptional;DATA#freeoptional" },
                { @"COMPILE TO DLL", @"COMPILE TO DLL#free;TO FILE#freeoptional" },

                // Command to Control RDP on Windows                
                { @"REMOTE DESKTOP", @"REMOTE DESKTOP#free;PORT#freeoptional;USER#freeoptional;PASSWORD#freeoptional;WIDTH#freeoptional;HEIGHT#freeoptional;FULL SCREEN#optional;ADMIN#optional;REDIRECT DISKS#optional;REDIRECT PRINTERS#optional;REDIRECT CLIPBOARD#optional" },

                { @"VERSION", @"VERSION#free" },

                //Previouse version compatibility 
                { @"SET SCRIPTS DIRECTORY", @"SET SCRIPTS DIRECTORY#free" },

                //System info
                { @"SYSTEM INFO", @"SYSTEM INFO#free" },
                { @"CONNECTION INFO", @"CONNECTION INFO#free" },
                { @"WHO IS", @"WHO IS#free;YOUR#free;DADY#free" }
            };

            return commands;

        }

    }

}
