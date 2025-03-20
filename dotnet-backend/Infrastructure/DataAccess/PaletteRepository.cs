using Core.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Core.Dtos;
using System.Reflection.Metadata;
using Infrastructure.Exceptions;
using Core.Services.Utils;

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
        
        public async Task<int> UploadAssets(IFormFile file, UploadAssetsReq request, IImageService _imageService)
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

                    // Decompress for converting to lossy webp
                    // TODO: guard to only convert images.
                    byte[] decompressedBuffer = FileCompressionHelper.Decompress(compressedData);
                    byte[] webpBuffer = _imageService.toWebpNetVips(decompressedBuffer);

                    // Compress the returned buffer
                    compressedData = FileCompressionHelper.Compress(webpBuffer);
                }

                // using (var ms = new MemoryStream())
                // {
                //     Console.WriteLine("toWebP");
                //     await file.CopyToAsync(ms);
                //     var webpBuffer = _imageService.toWebpNetVips(ms);
                //     // compressedData = ms.ToArray();
                //     compressedData = webpBuffer;
                // }
                
                // Create storage directory if it doesn't exist
                string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
                if (!Directory.Exists(storageDirectory))
                {
                    Directory.CreateDirectory(storageDirectory);
                }

                // TODO: Change the file extension to webp

                string finalName = file.FileName;
                string suffix = ".zst";
                if (finalName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    finalName = finalName.Substring(0, finalName.Length - suffix.Length);
                }
                string finalExtension = Path.GetExtension(finalName); // example: ".png" or "mp4"

                // Create an Asset instance with the file path
                var asset = new Asset
                {
                    FileName = finalName,
                    MimeType = finalExtension,
                    ProjectID = null,
                    UserID = request.UserId,
                    assetState = Asset.AssetStateType.UploadedToPalette
                };

            // Add the asset to the database context and save changes
            
                await _context.Assets.AddAsync(asset);
                int num = await _context.SaveChangesAsync();
                // TODOO USE BLOB FOR PROD
                // Console.WriteLine($"FileType before compression: {finalExtension}");
                await File.WriteAllBytesAsync(Path.Combine(storageDirectory, $"{asset.BlobID}.{asset.FileName}.zst"), compressedData);
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
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.BlobID == int.Parse(request.Name));

                // Delete the asset from the database
                await _context.Assets.Where(a => a.BlobID == int.Parse(request.Name)).ExecuteDeleteAsync();
                
                // Delete the corresponding file
                string filePath = Path.Combine(storageDirectory, $"{asset.BlobID}.{asset.FileName}.zst");
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
                    .Where(ass => ass.UserID == userId && ass.assetState == Asset.AssetStateType.UploadedToPalette)
                    .ToListAsync();

                // Create tasks for parallel file reading
                var readTasks = assetIds.Select(async asset => {
                    var filePath = Path.Combine(storageDirectory, $"{asset.BlobID}.{asset.FileName}.zst");
                    // Console.WriteLine(filePath);
                    // TODOO USE BLOB FOR PROD
                    var bytes = await File.ReadAllBytesAsync(filePath);

                    string fileName = $"{asset.BlobID}.{asset.FileName}.zst";
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

        public async Task<(List<int>, List<int>)> SubmitAssetstoDb(int projectID, List<int> blobIDs, int submitterID)        {
            List<int> successfulSubmissions = new List<int>();
 
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