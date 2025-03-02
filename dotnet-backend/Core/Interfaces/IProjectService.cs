using Core.Dtos;

namespace Core.Interfaces
{
    public interface IProjectService
    {
        Task<AddAssetsToProjectRes> AddAssetsToProject(string projectId, List<string> imageIds);
        Task<ArchiveProjectsRes> ArchiveProjects(List<string> projectIds);
        Task<GetArchivedProjectLogsRes> GetArchivedProjectLogs();
        Task<RetrieveProjectRes> RetrieveProject(string projectId);
        Task<GetProjectAssetsRes> GetProjectAssets(string projectId, string type, int pageNumber, int pageSize);
    }
}
