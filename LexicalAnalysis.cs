using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Data;
using System.IO;
using System.Security;
using AngelDBTools;

namespace AngelDB {

    public class DbLanguage
    {
        private const int SIGNIFICANT_WORDS = 4;
        public string errorString = "";
        public bool OnlyCheckSyntax = false;
        Dictionary<string, string> commands = null;
        Dictionary<string, string> AddCommands = new Dictionary<string, string>();
        public Dictionary<string, string> vars = new Dictionary<string, string>();
        public AngelDB.DB db = null;


        public DbLanguage(AngelDB.DB db = null) 
        { 
            this.db = db;
        }

        public string AddCommand(string command, string definition)
        {
            if (AddCommands.ContainsKey(command)) return "Error: Command already exists";
            this.AddCommands.Add(command, definition);
            return "Ok.";
        }
        
        public void SetCommands(Dictionary<string, string> commands)
        {
            this.commands = commands;
        }

        public void SetVariables(Dictionary<string, string> vars)
        {
            this.vars = vars;
        }

        public void ShowCommands()
        {
            foreach (var item in this.commands)
            {
                Console.WriteLine($"{item.Key} --> {item.Value}");
            }
        }

        public Dictionary<string, string> Interpreter(string command)
        {

            Dictionary<string, string> commandsList;

            if (this.commands == null)
            {
                commandsList = Commands.DbCommands();
                this.commands = commandsList;
            }
            else
            {
                commandsList = this.commands;
            }

            foreach (var item in AddCommands.Keys)
            {
                if (!commandsList.ContainsKey(item))
                {
                    commandsList.Add(item, AddCommands[item]);
                }
            }

            LexicalAnalysis l = new LexicalAnalysis();
            l.OnlyCheckSyntax = OnlyCheckSyntax;

            List<string> search = new List<string>();

            command = command.Trim();
            errorString = "";

            string word = "";
            int j = 0;

            string commandFinded = "";

            for (int n = 0; n < command.Length; ++n)
            {

                if (command.Substring(n, 1) != " ")
                {
                    word += command.Substring(n, 1);
                }
                else
                {
                    if (!String.IsNullOrEmpty(word))
                    {
                        search.Add(word.Trim());
                        word = "";
                        ++j;
                    }
                }

                if (j > SIGNIFICANT_WORDS)
                {
                    break;
                }
            }

            if (!String.IsNullOrEmpty(word))
            {
                search.Add(word.Trim());
            }

            int numeroPalabras = search.Count;

            while (true)
            {
                string searchFinal = "";
                int i = 0;

                foreach (var item in search)
                {
                    ++i;
                    searchFinal = searchFinal + item + " ";

                    if (numeroPalabras == i)
                    {
                        --numeroPalabras;
                        break;
                    }

                }

                if (commandsList.ContainsKey(searchFinal.Trim().ToUpper()))
                {
                    commandFinded = searchFinal.Trim().ToUpper();
                    break;
                }

                if (numeroPalabras == 0)
                {
                    break;
                }
            }

            if (String.IsNullOrEmpty(commandFinded))
            {

                if (OnlyCheckSyntax)
                {
                    this.errorString = "";
                    return null;
                }

                this.errorString = $"Error: No command given {command}";
                return null;
            }

            Dictionary<string, string> resultSyntax = l.GenValues(command, commandsList[commandFinded], db);

            if (!String.IsNullOrEmpty(l.errorString))
            {
                this.errorString = "Error: " + l.errorString;
                return null;
            }
            else
            {
                return resultSyntax;
            }
        }
    }

    public static class LanguageTools
    {
        public static bool Condition(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return false;

            var evaluator = new DataTable();
            string toEval = "5 > 4";
            return (bool)evaluator.Compute(toEval, string.Empty);
        }

    }

    public class LexicalAnalysis
    {

        readonly Dictionary<string, string> parts = new Dictionary<string, string>();
        readonly Dictionary<string, string> command_dictionary = new Dictionary<string, string>();
        public string errorString = "";
        public bool OnlyCheckSyntax = false;

        public Dictionary<string, string> GenValues(string command, string KeyWords, AngelDB.DB db)
        {

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            System.Text.StringBuilder sbResult = new System.Text.StringBuilder();
            command += " ";

            string[] listOfKeyword = KeyWords.Split(";");

            foreach (string item in listOfKeyword)
            {
                int posKey = item.IndexOf("#");
                if (posKey == -1)
                {
                    this.errorString = $"No definition keyword {item} found and is required, check definition syntax.";
                    return null;
                }

                string keyword = item.Substring(0, posKey).Trim();
                string stringRegex = item[(posKey + 1)..];

                parts.Add(keyword.Trim().ToLower().Replace(" ", "_"), stringRegex);

            }

            string processcommand = " " + command + " ";
            string lastkey = "";
            List<string> values = new List<string>();
            List<string> keys = new List<string>();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            int n = 0;

            foreach (var item in parts)
            {

                ++n;
                string space = "";

                if (n > 0) 
                {
                    space = " ";
                }

                int p = processcommand.IndexOf(space + item.Key.ToUpper().Replace("_", " ") + " ");

                if (p >= 0)
                {
                    if (processcommand.StartsWith("\"") || processcommand.StartsWith("\'"))
                    {

                        string delimiter = processcommand.Substring(0, 1);

                        System.Text.StringBuilder quotation_value = new System.Text.StringBuilder();

                        int i = 0;

                        for (i = 1; i < processcommand.Length; ++i)
                        {
                            if (processcommand.Substring(i, 1) == delimiter)
                            {
                                break;
                            }

                            quotation_value.Append(processcommand.Substring(i, 1));
                        }

                        values.Add(quotation_value.ToString());
                        lastkey = item.Key.Trim().ToLower().Replace(" ", "_");
                        keys.Add(lastkey);

                        if (i < processcommand.Length - 1)
                        {
                            processcommand = processcommand.Substring(i + 1);
                        }
                        else
                        {
                            processcommand = "";
                        }
                    }
                    else
                    {

                        if ( processcommand.Substring(0, p).Trim().IndexOf("'''") >= 0 )
                        {
                            continue;
                        }

                        values.Add( processcommand.Substring(0, p).Trim().Replace("'''", "") );
                        lastkey = item.Key.Trim().ToLower().Replace(" ", "_");
                        keys.Add(lastkey);

                        processcommand = processcommand[(p + item.Key.Length + 1)..];
                    }
                }
            }

            if (!String.IsNullOrEmpty(processcommand)) values.Add(processcommand);

            for (int i = 1; i < values.Count; ++i)
            {
                if (db is not null)
                {
                    string result = values[i].Trim().Replace("'''", "");

                    if (result.StartsWith("&("))
                    {
                        result = StringFunctions.ExtractContentInFirstParentheses(result.Substring(1));

                        if (result.StartsWith("Error:")) 
                        {
                            sb.AppendLine(result);
                            break;
                        }

                        command_dictionary.Add(keys[i - 1], db.Prompt( result ));
                    }
                    else 
                    {
                        command_dictionary.Add(keys[i - 1], result);
                    }
                    
                }
                else 
                {
                    command_dictionary.Add(keys[i - 1], values[i].Trim().Replace("'''", ""));
                }

            }

            foreach (var item in parts)
            {

                string valueKey = item.Key.ToLower();

                switch (item.Value)
                {
                    case "Systemtoken":

                        if (!command_dictionary.ContainsKey(valueKey))
                        {
                            if (OnlyCheckSyntax) {
                                sb.AppendLine($"Is required: {valueKey.ToUpper().Replace( "_", " ") }");
                                break;
                            }

                            sb.AppendLine($"The following KeyWord is required: {valueKey.ToUpper()}");
                            break;
                        }

                        if (!AngelDBTools.StringFunctions.IsStringAlphaNumberOrUndescore(command_dictionary[valueKey]))
                        {
                            if (OnlyCheckSyntax)
                            {
                                sb.AppendLine($"Invalid characters: {valueKey.ToUpper().Replace("_", " ") }");
                                break;
                            }

                            sb.AppendLine($"The following KeyWord has invalid characters: {valueKey.ToUpper()}");
                        }

                        break;

                    case "validDirectory":

                        if (!command_dictionary.ContainsKey(valueKey))
                        {

                            if (OnlyCheckSyntax)
                            {
                                sb.AppendLine($"Is required a valid directory name: {valueKey.ToUpper().Replace("_", " ") }");
                                break;
                            }


                            sb.AppendLine($"Syntax error, the following KeyWord is required: {valueKey.ToUpper()}");
                            break;
                        }

                        if (!AngelDBTools.StringFunctions.IsValidPath(command_dictionary[valueKey]))
                        {

                            if (OnlyCheckSyntax)
                            {
                                sb.AppendLine($"Directory name has invalid characters: {valueKey.ToUpper().Replace("_", " ") }");
                                break;
                            }

                            sb.AppendLine($"Syntax error, the following KeyWord is required as a valid directory name, has invalid characters: {valueKey.ToUpper()}");
                        }

                        break;

                    case "password":

                        if (!command_dictionary.ContainsKey(valueKey))
                        {

                            if (OnlyCheckSyntax)
                            {
                                sb.AppendLine($"Is required: {valueKey.ToUpper().Replace("_", " ") }");
                                break;
                            }

                            sb.AppendLine($"Syntax error, the following KeyWord is required: {valueKey.ToUpper()}");
                            break;
                        }

                        if (command_dictionary[valueKey] == "db")
                        {
                            break;
                        }

                        if (!AngelDBTools.StringFunctions.IsStringValidPassword(command_dictionary[valueKey]))
                        {
                            sb.AppendLine($"Syntax error, the following KeyWord, has invalid characters, only numbers and letters are allowed, minimum 8 characters: {valueKey.ToUpper()}");
                        }

                        break;

                    case "free":

                        if (!command_dictionary.ContainsKey(valueKey))
                        {

                            if (OnlyCheckSyntax)
                            {
                                sb.AppendLine($"Is required: {valueKey.ToUpper().Replace("_", " ") }");
                                break;
                            }


                            sb.AppendLine($"Syntax error, the following KeyWord is required: {valueKey.ToUpper()}");
                        }

                        break;

                    case "optional":

                        if (OnlyCheckSyntax)
                        {
                            sb.AppendLine($"Optional: [{valueKey.ToUpper().Replace("_", " ") }]");
                            break;
                        }


                        if (!command_dictionary.ContainsKey(valueKey))
                        {
                            command_dictionary.Add(item.Key, "false");
                        }
                        else
                        {
                            command_dictionary[item.Key] = "true";
                        }

                        break;

                    case "freeoptional":

                        if (OnlyCheckSyntax)
                        {
                            sb.AppendLine($"Optional: [{valueKey.ToUpper().Replace("_", " ") }]");
                            break;
                        }

                        if (!command_dictionary.ContainsKey(valueKey))
                        {
                            command_dictionary.Add(item.Key, "null");
                        }

                        break;

                    case "number":

                        if (!command_dictionary.ContainsKey(valueKey))
                        {

                            if (OnlyCheckSyntax)
                            {
                                sb.AppendLine($"Is required: [{valueKey.ToUpper().Replace("_", " ") }]");
                                break;
                            }

                            sb.AppendLine($"Syntax error, the following KeyWord is required: {valueKey.ToUpper()}");
                            break;
                        }

                        if (!AngelDBTools.StringFunctions.IsStringNumber(command_dictionary[valueKey]))
                        {
                            sb.AppendLine($"Syntax error, the following KeyWord, has invalid characters, only numbers are allowed: {valueKey.ToUpper()}");
                        }

                        break;

                    case "code":

                        if (!command_dictionary.ContainsKey(valueKey))
                        {

                            if (OnlyCheckSyntax)
                            {
                                sb.AppendLine($"Is required: [{valueKey.ToUpper().Replace("_", " ") }]");
                                break;
                            }

                            sb.AppendLine($"Syntax error, the following KeyWord is required: {valueKey.ToUpper()}");
                            break;
                        }

                        DbLanguage l = new DbLanguage();
                        l.OnlyCheckSyntax = OnlyCheckSyntax;

                        if (!string.IsNullOrEmpty(l.errorString))
                        {
                            if (OnlyCheckSyntax)
                            {
                                sb.AppendLine(l.errorString);
                                break;
                            }

                            sb.AppendLine($"The following keyword has a syntax error: {valueKey.ToUpper()} -->" + l.errorString); ;
                        }

                        break;


                    default:
                        break;
                }

            }

            this.errorString = sb.ToString();

            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(command_dictionary, Newtonsoft.Json.Formatting.Indented) );

            return command_dictionary;

        }

    }
}
