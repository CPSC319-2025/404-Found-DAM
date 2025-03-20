// In Infrastructure layer
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Core.Interfaces;
using Core.Dtos;

namespace Infrastructure.DataAccess
{
    public class AzureBlobStorageService : IBlobStorageService
    {
        private readonly string _connectionString;        
        public AzureBlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureBlobStorage");
        }
        
        public async Task<string> UploadAsync(IFormFile file, string containerName, UploadAssetsReq request)
        {
            try {
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
                string uniqueFileName = $"{Guid.NewGuid()}-{fileName}";
                
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
            } catch (Exception ex) {
                Console.WriteLine($"Error uploading blob: {ex.Message}");
                return null;
            }
            
        }
        
        public async Task<bool> DeleteAsync(string blobId, string containerName)
        {
            try {
                // Create blob client and container
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobId);
                
                // Delete the blob
                return await blobClient.DeleteIfExistsAsync();
            } catch (Exception ex) {
                Console.WriteLine($"Error deleting blob: {ex.Message}");
                return false;
            }
        }
        
        public async Task<List<IFormFile>> DownloadAsync(string containerName, int userId)
        {
            try {
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                
                // Make sure container exists
                if (!await containerClient.ExistsAsync())
                {
                    return new List<IFormFile>();
                }
                
                List<IFormFile> formFiles = new List<IFormFile>();
                
                // Get all blobs in the container
                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    
                    // Get a client for this specific blob
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    
                    // Download the blob
                    var response = await blobClient.DownloadAsync();
                    
                    // Create a memory stream to copy the blob content
                    var memoryStream = new MemoryStream();
                    await response.Value.Content.CopyToAsync(memoryStream);
                    memoryStream.Position = 0; // Reset position for reading
                    
                    // Get content type
                    var properties = await blobClient.GetPropertiesAsync();
                    
                    // Create a FormFile from the memory stream
                    var formFile = new FormFile(
                        baseStream: memoryStream,
                        baseStreamOffset: 0,
                        length: memoryStream.Length,
                        name: "file", // Form field name
                        fileName: Path.GetFileName(blobItem.Name)
                    );
                    
                    // Set content type if needed
                    formFile.ContentType = properties.Value.ContentType;
                    
                    formFiles.Add(formFile);
                }
            
                return formFiles;
            } catch (Exception ex) {
                Console.WriteLine($"Error downloading files: {ex.Message}");
                return new List<IFormFile>();
            }
            
        }
        
    }
}