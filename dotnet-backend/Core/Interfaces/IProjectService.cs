using Core.Dtos;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces
{
    public interface IProjectService
    {
        // For ProjectController
        Task<AssociateAssetsWithProjectRes> AssociateAssetsWithProject(AssociateAssetsWithProjectReq request, int submitterID);
        Task<ArchiveProjectsRes> ArchiveProjects(List<int> projectIDs);
        Task<GetArchivedProjectLogsRes> GetArchivedProjectLogs();
        Task<GetProjectRes> GetProject(int projectID);
        Task<GetAllProjectsRes> GetAllProjects(); 
        Task<GetPaginatedProjectAssetsRes> GetPaginatedProjectAssets(GetPaginatedProjectAssetsReq req, int reqeusterID);
        Task<UpdateProjectRes> UpdateProject(int projectID, UpdateProjectReq req);
        Task<List<GetProjectRes>> GetMyProjects(int userId);

        Task<string?> GetAssetNameByBlobIdAsync(string blobID);

        Task<string?> GetTagNameByIdAsync(int tagID);

        Task<string?> GetProjectNameByIdAsync(int projectID);

        Task DeleteAssetFromProject(int projectId, string blobId);
    }
}
