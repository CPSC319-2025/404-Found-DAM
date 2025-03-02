using System;
using System.Linq;
using System.Collections.Generic;
using Core.Interfaces;
using Core.Dtos;

namespace Infrastructure.DataAccess
{
    public class ProjectRepository : IProjectRepository
    {
        private MyDbContext _context;
        public ProjectRepository(MyDbContext context)
        {
            _context = context;
        }

        public AddAssetsToProjectRes AddAssetsToProject(string projectId, List<string> imageIds)
        {
            //TODO
            if (projectId == "") {
                throw new Exception("Empty project Id.");
            } else {
                List<AssignedAsset> assignedAssets = new List<AssignedAsset>();
                foreach (var imageId in imageIds)
                {
                    AssignedAsset assignedAsset = new AssignedAsset
                    {
                        id = imageId,
                        filename = imageId+".jpg"
                    };
                    assignedAssets.Add(assignedAsset);
                }
                AddAssetsToProjectRes result = new AddAssetsToProjectRes
                {
                    projectId = projectId, 
                    assignedAssets = assignedAssets, 
                    UploadedAt = DateTime.UtcNow
                };
                return result;
            }
        }

        public ArchiveProjectsRes ArchiveProjects(List<string> projectIds)
         {
            //TODO
            if (projectIds.Count == 0) {
                throw new Exception("Empty projectIds.");
            } else {
                ArchiveProjectsRes result = new ArchiveProjectsRes{archiveTimestamp = DateTime.UtcNow};
                return result;
            }
        }

        public GetArchivedProjectLogsRes GetArchivedProjectLogs()
        {
            //TODO
            ArchivedProjectLog log1 = new ArchivedProjectLog
            {
                projectId = "123",
                projectName = "P1",
                archivedAt = DateTime.UtcNow,
                admin = "John"
            };
            ArchivedProjectLog log2 = new ArchivedProjectLog
            {
                projectId = "456",
                projectName = "P2",
                archivedAt = DateTime.UtcNow,
                admin = "Betty"
            };
            List<ArchivedProjectLog> logs = new List<ArchivedProjectLog>();
            logs.Add(log1);
            logs.Add(log2);
            GetArchivedProjectLogsRes result = new GetArchivedProjectLogsRes{logs = logs};
            return result;
        }
    }
}
