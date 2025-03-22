using Core.Interfaces;
using Core.Dtos;
using Microsoft.AspNetCore.Http;
using System.Reflection.Metadata;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Exceptions;
using Core.Entities;


namespace Core.Services
{
    public class PaletteService : IPaletteService
    {
        private readonly IPaletteRepository _paletteRepository;
        private readonly IImageService _imageService;
   
        public PaletteService(IPaletteRepository paletteRepository, IImageService imageService)
        {
            _paletteRepository = paletteRepository;
            _imageService = imageService;
        }

        public async Task<Asset?> ProcessUploadAsync(IFormFile file, UploadAssetsReq request, bool convertToWebp)
        {
            try {
                return await _paletteRepository.UploadAssets(file, request, convertToWebp, _imageService);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error uploading assets: {ex.Message}");
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

        public async Task<List<IFormFile>> GetAssets(GetPaletteAssetsReq request) {
            return await _paletteRepository.GetAssetsAsync(request.UserId);
        }

        public async Task<List<string>> GetProjectTagsAsync(int projectId) {
            return await _paletteRepository.GetProjectTagsAsync(projectId);
        }

        public async Task<bool> AddTagsToPaletteImagesAsync(List<string> imageIds, int projectId) {
            var projectTags = await _paletteRepository.GetProjectTagsAsync(projectId);
            if (!projectTags.Any()) return false;
            return await _paletteRepository.AddTagsToPaletteImagesAsync(imageIds, projectTags);
        }

        public async Task<SubmitAssetsRes> SubmitAssets(int projectID, List<string> blobIDs, int submitterID)        {
            try 
            {
                (List<string> successfulSubmissions, List<string> failedSubmissions) = await _paletteRepository.SubmitAssetstoDb(projectID, blobIDs, submitterID);   
                SubmitAssetsRes result = new SubmitAssetsRes      
                {
                    projectID = projectID,
                    successfulSubmissions = successfulSubmissions,
                    failedSubmissions = failedSubmissions,
                    submittedAt = DateTime.UtcNow
                };
                 return result; 
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving blob project and tags: {ex.Message}");
                throw;
            }
        }

        public async Task<AssignTagResult> AssignTagToAssetAsync(string blobId, int tagId)
        {
            try
            {
                return await _paletteRepository.AssignTagToAssetAsync(blobId, tagId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error assigning tag to asset: {ex.Message}");
                throw;
            }
        }
    }
}
