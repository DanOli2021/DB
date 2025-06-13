using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Newtonsoft.Json;

namespace AngelDB
{
    public class TableArea
    {
        public string use_table = "";
        public string partition_key = "";
        public string where = "";
        public string order_by = "";
        public int record_number = 0;
        public bool IsEOF;
        public DB db = null;

        public Dictionary<string, object> fields = new Dictionary<string, object>();

        public string UseTable(Dictionary<string, string> d, DB db) 
        {
            this.db = db;
            string result = Tables.VerifyAccountAndDatabase(db);
            if (result.StartsWith("Error:")) return result;

            use_table = d["use_table"];
            where = d["where"];
            order_by = d["order_by"];
            record_number = 0;
            partition_key = d["partition_key"];

            if (string.IsNullOrEmpty(this.partition_key))
            {
                return "Error: Table partition not indicated";
            }

            result = db.Prompt($"GET TABLES WHERE tablename = '{use_table}'");

            if (result == "[]") 
            {
                return $"Error: The indicated table does not exist: {use_table}";
            }

            if( !fields.ContainsKey( "id") ) fields.Add("id", System.Guid.NewGuid().ToString());

            DataTable table = JsonConvert.DeserializeObject<DataTable>(result);
            string[] fields_list = table.Rows[0]["fieldlist"].ToString().Split(",");

            foreach (string item in fields_list)
            {
                if (!fields.ContainsKey(item.Trim())) 
                {
                    fields.Add(item.Trim(), null);
                }
            }

            result = db.Prompt($"SELECT * FROM {use_table} PARTITION KEY {partition_key} WHERE {where} ORDER BY {order_by} LIMIT 1 OFFSET {record_number}");

            if (result == "[]") 
            {
                this.IsEOF = true;
                return "Ok.";
            }

            table = JsonConvert.DeserializeObject<DataTable>(result);

            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn column in table.Columns)
                {
                    if (column.ColumnName == "file") continue;
                    if (column.ColumnName == "timestamp") continue;
                    if (column.ColumnName == "PartitionKey") continue;
                    fields[column.ColumnName] = Convert.ChangeType(row[column.ColumnName], Type.GetType(row[column.ColumnName].GetType().ToString()));
                }
            }

            return "Ok.";
        }

        public string EOF() 
        {
            return this.IsEOF.ToString();
        }


        public string Next() 
        {

            if (string.IsNullOrEmpty(this.use_table)) 
            {
                return "Error: No table selected";
            }

            ++this.record_number;

            string result = db.Prompt($"SELECT * FROM {use_table} PARTITION KEY {partition_key} WHERE {where} ORDER BY {order_by} LIMIT 1 OFFSET {record_number}");

            if (result == "[]")
            {
                --this.record_number;
                this.IsEOF = true;
                return this.record_number.ToString();
            }

            DataTable table = JsonConvert.DeserializeObject<DataTable>(result);

            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn column in table.Columns)
                {
                    fields[column.ColumnName] = Convert.ChangeType(row[column.ColumnName], Type.GetType(row.GetType().ToString()));
                }
            }

            return this.record_number.ToString();
        }


        public string Field(string field, string value) 
        {
            try
            {

                if (!this.fields.ContainsKey(field)) 
                {
                    return $"Error: No field found: {field}";
                }

                if (value != "null") 
                {
                    if (value.StartsWith("\""))
                    {
                        value = value.Trim('\"');
                        fields[field] = value;
                        return fields[field].ToString();
                    }
                    else if (AngelDBTools.StringFunctions.IsStringFloatNumber(value))
                    {
                        float floatnumber = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                        fields[field] = floatnumber;
                        return floatnumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (value == "NULL")
                    {
                        fields[field] = null;
                        return "NULL";
                    }
                    else if (value == "true")
                    {
                        fields[field] = true;
                        return "true";
                    }
                    else if (value == "false")
                    {
                        fields[field] = false;
                        return "false";
                    }
                    else if (value.StartsWith("@"))
                    {
                        if (!db.vars.ContainsKey(value.Substring(1)))
                        {
                            throw new Exception($"Error: No variable found: {value.Substring(1)}");
                        }

                        fields[field] = db.vars[value.Substring(1)];
                        return db.vars[value.Substring(1)].ToString();
                    }
                    else if (value.StartsWith("&")) 
                    {
                        string result = db.Prompt(value.Substring(2));
                        fields[field] = result;
                        return result;
                    }

                }

                if (fields[field] == null) return "NULL";
                return fields[field].ToString();
            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }

        public string UpdateData() 
        {

            if (string.IsNullOrEmpty(this.use_table))
            {
                return "Error: No table selected";
            }

            if (partition_key == null)
            {
                return "Error: Table partition (PARTITION KEY ) not indicated";
            }

            if (fields["id"] == null) 
            {
                return "Error: The registry IDENTIFIER (id) has not been indicated";
            }

            string sql = $"INSERT INTO {this.use_table} PARTITION KEY {this.partition_key} VALUES {JsonConvert.SerializeObject(this.fields)}";
            string result = db.Prompt(sql);
            return result;       
        
        }

        public string NewRow()
        {

            if (string.IsNullOrEmpty(this.use_table))
            {
                return "Error: No table selected";
            }

            if (partition_key == null)
            {
                return "Error: Table partition (PARTITION KEY ) not indicated";
            }

            fields["id"] = System.Guid.NewGuid().ToString();
            return "Ok.";

        }



    }
}
