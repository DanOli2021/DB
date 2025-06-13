using Azure.Data.Tables;
using Azure;
using Azure.Storage.Blobs;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AngelDB
{
    public class AzureBlobDownloader
    {
        public static async Task DownloadAndZipDirectlyAsync(string connectionString, string containerName, string outputZipPath, bool deleteContainer = false)
        {
            // Crear cliente de contenedor
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            if (!await containerClient.ExistsAsync())
            {
                Console.WriteLine($"El contenedor {containerName} no existe.");
                return;
            }

            // Crear archivo ZIP
            using (var zipStream = new FileStream(outputZipPath, FileMode.Create))
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false))
            {
                Console.WriteLine("Descargando y agregando blobs al ZIP...");

                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var zipEntry = zipArchive.CreateEntry(blobItem.Name, CompressionLevel.Optimal);

                    // Descargar el blob directamente al ZIP
                    using (var zipEntryStream = zipEntry.Open())
                    {
                        await blobClient.DownloadToAsync(zipEntryStream);
                        Console.WriteLine($"Blob agregado al ZIP: {blobItem.Name}");
                    }
                }
            }

            if (deleteContainer)
            {
                // Eliminar el contenedor
                Console.WriteLine("Eliminando el contenedor...");
                await containerClient.DeleteAsync();
                Console.WriteLine($"Contenedor {containerName} eliminado.");
            }
            else
            {
                Console.WriteLine("El contenedor no se eliminó.");
            }
        }

        public static async Task DownloadAddToZipAndDeleteBlobAsync(string connectionString, string containerName, string outputZipPath, bool deleteContainer = false)
        {
            // Crear cliente de contenedor
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            if (!await containerClient.ExistsAsync())
            {
                Console.WriteLine($"El contenedor {containerName} no existe.");
                return;
            }

            // Crear archivo ZIP
            using (var zipStream = new FileStream(outputZipPath, FileMode.Create))
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false))
            {
                Console.WriteLine("Descargando, agregando blobs al ZIP y eliminándolos...");

                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var zipEntry = zipArchive.CreateEntry(blobItem.Name, CompressionLevel.Optimal);

                    try
                    {
                        // Descargar el blob directamente al ZIP
                        using (var zipEntryStream = zipEntry.Open())
                        {
                            await blobClient.DownloadToAsync(zipEntryStream);
                            Console.WriteLine($"Blob agregado al ZIP: {blobItem.Name}");
                        }

                        // Eliminar el blob después de haberlo añadido al ZIP
                        await blobClient.DeleteAsync();
                        Console.WriteLine($"Blob eliminado: {blobItem.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error con el blob {blobItem.Name}: {ex.Message}");
                        // Manejo de errores si es necesario
                    }
                }
            }

            if (deleteContainer)
            {
                // Eliminar el contenedor
                Console.WriteLine("Eliminando el contenedor...");
                await containerClient.DeleteAsync();
                Console.WriteLine($"Contenedor {containerName} eliminado.");
            }
            else
            {
                Console.WriteLine("El contenedor no se eliminó.");
            }

        }

        public static async Task DeleteContainerAsync(string connectionString, string containerName)
        {
            // Crear cliente de contenedor
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            if (!await containerClient.ExistsAsync())
            {
                Console.WriteLine($"El contenedor {containerName} no existe.");
                return;
            }

            // Eliminar el contenedor
            Console.WriteLine("--> Eliminando el contenedor...");
            await containerClient.DeleteAsync();
            Console.WriteLine($"Contenedor {containerName} eliminado.");

        }
    }

    public class AzureTableHelper
    {
        private readonly TableClient _tableClient;
        private readonly string tableName;

        public AzureTableHelper(string connectionString, string tableName)
        {
            _tableClient = new TableClient(connectionString, tableName);
            this.tableName = tableName;
        }
        public async Task DeleteEntitiesByPartitionKeyAsync(string partitionKey)
        {
            try
            {
                AsyncPageable<TableEntity> queryResults = _tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{partitionKey}'");

                List<TableTransactionAction> batchActions = new List<TableTransactionAction>();
                int batchSize = 100; // Tamaño máximo permitido por batch
                int totalDeleted = 0;

                await foreach (TableEntity entity in queryResults)
                {
                    batchActions.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));

                    if (batchActions.Count == batchSize)
                    {
                        await _tableClient.SubmitTransactionAsync(batchActions);
                        totalDeleted += batchActions.Count;
                        Console.WriteLine($"Deleted {totalDeleted} entities from table {tableName} with PartitionKey: {partitionKey}");
                        batchActions.Clear();
                    }
                }

                // Procesa las entidades restantes si hay menos de 100
                if (batchActions.Count > 0)
                {
                    await _tableClient.SubmitTransactionAsync(batchActions);
                    totalDeleted += batchActions.Count;
                    Console.WriteLine($"Deleted {totalDeleted} entities from table {tableName} with PartitionKey: {partitionKey}");
                }

                Console.WriteLine("All entities with the specified PartitionKey have been deleted.");
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error deleting entities: {ex.Message}");
            }
        }
    }

}