using System;
using System.Linq;
using System.Collections.Generic;
using Core.Interfaces;
using Core.Dtos;
using DataModel;

namespace Infrastructure.DataAccess
{
    public class ProjectRepository : IProjectRepository
    {
        private MyDbContext _context;
        public ProjectRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddAssetsToProjectInDb(string projectId, List<string> imageIds)
        {
            //TODO
            return projectId != "" ? true : false;
        }

        public async Task<bool> ArchiveProjectsInDb(List<string> projectIds)
         {
            //TODO
            return projectIds.Count != 0 ? true : false;
        }

        public async Task<List<Log>> GetArchivedProjectLogsInDb()
        {
            //TODO
            return null;
        }

        public async Task<Project> RetrieveProjectInDb(string projectId) 
        {
            //TODO 
            return null;
        }


        public async Task<List<Asset>> GetProjectAssetsInDb(string projectId, string type, int offset, int pageSize)
        {
            //TODO
            return null;
        }
    }
}
