using Core.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Core.Dtos;
// using Core.Interfaces;
using System.Reflection.Metadata;
using Infrastructure.Exceptions;
using Core.Services.Utils;
using NetVips;

namespace Infrastructure.DataAccess {
    //
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
        
        public async Task<Asset> UploadAssets(IFormFile file, UploadAssetsReq request, bool convertToWebp, IImageService _imageService)
        {
            using var _context = _contextFactory.CreateDbContext();
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            byte[] compressedData;

            try 
            {
                // Process file name first in case of conversion
                string fileNameWithoutZstExtension = file.FileName;
                string suffix = ".zst";
                if (fileNameWithoutZstExtension.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    fileNameWithoutZstExtension = fileNameWithoutZstExtension.Substring(0, fileNameWithoutZstExtension.Length - suffix.Length);
                }

                // Check if Asset is an webp image and if conversion is required
                if (request.AssetMimeType.StartsWith("image") && !request.AssetMimeType.EndsWith("webp") && convertToWebp)
                {
                    try 
                    {
                        using (var ms = new MemoryStream())
                        {
                            await file.CopyToAsync(ms);
                            compressedData = ms.ToArray();

                            // Decompress for converting to lossy webp
                            // TODO: guard to only convert images, and skip those that are already in webp
                            byte[] decompressedBuffer = FileCompressionHelper.Decompress(compressedData);
                            byte[] webpLossyBuffer = _imageService.toWebpNetVips(decompressedBuffer, false);

                            // Compress the returned buffer
                            compressedData = FileCompressionHelper.Compress(webpLossyBuffer);

                            // Change fileName extension and mimetype
                            string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileNameWithoutZstExtension);
                            fileNameWithoutZstExtension = fileNameNoExtension + ".webp";
                            string[] mimeTypeParts = request.AssetMimeType.Split('/');
                            if (mimeTypeParts.Length > 0) 
                            {
                                request.AssetMimeType = mimeTypeParts[0] + "/" + "webp";
                            }
                        }
                    }
                    catch (VipsException)
                    {
                        // TODO: Consider notifying users of failed conversion
                        // Console.WriteLine($"Failed to convert image to webp; proceed with the original format");
                        using (var ms = new MemoryStream())
                        {
                            await file.CopyToAsync(ms);
                            compressedData = ms.ToArray();
                        }
                    }
                }
                else // Asset is video, or webp image, or image to which user does not require webp conversion to be applied
                {
                    using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms);
                        compressedData = ms.ToArray();
                    }
                }

                // string finalExtension = Path.GetExtension(finalName); // example: ".png" or "mp4"
                // string mimeType = request.Type.ToLower() + "/" + finalExtension;
               
                // Create an Asset instance with the file path
                var asset = new Asset
                {
                    BlobID = "temp",
                    FileName = fileNameWithoutZstExtension,
                    MimeType = request.AssetMimeType,
                    ProjectID = null,
                    UserID = request.UserId,
                    FileSizeInKB = compressedData.Length / 1024.0,
                    LastUpdated = DateTime.UtcNow,
                    assetState = Asset.AssetStateType.UploadedToPalette,
                };

                if (compressedData.Length / 1024.0 > 2000)
                {
                    throw new Exception();
                }

                string blobId = await _blobStorageService.UploadAsync(compressedData, "palette-assets", asset);
                asset.BlobID = blobId;
                    // Add the asset to the database context and save changes
                await _context.Assets.AddAsync(asset);
                int num = await _context.SaveChangesAsync();
                return asset;
            }
            catch (Exception) 
            {
                // Console.WriteLine($"Error saving asset to database: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAsset(DeletePaletteAssetReq request)
        {
            using var _context = _contextFactory.CreateDbContext();

            // Create storage directory if it doesn't exist
            string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            if (!Directory.Exists(storageDirectory))
            {
                Directory.CreateDirectory(storageDirectory);
            }
            
            // Get the asset to retrieve filename before deletion
            var asset = await _context.Assets.FirstOrDefaultAsync(a => a.BlobID == request.Name);
            
            // Check if asset exists
            if (asset == null)
            {
                return false;
            }

            // Delete the asset from the database
            await _context.Assets.Where(a => a.BlobID == request.Name).ExecuteDeleteAsync();
            
            // Delete from blob storage
            var res = await _blobStorageService.DeleteAsync(asset, "palette-assets");
            await _context.SaveChangesAsync();
            return res;
        }

        public async Task<List<IFormFile>> GetAssetsAsync(int userId) {
            using var _context = _contextFactory.CreateDbContext();
            // Get all Assets for the user
            var assets = await _context.Assets
                .Where(ass => ass.UserID == userId && ass.assetState == Asset.AssetStateType.UploadedToPalette)
                .ToListAsync();

            if (assets == null || assets.Count == 0) 
            {
                return new List<IFormFile>(); // Return an empty list
            }
            else 
            {
                            
                // Construct tuple list to be passed into DownloadAsync
                List<(string, string)> assetIdNameTupleList = assets
                    .Select(a => (a.BlobID, a.FileName))
                    .ToList();

                return await _blobStorageService.DownloadAsync("palette-assets", assetIdNameTupleList);
            }
        }

        public async Task<List<IFormFile>> GetAssets(GetPaletteAssetsReq request) {
            using var _context = _contextFactory.CreateDbContext();
            // Get all Assets for the user
            var assets = await _context.Assets
                .Where(ass => ass.UserID == request.UserId && ass.assetState == Asset.AssetStateType.UploadedToPalette)
                .ToListAsync();
            
            // Convert assets to list of tuples (BlobID, FileName)
            var assetTuples = assets.Select(a => (a.BlobID, a.FileName)).ToList();
            return await _blobStorageService.DownloadAsync("palette-assets", assetTuples);
        }

        public async Task<IFormFile?> GetAssetByBlobIdAsync(string blobId, int userId) {
            using var _context = _contextFactory.CreateDbContext();
            // Get the asset with the specified blobId that belongs to the user
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.BlobID == blobId && a.UserID == userId);
            
            if (asset == null) {
                return null;
            }
            
            // Download the single asset - convert to tuple list
            var assetTuples = new List<(string, string)> { (asset.BlobID, asset.FileName) };
            var files = await _blobStorageService.DownloadAsync("palette-assets", assetTuples);
            
            // Return the first (and only) file, or null if no files were downloaded
            return files.FirstOrDefault();
        }

        public async Task<(List<string>, List<string>)> SubmitAssetstoDb(int projectID, List<string> blobIDs, int submitterID, bool autoNaming = false)
        {
            List<string> successfulSubmissions = new List<string>();

            // check project exist & if submitter is a member
            using DAMDbContext _context = _contextFactory.CreateDbContext();
            var isProjectFound = await _context.Projects.AnyAsync(p => p.ProjectID == projectID);
            if (isProjectFound) 
            {
                var isSubmitterMember = await _context.ProjectMemberships.AnyAsync(pm => pm.ProjectID == projectID && pm.UserID == submitterID);
                if (isSubmitterMember) 
                {
                    // Get project name for auto-naming
                    string projectName = "";
                    if (autoNaming)
                    {
                        var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectID == projectID);
                        if (project != null)
                        {
                            projectName = project.Name;
                        }
                    }

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
                        // For auto-naming, prepare the file counter
                        int fileCounter = 1;
                        if (autoNaming)
                        {
                            // Get the current count of auto-named assets in the project
                            var existingAssetsCount = await _context.Assets
                                .Where(a => a.ProjectID == projectID && 
                                          a.assetState == Asset.AssetStateType.SubmittedToProject &&
                                          a.FileName.StartsWith($"{projectName}-Asset"))
                                .CountAsync();
                            fileCounter = existingAssetsCount + 1;
                        }

                        // process assets, if in project & done, add to successfulSubmissions
                        foreach (Asset a in assetsToBeSubmitted) 
                        {
                            if (blobIDs.Contains(a.BlobID))
                            {
                                // Store original filename and extension
                                string originalFileName = a.FileName;
                                string fileExtension = Path.GetExtension(originalFileName);

                                // Add Ai naming maybe?
                                // Create new filename if auto-naming is enabled
                                string newFileName = autoNaming 
                                    ? $"{projectName}-Asset{fileCounter:D3}{fileExtension}"
                                    : originalFileName;

                                // Download the file from palette-assets
                                var file = await _blobStorageService.DownloadAsync("palette-assets", new List<(string, string)> { (a.BlobID, originalFileName) }); 
                                
                                // Delete from palette-assets
                                await _blobStorageService.DeleteAsync(a, "palette-assets");

                                // Update the asset's properties
                                a.assetState = Asset.AssetStateType.SubmittedToProject;
                                a.LastUpdated = DateTime.UtcNow;
                                a.FileName = newFileName;

                                // Upload to project-specific storage with new filename
                                using (var memoryStream = new MemoryStream())
                                {
                                    await file.First().CopyToAsync(memoryStream);
                                    var fileBytes = memoryStream.ToArray();
                                    await _blobStorageService.UploadAsync(fileBytes, "project-" + projectID + "-assets", a);
                                }

                                successfulSubmissions.Add(a.BlobID);
                                
                                // Only increment counter if auto-naming is enabled
                                if (autoNaming)
                                {
                                    fileCounter++;
                                }
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

        public async Task<bool> RemoveAssetTagsFromDb(string blobId, int tagId)
        {
            using var context = _contextFactory.CreateDbContext();
            var assetTag = await context.AssetTags.FirstOrDefaultAsync(at => at.BlobID == blobId && at.TagID == tagId);
            if (assetTag == null)
            {
                return false;
            }
            context.AssetTags.Remove(assetTag);
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<bool> AssetTagAssociationExistsAsync(string blobId, int tagId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.AssetTags.AnyAsync(at => at.BlobID == blobId && at.TagID == tagId);
        }

        public async Task<GetBlobProjectAndTagsRes> GetBlobProjectAndTagsAsync(string blobId)
        {
            using var _context = _contextFactory.CreateDbContext();
            
            var asset = await _context.Assets
                .Where(a => a.BlobID == blobId)
                .Include(a => a.Project)
                .Include(a => a.AssetTags)
                .ThenInclude(at => at.Tag)
                .FirstOrDefaultAsync();
                
            if (asset == null)
            {
                throw new DataNotFoundException($"Asset with BlobID {blobId} not found");
            }
            
            // Extract tags and tagIds together to ensure they're in the same order
            var tagData = asset.AssetTags?.Select(at => new { Name = at.Tag.Name, Id = at.Tag.TagID }).ToList();
            
            var response = new GetBlobProjectAndTagsRes
            {
                BlobId = asset.BlobID,
                FileName = asset.FileName,
                Project = asset.Project != null ? new ProjectInfo
                {
                    ProjectId = asset.Project.ProjectID,
                    Name = asset.Project.Name,
                    Description = asset.Project.Description,
                    Location = asset.Project.Location
                } : null,
                Tags = tagData?.Select(t => t.Name).ToList() ?? new List<string>(),
                TagIds = tagData?.Select(t => t.Id).ToList() ?? new List<int>()
            };
            
            return response;
        }

        public async Task<AssignTagResult> AssignTagToAssetAsync(string blobId, int tagId)
        {
            using var context = _contextFactory.CreateDbContext();
            
            // Check if asset exists
            var asset = await context.Assets.FirstOrDefaultAsync(a => a.BlobID == blobId);
            if (asset == null)
            {
                return new AssignTagResult
                {
                    Success = false,
                    BlobId = blobId,
                    TagId = tagId,
                    Message = $"Asset with BlobID {blobId} not found"
                };
            }
            
            // Check if tag exists
            var tag = await context.Tags.FirstOrDefaultAsync(t => t.TagID == tagId);
            if (tag == null)
            {
                return new AssignTagResult
                {
                    Success = false,
                    BlobId = blobId,
                    TagId = tagId,
                    Message = $"Tag with ID {tagId} not found"
                };
            }
            
            // Check if association already exists
            bool associationExists = await AssetTagAssociationExistsAsync(blobId, tagId);
            if (associationExists)
            {
                return new AssignTagResult
                {
                    Success = true,
                    BlobId = blobId,
                    TagId = tagId,
                    Message = "Tag already assigned to asset"
                };
            }
            
            // Create new association
            var assetTag = new AssetTag
            {
                BlobID = blobId,
                Asset = asset,
                TagID = tagId,
                Tag = tag
            };
            
            await context.AssetTags.AddAsync(assetTag);
            await context.SaveChangesAsync();
            
            return new AssignTagResult
            {
                Success = true,
                BlobId = blobId,
                TagId = tagId,
                Message = "Tag successfully assigned to asset"
            };
        }

        public async Task<List<int>> GetProjectTagIdsAsync(int projectId)
        {
            using var context = _contextFactory.CreateDbContext();
            
            var tagIds = await context.ProjectTags
                .Where(pt => pt.ProjectID == projectId)
                .Select(pt => pt.TagID)
                .ToListAsync();

            return tagIds;
        }

        public async Task<AssignProjectTagsResult> AssignProjectTagsToAssetAsync(string blobId, List<int> tagIds)
        {
            using var context = _contextFactory.CreateDbContext();
            
            // Check if asset exists
            var asset = await context.Assets.FirstOrDefaultAsync(a => a.BlobID == blobId);
            if (asset == null)
            {
                return new AssignProjectTagsResult
                {
                    Success = false,
                    BlobId = blobId,
                    Message = $"Asset with BlobID {blobId} not found"
                };
            }

            var result = new AssignProjectTagsResult
            {
                BlobId = blobId,
                Success = true,
                Message = "Successfully assigned project tags to asset"
            };

            foreach (var tagId in tagIds)
            {
                // Check if tag exists
                var tag = await context.Tags.FirstOrDefaultAsync(t => t.TagID == tagId);
                if (tag == null)
                {
                    continue;
                }
                
                // Check if association already exists
                bool associationExists = await AssetTagAssociationExistsAsync(blobId, tagId);
                if (!associationExists)
                {
                    // Create new association
                    var assetTag = new AssetTag
                    {
                        BlobID = blobId,
                        Asset = asset,
                        TagID = tagId,
                        Tag = tag
                    };
                    
                    await context.AssetTags.AddAsync(assetTag);
                    result.AssignedTagIds.Add(tagId);
                }
            }

            if (result.AssignedTagIds.Any())
            {
                await context.SaveChangesAsync();
            }

            return result;
        }

        public async Task<Asset> UploadMergedChunkToDb(string filePath, string filename, string mimeType, int userId, bool convertToWebp = true, IImageService? imageService = null)  {
            using var _context = _contextFactory.CreateDbContext();
            try 
            {
                // Read file from disk
                byte[] fileData = await File.ReadAllBytesAsync(filePath);
                
                // Check if file is an image and convert to WebP if needed
                if (convertToWebp && mimeType.StartsWith("image/") && !mimeType.EndsWith("webp") && imageService != null)
                {
                    try 
                    {
                        // Convert to WebP using the image service
                        byte[] webpBuffer = imageService.toWebpNetVips(fileData, false);
                        fileData = webpBuffer;
                        
                        // Update file extension and MIME type
                        mimeType = "image/webp";
                        string fileNameNoExtension = Path.GetFileNameWithoutExtension(filename);
                        filename = fileNameNoExtension + ".webp";
                    }
                    catch (VipsException)
                    {
                        Console.WriteLine("Failed to convert to WebP");
                    }
                }
                
                // Compress the data
                byte[] compressedData = FileCompressionHelper.Compress(fileData);
                
                // Create an Asset instance with the file path
                var asset = new Asset
                {
                    BlobID = "temp",
                    FileName = filename,
                    MimeType = mimeType,
                    ProjectID = null,
                    UserID = userId,
                    FileSizeInKB = compressedData.Length / 1024.0,
                    LastUpdated = DateTime.UtcNow,
                    assetState = Asset.AssetStateType.UploadedToPalette,
                };
                
                // Upload to blob storage (same location as UploadAssets)
                string blobId = await _blobStorageService.UploadAsync(compressedData, "palette-assets", asset);
                asset.BlobID = blobId;
                
                // Add the asset to the database and save changes
                await _context.Assets.AddAsync(asset);
                await _context.SaveChangesAsync();
                
                return asset;
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Error uploading merged chunk: {ex.Message}");
                throw;
            }
        }

        public async Task<Asset> UpdateAssetAsync(IFormFile file, UpdateAssetReq request, bool convertToWebp, IImageService imageService)
        {
            using var _context = _contextFactory.CreateDbContext();
            
            try
            {
                // Check if asset exists
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.BlobID == request.BlobId);
                if (asset == null)
                {
                    throw new DataNotFoundException($"Asset with blob ID {request.BlobId} not found");
                }

                // Check if user has permission to update the asset
                if (asset.UserID != request.UserId)
                {
                    throw new UnauthorizedAccessException("User does not have permission to update this asset");
                }

                // Process the file
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }

                // Compress the file for storage
                byte[] compressedBytes = FileCompressionHelper.Compress(fileBytes);
                
                // Update the blob storage with the new compressed file while preserving the blob ID
                await _blobStorageService.UpdateAsync(compressedBytes, "palette-assets", asset);

                // Update the remaining asset properties in the database
                asset.FileSizeInKB = fileBytes.Length / 1024.0;
                asset.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                return asset;
            }
            catch (Exception)
            {
                // Console.WriteLine($"Error updating asset with blob ID {request.BlobId}: {ex.Message}");
                throw;
            }
        }

        public async Task<GetBlobFieldsRes> GetBlobFieldsAsync(string blobId)
        {
            using var _context = _contextFactory.CreateDbContext();

            try
            {
                // Check if the asset exists
                var asset = await _context.Assets
                    .Where(a => a.BlobID == blobId)
                    .FirstOrDefaultAsync();

                if (asset == null)
                {
                    throw new DataNotFoundException($"Asset with BlobID {blobId} not found");
                }

                // Get all metadata fields for the asset
                var assetMetadata = await _context.AssetMetadata
                    .Where(am => am.BlobID == blobId)
                    .Include(am => am.ProjectMetadataField)
                    .ToListAsync();

                // Create the response
                var response = new GetBlobFieldsRes
                {
                    BlobId = blobId,
                    Fields = assetMetadata.Select(am => new BlobFieldDto
                    {
                        FieldId = am.FieldID,
                        FieldValue = am.FieldValue,
                        FieldName = am.ProjectMetadataField.FieldName,
                        FieldType = am.ProjectMetadataField.FieldType.ToString()
                    }).ToList()
                };

                return response;
            }
            catch (DataNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving blob fields: {ex.Message}");
                throw;
            }
        }

        public async Task<string?> GetAssetNameByBlobIdAsync(string blobID)
        {
            using var _context = _contextFactory.CreateDbContext(); // first create context
            return await _context.Assets
                .Where(a => a.BlobID == blobID)
                .Select(a => a.FileName)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetTagNameByIdAsync(int tagId)
        {
            using var context = _contextFactory.CreateDbContext();
            
            return await context.Tags
                .Where(t => t.TagID == tagId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetProjectNameByIdAsync(int projectId)
        {
            using var context = _contextFactory.CreateDbContext();
            
            return await context.Projects
                .Where(p => p.ProjectID == projectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();
        }


    } 
}