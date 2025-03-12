using Core.Dtos;

namespace Core.Interfaces
{
    public interface IProjectService
    {
        // For ProjectController
        Task<SubmitAssetsRes> SubmitAssets(int projectID, List<int> blobIDs);
        Task<ArchiveProjectsRes> ArchiveProjects(List<int> projectIDs);
        Task<GetArchivedProjectLogsRes> GetArchivedProjectLogs();
        Task<GetProjectRes> GetProject(int projectID);
        Task<GetAllProjecsRes> GetAllProjects(int userID); 
        Task<GetPaginatedProjectAssetsRes> GetPaginatedProjectAssets(GetPaginatedProjectAssetsReq req, int requesterID);
        Task<(string, byte[])> ExportProject(int projectID);
    }
}
