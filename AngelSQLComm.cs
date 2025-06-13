using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace AngelDB
{
    public class AngelSQLComm
    {

        public string Acccount { get; set; }
        public string Token { get; set; }
        public string User { get; set; }
        public string Url { get; set; }
        public string Language { get; set; }

        public AngelSQLComm(string account, string token, string user, string url, string language)
        {
            this.Acccount = account;
            this.Token = token;
            this.User = user;
            this.Url = url;
            this.Language = language;
        }

        public string Send(string api_name, string OPerationType, dynamic object_data)
        {

            AngelApiOperation d = new AngelApiOperation();
            d.api = api_name;
            d.account = this.Acccount;
            d.OperationType = OPerationType;
            d.Token = this.Token;
            d.User = this.User;
            d.language = "C#";
            d.message = new
            {
                OperationType = OPerationType,
                account = this.Acccount,
                Token = this.Token,
                UserLanguage = Language,
                DataMessage = object_data
            };

            string result = SendJsonToUrl(this.Url + "/AngelPOST", JsonConvert.SerializeObject(d, Formatting.Indented));
            AngelDB.AngelResponce responce = JsonConvert.DeserializeObject<AngelDB.AngelResponce>(result);
            return responce.result;

        }

        public string SendJsonToUrl(string url, string json)
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
                return $"Error: ReadUrl: {e.ToString()}";
            }

        }


    }

    public class AngelApiOperation
    {
        public string api { get; set; }
        public string account { get; set; }
        public string OperationType { get; set; }
        public string Token { get; set; }
        public string User { get; set; }
        public string language { get; set; }
        public string UserLanguage { get; set; }
        public dynamic message { get; set; }
    }
}
