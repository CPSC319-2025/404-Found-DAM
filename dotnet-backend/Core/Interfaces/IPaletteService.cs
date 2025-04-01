using Core.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IPaletteService
    {
        Task<Asset?> ProcessUploadAsync(IFormFile file, UploadAssetsReq request, bool convertToWebp);
        Task<ProcessedAsset[]> ProcessUploadsAsync(List<IFormFile> files, UploadAssetsReq request, bool convertToWebp);
        Task<ProcessedAsset> UpdateAssetAsync(IFormFile file, UpdateAssetReq req, bool convertToWebp);
        Task<bool> DeleteAssetAsync(DeletePaletteAssetReq request);
        Task<List<IFormFile>> GetAssets(GetPaletteAssetsReq request);
        Task<IFormFile?> GetAssetByBlobIdAsync(string blobId, int userId);
        Task<byte[]> DecompressZstdAsync(byte[] compressedData);
        Task<List<string>> GetProjectTagsAsync(int projectId);
        Task<SubmitAssetsRes> SubmitAssets(int projectID, List<string> blobIDs, int submitterID);   
        Task<RemoveTagsResult> RemoveTagsFromAssetsAsync(List<string> blobIds, List<int> tagIds);     
        Task<GetBlobProjectAndTagsRes> GetBlobProjectAndTagsAsync(string blobId);
        Task<AssignTagResult> AssignTagToAssetAsync(string blobId, int tagId);
        Task<AssignProjectTagsResult> AssignProjectTagsToAssetAsync(AssignProjectTagsToAssetReq request);
        Task<GetBlobFieldsRes> GetBlobFieldsAsync(string blobId);
    }
}
