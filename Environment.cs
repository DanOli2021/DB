using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AngelDBTools
{
    public class MyEnvironment
    {

        public Dictionary<string, DataConnection> Connections { get; set; } = new Dictionary<string, DataConnection>();
        public DataConnection Connection { get; set; } = new DataConnection();
        public string NameToEditOrDelete { get; set; } = "";

        public string SaveEnvironment()
        {
            var path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Global");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string result = AngelDBTools.StringFunctions.SaveEncriptedConfig(path + "WebMiConfig.config", this);

            if (result.StartsWith("Error:"))
            {
                return result;
            }

            return "Ok.";

        }


    }

    public class DataConnection
    {
        public string Name { get; set; } = "";
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public string Server { get; set; } = "";
        public int port { get; set; } = 0;
        public string Secret { get; set; } = "";
    }

}
