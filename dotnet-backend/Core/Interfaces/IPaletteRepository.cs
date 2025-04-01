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
        
        Task<(List<string> successfulSubmissions, List<string> failedSubmissions)> SubmitAssetstoDb(int projectID, List<string> blobIDs, int submitterID);    

        Task<bool> AssetTagAssociationExistsAsync(string blobId, int tagId);
        Task<bool> RemoveAssetTagsFromDb(string blobId, int tagId);
        Task<GetBlobProjectAndTagsRes> GetBlobProjectAndTagsAsync(string blobId);
        Task<AssignTagResult> AssignTagToAssetAsync(string blobId, int tagId);

        Task<Asset> UploadMergedChunkToDb(string filePath, string filename, string mimeType, int userId);
    
        Task<Asset> UploadEditedImage(IFormFile file, string blobID, UploadAssetsReq req, IImageService _imageService);

    }
}