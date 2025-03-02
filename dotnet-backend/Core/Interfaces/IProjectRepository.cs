using Core.Dtos;

namespace Core.Interfaces
{
    public interface IProjectRepository
    {
        AddAssetsToProjectRes AddAssetsToProject(string projectId, List<string> imageIds);
        ArchiveProjectsRes ArchiveProjects(List<string> projectIds);
        GetArchivedProjectLogsRes GetArchivedProjectLogs();
    }
}
