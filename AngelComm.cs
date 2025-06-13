using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;

namespace AngelDB
{
    public class AngelComm
    {

        public string Url { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string User { get; set; } = "";
        public string Token { get; set; } = "";
        public string UserLanguage { get; set; } = "";


        public AngelComm(string url, string AccountName, string UserLanguage)
        {
            this.Url = url;
            this.AccountName = AccountName;
            this.UserLanguage = UserLanguage;
        }


        public string Login(string AccountName, string User, string Password )
        {
            this.User = User;
            this.AccountName = AccountName;
            this.Token = Send("tokens/admintokens", "GetTokenFromUser", new { User, Password }, "C#");
            return this.Token;
        }

        public string Send(string api_name, string OPerationType, dynamic object_data,  string language)
        {

            AngelApiOperation d = new AngelApiOperation();
            d.api = api_name;
            d.account = this.AccountName;
            d.OperationType = OPerationType;
            d.Token = this.Token;
            d.User = this.User;
            d.language = language;
            d.message = new
            {
                OperationType = OPerationType,
                account = this.AccountName,
                this.Token,
                this.UserLanguage,
                DataMessage = object_data
            };

            string result = SendJsonToUrl(this.Url + "/AngelPOST", JsonConvert.SerializeObject(d, Formatting.Indented));

            if(result.StartsWith("Error:"))
            {
                return result + " 1";
            }

            AngelDB.AngelResponce responce = JsonConvert.DeserializeObject<AngelDB.AngelResponce>(result);
            return responce.result;

        }


        string SendJsonToUrl(string url, string json)
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
}