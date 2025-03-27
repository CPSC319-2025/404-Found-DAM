using Core.Dtos;
using Core.Entities;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces  {
    public interface IPaletteRepository {
        public Task<List<string>> GetProjectTagsAsync(int projectId);

        public Task<bool> AddTagsToPaletteImagesAsync(List<string> imageIds, List<string> tags);

        public Task<bool> DeleteAsset(DeletePaletteAssetReq request);

        public Task<Asset> UploadAssets(IFormFile file, UploadAssetsReq request, bool convertToWebp, IImageService _imageService);
        public Task<List<IFormFile>> GetAssetsAsync(int userId);
        Task<(List<string> successfulSubmissions, List<string> failedSubmissions)> SubmitAssetstoDb(int projectID, List<string> blobIDs, int submitterID);    

        Task<bool> AssetTagAssociationExistsAsync(string blobId, int tagId);
        Task<bool> RemoveAssetTagsFromDb(string blobId, int tagId);
        Task<GetBlobProjectAndTagsRes> GetBlobProjectAndTagsAsync(string blobId);
        Task<AssignTagResult> AssignTagToAssetAsync(string blobId, int tagId);

        Task<string?> GetAssetNameByBlobIdAsync(string blobID);
        Task<string?> GetTagNameByIdAsync(int tagId);
        Task<string?> GetProjectNameByIdAsync(int projectID);
    }
}