using System.Text;
using System.Data;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AngelDB
{
    public class DBTools
    {

        public DB db;
        bool serverConnection = false;        
        DataTable t = new DataTable();
        Dictionary<string, object> fieldsList = new Dictionary<string, object>();
        Dictionary<string, object> data = new Dictionary<string, object>();

        public DBTools(DB db, bool toServer) {
            this.db = db;
            this.serverConnection = toServer;
        }

        public DBTools(DB db)
        {
            this.db = db;
            this.serverConnection = false;
        }


        public void Reset() {
            this.fieldsList.Clear();
            t.Clear();
        }            

        public void AddField(string fieldName, object value)
        {
            data.Add(fieldName, value);
        }

        public string InsertInto(string tableName, string partitionKey, string id) {
            return this.db.Prompt(InsertInto(tableName, partitionKey, id, this.data));
        }


        public string InsertInto(string tableName, string partitionKey, string id, string values) {

            StringBuilder sb = new StringBuilder();

            if (serverConnection) {
                sb.Append("SERVER ");
            }

            sb.Append("INSERT INTO ");
            sb.Append(tableName);
            sb.Append("PARTITION KEY ");
            sb.Append(partitionKey);

            if (!string.IsNullOrEmpty(id)) {
                sb.Append("ID ");
                sb.Append(id);
            }

            sb.Append("VALUES ");
            sb.Append(values);

            return this.db.Prompt(sb.ToString());

        }


        public string InsertInto(string tableName, string partitionKey, string id, Dictionary<string, object> values)
        {

            StringBuilder sb = new StringBuilder();

            if (serverConnection)
            {
                sb.Append("SERVER ");
            }

            sb.Append("INSERT INTO ");
            sb.Append(tableName);
            sb.Append(" PARTITION KEY ");
            sb.Append(partitionKey);

            if (!string.IsNullOrEmpty(id))
            {
                sb.Append(" ID ");
                sb.Append(id);
            }

            foreach (var item in data)
            {
                if (!t.Columns.Contains(item.Key)) {
                    t.Columns.Add(item.Key);
                }
            }

            t.Rows.Add();

            foreach (var item in data)
            {
                t.Rows[0][item.Key] = item.Value;
            }

            sb.Append(" VALUES ");
            sb.Append(JsonConvert.SerializeObject(t));

            return this.db.Prompt(sb.ToString());

        }

        public DataSet SQLDataSet(string query) {

            if (serverConnection)
            {
                query = "SERVER " + query;
            }

            string result = this.db.Prompt(query);

            if (result.StartsWith("Error:")) {
                System.Exception e = new System.Exception(result);
                throw e;
            }

            return JsonConvert.DeserializeObject<DataSet>(result);
        }


        public DataTable SQLDataTable(string query)
        {

            if (!query.Contains(" PARTITION KEY ", System.StringComparison.InvariantCulture))
            {
                System.Exception e = new System.Exception("Error: A DataTable can only be filled with if the PARTITION KEY is specified");
                throw e;
            }

            if (serverConnection)
            {
                query = "SERVER " + query;
            }

            string result = this.db.Prompt(query);

            if (result.StartsWith("Error:"))
            {
                System.Exception e = new System.Exception(result);
                throw e;
            }

            return JsonConvert.DeserializeObject<DataTable>(result);
        }


        public string SQLExec(string query) {

            if (serverConnection)
            {
                query = "SERVER " + query;
            }

            return this.db.Prompt(query);
        }


    }
}
