using Core.Entities;
using Core.Dtos;

namespace Core.Interfaces
{
    public interface IProjectRepository
    {
        // Suffixing InDb to differentiate from service operations.  
        Task<(List<int> successfulSubmissions, List<int> failedSubmissions)> SubmitAssetstoDb(int projectID, List<int> blobIDs, int submitterID);
        Task<bool> ArchiveProjectsInDb(List<int> projectIDs);
        Task<List<Log>> GetArchivedProjectLogsInDb();
        Task<Project> GetProjectInDb(int projectID);
        Task<(List<Project>, List<User>, List<ProjectMembership>)> GetAllProjectsInDb(int userID);
        Task<List<Asset>> GetProjectAssetsInDb(int projectID);
        Task<List<Asset>> GetPaginatedProjectAssetsInDb(GetPaginatedProjectAssetsReq req, int offset, int requesterID);
    }
}
