using Core.Entities;
using Core.Dtos;
using System.Text.Json;

namespace Core.Interfaces
{
    public interface IProjectRepository
    {
        // Suffixing InDb to differentiate from service operations.  
        Task<(List<string> successfulAssociations, List<string> failedAssociation)> AssociateAssetsWithProjectInDb(int projectID, List<string> blobIDs, int submitterID);
        Task<(List<int>, Dictionary<int, DateTime>, Dictionary<int, DateTime>)> ArchiveProjectsInDb(List<int> projectIDs);
        Task<List<Log>> GetArchivedProjectLogsInDb();
        Task<Project> GetProjectInDb(int projectID);
        Task<(List<Project>, List<User>, List<ProjectMembership>)> GetAllProjectsInDb();
        Task<(List<Asset>, int)> GetPaginatedProjectAssetsInDb(GetPaginatedProjectAssetsReq req, int offset, int requesterID);
        Task<UpdateProjectRes> UpdateProjectInDb(int projectID, UpdateProjectReq req);
        Task AddAssetTagAssociationAsync(string imageId, int tagId);
        Task<bool> CheckProjectAssetExistence(int projectID, string blobId, int userID);
        Task<List<Asset>> GetProjectAndAssetsInDb(int projectID);
        Task UpsertAssetMetadataAsync(string imageId, int fieldId, JsonElement fieldValueElement);
        Task<List<Project>> GetProjectsForUserInDb(int userId);
    }

}