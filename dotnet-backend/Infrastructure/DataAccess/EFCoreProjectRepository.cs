using System;
using System.Linq;
using System.Collections.Generic;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;

namespace Infrastructure.DataAccess
{
    public class EFCoreProjectRepository : IProjectRepository
    {
        private DAMDbContext _context;
        public EFCoreProjectRepository(DAMDbContext context)
        {
            _context = context;
        }

        public async Task<bool> SubmitAssetstoDb(int projectID, List<int> blobIDs)
        {
            //TODO
            return projectID != 0 ? true : false;
        }

        public async Task<bool> ArchiveProjectsInDb(List<int> projectIDs)
         {
            //TODO
            return projectIDs.Count != 0 ? true : false;
        }

        public async Task<List<Log>> GetArchivedProjectLogsInDb()
        {
            //TODO
            return null;
        }

        public async Task<(Project, string, List<string>)> GetProjectInDb(int projectID) 
        {
            await Task.Delay(1); // Replace this after the actual database access call
            Project project = new Project
            {
                ProjectID = projectID,
                Name = "Mocked Project",
                Version = "1.0",
                Location = "a mocked location",
                Description = "a mocked project",
                CreationTime = DateTime.Now,
                Active = true
            };
            string admin = "Jane Doe";
            List<string> tags = new List<string> {"apple", "orange"};
            return (project, admin, tags);
        }

        // Get ALL assets of a project from database
        public async Task<List<Asset>> GetProjectAssetsInDb(int projectID)
        {
            // TODO
            return null;
        }

        public async Task<List<Asset>> GetPaginatedProjectAssetsInDb(GetPaginatedProjectAssetsReq req, int offset)
        {
            // TODO
            // Build query layer by layer with filters
            return null;
        }
    }
}
