using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AngelDB
{
    public static class DBBatch
    {
        public static string RunBatch(string filename, bool show_in_console, AngelDB.DB db)
        {
            if (!File.Exists(filename))
            {
                return $"Error: {filename}File not found";
            }

            try
            {
                string code = File.ReadAllText(filename);
                return RunCode(code, show_in_console, db);
            }
            catch (global::System.Exception e)
            {
                return $"Error: {e.Message}";
            }

        }

        public static string RunCode(string code, bool show_in_console, AngelDB .DB db)
        {
            string result = "";
            string[] lines = code.Split("\n");
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            System.Text.StringBuilder results = new System.Text.StringBuilder();


            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("/*"))
                {
                    while (!lines[i].EndsWith("*/"))
                    {
                        i++;
                    }
                }
                else if (lines[i].StartsWith("//"))
                {
                    continue;
                }
                else if (string.IsNullOrEmpty(lines[i]))
                {
                    continue;
                }
                else if (lines[i].Trim() == "\r") 
                {
                    continue;
                }

                sb.Append( lines[i] );

                if (lines[i].Trim().EndsWith(";"))
                {
                    string line = sb.ToString().Replace("\r", "");
                    string command = line.Substring(0, line.Length - 1);
                    
                    if (command == "QUIT") return "QUIT";

                    result = db.Prompt(command);
                    results.AppendLine(result);
                    sb.Clear();
                    
                    if (result.StartsWith("Error:"))
                    {
                        return result + $" at line {i + 1}";
                    }
                }
            }

            string lastLine = sb.ToString().Replace("\r", "");

            if (!string.IsNullOrEmpty(lastLine)) 
            {
                results.AppendLine(db.Prompt(lastLine));
            }

            if (show_in_console)
            {
                return results.ToString();
            }
            else 
            {
                return "";
            }

        }

    }
}

