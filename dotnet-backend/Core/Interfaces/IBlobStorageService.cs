using Microsoft.AspNetCore.Http;
using Core.Dtos;

namespace Core.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(IFormFile file, string containerName, UploadAssetsReq request);
        Task<bool> DeleteAsync(string blobId, string containerName);
        Task<List<IFormFile>> DownloadAsync(string containerName, int userId);
    }
}