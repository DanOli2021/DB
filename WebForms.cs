using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    public class WebForms
    {

        public Dictionary<string, Dictionary<string, string>> WebObjects = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, Dictionary<string, string>> WebGroup = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, string> events = new Dictionary<string, string>();
        public string group = "";

        public string WebFormsPrompt(DB db, string command)
        {

            DbLanguage l = new DbLanguage();
            l.SetCommands(WebFormCommands.Commands());
            Dictionary<string, string> d;

            d = l.Interpreter(command);

            if (d == null)
            {
                return l.errorString;
            }

            if (d.Count == 0)
            {
                return "Error: not command found";
            }

            switch (d.First().Key)
            {
                case "group":

                    this.group = d["group"];
                    return "Ok.";

                case "end_group":

                    d.Add("control", this.group);
                    d.Add("type", "group");
                    d.Add("data", JsonConvert.SerializeObject(WebGroup, Formatting.Indented));
                    WebGroup = new Dictionary<string, Dictionary<string, string>>();
                    this.group = "";
                    return AddObject(d, db);

                case "control":
                    
                    return AddObject(d, db);

                case "event":

                    if (!events.ContainsKey(d["event"]))
                    {
                        events.Add(d["event"], JsonConvert.SerializeObject(d, Formatting.Indented));
                    }
                    else 
                    {
                        events[d["event"]] = JsonConvert.SerializeObject(d, Formatting.Indented);
                    }
                    
                    return "Ok.";

                case "remove_control":

                case "get_controls":
                    
                    return GetWebObjects();
                    
                case "clear":

                    events.Clear();
                    WebObjects.Clear();
                    return "Ok.";                    

                default:
                    return JsonConvert.SerializeObject(d, Formatting.Indented);
            }

        }

        public string AddObject(Dictionary<string, string> d, DB db)
        {

            if (!string.IsNullOrEmpty(this.group))
            {
                if (!WebGroup.ContainsKey(d["control"]))
                {
                    WebGroup.Add(d["control"], d);
                }
                else
                {
                    WebGroup[d["control"]] = d;
                }

                return "Ok.";

            }

            if (!WebObjects.ContainsKey(d["control"]))
            {
                WebObjects.Add(d["control"], d);
            }
            else
            {
                WebObjects[d["control"]] = d;
            }

            return "Ok.";
        }

        public void RemoveControl(Dictionary<string, string> d)
        {
            if (WebObjects.ContainsKey(d["control"]))
            {
                WebObjects.Remove(d["control"]);
            }
        }

        public string GetWebObjects()
        {

            WebData w = new WebData();
            w.WebObjects = JsonConvert.SerializeObject(WebObjects, Formatting.Indented);
            w.WebEvents = JsonConvert.SerializeObject(events, Formatting.Indented);

            return JsonConvert.SerializeObject(w, Formatting.Indented);
        }


        private class WebData 
        {
            public string WebObjects { get; set; } = "";
            public string WebEvents { get; set; } = "";            
            
        }

    }

}