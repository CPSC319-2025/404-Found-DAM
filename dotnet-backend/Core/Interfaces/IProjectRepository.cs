﻿using DataModel;
using System;

namespace Core.Interfaces
{
    public interface IProjectRepository
    {
        // Suffixing InDb to differentiate from service operations.  
        Task<bool> AddAssetsToProjectInDb(string projectId, List<string> assetIds);
        Task<bool> ArchiveProjectsInDb(List<string> projectIds);
        Task<List<Log>> GetArchivedProjectLogsInDb();
        Task<Project> RetrieveProjectInDb(string projectId);
        Task<List<Asset>> GetProjectAssetsInDb(string projectId);
        Task<List<Asset>> GetProjectAssetsInDb(string projectId, string type, int offset, int pageSize);
    }
}
