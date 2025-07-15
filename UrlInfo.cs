using System;
using System.Text.Json;

namespace AngelDBTools 
{
    public class UrlInfo
    {
        public string Protocol { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }

    public static class UrlParser
    {
        public static string GetUrlInfoAsJson(string url)
        {
            try
            {
                var uri = new Uri(url);

                int defaultPort = uri.Scheme switch
                {
                    "http" => 80,
                    "https" => 443,
                    _ => uri.Port // para otros protocolos, usamos el puerto si está presente
                };

                var info = new UrlInfo
                {
                    Protocol = uri.Scheme,
                    Host = uri.Host,
                    Port = uri.IsDefaultPort ? defaultPort : uri.Port
                };

                return JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { Error = ex.Message });
            }
        }
    }

}
