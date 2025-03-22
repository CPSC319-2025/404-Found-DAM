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

        Task<RemoveTagsResult> RemoveTagsFromAssetsAsync(List<string> blobIds, List<int> tagIds);     
        Task<GetBlobProjectAndTagsRes> GetBlobProjectAndTagsAsync(string blobId);
        Task<AssignTagResult> AssignTagToAssetAsync(string blobId, int tagId);
    }
}
