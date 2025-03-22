using Core.Interfaces;
using Core.Dtos;
using Microsoft.AspNetCore.Http;
using System.Reflection.Metadata;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Exceptions;


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

        public async Task<string> ProcessUploadAsync(IFormFile file, UploadAssetsReq request)
        {
            Console.WriteLine("ProcessUploadAsync");
            try {
                return await _paletteRepository.UploadAssets(file, request, _imageService);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error uploading assets: {ex.Message}");
                return -1;
            }
        }

        public async Task<object[]> ProcessUploadsAsync(List<IFormFile> files, UploadAssetsReq request)
        {
            // Create a list of tasks with explicit return type
            var uploadTasks = new List<Task<object>>();
            
            foreach (var file in files)
            {
                // Use Task.Run with explicit Function<Task<object>> signature
                uploadTasks.Add(Task.Run<object>(async () => 
                {
                    var res = await ProcessUploadAsync(file, request);
                    if (!string.IsNullOrEmpty(res))
                    {
                        return new {
                            BlobID = res, 
                            Success = true, 
                            FileName = file.FileName, 
                            Size = file.Length
                        };
                    }
                    else 
                    {
                        return new {
                            BlobID = "",
                            Success = false, 
                            FileName = file.FileName, 
                            Size = file.Length
                        };
                    }
                }));
            }

            // Wait for all tasks to complete and return results
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
    }
}
