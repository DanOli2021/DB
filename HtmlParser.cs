using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using HtmlAgilityPack;

namespace AngelDB 
{
    public static class HtmlParser
    {
        public static string GetHtmlSectionsAsJson(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var result = new Dictionary<string, object>();

            // HEAD
            var headNode = doc.DocumentNode.SelectSingleNode("//head");
            result["HEAD"] = headNode?.InnerHtml?.Trim() ?? "";

            // BODY
            var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
            result["BODY"] = bodyNode?.InnerHtml?.Trim() ?? "";

            // CSS (estilos en <style>)
            var styleNodes = doc.DocumentNode.SelectNodes("//style");
            result["CSS"] = styleNodes != null
                ? string.Join("\n", styleNodes.Select(n => n.InnerText.Trim()))
                : "";

            // JAVASCRIPT (scripts dentro de <script>)
            var scriptNodes = doc.DocumentNode.SelectNodes("//script");
            result["JAVASCRIPT"] = scriptNodes != null
                ? string.Join("\n", scriptNodes.Select(n => n.InnerText.Trim()))
                : "";

            // FEATURES (cards dentro del section#features)
            var featuresList = new List<Dictionary<string, string>>();
            var featureCards = doc.DocumentNode.SelectNodes("//section[@id='features']//div[contains(@class, 'feature-card')]");

            if (featureCards != null)
            {
                foreach (var card in featureCards)
                {
                    var titleNode = card.SelectSingleNode(".//h3");
                    var descNode = card.SelectSingleNode(".//p");

                    featuresList.Add(new Dictionary<string, string>
                {
                    { "title", titleNode?.InnerText.Trim() ?? "" },
                    { "description", descNode?.InnerText.Trim() ?? "" }
                });
                }
            }

            result["FEATURES"] = featuresList;

            string htmlJson = JsonConvert.SerializeObject(result, Formatting.Indented);
            //Console.WriteLine(htmlJson);

            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }
    }
}


