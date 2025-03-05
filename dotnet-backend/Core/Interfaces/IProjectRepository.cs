using Core.Entities;
using System;

namespace Core.Interfaces
{
    public interface IProjectRepository
    {
        // Suffixing InDb to differentiate from service operations.  
        Task<bool> AddAssetsToProjectInDb(int projectID, List<int> blobIDs);
        Task<bool> ArchiveProjectsInDb(List<int> projectIDs);
        Task<List<Log>> GetArchivedProjectLogsInDb();
        Task<Project> RetrieveProjectInDb(int projectID);
        Task<List<Asset>> GetProjectAssetsInDb(int projectID);
        Task<List<Asset>> GetProjectAssetsInDb(int projectID, string type, int offset, int pageSize);
    }
}
