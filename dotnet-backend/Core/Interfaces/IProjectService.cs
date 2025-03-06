﻿using Core.Dtos;

namespace Core.Interfaces
{
    public interface IProjectService
    {
        // For ProjectController
        Task<AddAssetsToProjectRes> AddAssetsToProject(int projectID, List<int> blobIDs);
        Task<ArchiveProjectsRes> ArchiveProjects(List<int> projectIDs);
        Task<GetArchivedProjectLogsRes> GetArchivedProjectLogs();
        Task<GetProjectRes> GetProject(int projectID);
        Task<GetPaginatedProjectAssetsRes> GetPaginatedProjectAssets(GetPaginatedProjectAssetsReq req);
        Task<(string, byte[])> ExportProject(int projectID);
    }
}
