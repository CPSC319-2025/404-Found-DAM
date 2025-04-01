using Core.Dtos;
using Core.Entities;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces  {
    public interface IPaletteRepository {
        public Task<List<string>> GetProjectTagsAsync(int projectId);

        public Task<bool> DeleteAsset(DeletePaletteAssetReq request);

        public Task<Asset> UploadAssets(IFormFile file, UploadAssetsReq request, bool convertToWebp, IImageService _imageService);
        
        // Update method signature to accept GetPaletteAssetsReq
        public Task<List<IFormFile>> GetAssets(GetPaletteAssetsReq request);
        
        // New method to get a specific asset by blobId
        public Task<IFormFile?> GetAssetByBlobIdAsync(string blobId, int userId);
        
        Task<(List<string> successfulSubmissions, List<string> failedSubmissions)> SubmitAssetstoDb(int projectID, List<string> blobIDs, int submitterID, bool autoNaming = false);    

        Task<bool> AssetTagAssociationExistsAsync(string blobId, int tagId);
        Task<bool> RemoveAssetTagsFromDb(string blobId, int tagId);
        Task<GetBlobProjectAndTagsRes> GetBlobProjectAndTagsAsync(string blobId);
        Task<AssignTagResult> AssignTagToAssetAsync(string blobId, int tagId);
        Task<List<int>> GetProjectTagIdsAsync(int projectId);
        Task<AssignProjectTagsResult> AssignProjectTagsToAssetAsync(string blobId, List<int> tagIds);

        Task<string?> GetAssetNameByBlobIdAsync(string blobID);
        Task<string?> GetTagNameByIdAsync(int tagId);
        Task<string?> GetProjectNameByIdAsync(int projectID);

        Task<Asset> UploadMergedChunkToDb(string filePath, string filename, string mimeType, int userId);
        
        // Update an existing asset by blob ID
        Task<Asset> UpdateAssetAsync(IFormFile file, UpdateAssetReq request, bool convertToWebp, IImageService imageService);

        // Get all fields for a specific blob
        Task<GetBlobFieldsRes> GetBlobFieldsAsync(string blobId);
    }
}