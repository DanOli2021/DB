using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

#pragma warning disable CS8600 // Se va a convertir un literal nulo o un posible valor nulo en un tipo que no acepta valores NULL
#pragma warning disable CS8602 // Desreferencia de una referencia posiblemente NULL.

namespace AngelSQL
{

    public class Angel
    {

        string angel_token = "";
        string angel_url = "";
        string angel_user = "";

        public string Connect(string server, string user, string password )
        {

            AngelQuery query = new AngelQuery();
            query.type = "IDENTIFICATION";
            query.User = user;
            query.password = password;

            string result = WebTools.SendJsonToUrl(server, JsonConvert.SerializeObject(query));

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            AngelResponce angelResponce = JsonConvert.DeserializeObject<AngelResponce>(result);


            if (angelResponce.type.Trim() == "ERROR")
            {
                return angelResponce.result;
            }

            this.angel_token = angelResponce.token;
            this.angel_url = server;
            this.angel_user = user;

            return "Ok.";
        }



        public string Prompt( string command )
        {

            if (string.IsNullOrEmpty(this.angel_url))
            {
                return "Error: You have not indicated the URL to which you see to connect, please use ANGEL CONNECT first";
            }

            if (string.IsNullOrEmpty(this.angel_token))
            {
                return "Error: You have not indicated the token to which you see to connect, please use ANGEL CONNECT first";
            }

            AngelQuery query = new AngelQuery();
            query.type = "QUERY";
            query.token = this.angel_token;
            query.command = command;
            string result = WebTools.SendJsonToUrl(this.angel_url, JsonConvert.SerializeObject(query));

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            AngelResponce angelResponce = JsonConvert.DeserializeObject<AngelResponce>(result);
            return angelResponce.result;
        }

    }

    public static class WebTools
    {
        public static string ReadUrl(string url)
        {
            try
            {
                HttpClient web = new HttpClient();
                HttpResponseMessage response = web.GetAsync(url).GetAwaiter().GetResult();

                var byteArray = response.Content.ReadAsByteArrayAsync().Result;
                var result = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                return result;

            }
            catch (Exception e)
            {
                return $"Error: ReadUrl: {e.ToString()}";
            }

        }

        public static string SendJsonToUrl(string url, string json)
        {
            try
            {

                HttpClient web = new HttpClient();
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var result = web.PostAsync(url, content).Result;

                if (!result.IsSuccessStatusCode)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Error: Reason: " + result.ReasonPhrase);
                    return sb.ToString();
                }

                var byteArray = result.Content.ReadAsByteArrayAsync().Result;
                return Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);

            }
            catch (Exception e)
            {
                return $"Error: SendJsonToUrl: {e.ToString()}";
            }

        }
    }

    public class AngelQuery
    {
        public string type { get; set; } = "";
        public string User { get; set; } = "";
        public string password { get; set; } = "";
        public string token { get; set; } = "";
        public bool on_iis { get; set; } = false;
        public string command { get; set; } = "";

    }


    public class AngelResponce
    {
        public string type { get; set; } = "";
        public string token { get; set; } = "";
        public string result { get; set; } = "";
    }



}
