using Microsoft.AspNetCore.Http;
using Core.Dtos;
using Core.Entities;
namespace Core.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(byte[] file, string containerName, Asset assetMetaData);
        Task<bool> DeleteAsync(Asset asset, string containerName);
        Task<List<IFormFile>> DownloadAsync(string containerName, List<(string, string)> assetIdNameTuples);
    
        Task<string> UploadEditedImageAsync(byte[] file, string blobID, string containerName, Asset assetMeta);
    }
}