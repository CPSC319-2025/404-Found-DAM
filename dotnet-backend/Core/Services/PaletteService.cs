using Core.Interfaces;
using Core.Dtos;
using Microsoft.AspNetCore.Http;
using System.Reflection.Metadata;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Exceptions;
using Core.Entities;
using System.IO;
using ZstdSharp;

namespace Core.Services
{
    public class PaletteService : IPaletteService
    {
        private readonly IPaletteRepository _paletteRepository;
        private readonly IImageService _imageService;
        private readonly IActivityLogService _activityLogService;
        private readonly IUserService _userService;
        private readonly IProjectService _projectService;

        private const bool verboseLogs = false;
        private const bool logDebug = false;
        private const bool AdminActionTrue = true;

        // Create imageService here in case later we need to move business logic from paletteRepository's UploadAssets to here.  
        public PaletteService(
            IPaletteRepository paletteRepository,
            IImageService imageService,
            IActivityLogService activityLogService,
            IUserService userService,
            IProjectService projectService)
        {
            _paletteRepository = paletteRepository;
            _imageService = imageService;
            _activityLogService = activityLogService;
            _userService = userService;
            _projectService = projectService;
        }

        public async Task<Asset?> ProcessUploadAsync(IFormFile file, UploadAssetsReq request, bool convertToWebp)
        {
            try {
                return await _paletteRepository.UploadAssets(file, request, convertToWebp, _imageService);
            }
            catch (Exception) {
                // Console.WriteLine($"Error uploading assets: {ex.Message}");
                return null;
            }
        }

        public async Task<ProcessedAsset[]> ProcessUploadsAsync(List<IFormFile> files, UploadAssetsReq request, bool convertToWebp)
        {
            // Create a list of tasks with explicit return type
            var uploadTasks = new List<Task<ProcessedAsset>>();
            
            foreach (var file in files)
            {
                // Use Task.Run with explicit Function<Task<object>> signature
                uploadTasks.Add(Task.Run<ProcessedAsset>(async () => 
                {
                    var asset = await ProcessUploadAsync(file, request, convertToWebp);
                    if (asset != null) 
                    {
                        ProcessedAsset res = new ProcessedAsset
                        {
                            BlobID = asset.BlobID, 
                            FileName = asset.FileName, 
                            SizeInKB = asset.FileSizeInKB,
                            Success = true
                        };
                        return res;
                    }
                    else 
                    {
                        ProcessedAsset res = new ProcessedAsset
                        {
                            FileName = file.FileName, 
                            SizeInKB = file.Length / 1024.0,
                            Success = false
                        };
                        return res;
                    }
                }));
            }

            // Wait for all tasks to complete and return results of successful and failed cases
            return await Task.WhenAll(uploadTasks);
        }

        public async Task<bool> DeleteAssetAsync(DeletePaletteAssetReq request)
        {
            return await _paletteRepository.DeleteAsset(request);
        }

        public async Task<GetAssetsRes> GetAssets(GetPaletteAssetsReq request) {
            return await _paletteRepository.GetAssets(request);
        }

        public async Task<byte[]> DecompressZstdAsync(byte[] compressedData)
        {
            return await Task.Run(() => 
            {
                try 
                {
                    // Using ZstdSharp for decompression - simplest approach
                    using var decompressor = new ZstdSharp.Decompressor();
                    return decompressor.Unwrap(compressedData).ToArray();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error decompressing data: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task<List<string>> GetProjectTagsAsync(int projectId) {
            return await _paletteRepository.GetProjectTagsAsync(projectId);
        }

        public async Task<SubmitAssetsRes> SubmitAssets(int projectID, List<string> blobIDs, int submitterID, bool autoNaming = false)
        {
            try
            {
                var (successfulSubmissions, failedSubmissions) = await _paletteRepository.SubmitAssetstoDb(projectID, blobIDs, submitterID, autoNaming);

                // Log successful submissions
                foreach (var blobID in successfulSubmissions)
                {
                    try
                    {
                        var user = await _userService.GetUser(submitterID);
                        var assetName = await _projectService.GetAssetNameByBlobIdAsync(blobID);
                        var projectName = await _projectService.GetProjectNameByIdAsync(projectID);

                        string description = $"{user.Email} added {assetName} into project {projectName}";
                        if (verboseLogs)
                        {
                            description = $"{user.Name} (User ID: {submitterID}) added asset {assetName} (Asset ID: {blobID}) into project {projectName} (Project ID: {projectID}).";
                        }

                        if (logDebug)
                        {
                            description += "[Add Log called by PaletteService.SubmitAssets]";
                            Console.WriteLine(description);
                        }

                        await _activityLogService.AddLogAsync(new CreateActivityLogDto
                        {
                            userID = submitterID,
                            changeType = "Added",
                            description = description,
                            projID = projectID,
                            assetID = blobID,
                            isAdminAction = !AdminActionTrue
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to add log for asset {blobID}: {ex.Message}");
                    }
                }

                return new SubmitAssetsRes
                {
                    projectID = projectID,
                    successfulSubmissions = successfulSubmissions,
                    failedSubmissions = failedSubmissions,
                    submittedAt = DateTime.UtcNow
                };
            }
            catch (DataNotFoundException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SubmitAssets: {ex.Message}");
                throw;
            }
        }

        public async Task<RemoveTagsResult> RemoveTagsFromAssetsAsync(List<string> blobIds, List<int> tagIds)
        {
            var result = new RemoveTagsResult();
            
            foreach (string blobId in blobIds)
            {
                foreach (int tagId in tagIds)
                {
                    bool exists = await _paletteRepository.AssetTagAssociationExistsAsync(blobId, tagId);
                    if (exists)
                    {
                        bool removed = await _paletteRepository.RemoveAssetTagsFromDb(blobId, tagId);
                        if (removed)
                        {
                            result.RemovedAssociations.Add(new AssetTagAssociationDto
                            {
                                BlobId = blobId,
                                TagId = tagId
                            });
                        }
                        else
                        {
                            result.NotFoundAssociations.Add(new AssetTagAssociationDto
                            {
                                BlobId = blobId,
                                TagId = tagId
                            });
                        }
                    }
                    else
                    {
                        result.NotFoundAssociations.Add(new AssetTagAssociationDto
                        {
                            BlobId = blobId,
                            TagId = tagId
                        });
                    }
                }
            }
            return result;
        }

        public async Task<GetBlobProjectAndTagsRes> GetBlobProjectAndTagsAsync(string blobId)
        {
            try
            {
                return await _paletteRepository.GetBlobProjectAndTagsAsync(blobId);
            }
            catch (DataNotFoundException)
            {
                throw;
            }
            catch (Exception)
            {
                // Console.WriteLine($"Error retrieving blob project and tags: {ex.Message}");
                throw;
            }
        }

        public async Task<AssignTagResult> AssignTagToAssetAsync(string blobId, int tagId)
        {
            try
            {
                return await _paletteRepository.AssignTagToAssetAsync(blobId, tagId);
            }
            catch (Exception)
            {
                // Console.WriteLine($"Error assigning tag to asset: {ex.Message}");
                throw;
            }
        }
        public async Task<string?> GetAssetNameByBlobIdAsync(string blobID)
        {
            return await _paletteRepository.GetAssetNameByBlobIdAsync(blobID);
        }

        public async Task<string?> GetProjectNameByIdAsync(int projectID) {
            return await _paletteRepository.GetProjectNameByIdAsync(projectID);
        }

        public async Task<string?> GetTagNameByIdAsync(int tagID) {
            return await _paletteRepository.GetTagNameByIdAsync(tagID);
        }

        public async Task<AssignProjectTagsResult> AssignProjectTagsToAssetAsync(AssignProjectTagsToAssetReq request)
        {
            try
            {
                // Get all tag IDs for the project
                var tagIds = await _paletteRepository.GetProjectTagIdsAsync(request.ProjectId);
                
                if (tagIds == null || !tagIds.Any())
                {
                    return new AssignProjectTagsResult
                    {
                        Success = false,
                        BlobId = request.BlobId,
                        Message = $"No tags found for project {request.ProjectId}"
                    };
                }

                // Assign all project tags to the asset
                var result = await _paletteRepository.AssignProjectTagsToAssetAsync(request.BlobId, tagIds);
                return result;
            }
            catch (Exception)
            {
                // Console.WriteLine($"Error assigning project tags to asset: {ex.Message}");
                throw;
            }
        }
        
        public async Task<ProcessedAsset> UpdateAssetAsync(IFormFile file, UpdateAssetReq request, bool convertToWebp)
        {
            try
            {
                // For now, call ProcessUploadAsync with the existing blob ID
                // We'll need to update IPaletteRepository later to add UpdateAssetAsync
                var asset = await _paletteRepository.UpdateAssetAsync(file, request, convertToWebp, _imageService);
                
                if (asset != null)
                {
                    return new ProcessedAsset
                    {
                        BlobID = asset.BlobID,
                        FileName = asset.FileName,
                        SizeInKB = asset.FileSizeInKB,
                        Success = true
                    };
                }
                else
                {
                    return new ProcessedAsset
                    {
                        FileName = file.FileName,
                        SizeInKB = file.Length / 1024.0,
                        Success = false,
                        ErrorMessage = "Failed to update asset"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating asset: {ex.Message}");
                return new ProcessedAsset
                {
                    FileName = file.FileName,
                    SizeInKB = file.Length / 1024.0,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<GetBlobFieldsRes> GetBlobFieldsAsync(string blobId)
        {
            try
            {
                return await _paletteRepository.GetBlobFieldsAsync(blobId);
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
    }
}
