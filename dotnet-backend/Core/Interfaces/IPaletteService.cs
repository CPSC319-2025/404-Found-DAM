


using Core.Dtos.PaletteService;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces
{
    public interface IPaletteService
    {
        Task<bool> ProcessUploadAsync(IFormFile file, UploadAssetsReq request);
        Task<object[]> ProcessUploadsAsync(IList<IFormFile> files, UploadAssetsReq request);
        Task<List<IFormFile>> GetAssets(GetPaletteAssetsReq request);
        Task<List<string>> GetProjectTagsAsync(int projectId);
        Task<bool> AddTagsToPaletteImagesAsync(List<int> imageIds, int projectId);

    }
}
