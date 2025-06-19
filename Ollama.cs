using DocumentFormat.OpenXml.Office2010.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace AngelDB
{

    public class OllamaClient
    {
        private readonly HttpClient _httpClient;

        // Propiedades públicas
        public string BaseUrl { get; set; } = "http://localhost:11434/api/chat";
        public bool Stream { get; set; } = false;
        public string Model { get; set; }

        private string _rawResult = "";

        // Historial interno
        private readonly List<(string role, string content)> _history = new();

        public OllamaClient(string model = "llama3.2")
        {
            _httpClient = new HttpClient();
            Model = model;
        }

        // Métodos para manipular el historial
        public void AddSystemMessage(string content) => _history.Add(("system", content));
        public void AddUserMessage(string content) => _history.Add(("user", content));
        public void AddAssistantMessage(string content) => _history.Add(("assistant", content));
        public void ClearHistory() => _history.Clear();



        // Envío de conversación con historial
        public async Task<string> ChatAsync()
        {
            if (string.IsNullOrWhiteSpace(Model))
                throw new InvalidOperationException("Model no puede ser null o vacío.");

            var chatMessages = new List<Dictionary<string, string>>();
            foreach (var (role, content) in _history)
            {
                chatMessages.Add(new Dictionary<string, string>
            {
                { "role", role },
                { "content", content }
            });
            }

            var requestBody = new
            {
                model = Model,
                messages = chatMessages,
                stream = this.Stream
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var contentData = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl.TrimEnd('/')}", contentData);
                response.EnsureSuccessStatusCode();
                _rawResult = await response.Content.ReadAsStringAsync();

                DataTable ollama_result = JsonToDataTable(_rawResult);

                return ollama_result.Rows[0]["message_content"].ToString();
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }


        // Chat last result
        public string GetLastRawResult() 
        {
            return _rawResult;
        }

        // Métodos existentes
        public async Task<string> GenerateAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(Model))
                throw new InvalidOperationException("Model no puede ser null o vacío.");

            var requestBody = new
            {
                model = Model,
                prompt = prompt,
                stream = this.Stream
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl.TrimEnd('/')}", content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<bool> WarmUpModelAsync()
        {
            var requestBody = new
            {
                model = Model,
                prompt = " ",
                stream = this.Stream
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl.TrimEnd('/')}", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsModelLoadedAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl.TrimEnd('/')}/api/tags");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return json.Contains($"\"name\":\"{Model}\"");
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PullModelAsync()
        {
            var requestBody = new { name = Model };
            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl.TrimEnd('/')}/api/pull", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public bool IsOllamaInstalled()
        {
            // Primero intentamos correr el comando
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit(2000); // espera 2 segundos máximo
                    if (process.ExitCode == 0) return true;
                }
            }
            catch
            {
                // Si falla, seguimos al plan B
            }

            // Plan B: buscar el ejecutable en ruta típica
            string fallbackPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "ollama", "ollama.exe"
            );

            return File.Exists(fallbackPath);
        }



        static DataTable JsonToDataTable(string json)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            DataTable table = new DataTable();
            DataRow row = table.NewRow();

            foreach (JsonProperty prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    // Aplanar propiedades anidadas
                    foreach (JsonProperty nested in prop.Value.EnumerateObject())
                    {
                        string columnName = $"{prop.Name}_{nested.Name}";
                        if (!table.Columns.Contains(columnName))
                            table.Columns.Add(columnName);
                        row[columnName] = nested.Value.ToString();
                    }
                }
                else
                {
                    if (!table.Columns.Contains(prop.Name))
                        table.Columns.Add(prop.Name);
                    row[prop.Name] = prop.Value.ToString();
                }
            }

            table.Rows.Add(row);
            return table;
        }


    }

}
