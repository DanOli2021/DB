using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.EMMA;
using Newtonsoft.Json;

namespace AngelDB
{
    public class OpenAIChatbot
    {
        private string ApiKey = "tu_clave_api_aquí";
        private string Endpoint = "https://api.openai.com/v1/chat/completions";
        private static HttpClient _httpClient;
        private static string model = "gpt-4o";
        private DbLanguage lex =null;
        private OpenAIChat chat = null;

        public AngelDB.DB db = null;

        public OpenAIChatbot(AngelDB.DB db)
        {
            lex = new DbLanguage();
            lex.SetCommands(GPTCommands.Commands());
            _httpClient = new HttpClient();
            this.db = db;
        }


        public string ProcessCommand(string command) 
        {
            Dictionary<string, string> d = lex.Interpreter(command);

            if (!string.IsNullOrEmpty(lex.errorString)) return lex.errorString;
            string commandkey = d.First().Key;

            switch (commandkey) 
            {
                case "set_api_key":
                    return this.SetApiKey(d["set_api_key"]);
                case "set_end_point":
                    return this.SetEndpoint(d["set_end_point"]);
                case "save_account_to":
                    return SaveAccountsTo(d["save_account_to"], d["password"]);
                case "restore_account_from":
                    return RestorAccount(d["save_account_to"], d["password"]);
                case "start_chat":                    
                    return StartChat(d);
                case "get_chat":
                    if(chat == null) return "Error: Chat not started. Use START CHAT";
                    return chat.GetConversationHistory();
                case "clear_chat":
                    if (chat == null) return "Error: Chat not started. Use START CHAT";
                    chat.ClearConversationHistory();
                    return "Ok.";
                case "prompt_preview":
                    return PromptPreview(d["prompt_preview"]);
                case "read_files":

                    string result;

                    if (d["include_file_name"] == "true")
                    {
                        result = ReadTextFiles(d["from_directory"], d["read_files"], true);
                    }
                    else 
                    {
                        result = ReadTextFiles(d["from_directory"], d["read_files"], false);
                    }

                    return result;

                case "read_file":

                    if (d["include_file_name"] == "true")
                    {
                        return ReadtFile(d["read_file"], true);
                    }
                    else
                    {
                        return ReadtFile(d["read_file"], false);
                    }

                case "set_model":

                    return this.SetModel(d["set_model"]);

                case "add_context":

                    return AddContext(d["add_context"]);

                case "prompt":

                    return Prompt(d[commandkey]).Result;

                default:

                    return "Error: Command not found.";
            }
        }

        public string SetApiKey(string apiKey)
        {
            ApiKey = apiKey;
            return "Ok.";
        }

        public string SetEndpoint(string endpoint)
        {
            Endpoint = endpoint;
            chat.apiUrl = endpoint;
            return "Ok.";
        }

        public string SetModel(string use_model)
        {            
            model = use_model;
            chat.model = model;
            return "Ok.";
        }

        public string SaveAccountsTo(string file, string password)
        {
            try
            {

                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("api_key", ApiKey);
                //dic.Add("endpoint", Endpoint);
                //dic.Add("max_tokens", maxTokens.ToString());

                string json = JsonConvert.SerializeObject(dic);
                return AngelDBTools.StringFunctions.SaveEncriptedFile(file, json, password);
            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }

        public string RestorAccount(string file, string password)
        {
            try
            {

                if (!File.Exists(file))
                {
                    return $"Error: The file does not exists {file}";
                }

                string json = AngelDBTools.StringFunctions.RestoreEncriptedFile(file, password);

                Dictionary<string, string> dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                this.ApiKey = dic["api_key"];
                //this.Endpoint = dic["endpoint"];
                //this.maxTokens = int.Parse(dic["max_tokens"]);
                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }


        public string StartChat(Dictionary<string, string> d)
        {
            try
            {

                if (chat != null) chat.ClearConversationHistory();

                if (!string.IsNullOrEmpty(d["start_chat"])) 
                {
                    chat.AddToHistory("system", d["start_chat"]);
                }

                chat = new OpenAIChat(ApiKey, Endpoint, model);
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: StartChat() {e}";
            }
        }


        public string ReadTextFiles( string directory_name, string file_extension, bool includeFileTittle) 
        {
            try
            {
                string[] files = Directory.GetFiles(directory_name, file_extension);

                StringBuilder sb = new StringBuilder();

                foreach (var file in files)
                {
                    string text = File.ReadAllText(file);

                    if (includeFileTittle) 
                    {
                        sb.AppendLine($"File: {file}");
                    }                    

                    sb.AppendLine(text);
                    sb.AppendLine();
                }
                
                this.chat.AddToHistory("system", sb.ToString());

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: ResumeFromTextFiles() {e}";
            }
        }


        public string AddContext(string context) 
        {
            try
            {
                chat.AddToHistory("system", context);
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: AddContext() {e}";
            }
        }

        public string ReadtFile(string file_name, bool includeFileTittle)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                string text = File.ReadAllText(file_name);

               if (includeFileTittle)
               {
                        sb.AppendLine($"File: {file_name}");
               }

               sb.AppendLine(text);
               sb.AppendLine();

               this.chat.AddToHistory("system", sb.ToString());

               return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: ResumeFromTextFiles() {e}";
            }
        }


        public string PromptPreview(string prompt)
        {
            try
            {

                List<string> tokens = Tokenizer.Tokenize(prompt, "<&", "&>");

                StringBuilder sb = new StringBuilder();

                foreach (var item in tokens)
                {
                    if (item.StartsWith("^"))
                    {
                        string key = item.Substring(1);
                        string value = this.db.Prompt(key);
                        sb.AppendLine(value.Trim());
                        sb.AppendLine();
                    }
                    else 
                    {
                        sb.AppendLine(item.Trim());
                        sb.AppendLine();
                    }                    
                }

                return sb.ToString();

            }
            catch (Exception e)
            {
                return $"Error: PromptPreview() {e}";
            }
        }


        public async Task<string> Prompt(string prompt)
        {
            try
            {

                if(chat == null) return "Error: Chat not started. Use START CHAT";

                string result =  await chat.SendMessageAsync(PromptPreview(prompt));
                return result;

            }
            catch (Exception e)
            {
                return $"Error: Prompt() {e}";
            }        
        }
    }

    public class OpenAIChat
    {
        public string model { set; get; } = "gpt-4o"; // O puedes usar "gpt-4"
        private readonly string apiKey;
        public string apiUrl = "https://api.openai.com/v1/chat/completions";        

        // Historial de conversación
        private List<dynamic> conversationHistory = new List<dynamic>
    {
        new { role = "system", content = "Eres un asistente útil." } // Mensaje inicial del sistema
    };

        // Constructor
        public OpenAIChat(string apiKey, string apiUrl, string model )
        {
            this.apiUrl = apiUrl;
            this.apiKey = apiKey;
            this.model = model;
        }

        // Método para enviar un mensaje y recibir la respuesta
        public async Task<string> SendMessageAsync(string userInput)
        {
            // Agregar el mensaje del usuario al historial
            conversationHistory.Add(new { role = "user", content = userInput });

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestBody = new
                {
                    model = this.model, // O puedes usar "gpt-4"
                    messages = conversationHistory,
                };

                var jsonRequestBody = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    dynamic responseObject = JsonConvert.DeserializeObject(responseString);

                    // Obtener la respuesta del asistente
                    string assistantResponse = responseObject.choices[0].message.content.ToString();

                    // Agregar la respuesta del asistente al historial
                    conversationHistory.Add(new { role = "assistant", content = assistantResponse });

                    return assistantResponse;
                }
                else
                {
                    return $"Error: {response.StatusCode}";
                }
            }
        }


        public string AddToHistory(string role, string content)
        {
            conversationHistory.Add(new { role = role, content = content });
            return "Ok.";
        }

        // Método para obtener el historial de la conversación
        public string GetConversationHistory()
        {
            var historyBuilder = new StringBuilder();
            foreach (var message in conversationHistory)
            {
                historyBuilder.AppendLine($"{message.role}: {message.content}");
            }
            return historyBuilder.ToString();
        }

        // Método para limpiar el historial de la conversación
        public void ClearConversationHistory()
        {
            conversationHistory.Clear();
            conversationHistory.Add(new { role = "system", content = "Eres un asistente útil." });
        }
    }

}
