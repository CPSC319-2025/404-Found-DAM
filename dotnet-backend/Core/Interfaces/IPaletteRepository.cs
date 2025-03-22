using Core.Dtos;
using Core.Entities;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces  {
    public interface IPaletteRepository {
        public Task<List<string>> GetProjectTagsAsync(int projectId);

        public Task<bool> AddTagsToPaletteImagesAsync(List<string> imageIds, List<string> tags);

        public Task<bool> DeleteAsset(DeletePaletteAssetReq request);

        public Task<string> UploadAssets(IFormFile file, UploadAssetsReq request);
        public Task<List<IFormFile>> GetAssetsAsync(int userId);
        Task<(List<string> successfulSubmissions, List<string> failedSubmissions)> SubmitAssetstoDb(int projectID, List<string> blobIDs, int submitterID);    
        Task<GetBlobProjectAndTagsRes> GetBlobProjectAndTagsAsync(string blobId);
    }
}