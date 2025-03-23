using Core.Dtos;
using Microsoft.AspNetCore.Http;

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
        Task<(byte[], string)> GetAssetFileFromStorage(int projectID, string blobID, string filename, int requesterID);
    }
}
