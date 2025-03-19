using Core.Entities;
using Core.Dtos;

namespace Core.Interfaces
{
    public interface IProjectRepository
    {
        // Suffixing InDb to differentiate from service operations.  
        Task<(List<int> successfulAssociations, List<int> failedAssociation)> AssociateAssetsWithProjectinDb(int projectID, List<int> blobIDs, int submitterID);
        Task<(List<int>, Dictionary<int, DateTime>, Dictionary<int, DateTime>)> ArchiveProjectsInDb(List<int> projectIDs);
        Task<List<Log>> GetArchivedProjectLogsInDb();
        Task<Project> GetProjectInDb(int projectID);
        Task<(List<Project>, List<User>, List<ProjectMembership>)> GetAllProjectsInDb(int requesterID);
        Task<List<Asset>> GetProjectAssetsInDb(int projectID);
        Task<(List<Asset>, int)> GetPaginatedProjectAssetsInDb(GetPaginatedProjectAssetsReq req, int offset, int requesterID);
    }
}
