using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Newtonsoft.Json;

namespace AngelDB
{
    public class AzureTable
    {

        TableServiceClient serviceClient;
        TableClient tableClient;
        Pageable<TableEntity> queryResultsFilter;
        string Continuationtoken = "";
        public string ConnectionString = null;
        private bool stop_querying = false;

        public string TableServiceClient( string ConnectionString ) 
        {
            try
            {
                // Construct a new "TableServiceClient using a TableSharedKeyCredential.
                serviceClient = new TableServiceClient( ConnectionString );
                this.ConnectionString = ConnectionString;
                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: TableServiceClient: {e.ToString()}";
            }
        }

        public string CreateTable(string tableName)
        {
            try
            {
                // Create a new table client.
                tableClient = serviceClient.GetTableClient(tableName);
                // Create the table if it doesn't exist.
                tableClient.CreateIfNotExists();

                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: CreateTable: {e.ToString()}";
            }
        }


        public string GetTables(string filter) 
        {
            try
            {
                // Use the <see cref="TableServiceClient"> to query the service. Passing in OData filter strings is optional.

                Pageable<TableItem> queryTableResults = serviceClient.Query(filter: filter);
                return JsonConvert.SerializeObject(queryTableResults, Formatting.Indented);

            }
            catch (Exception e)
            {
                return $"Error: GetTables: {e.ToString()}";
            }
        }


        public string DeleteTable(string tableName)
        {
            try 
            {
                // Create a new table client.
                tableClient = serviceClient.GetTableClient(tableName);
                // Delete the table if it exists.
                tableClient.Delete();
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: DeleteTable: {e.ToString()}";
            }
        }

        public string Insert(string jSon) 
        {
            try
            {
                TableEntity t = JsonConvert.DeserializeObject<TableEntity>(jSon);
                tableClient.AddEntity(t);
                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: Insert: {e.ToString()}";
            }
        }

        public string Update(string jSon)
        {
            try
            {
                Azure.ETag ifMatch = new Azure.ETag("*");
                TableEntity t = JsonConvert.DeserializeObject<TableEntity>(jSon);
                tableClient.UpdateEntity(t, ifMatch);
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: Insert: {e.ToString()}";
            }
        }


        public string InsertValues( string json ) 
        {
            try
            {
                List<TableEntity> entityList = JsonConvert.DeserializeObject<List<TableEntity>>(json);
                List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
                addEntitiesBatch.AddRange(entityList.Select(e => new TableTransactionAction(TableTransactionActionType.UpsertReplace, e)));
                tableClient.SubmitTransaction(addEntitiesBatch);
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: InsertValues: {e.ToString()}";
            }
        }
        
        

        public string Query( string filter ) 
        {
            try
            {
                stop_querying = false;
                this.queryResultsFilter = tableClient.Query<TableEntity>(filter: filter);
                this.Continuationtoken = null;
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: Query: {e.ToString()}";
            }

        }

        
        public string GetQueryResults( int limit = 1000 ) 
        {
            try
            {

                if (stop_querying == true) 
                {
                    return "[]";
                }

                foreach (var p in this.queryResultsFilter.AsPages(this.Continuationtoken, limit))
                {

                    JsonSerializerSettings microsoftDateFormatSettings = new JsonSerializerSettings
                    {
                        DateFormatString = "yyyy-MM-dd HH:mm:ss.fffffff"
                    };

                    this.Continuationtoken = p.ContinuationToken;

                    if (p.ContinuationToken is null) 
                    {
                        stop_querying = true;
                    }

                    return JsonConvert.SerializeObject(p.Values, Formatting.Indented, microsoftDateFormatSettings);
                }

                return "[]";

            }
            catch (Exception e)
            {
                return $"Error: GetQueryResults: {e.ToString()}";
            }

        }

        public string DeleteEntity(string partitionKey, string rowKey)
        {
            try 
            {
                tableClient.DeleteEntity(partitionKey, rowKey);
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: DeleteEntity: {e.ToString()}";
            }
        }

    }
}