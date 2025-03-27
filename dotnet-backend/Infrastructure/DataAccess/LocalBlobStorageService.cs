using Microsoft.AspNetCore.Http;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Core.Entities;

namespace Infrastructure.DataAccess
{
    public class LocalBlobStorageService : IBlobStorageService
    {

        public async Task<string> UploadEditedImageAsync (byte[] file, string blobID, string containerName, Asset assetMeta) {
            string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            if (!Directory.Exists(storageDirectory)) {
                Directory.CreateDirectory(storageDirectory);
            }

            // re-use the same blobID 
            await File.WriteAllBytesAsync(Path.Combine(storageDirectory, $"{blobID}.{assetMeta.FileName}.zst"), file);
            return blobID;
        }


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
            
            // TODO keeping this for Dennnis to review
            await File.WriteAllBytesAsync(Path.Combine(storageDirectory, $"{uniqueBlobName}.{assetMetaData.FileName}.zst"), file);

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
            string filePath = Path.Combine(storageDirectory, $"{asset.BlobID}.{asset.FileName}.zst");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return true;
        }

        public async Task<List<IFormFile>> DownloadAsync(string containerName, List<(string, string)> assetIdNameTuples)
        {
            var compressedFiles = new List<IFormFile>();
            
            // Create storage directory if it doesn't exist
            string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            if (!Directory.Exists(storageDirectory)) {
                Directory.CreateDirectory(storageDirectory);
            }
                
            // Create tasks for parallel file reading
            var readTasks = assetIdNameTuples.Select(async assetIdNameTuple => {
                var filePath = Path.Combine(storageDirectory, $"{assetIdNameTuple.Item1}.{assetIdNameTuple.Item2}.zst");
                var bytes = await File.ReadAllBytesAsync(filePath);
                
                string fileName = $"{assetIdNameTuple.Item1}.{assetIdNameTuple.Item2}.zst";
                
                // Convert byte array to IFormFile
                var stream = new MemoryStream(bytes);
                var formFile = new FormFile(
                    baseStream: stream,
                    baseStreamOffset: 0,
                    length: bytes.Length,
                    name: "file",
                    fileName: fileName
                );
                
                return formFile;
            }).ToList();
            
            // Wait for all tasks to complete
            var files = await Task.WhenAll(readTasks);
            
            compressedFiles.AddRange(files);
            return compressedFiles;
        }  
    }
}