using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Core;
using Azure.Data.Tables.Models;
using Azure;

namespace WebMi
{
    public class AzureSQL
    {

        string storageUri = "";
        string accountName = "";
        string storageAccountKey = "";
        string tableName = "";
        TableServiceClient serviceClient = null;
        TableClient tableClient = null;
        
        bool Connected = false;

        public AzureSQL( string sotarageUri, string accountName, string storageAccountKey, string tableName  ) 
        {
            this.storageUri = sotarageUri;
            this.accountName = accountName;
            this.storageAccountKey = storageAccountKey;
            this.tableName = tableName;
        }

        public string Connect() 
        {
            try
            {
                serviceClient = new TableServiceClient(
                               new Uri(this.storageUri),
                               new TableSharedKeyCredential(this.accountName, this.storageAccountKey));

                this.tableClient = serviceClient.GetTableClient(tableName);
                tableClient.CreateIfNotExists();

                this.Connected = true;
            }
            catch (Exception ex) 
            {
                return $"Error: {ex.ToString()}";
            }

            return "Ok.";
        }


        public string DeleteTable() 
        {

            if (!IsConnected()) 
            {
                return "Error: Not Azure Connection";
            }

            return "Ok.";
        }

        bool IsConnected() 
        { 
            return Connected;
        }


        public string QueryTable( string query ) 
        {
            Pageable<> item TableItem> queryTableResults = tableClient.Query(query);

            foreach (TableItem table in queryTableResults)
            {
                Console.WriteLine(table.Name);
            }

            return "Ok.";
        }

    }
}
