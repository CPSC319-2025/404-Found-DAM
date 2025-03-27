using Core.Dtos;

namespace Core.Interfaces
{
    public interface IProjectService
    {
        // For ProjectController
        Task<AssociateAssetsRes> AssociateAssetsWithProject(int projectID, List<string> blobIDs, int submitterID);
        Task<ArchiveProjectsRes> ArchiveProjects(List<int> projectIDs);
        Task<GetArchivedProjectLogsRes> GetArchivedProjectLogs();
        Task<GetProjectRes> GetProject(int projectID);
        Task<GetAllProjectsRes> GetAllProjects(int requesterID); 
        Task<GetPaginatedProjectAssetsRes> GetPaginatedProjectAssets(GetPaginatedProjectAssetsReq req, int reqeusterID);
        Task<UpdateProjectRes> UpdateProject(int projectID, UpdateProjectReq req);
        Task<List<GetProjectRes>> GetMyProjects(int userId);

        Task<string?> GetAssetNameByBlobIdAsync(string blobID);

        Task<string?> GetTagNameByIdAsync(int tagID);

        Task<string?> GetProjectNameByIdAsync(int projectID);
    }
}
