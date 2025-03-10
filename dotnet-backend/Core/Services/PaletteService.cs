using Core.Interfaces;
using Core.Dtos;
using Microsoft.AspNetCore.Http;


namespace Core.Services
{
    public class PaletteService : IPaletteService
    {
        private readonly IPaletteRepository _paletteRepository;
        
        public PaletteService(IPaletteRepository paletteRepository)
        {
            _paletteRepository = paletteRepository;
        }

        public async Task<bool> ProcessUploadAsync(IFormFile file, UploadAssetsReq request)
        {
            try {
                return await _paletteRepository.UploadAssets(file, request);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error uploading assets: {ex.Message}");
                return false;
            }
        }

        public async Task<object[]> ProcessUploadsAsync(List<IFormFile> files, UploadAssetsReq request)
        {
            var uploadTasks = files.Select(async file => 
            {
                var res = await ProcessUploadAsync(file, request);
                if (res){
                    return new { 
                        Success = true, 
                        FileName = file.FileName, 
                        Size = file.Length
                    };
                }
                else {
                    return new { 
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

    }
}
