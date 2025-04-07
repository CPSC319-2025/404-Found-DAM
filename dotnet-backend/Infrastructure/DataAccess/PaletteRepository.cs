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
                string fileName = file.FileName;
                
                // Check if Asset is an webp image and if conversion is required
                if (request.AssetMimeType.StartsWith("image") && !request.AssetMimeType.EndsWith("webp") && convertToWebp)
                {
                    try 
                    {
                        using (var ms = new MemoryStream())
                        {
                            await file.CopyToAsync(ms);
                            compressedData = ms.ToArray();

                            // Convert to webp directly without compression/decompression
                            byte[] webpLossyBuffer = _imageService.toWebpNetVips(compressedData, false);
                            compressedData = webpLossyBuffer;

                            // Change fileName extension and mimetype
                            string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);
                            fileName = fileNameNoExtension + ".webp";
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

                // Create an Asset instance with the file path
                var asset = new Asset
                {
                    BlobID = "temp",
                    FileName = fileName,
                    MimeType = request.AssetMimeType,
                    ProjectID = null,
                    UserID = request.UserId,
                    FileSizeInKB = compressedData.Length / 1024.0,
                    LastUpdated = DateTime.UtcNow,
                    assetState = Asset.AssetStateType.UploadedToPalette,
                };

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

        public async Task<GetAssetsRes> GetAssets(GetPaletteAssetsReq request) {
            using var _context = _contextFactory.CreateDbContext();
            // Get all Assets for the user
            var assets = await _context.Assets
                .Where(ass => ass.UserID == request.UserId && ass.assetState == Asset.AssetStateType.UploadedToPalette)
                .ToListAsync();
            
            var res = new GetAssetsRes();
            // Convert assets to list of tuples (BlobID, FileName)
            // TODO test this
            var assetTuples = assets.Select(a => {
                res.FileNames.Add(a.FileName);
                return (a.BlobID, a.FileName);
                }).ToList();
            var blobUris = await _blobStorageService.DownloadAsync("palette-assets", assetTuples);
            res.BlobUris = blobUris;
            return res;
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
                                
                                // Update the asset's properties
                                a.assetState = Asset.AssetStateType.SubmittedToProject;
                                a.LastUpdated = DateTime.UtcNow;
                                a.FileName = newFileName;

                                // use move to move between containers
                                await _blobStorageService.MoveAsync("palette-assets", a.BlobID, "project-" + projectID + "-assets");

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
            try 
            {
                byte[] fileData = await File.ReadAllBytesAsync(filePath);
                bool conversionAttempted = false;
                
                // Check if file is an image and if conversion is required
                if (mimeType.StartsWith("image/") && !mimeType.EndsWith("webp") && convertToWebp && imageService != null)
                {
                    // Only attempt conversion for images smaller than 10MB to avoid long processing times
                    if (fileData.Length < 10 * 1024 * 1024)
                    {
                        conversionAttempted = true;
                        try 
                        {
                            // Process image conversion in a separate task with timeout
                            var conversionTask = Task.Run(() => imageService.toWebpNetVips(fileData, false));
                            
                            // Set a timeout for conversion (5 seconds)
                            if (await Task.WhenAny(conversionTask, Task.Delay(5000)) == conversionTask)
                            {
                                // Task completed within timeout
                                byte[] webpLossyBuffer = conversionTask.Result;
                                
                                // Only use conversion if it resulted in smaller file
                                if (webpLossyBuffer.Length < fileData.Length)
                                {
                                    fileData = webpLossyBuffer;
                                    
                                    // Update filename and mimetype
                                    string fileNameNoExtension = Path.GetFileNameWithoutExtension(filename);
                                    filename = fileNameNoExtension + ".webp";
                                    string[] mimeTypeParts = mimeType.Split('/');
                                    if (mimeTypeParts.Length > 0) 
                                    {
                                        mimeType = mimeTypeParts[0] + "/" + "webp";
                                    }
                                }
                            }
                            else
                            {
                                // Conversion took too long, skip it
                                Console.WriteLine($"Image conversion timeout for {filename}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Failed to convert, continue with original format
                            Console.WriteLine($"Failed to convert image: {ex.Message}");
                        }
                    }
                }
                
                // Create an Asset instance
                var asset = new Asset
                {
                    BlobID = "temp", // Will be updated after upload
                    FileName = filename,
                    MimeType = mimeType,
                    ProjectID = null,
                    UserID = userId,
                    FileSizeInKB = fileData.Length / 1024.0,
                    LastUpdated = DateTime.UtcNow,
                    assetState = Asset.AssetStateType.UploadedToPalette,
                };

                // Upload the file to blob storage
                Console.WriteLine($"Uploading file to blob storage: {filename}");
                string blobId = await _blobStorageService.UploadAsync(fileData, "palette-assets", asset);
                asset.BlobID = blobId;
                
                // Add to database
                using var context = _contextFactory.CreateDbContext();
                await context.Assets.AddAsync(asset);
                await context.SaveChangesAsync();
                
                return asset;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UploadMergedChunkToDb: {ex.Message}");
                throw;
            }
        }

        public async Task<Asset> UpdateAssetAsync(IFormFile file, UpdateAssetReq request, bool convertToWebp, IImageService imageService)
        {
            try
            {
                using var _context = _contextFactory.CreateDbContext();
                
                // Get existing asset
                var existingAsset = await _context.Assets.FirstOrDefaultAsync(a => a.BlobID == request.BlobId);
                if (existingAsset == null)
                {
                    throw new DataNotFoundException($"Asset with ID {request.BlobId} not found");
                }
                
                byte[] fileBytes;
                string fileName = file.FileName;
                
                // Process the file
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }
                
                // Check if conversion to webp is needed
                if (request.AssetMimeType.StartsWith("image") && !request.AssetMimeType.EndsWith("webp") && convertToWebp)
                {
                    try
                    {
                        // Convert to webp directly
                        byte[] webpLossyBuffer = imageService.toWebpNetVips(fileBytes, false);
                        fileBytes = webpLossyBuffer;
                        
                        // Update filename and mimetype
                        string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);
                        fileName = fileNameNoExtension + ".webp";
                        string[] mimeTypeParts = request.AssetMimeType.Split('/');
                        if (mimeTypeParts.Length > 0)
                        {
                            request.AssetMimeType = mimeTypeParts[0] + "/webp";
                        }
                    }
                    catch (Exception)
                    {
                        // Failed to convert, use original format
                    }
                }
                
                // Update asset properties
                existingAsset.FileName = fileName;
                existingAsset.MimeType = request.AssetMimeType;
                existingAsset.FileSizeInKB = fileBytes.Length / 1024.0;
                existingAsset.LastUpdated = DateTime.UtcNow;
                
                // Update the file in blob storage
                await _blobStorageService.UpdateAsync(fileBytes, "palette-assets", existingAsset);
                
                // Save changes to database
                await _context.SaveChangesAsync();
                
                return existingAsset;
            }
            catch (DataNotFoundException)
            {
                throw;
            }
            catch (Exception)
            {
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