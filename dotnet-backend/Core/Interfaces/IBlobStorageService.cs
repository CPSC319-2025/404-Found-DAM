using Microsoft.AspNetCore.Http;

namespace Core.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(IFormFile file, string containerName, string projectPrefix = null);
        Task<bool> DeleteAsync(string blobId, string containerName);
        Task<Stream> DownloadAsync(string blobId, string containerName);
    }
}