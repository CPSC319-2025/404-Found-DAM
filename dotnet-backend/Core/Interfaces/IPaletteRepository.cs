using Core.Dtos;
using Core.Entities;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces  {
    public interface IPaletteRepository {
        public Task<List<Asset>> GetAssetsFromPalette();

        public Task<List<string>> GetProjectTagsAsync(int projectId);

        public Task<bool> AddTagsToPaletteImagesAsync(List<int> imageIds, List<string> tags);

        public Task<bool> DeleteAsset(DeletePaletteAssetReq request);

        public Task<int> UploadAssets(IFormFile file, UploadAssetsReq request);
        public Task<List<IFormFile>> GetAssetsAsync(int userId);
    }
}