using Core.Interfaces;
using Core.Dtos;
using Microsoft.AspNetCore.Http;
using System.Reflection.Metadata;
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

        public async Task<Asset> ProcessUploadAsync(IFormFile file, UploadAssetsReq request, bool convertToWebp)
        {
            Console.WriteLine("ProcessUploadAsync");
            try {
                return await _paletteRepository.UploadAssets(file, request, convertToWebp, _imageService);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error uploading assets: {ex.Message}");
                throw;
            }
        }

        public async Task<List<UploadAssetsRes>> ProcessUploadsAsync(List<IFormFile> files, UploadAssetsReq request, bool convertToWebp)
        {
            var uploadTasks = files.Select(async file => 
            {
                Asset asset = await ProcessUploadAsync(file, request, convertToWebp);
                if (asset != null) 
                {
                    UploadAssetsRes res = new UploadAssetsRes
                    {
                        BlobID = asset.BlobID, 
                        Success = true, 
                        FileName = asset.FileName, 
                        SizeInKB = asset.FileSizeInKB
                    };
                    return res;
                }
                else 
                {
                    UploadAssetsRes res = new UploadAssetsRes
                    {
                        Success = false, 
                        FileName = file.FileName, 
                        SizeInKB = file.Length / 1024.0
                    };
                    return res;
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
