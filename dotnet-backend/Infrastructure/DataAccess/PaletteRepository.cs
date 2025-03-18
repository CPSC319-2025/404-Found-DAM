using Core.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Core.Dtos;
using System.Reflection.Metadata;

namespace Infrastructure.DataAccess {
    public class PaletteRepository : IPaletteRepository {

        private readonly IDbContextFactory<DAMDbContext> _contextFactory;
        public PaletteRepository(IDbContextFactory<DAMDbContext> contextFactory) 
        {
            _contextFactory = contextFactory;
        }
        
        public Task<List<Asset>> GetAssetsFromPalette() {
            using var _context = _contextFactory.CreateDbContext();
            return _context.Assets.ToListAsync();
        }
        

        public async Task<List<string>> GetProjectTagsAsync(int projectId) {
            using var _context = _contextFactory.CreateDbContext();
            
            // TODO: update DataModel.cs to include tags in projects
            var projectTags = await _context.ProjectTags
            .Where(pt => pt.ProjectID == projectId)
            .Include(pt => pt.Tag)
            .Select(pt => pt.Tag.Name)
            .ToListAsync();

            return projectTags ?? new List<string>();
        }

        public async Task<bool> AddTagsToPaletteImagesAsync(List<int> imageIds, List<string> tags) {
            using var _context = _contextFactory.CreateDbContext();

            var assets = await _context.Assets
            .Where(a => imageIds
            .Contains(a.BlobID))
            .Include(a => a.AssetTags)
            .ThenInclude(at => at.Tag)
            .ToListAsync();

            if (!assets.Any()) {
                return false; 
            }

            foreach (var asset in assets) {

                var existingTags = asset.AssetTags.Select(at => at.Tag.Name).ToHashSet();

                foreach (var tagName in tags) {
                    if (!existingTags.Contains(tagName)) {
                        var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                        if (tag == null) {
                            tag = new Tag { Name = tagName };
                            await _context.Tags.AddAsync(tag);
                            await _context.SaveChangesAsync();
                        } 
                        asset.AssetTags.Add(new AssetTag {
                            BlobID = asset.BlobID,
                            Asset = asset,
                            TagID = tag.TagID,
                            Tag = tag
                        });
                    }
                }
            }
            return await _context.SaveChangesAsync() > 0;
        }
        
        public async Task<int> UploadAssets(IFormFile file, UploadAssetsReq request)
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
                
                // Create storage directory if it doesn't exist
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
                };

            // Add the asset to the database context and save changes
            
                await _context.Assets.AddAsync(asset);
                int num = await _context.SaveChangesAsync();
                // TODOO USE BLOB FOR PROD
                await File.WriteAllBytesAsync(storageDirectory + "/" + asset.BlobID + ".zst", compressedData);
                return asset.BlobID;
            } catch (Exception ex) {
                Console.WriteLine($"Error saving asset to database: {ex.Message}");
                return -1;
            }
        }

        public async Task<bool> DeleteAsset(DeletePaletteAssetReq request)
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
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.FileName == request.Name);

                // Delete the asset from the database
                await _context.Assets.Where(a => a.FileName == request.Name).ExecuteDeleteAsync();
                
                // Delete the corresponding file
                string filePath = Path.Combine(storageDirectory, asset.BlobID + ".zst");
                if (File.Exists(filePath))
                {
                    // TODOO USE BLOB FOR PROD
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

        public async Task<List<IFormFile>> GetAssetsAsync(int userId) {
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
                    // TODOO USE BLOB FOR PROD
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