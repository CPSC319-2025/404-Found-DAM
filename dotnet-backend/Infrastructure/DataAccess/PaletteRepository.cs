using Core.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Core.Dtos;
using Core.Interfaces;
using System.Reflection.Metadata;

namespace Infrastructure.DataAccess {
    public class PaletteRepository : IPaletteRepository {

        private readonly IDbContextFactory<DAMDbContext> _contextFactory;
        private readonly IBlobStorageService _blobStorageService;
        public PaletteRepository(IDbContextFactory<DAMDbContext> contextFactory, IBlobStorageService blobStorageService) 
        {
            _contextFactory = contextFactory;
            _blobStorageService = blobStorageService;
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
        
        public async Task<string> UploadAssets(IFormFile file, UploadAssetsReq request)
        {
            return await _blobStorageService.UploadAsync(file, "palette-assets", request);
        }

        public async Task<bool> DeleteAsset(DeletePaletteAssetReq request)
        {
            // TODO change to blob ID
            return await _blobStorageService.DeleteAsync(request.Name, "palette-assets");
        }

        public async Task<List<IFormFile>> GetAssetsAsync(int userId) {
            return await _blobStorageService.DownloadAsync("palette-assets", userId);
        }
    }
}