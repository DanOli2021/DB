using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;

namespace AngelDB
{

    public static class DataTools
    {

        public static DataTable GetDataTable(string jSon)
        {
            return JsonConvert.DeserializeObject<DataTable>(jSon);
        }

        public static DataSet GetDataSetFromJson(string jSon)
        {
            return JsonConvert.DeserializeObject<DataSet>(jSon);
        }

        public static string DataTableToJson(DataTable table)
        {
            return JsonConvert.SerializeObject(table, Formatting.Indented);
        }

        public static Dictionary<string, string> JsonToDataDictionary(string jSon)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(jSon);
        }

        public static Dictionary<string, DataTable> JsonToWebMiDictionary(string jSon)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, DataTable>>(jSon);
        }


        public static DataTable JsonWebMiDictionaryToDataTable(string jSon)
        {
            Dictionary<string, DataTable> d = JsonConvert.DeserializeObject<Dictionary<string, DataTable>>(jSon);

            if (d.Count > 0) {
                return d.First().Value;
            }

            return null;
        }

        public static DataRow JsonWebMiDictionaryToDataRow(string jSon)
        {
            Dictionary<string, DataTable> d = JsonConvert.DeserializeObject<Dictionary<string, DataTable>>(jSon);

            if (d.Count > 0)
            {
                return d.First().Value.Rows[0];
            }

            return null;
        }

        public static string GetFirstElement(Dictionary<string, string> data)
        {
            return data.First().Key;
        }

    }

}
