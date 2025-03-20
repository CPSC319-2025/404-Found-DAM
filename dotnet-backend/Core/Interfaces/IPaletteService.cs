


using Core.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Core.Interfaces
{
    public interface IPaletteService
    {
        Task<string> ProcessUploadAsync(IFormFile file, UploadAssetsReq request);
        Task<object[]> ProcessUploadsAsync(List<IFormFile> files, UploadAssetsReq request);

        Task<bool> DeleteAssetAsync(DeletePaletteAssetReq request);
        Task<List<IFormFile>> GetAssets(GetPaletteAssetsReq request);
        Task<List<string>> GetProjectTagsAsync(int projectId);
        Task<bool> AddTagsToPaletteImagesAsync(List<int> imageIds, int projectId);
        Task<SubmitAssetsRes> SubmitAssets(int projectID, List<int> blobIDs, int submitterID);    
    }
}
