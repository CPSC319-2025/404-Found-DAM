using Core.Dtos;

namespace Core.Interfaces
{
    public interface IProjectService
    {
        // For ProjectController
        Task<AssociateAssetsRes> AssociateAssetsWithProject(int projectID, List<int> blobIDs, int submitterID);
        Task<ArchiveProjectsRes> ArchiveProjects(List<int> projectIDs);
        Task<GetArchivedProjectLogsRes> GetArchivedProjectLogs();
        Task<GetProjectRes> GetProject(int projectID);
        Task<GetAllProjectsRes> GetAllProjects(int requesterID); 
        Task<GetPaginatedProjectAssetsRes> GetPaginatedProjectAssets(GetPaginatedProjectAssetsReq req, int reqeusterID);
    }
}
