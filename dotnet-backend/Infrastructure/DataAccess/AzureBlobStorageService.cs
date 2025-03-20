// In Infrastructure layer
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Core.Interfaces;

namespace Infrastructure.DataAccess
{
    public class AzureBlobStorageService : IBlobStorageService
    {
        private readonly string _connectionString;        
        public AzureBlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureBlobStorage");
        }
        
        public async Task<string> UploadAsync(IFormFile file, string containerName, string projectPrefix = null)
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
            string fileName = file.FileName;
            string uniqueFileName = string.IsNullOrEmpty(projectPrefix) 
                ? $"{Guid.NewGuid()}-{fileName}"
                : $"{projectPrefix}__{Guid.NewGuid()}-{fileName}";
            
            // Get blob client and upload file
            var blobClient = containerClient.GetBlobClient(uniqueFileName);
            
            // Set content type metadata
            var blobOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                }
            };
            
            // Upload file
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, blobOptions);
            }
            
            // Return blob ID (full path)
            return blobClient.Name;
        }
        
        public async Task<bool> DeleteAsync(string blobId, string containerName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobId);
            
            return await blobClient.DeleteIfExistsAsync();
        }
        
        public async Task<Stream> DownloadAsync(string blobId, string containerName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobId);
            
            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }
        
    }
}