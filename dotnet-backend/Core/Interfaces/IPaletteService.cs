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
        Task<bool> AddTagsToPaletteImagesAsync(List<string> imageIds, int projectId);
        Task<SubmitAssetsRes> SubmitAssets(int projectID, List<string> blobIDs, int submitterID);      
        Task<GetBlobProjectAndTagsRes> GetBlobProjectAndTagsAsync(string blobId);
    }
}
