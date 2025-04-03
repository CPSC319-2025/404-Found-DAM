// In Infrastructure layer
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;
using Azure.Storage;

namespace Infrastructure.DataAccess
{
    public class AzureBlobStorageService : IBlobStorageService
    {
        private readonly string _connectionString;
        private readonly StorageSharedKeyCredential storageSharedKeyCredential;        
        public AzureBlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureBlobStorage");

            var parts = _connectionString.Split(';');
            string accountName = null;
            string accountKey = null;

            foreach (var part in parts)
            {
                if (part.StartsWith("AccountName="))
                {
                    accountName = part.Substring("AccountName=".Length);
                }
                else if (part.StartsWith("AccountKey="))
                {
                    accountKey = part.Substring("AccountKey=".Length);
                }
            }
            storageSharedKeyCredential = new StorageSharedKeyCredential(
            accountName, 
            accountKey);

        }
        
        public async Task<string> UploadAsync(byte[] file, string containerName, Asset assetMetaData)
        {
            // Validate parameters
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null", nameof(file));
                
            // Create blob client and container
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            
            // Create container if it doesn't exist
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            
            // Generate a unique blob name
            // string fileName = assetMetaData.FileName;
            string uniqueFileName = $"{Guid.NewGuid()}";
            
            // Get blob client and upload file
            var blobClient = containerClient.GetBlobClient(uniqueFileName);
            
            // Set content type metadata
            var blobOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = assetMetaData.MimeType
                }
            };
            
            // Upload file
            using (var stream = new MemoryStream(file))
            {
                await blobClient.UploadAsync(stream, blobOptions);
            }
            
            // Return blob ID (full path)
            Console.WriteLine(blobClient.Name);
            return blobClient.Name;
            
        }
        
        public async Task<bool> DeleteAsync(Asset asset, string containerName)
        {
            // Create blob client and container
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            
            // Use the same blob naming convention as in upload
            var blobClient = containerClient.GetBlobClient($"{asset.BlobID}");
            
            // Delete the blob
            return await blobClient.DeleteIfExistsAsync();
        }

        public async Task<string> MoveAsync(string sourceContainer, string blobId, string targetContainer)
        {

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var sourceContainerClient = blobServiceClient.GetBlobContainerClient(sourceContainer);
            var targetContainerClient = blobServiceClient.GetBlobContainerClient(targetContainer);

            // Create container if it doesn't exist
            await targetContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            
            BlobClient sourceBlob = sourceContainerClient.GetBlobClient(blobId);
            BlobClient targetBlob = targetContainerClient.GetBlobClient(blobId);

            if (!await sourceBlob.ExistsAsync())
            {
                Console.WriteLine("Source file not found!");
                return null;
            }

            // Start Copy
            await targetBlob.StartCopyFromUriAsync(sourceBlob.Uri);

            // Wait for copy to complete
            BlobProperties targetBlobProperties = await targetBlob.GetPropertiesAsync();
            while (targetBlobProperties.CopyStatus == CopyStatus.Pending)
            {
                await Task.Delay(500);
                targetBlobProperties = await targetBlob.GetPropertiesAsync();
            }

            // Delete source file after successful copy
            if (targetBlobProperties.CopyStatus == CopyStatus.Success)
            {
                await sourceBlob.DeleteAsync();
                Console.WriteLine($"File moved successfully");
                return targetBlob.Uri.ToString(); 
            }

            return null;
        }
        
        public async Task<List<string>> DownloadAsync(string containerName, List<(string, string)> assetIdNameTuples)
        {
            // assetIdNameTuples.Item2 e.g., "land_picture.webp"
            
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            
            // Make sure container exists
            if (!await containerClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Container {containerName} not found");
            }
                
            // Initialize the list of form files
            // formFiles NOT USED
            // List<IFormFile> formFiles = new List<IFormFile>();
            
            // Create a list of tasks for parallel execution
            var downloadTasks = assetIdNameTuples.Select(async assetIdNameTuple =>
            {
                // Get a client for this specific blob
                var blobClient = containerClient.GetBlobClient(assetIdNameTuple.Item1);

                // Create SAS token with appropriate permissions and expiration
                BlobSasBuilder sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b", // "b" for blob
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // Set expiration (1 hour in this example)
                };

                // Set permissions (read only in this example)
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                // Generate the SAS token
                string sasToken = sasBuilder.ToSasQueryParameters(storageSharedKeyCredential).ToString();

                // Construct the full URL with SAS token
                string blobUrlWithSas = $"{blobClient.Uri}?{sasToken}";

                // Return the URL to the frontend
                return blobUrlWithSas;
                
            }).ToList();
            
            // Wait for all downloads to complete
            var results = await Task.WhenAll(downloadTasks);
            
            // Return the list of form files
            return results.ToList();
        }
        
        public async Task<bool> UpdateAsync(byte[] file, string containerName, Asset assetMetaData)
        {
            // Validate parameters
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null", nameof(file));
                
            // Create blob client and container
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            
            // Make sure container exists
            if (!await containerClient.ExistsAsync())
            {
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            }
            
            // Get blob client for the existing asset - use the existing BlobID
            var blobClient = containerClient.GetBlobClient(assetMetaData.BlobID);
            
            // Set content type metadata
            var blobOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = assetMetaData.MimeType
                }
            };
            
            // Upload file - this will overwrite existing blob
            using (var stream = new MemoryStream(file))
            {
                await blobClient.UploadAsync(stream, blobOptions);
            }
            
            return true;
        }  
    }
}
