// In Infrastructure layer
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;

namespace Infrastructure.DataAccess
{
    public class AzureBlobStorageService : IBlobStorageService
    {
        private readonly string _connectionString;        
        public AzureBlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureBlobStorage");
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
            string fileName = assetMetaData.FileName;
            string uniqueFileName = $"{Guid.NewGuid()}-{fileName}";
            
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
            return blobClient.Name;
            
        }
        
        public async Task<bool> DeleteAsync(Asset asset, string containerName)
        {
            // Create blob client and container
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(asset.BlobID);
            
            // Delete the blob
            return await blobClient.DeleteIfExistsAsync();
        }
        
        public async Task<List<IFormFile>> DownloadAsync(string containerName, List<Asset> assets)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            
            // Make sure container exists
            if (!await containerClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Container {containerName} not found");
            }
            
            // Initialize the list of form files
            List<IFormFile> formFiles = new List<IFormFile>();
            
            // Create a list of tasks for parallel execution
            var downloadTasks = assets.Select(async asset =>
            {
                // Get a client for this specific blob
                var blobClient = containerClient.GetBlobClient(asset.BlobID);
                
                // Download the blob
                var response = await blobClient.DownloadAsync();
                
                // Create a memory stream to copy the blob content
                var memoryStream = new MemoryStream();
                await response.Value.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Reset position for reading

                // Create a FormFile from the memory stream
                var formFile = new FormFile(
                    baseStream: memoryStream,
                    baseStreamOffset: 0,
                    length: memoryStream.Length,
                    name: "file", // Form field name
                    fileName: Path.GetFileName(asset.BlobID)
                );
                
                // Set content type if needed
                // formFile.ContentType = properties.Value.ContentType;
                
                return formFile;
            }).ToList();
            
            // Wait for all downloads to complete
            var results = await Task.WhenAll(downloadTasks);
            
            // Add all results to the existing formFiles list
            formFiles.AddRange(results);
            
            // Return the list of form files
            return formFiles;
        }
        
    }
}