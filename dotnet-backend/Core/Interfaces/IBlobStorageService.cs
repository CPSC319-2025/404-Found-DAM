using Microsoft.AspNetCore.Http;
using Core.Dtos;
using Core.Entities;
namespace Core.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(byte[] file, string containerName, Asset assetMetaData);
        Task<bool> DeleteAsync(Asset asset, string containerName);
        Task<List<string>> DownloadAsync(string containerName, List<(string, string)> assetIdNameTuples);
        Task<string> MoveAsync(string sourceFolder, string fileName, string targetFolder);
        Task<bool> UpdateAsync(byte[] file, string containerName, Asset assetMetaData);
    }
}