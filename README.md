# 404-Found-DAM
# How to connect to blob storage


```bash
using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace DAMDataMigration
{
    class Program
    {
        // Read from environment variables (set in setup.sh or in your app configuration)
        private static readonly string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        private static readonly string containerName = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONTAINER");

        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: DAMDataMigration <local-file-path>");
                return;
            }

            string filePath = args[0];
            string blobName = Path.GetFileName(filePath);

            try
            {
                // Create a BlobServiceClient using the connection string.
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

                // Get a reference to a container and create it if it doesnt already exist.
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                // Get a reference to the blob (file) you want to upload.
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                // Upload the file.
                using FileStream uploadFileStream = File.OpenRead(filePath);
                await blobClient.UploadAsync(uploadFileStream, overwrite: true);
                uploadFileStream.Close();

                Console.WriteLine($"Successfully uploaded {blobName} to container '{containerName}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
            }
        }
    }
}
