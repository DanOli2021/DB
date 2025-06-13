using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System.Data;

namespace AngelDB
{

#pragma warning disable CS8618
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8618


    public class AzureTableStorage<T> where T : AzureEntity, new()
    {

        public string ConnectionString { get; set; }
        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private Dictionary<string, CloudTable> tables = new Dictionary<string, CloudTable>();
        private Dictionary<string, TableContinuationToken> continuationToken = new Dictionary<string, TableContinuationToken>();
        private Dictionary<string, int> counters = new Dictionary<string, int>();


        public AzureTableStorage(string connectionString)
        {
            // Retrieve the storage account from the connection string.
            storageAccount = CloudStorageAccount.Parse(connectionString);
            this.ConnectionString = connectionString;
            // Create the table if it doesn't exist.            
            // Create the table client.
            tableClient = storageAccount.CreateCloudTableClient();

        }

        public string GetTable(string tableName)
        {
            try
            {
                if (!tables.ContainsKey(tableName))
                {
                    tables.Add(tableName, tableClient.GetTableReference(tableName));
                    //tables[tableName].CreateIfNotExistsAsync().GetAwaiter().GetResult();
                }

                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: {e.ToString()}";
            }
        }


        public string Query(string tablename, string fieldlist, string filter, string use_rowkey, string exclude_counter, string limit)
        {

            try
            {
                string result = GetTable(tablename);

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                if (!continuationToken.ContainsKey(tablename))
                {
                    continuationToken.Add(tablename, null);
                }

                TableQuery<T> query = new TableQuery<T>().Where(filter);

                var results = new List<T>();
                var response = tables[tablename].ExecuteQuerySegmentedAsync(query, continuationToken[tablename]).GetAwaiter().GetResult();
                continuationToken[tablename] = response.ContinuationToken;

                DataTable dt = new DataTable();

                if (!counters.ContainsKey(tablename))
                {
                    counters.Add(tablename, 0);
                }

                List<string> fields = new List<string>();

                if (fieldlist.Trim() != "*")
                {
                    string[] field_list = fieldlist.Split(",");

                    foreach (string field in field_list)
                    {
                        fields.Add(field.Trim());
                    }
                }

                int nlimit = 0;
                int n = 0;

                if (limit != "null")
                {
                    nlimit = int.Parse(limit.Trim());
                    n = 0;
                }

                foreach (T item in response.Results)
                {
                    if (dt.Columns.Count == 0)
                    {
                        if (exclude_counter == "false")
                        {
                            dt.Columns.Add("Counter_");
                        }

                        dt.Columns.Add("AzurePartitionKey");

                        if (use_rowkey == "false")
                        {
                            dt.Columns.Add("id");
                        }
                        else
                        {
                            dt.Columns.Add("RowKey");
                        }

                        dt.Columns.Add("AzureTimestamp");

                        if (fields.Count > 0)
                        {
                            foreach (string column in fields)
                            {
                                dt.Columns.Add(column);
                            }
                        }
                        else
                        {
                            foreach (string column in item.properties.Keys)
                            {
                                dt.Columns.Add(column);
                            }
                        }

                    }

                    ++counters[tablename];

                    DataRow dr = dt.NewRow();

                    if (exclude_counter == "false")
                    {
                        dr["Counter_"] = counters[tablename]; ;
                    }

                    dr["AzurePartitionKey"] = item.PartitionKey;

                    if (use_rowkey == "false")
                    {
                        dr["id"] = item.RowKey;
                    }
                    else
                    {
                        dr["RowKey"] = item.RowKey;
                    }

                    dr["AzureTimestamp"] = item.Timestamp.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff");

                    if (fields.Count > 0)
                    {
                        foreach (string column in fields)
                        {
                            if (dr.Table.Columns.Contains(column))
                            {
                                try
                                {
                                    dr[column] = item.properties[column].StringValue;
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (string column in item.properties.Keys)
                        {
                            if (dr.Table.Columns.Contains(column))
                            {
                                try
                                {
                                    dr[column] = item.properties[column].StringValue;
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }

                    dt.Rows.Add(dr);

                    if (limit != "null")
                    {
                        ++n;
                        if (n == nlimit) break;
                    }
                }

                if (continuationToken[tablename] is null)
                {
                    counters[tablename] = 0;
                }

                return JsonConvert.SerializeObject(dt, Formatting.Indented);


            }
            catch (Exception e)
            {
                return $"Error: {e.ToString()}";

            }
        }


        public string AreThereMore(string tableName)
        {

            if (!continuationToken.ContainsKey(tableName))
            {
                return $"Error: The indicated table does not exist {tableName}";
            }

            if (continuationToken[tableName] is null)
            {
                return "NO";
            }
            else
            {
                return "YES";
            }
        }

        public string ResetQuery(string tablename)
        {

            if (!continuationToken.ContainsKey(tablename))
            {
                return $"Error: The indicated table does not exist {tablename}";
            }

            counters[tablename] = 0;
            continuationToken[tablename] = null;
            return "Ok.";
        }




    }

    public class AzureEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string ETag { get; set; }
        public IDictionary<string, EntityProperty> properties;

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            if (properties.ContainsKey("id"))
            {
                properties.Remove("id");
            }

            this.properties = properties;
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            throw new NotImplementedException();
        }
    }


}
