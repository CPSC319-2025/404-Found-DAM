


using Core.Dtos;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces
{
    public interface IPaletteService
    {
        Task<bool> ProcessUploadAsync(IFormFile file, UploadAssetsReq request);
        Task<bool[]> ProcessUploadsAsync(IList<IFormFile> files, UploadAssetsReq request);
        Task<List<string>> GetProjectTagsAsync(int projectId);
        Task<bool> AddTagsToPaletteImagesAsync(List<int> imageIds, int projectId);

    }
}
