


using Core.Dtos;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces
{
    public interface IPaletteService
    {
        Task<int> ProcessUploadAsync(IFormFile file, UploadAssetsReq request);
        Task<object[]> ProcessUploadsAsync(List<IFormFile> files, UploadAssetsReq request);

        Task<bool> DeleteAssetAsync(DeletePaletteAssetReq request);
        Task<List<IFormFile>> GetAssets(GetPaletteAssetsReq request);
        Task<List<string>> GetProjectTagsAsync(int projectId);
        Task<bool> AddTagsToPaletteImagesAsync(List<int> imageIds, int projectId);

    }
}
