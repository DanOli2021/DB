using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace AngelDB
{

    public static class Curl
    {
        public static async Task<string> Get(string url, WebHeaderCollection headers = null)
        {
            using (HttpClient client = new HttpClient())
            {
                if (headers != null)
                {
                    foreach (string key in headers.AllKeys)
                    {
                        client.DefaultRequestHeaders.Add(key, headers[key]);
                    }
                }

                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        public static async Task<string> Post(string url, string data, WebHeaderCollection headers = null)
        {
            using (HttpClient client = new HttpClient())
            {
                if (headers != null)
                {
                    foreach (string key in headers.AllKeys)
                    {
                        client.DefaultRequestHeaders.Add(key, headers[key]);
                    }
                }

                StringContent content = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");

                HttpResponseMessage response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
