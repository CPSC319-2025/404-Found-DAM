using Core.Entities;
using Core.Dtos;
using System.Text.Json;

namespace Core.Interfaces
{
    public interface IProjectRepository
    {
        // Suffixing InDb to differentiate from service operations.  
        Task<(List<string> successfulAssociations, List<string> failedAssociation)> AssociateAssetsWithProjectInDb(int projectID, List<string> blobIDs, int submitterID);
        Task ArchiveProjectInDb(int projectID);
        Task<List<Log>> GetArchivedProjectLogsInDb();
        Task<Project> GetProjectInDb(int projectID);
        Task<(List<Project>, List<User>, List<ProjectMembership>)> GetAllProjectsInDb();
        Task<(List<Asset>, int, List<string>)> GetPaginatedProjectAssetsInDb(GetPaginatedProjectAssetsReq req, int offset, int requesterID);
        Task<UpdateProjectRes> UpdateProjectInDb(int projectID, UpdateProjectReq req);
        Task AddAssetTagAssociationAsync(string imageId, int tagId);
        Task<bool> CheckProjectAssetExistence(int projectID, string blobId, int userID);
        Task<List<Asset>> GetProjectAndAssetsInDb(int projectID);
        Task UpsertAssetMetadataAsync(string imageId, int fieldId, JsonElement fieldValueElement);
        Task<List<Project>> GetProjectsForUserInDb(int userId);
        Task<string?> GetAssetNameByBlobIdAsync(string blobID);
        Task<string?> GetTagNameByIdAsync(int tagId);
        Task<string?> GetProjectNameByIdAsync(int projectID);
        Task DeleteAssetFromProjectInDb(int projectId, string blobId);
        Task<string?> GetCustomMetadataNameByIdAsync(int fieldID);

        Task<Asset> GetAssetInDb(int projectID, string assetID);
    }
}