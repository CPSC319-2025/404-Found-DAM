using Microsoft.AspNetCore.Http;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Dtos;

namespace Infrastructure.DataAccess
{
    public class LocalBlobStorageService : IBlobStorageService
    {
        private readonly IDbContextFactory<DAMDbContext> _contextFactory;
        public LocalBlobStorageService(IDbContextFactory<DAMDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
        
        public async Task<string> UploadAsync(IFormFile file, string containerName, UploadAssetsReq request)
        {
            using var _context = _contextFactory.CreateDbContext();
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            // Read the IFormFile into a byte array
            byte[] compressedData;
            try {
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    compressedData = ms.ToArray();
                }
                
                string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
                if (!Directory.Exists(storageDirectory))
                {
                    Directory.CreateDirectory(storageDirectory);
                }

                // Create an Asset instance with the file path
                var asset = new Asset
                {
                    FileName = request.Name,
                    MimeType = file.ContentType,
                    ProjectID = null,
                    UserID = request.UserId,
                    assetState = Asset.AssetStateType.UploadedToPalette
                };

                // Add the asset to the database context and save changes
                await _context.Assets.AddAsync(asset);
                int num = await _context.SaveChangesAsync();
                await File.WriteAllBytesAsync(storageDirectory + "/" + asset.BlobID + ".zst", compressedData);
                return asset.BlobID.ToString();
            } catch (Exception ex) {
                Console.WriteLine($"Error saving asset to database: {ex.Message}");
                return null;
            }
        }
        
        public async Task<bool> DeleteAsync(string blobId, string containerName)
        {
            using var _context = _contextFactory.CreateDbContext();

            try {
                // Create storage directory if it doesn't exist
                string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
                if (!Directory.Exists(storageDirectory))
                {
                    Directory.CreateDirectory(storageDirectory);
                }

                // Get the asset to retrieve filename before deletion
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.FileName == blobId);

                // Delete the asset from the database
                await _context.Assets.Where(a => a.FileName == blobId).ExecuteDeleteAsync();
                
                // Delete the corresponding file
                string filePath = Path.Combine(storageDirectory, asset.BlobID + ".zst");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            } 
            catch (Exception ex) 
            {
                Console.WriteLine($"Error deleting asset: {ex.Message}");
                return false;
            }

            return true;
        }
        
        public async Task<List<IFormFile>> DownloadAsync(string containerName, int userId)
        {
            using var _context = _contextFactory.CreateDbContext();

            try {
                var compressedFiles = new List<IFormFile>();
                
                // Create storage directory if it doesn't exist
                string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
                if (!Directory.Exists(storageDirectory)) {
                    Directory.CreateDirectory(storageDirectory);
                }
                
                // Get all Assets for the user
                var assetIds = await _context.Assets
                    .Where(ass => ass.UserID == userId)
                    .Select(ass => ass.BlobID)
                    .ToListAsync();
                
                // Create tasks for parallel file reading
                var readTasks = assetIds.Select(async assetId => {
                    var filePath = Path.Combine(storageDirectory, $"{assetId}.zst");
                    var bytes = await File.ReadAllBytesAsync(filePath);
                    
                    string fileName = $"{assetId}.zst";
                    
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
            } catch (Exception ex) {
                Console.WriteLine($"Error retrieving assets: {ex.Message}");
                return new List<IFormFile>();
            }
        }
        
    }
}