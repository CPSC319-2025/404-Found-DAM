using Core.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Core.Dtos;
using Core.Interfaces;
using System.Reflection.Metadata;
using Infrastructure.Exceptions;

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

        public async Task<bool> AddTagsToPaletteImagesAsync(List<string> imageIds, List<string> tags) {
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
            byte[] compressedData;
            try {
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    compressedData = ms.ToArray();
                }
                using var _context = _contextFactory.CreateDbContext();
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty");
                string finalName = file.FileName;
                string suffix = ".zst";
                if (finalName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    finalName = finalName.Substring(0, finalName.Length - suffix.Length);
                }
                string finalExtension = Path.GetExtension(finalName); // example: ".png" or "mp4"
                var asset = new Asset
                    {
                        BlobID = "temp",
                        FileName = finalName,
                        MimeType = finalExtension,
                        ProjectID = null,
                        UserID = request.UserId,
                        assetState = Asset.AssetStateType.UploadedToPalette
                    };
                string blobId = await _blobStorageService.UploadAsync(compressedData, "palette-assets", asset);
                asset.BlobID = blobId;
                 // Add the asset to the database context and save changes
                await _context.Assets.AddAsync(asset);
                int num = await _context.SaveChangesAsync();
                return blobId;
            } catch (Exception ex) {
                Console.WriteLine($"Error uploading asset: {ex.Message}");
                return null;
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
                _context.Assets.Remove(asset);
                // TODO change to blob ID
                var res = await _blobStorageService.DeleteAsync(asset, "palette-assets");
                await _context.SaveChangesAsync();
                return res;
            } catch (Exception ex) {
                Console.WriteLine($"Error deleting asset: {ex.Message}");
                return false;
            }

        }

        public async Task<List<IFormFile>> GetAssetsAsync(int userId) {
            using var _context = _contextFactory.CreateDbContext();
            try {
                // Get all Assets for the user
                var assets = await _context.Assets
                    .Where(ass => ass.UserID == userId && ass.assetState == Asset.AssetStateType.UploadedToPalette)
                    .ToListAsync();
                return await _blobStorageService.DownloadAsync("palette-assets", assets);
            } catch (Exception ex) {
                Console.WriteLine($"Error getting assets: {ex.Message}");
                return null;
            }
        }

        public async Task<(List<string>, List<string>)> SubmitAssetstoDb(int projectID, List<string> blobIDs, int submitterID)        {
            List<string> successfulSubmissions = new List<string>();
 
             // check project exist & if submitter is a member
             using DAMDbContext _context = _contextFactory.CreateDbContext();
             var isProjectFound = await _context.Projects.AnyAsync(p => p.ProjectID == projectID);
             if (isProjectFound) 
             {
                 var isSubmitterMember = await _context.ProjectMemberships.AnyAsync(pm => pm.ProjectID == projectID && pm.UserID == submitterID);
                 if (isSubmitterMember) 
                 {
                     // Retrieve assets using blobIDs
                     var assetsToBeSubmitted = await _context.Assets
                         .Where(a => blobIDs.Contains(a.BlobID) && a.ProjectID == projectID)
                         .ToListAsync();
                     
                     if (assetsToBeSubmitted == null || assetsToBeSubmitted.Count == 0) 
                     {
                         // No assets to be submitted, return empty successfulSubmissions, and blobIDs = failedSubmissions
                         return (successfulSubmissions, blobIDs);
                     }
                     else 
                     {
                         // process assets, if in project & done, add to successfulSubmissions
                         foreach (Asset a in assetsToBeSubmitted) 
                         {
                             if (blobIDs.Contains(a.BlobID))
                             {
                                 a.assetState = Asset.AssetStateType.SubmittedToProject;
                                 a.LastUpdated = DateTime.UtcNow;
                                 successfulSubmissions.Add(a.BlobID);
                             } 
                         }
                         await _context.SaveChangesAsync();
                         return (successfulSubmissions, blobIDs.Except(successfulSubmissions).ToList());
                     }
                 }
                 else 
                 {
                     throw new DataNotFoundException($"User ${submitterID} not a member of project ${projectID}");
                 }
             }
             else 
             {
                 throw new DataNotFoundException($"Project ${projectID} not found");
             }           
        }
    }
}