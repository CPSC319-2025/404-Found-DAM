using Microsoft.AspNetCore.Http;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Core.Entities;

namespace Infrastructure.DataAccess
{
    public class LocalBlobStorageService : IBlobStorageService
    {
        public async Task<string> UploadAsync(byte[] file, string containerName, Asset assetMetaData)
        {
            string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            if (!Directory.Exists(storageDirectory))
            {
                Directory.CreateDirectory(storageDirectory);
            }
            // Create an Asset instance with the file path
            // Generate a unique blob name
            // string fileName = assetMetaData.FileName;
            string uniqueBlobName = $"{Guid.NewGuid()}";
            
            // Store raw file without zst extension
            await File.WriteAllBytesAsync(Path.Combine(storageDirectory, $"{uniqueBlobName}.{assetMetaData.FileName}"), file);

            return uniqueBlobName;
        }
        
        public async Task<bool> DeleteAsync(Asset asset, string containerName)
        {
            // Create storage directory if it doesn't exist
            string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            if (!Directory.Exists(storageDirectory))
            {
                Directory.CreateDirectory(storageDirectory);
            }
            // Delete the corresponding file
            string filePath = Path.Combine(storageDirectory, $"{asset.BlobID}.{asset.FileName}");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return true;
        }

        public async Task<List<string>> DownloadAsync(string containerName, List<(string, string)> assetIdNameTuples)
        {
            // Create storage directory if it doesn't exist
            string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            if (!Directory.Exists(storageDirectory)) {
                Directory.CreateDirectory(storageDirectory);
            }
            
            // Create a list to store file paths
            var filePaths = new List<string>();
            
            // Process each asset tuple
            foreach (var assetIdNameTuple in assetIdNameTuples) {
                var filePath = Path.Combine(storageDirectory, $"{assetIdNameTuple.Item1}.{assetIdNameTuple.Item2}");
                
                // Check if file exists
                if (File.Exists(filePath)) {
                    filePaths.Add(filePath);
                }
            }
            
            return filePaths;
        }

        public async Task<string> MoveAsync(string sourceContainer, string blobId, string targetContainer)
        {
            // nothing to do here, just return true
            return blobId;
        }

        public async Task<bool> UpdateAsync(byte[] file, string containerName, Asset assetMetaData)
        {
            string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            if (!Directory.Exists(storageDirectory))
            {
                Directory.CreateDirectory(storageDirectory);
            }
            
            // Delete the old file if it exists
            string oldFilePath = Path.Combine(storageDirectory, $"{assetMetaData.BlobID}.{assetMetaData.FileName}");
            if (File.Exists(oldFilePath))
            {
                File.Delete(oldFilePath);
            }
            
            // Write the new file using the same BlobID
            await File.WriteAllBytesAsync(Path.Combine(storageDirectory, $"{assetMetaData.BlobID}.{assetMetaData.FileName}"), file);
            
            return true;
        }  
    }
}