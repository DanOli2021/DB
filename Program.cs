using System;
using AngelDB;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;
using Python.Runtime;

namespace dbcmd
{
    class Program
    {

        static void Main(string[] args)
        {
            string commandLine = string.Join(" ", args);

            string assemblyLocationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "";

            if (string.Compare(Environment.CurrentDirectory, assemblyLocationFolder, StringComparison.OrdinalIgnoreCase) != 0)
            {
                Environment.CurrentDirectory = assemblyLocationFolder;
            }

            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";

            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);

            AngelDB.DB db = new AngelDB.DB();
            string result = "";

            if (commandLine.Trim().ToUpper() != "START TASKS") 
            {
                if (!string.IsNullOrEmpty(commandLine))
                {
                    result = db.Prompt(commandLine);
                    Console.WriteLine(result);
                    return;
                }
            }

            result = db.Prompt("SCRIPT FILE config/startdb.csx ON APPLICATION DIRECTORY");

            if (result.IndexOf("The script file does not exists") < 0) 
            {
                if (result.StartsWith("Error:"))
                {
                    Console.WriteLine("Error: On ");
                    Console.WriteLine(result);
                }
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            
            result = db.Prompt("SCRIPT FILE config/angeldb.csx ON APPLICATION DIRECTORY");

            if (result.IndexOf("The script file does not exists") < 0)
            {
                if (!result.StartsWith("Error:"))
                {
                    parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
                }
                else
                {
                    Console.WriteLine(result);
                }
            }


            if (commandLine.Trim().ToUpper() == "START TASKS") 
            {
                Task.Run(() =>
                {

                    try
                    {
                        if (!parameters.ContainsKey("service_command"))
                        {
                            parameters.Add("service_command", "SCRIPT FILE manager/tasksdb.csx ON APPLICATION DIRECTORY");
                        }

                        if (!parameters.ContainsKey("service_delay"))
                        {
                            parameters.Add("service_delay", "300000");
                        }


                        while (true)
                        {
                            int delay = 300000;
                            string result = db.Prompt(parameters["service_command"]);

                            if (int.TryParse(parameters["service_delay"], out delay))
                            {
                                Thread.Sleep(delay);
                            }
                            else
                            {
                                Thread.Sleep(delay);
                                Thread.Sleep(300000);
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e);
                    }

                });

                Task.Run(() =>
                {

                    try
                    {
                        if (!parameters.ContainsKey("service_command_1"))
                        {
                            parameters.Add("service_command_1", "SCRIPT FILE manager/tasksdb1.csx ON APPLICATION DIRECTORY");
                        }

                        if (!parameters.ContainsKey("service_delay_1"))
                        {
                            parameters.Add("service_delay_1", "300000");
                        }


                        while (true)
                        {
                            int delay = 300000;
                            string result = db.Prompt(parameters["service_command_1"]);

                            if (int.TryParse(parameters["service_delay_1"], out delay))
                            {
                                Thread.Sleep(delay);
                            }
                            else
                            {
                                Thread.Sleep(delay);
                                Thread.Sleep(300000);
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e);
                    }

                });

            }

            void ShowHeader() 
            {
                AngelDB.Monitor.ShowLine("===================================================================", ConsoleColor.Magenta);
                AngelDB.Monitor.ShowLine(" =>  DataBase software, powerful and simple at the same time", ConsoleColor.Magenta);
                AngelDB.Monitor.ShowLine(" =>  We explain it to you in 20 words or fewer:", ConsoleColor.Magenta);
                AngelDB.Monitor.ShowLine(" =>  DB", ConsoleColor.Yellow);
                AngelDB.Monitor.ShowLine("===================================================================", ConsoleColor.Magenta);
            }

            ShowHeader();

            for (; ; )
            {

                // All operations are done here
                string line;
                string prompt = "";

                if (db.always_use_AngelSQL == true)
                {
                    prompt = db.angel_url + ">" + db.angel_user;
                }
                else 
                {
                    if (db.IsLogged)
                    {
                        prompt = new DirectoryInfo(db.BaseDirectory + "/").Name + ">" + db.account + ">" + db.database + ">" + db.user;
                    }
                }

                line = AngelDB.Monitor.Prompt(prompt + " $> ");

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.Trim().ToUpper() == "QUIT")
                {
                    PythonEngine.Shutdown();
                    Environment.Exit(0);
                    break;
                }

                if (line.Trim().ToUpper() == "CLEAR")
                {
                    Console.Clear();
                    ShowHeader();
                    continue;
                }

                result = db.Prompt("BATCH " + line + " SHOW IN CONSOLE");

                if (result.StartsWith("Error:"))
                {
                    AngelDB.Monitor.ShowError(result);
                }
                else
                {
                    AngelDB.Monitor.Show(result);
                }

            }

            return;
        }
    }

}
