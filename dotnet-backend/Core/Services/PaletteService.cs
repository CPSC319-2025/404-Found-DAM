using Core.Interfaces;
using Core.Dtos;
using Microsoft.AspNetCore.Http;
using System.Reflection.Metadata;
using Infrastructure.Exceptions;


namespace Core.Services
{
    public class PaletteService : IPaletteService
    {
        private readonly IPaletteRepository _paletteRepository;
    
        public PaletteService(IPaletteRepository paletteRepository)
        {
            _paletteRepository = paletteRepository;
        }

        public async Task<int> ProcessUploadAsync(IFormFile file, UploadAssetsReq request)
        {
            try {
                return await _paletteRepository.UploadAssets(file, request);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error uploading assets: {ex.Message}");
                return -1;
            }
        }

        public async Task<object[]> ProcessUploadsAsync(List<IFormFile> files, UploadAssetsReq request)
        {
            var uploadTasks = files.Select(async file => 
            {
                var res = await ProcessUploadAsync(file, request);
                if (res != -1) {
                    return new {
                        BlobID = res, 
                        Success = true, 
                        FileName = file.FileName, 
                        Size = file.Length
                    };
                }
                else {
                    return new { 
                        BlobID = -1,
                        Success = false, 
                        FileName = file.FileName, 
                        Size = file.Length
                    };
                }
            }).ToList();

            return await Task.WhenAll(uploadTasks);
        }

        public async Task<bool> DeleteAssetAsync(DeletePaletteAssetReq request)
        {
            try {
                return await _paletteRepository.DeleteAsset(request);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error deleting assets: {ex.Message}");
                return false;
            }
        }

        public async Task<List<IFormFile>> GetAssets(GetPaletteAssetsReq request) {
            return await _paletteRepository.GetAssetsAsync(request.UserId);
        }

        public async Task<List<string>> GetProjectTagsAsync(int projectId) {
            return await _paletteRepository.GetProjectTagsAsync(projectId);
        }

        public async Task<bool> AddTagsToPaletteImagesAsync(List<int> imageIds, int projectId) {
            var projectTags = await _paletteRepository.GetProjectTagsAsync(projectId);
            if (!projectTags.Any()) return false;
            return await _paletteRepository.AddTagsToPaletteImagesAsync(imageIds, projectTags);
        }

        public async Task<SubmitAssetsRes> SubmitAssets(int projectID, List<int> blobIDs, int submitterID)        {
            try 
            {
                (List<int> successfulSubmissions, List<int> failedSubmissions) = await _paletteRepository.SubmitAssetstoDb(projectID, blobIDs, submitterID);   
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
