using Core.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Core.Dtos;
using ZstdSharp;
using DocumentFormat.OpenXml.InkML;
using System.Reflection.Metadata;

namespace Infrastructure.DataAccess {
    public class PaletteRepository : IPaletteRepository {

        private readonly IDbContextFactory<DAMDbContext> _contextFactory;
        private readonly bool _useZstd;
        private readonly IProjectService _projectService;

        public PaletteRepository(IDbContextFactory<DAMDbContext> contextFactory, IProjectService projectService) 
        {
            _contextFactory = contextFactory;
            _projectService = projectService;
        }
        
        private byte[] DecompressData(byte[] compressedData)
        {
            try
            {
                Decompressor _decompressor = new Decompressor();
                return _decompressor.Unwrap(compressedData).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decompression error: {ex.Message}");
                throw new Exception($"Failed to decompress data: {ex.Message}", ex);
            }
        }
        
        public Task<List<Asset>> GetAssetsFromPalette() {
            using var _context = _contextFactory.CreateDbContext();
            return _context.Assets.ToListAsync();
        }
        

        public async Task<List<string>> GetProjectTagsAsync(int projectId) {
            using var _context = _contextFactory.CreateDbContext();
            
            // TODO: update DataModel.cs to include tags in projects
            var projectData = await _projectService.GetProject(projectId);

            if (projectData == null || projectData.tags == null || !projectData.tags.Any()) {
                return new List<string>();
            }

            return projectData.tags;
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
        
        public async Task<bool> UploadAssets(IFormFile file, UploadAssetsReq request)
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

                // Decompress the data using ZstdSharp
                byte[] decompressedData = DecompressData(compressedData);
                
                // Create storage directory if it doesn't exist
                string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
                if (!Directory.Exists(storageDirectory))
                {
                    Directory.CreateDirectory(storageDirectory);
                }
                // Create an Asset instance with the file path
                var asset = new Asset
                {
                    FileName = file.FileName,
                    MimeType = file.ContentType,
                    ProjectID = null,
                    UserID = request.UserId,
                };

            // Add the asset to the database context and save changes
            
                await _context.Assets.AddAsync(asset);
                int num = await _context.SaveChangesAsync();
                await File.WriteAllBytesAsync(asset.BlobID + ".zst", decompressedData);
            } catch (Exception ex) {
                Console.WriteLine($"Error saving asset to database: {ex.Message}");
                return false;
            }

            return true;
        }
    }
}