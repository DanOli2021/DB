using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    public static class WebTools
    {
        public static string ReadUrl(string url) 
        {
            try
            {
                HttpClient web = new HttpClient();
                HttpResponseMessage response = web.GetAsync( url).GetAwaiter().GetResult();

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
                return $"Error: ReadUrl: {e.ToString()}";
            }

        }
    }
}
