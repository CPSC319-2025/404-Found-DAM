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
    }
}
