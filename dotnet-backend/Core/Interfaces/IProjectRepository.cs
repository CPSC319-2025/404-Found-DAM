using Core.Entities;
using Core.Dtos;

namespace Core.Interfaces
{
    public interface IProjectRepository
    {
        // Suffixing InDb to differentiate from service operations.  
        Task<bool> AddAssetsToProjectInDb(int projectID, List<int> blobIDs);
        Task<bool> ArchiveProjectsInDb(List<int> projectIDs);
        Task<List<Log>> GetArchivedProjectLogsInDb();
        Task<(Project, string, List<string>)> GetProjectInDb(int projectID);
        Task<List<Asset>> GetProjectAssetsInDb(int projectID);
        Task<List<Asset>> GetPaginatedProjectAssetsInDb(GetPaginatedProjectAssetsReq req, int offset);
    }
}
