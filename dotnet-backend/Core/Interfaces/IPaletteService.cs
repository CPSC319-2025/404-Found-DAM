using Core.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IPaletteService
    {
        Task<Asset?> ProcessUploadAsync(IFormFile file, UploadAssetsReq request, bool convertToWebp);
        Task<ProcessedAsset[]> ProcessUploadsAsync(List<IFormFile> files, UploadAssetsReq request, bool convertToWebp);

        Task<bool> DeleteAssetAsync(DeletePaletteAssetReq request);
        Task<List<IFormFile>> GetAssets(GetPaletteAssetsReq request);
        Task<List<string>> GetProjectTagsAsync(int projectId);
        Task<bool> AddTagsToPaletteImagesAsync(List<string> imageIds, int projectId);
        Task<SubmitAssetsRes> SubmitAssets(int projectID, List<string> blobIDs, int submitterID);   
        Task<RemoveTagsResult> RemoveTagsFromAssetsAsync(List<string> blobIds, List<int> tagIds);     
        Task<GetBlobProjectAndTagsRes> GetBlobProjectAndTagsAsync(string blobId);
        Task<AssignTagResult> AssignTagToAssetAsync(string blobId, int tagId);
    }
}
