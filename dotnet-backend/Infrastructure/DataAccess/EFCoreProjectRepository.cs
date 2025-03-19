using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;
using Infrastructure.Exceptions;
using System.Reflection.Metadata.Ecma335;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Infrastructure.DataAccess
{
    public class EFCoreProjectRepository : IProjectRepository
    {
        private IDbContextFactory<DAMDbContext> _contextFactory;
        public EFCoreProjectRepository(IDbContextFactory<DAMDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<(List<int>, List<int>)> AssociateAssetsWithProjectinDb(int projectID, List<int> blobIDs, int submitterID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            List<int> successfulAssociations = new List<int>();

            // check project exist & if submitter is a member
            var isProjectFound = await _context.Projects.AnyAsync(p => p.ProjectID == projectID);
            if (isProjectFound) 
            {
                var isSubmitterMember = await _context.ProjectMemberships.AnyAsync(pm => pm.ProjectID == projectID && pm.UserID == submitterID);
                if (isSubmitterMember) 
                {
                    // Retrieve assets using blobIDs
                    var assetsToBeAssociated = await _context.Assets
                        .Where(a => blobIDs.Contains(a.BlobID))
                        .ToListAsync();
                    
                    if (assetsToBeAssociated == null || assetsToBeAssociated.Count == 0) 
                    {
                        // No assets to be associated, return empty successfulAssociations, and blobIDs = failedAssociations
                        return (successfulAssociations, blobIDs);
                    }
                    else 
                    {
                        // Assign projectID t0 each asset and add to successfulAssociations
                        foreach (Asset a in assetsToBeAssociated)
                        {
                            a.ProjectID = projectID;
                            a.LastUpdated = DateTime.UtcNow;
                            successfulAssociations.Add(a.BlobID);
                        }
                        await _context.SaveChangesAsync();
                        return (successfulAssociations, blobIDs.Except(successfulAssociations).ToList());
                    }
                }
                else 
                {
                    throw new DataNotFoundException($"User ${submitterID} not a member of project ${projectID}");
                }
            }
            else 
            {
                throw new DataNotFoundException($"Project ${projectID} not found");
            }            
        }

        public async Task<(List<int>, Dictionary<int, DateTime>, Dictionary<int, DateTime>)> ArchiveProjectsInDb(List<int> projectIDs)
         {
            // Create empty lists and dictionaries for storing process results
            List<int> unfoundProjectIDs = new List<int>();
            Dictionary<int, DateTime> NewArchivedProjects = new Dictionary<int, DateTime>();
            Dictionary<int, DateTime> ProjectsArchivedAlready = new Dictionary<int, DateTime>();
            try 
            {            
                // Set each project Active to false for archiving
                using DAMDbContext _context = _contextFactory.CreateDbContext();

                Console.WriteLine("Fetch all projects in a single query");
                // Fetch all projects in a single query
                var projects = await _context.Projects
                    .Include(p => p.ProjectMemberships)
                    .ThenInclude(pm => pm.User)
                    .Where(p => projectIDs.Contains(p.ProjectID))
                    .ToListAsync();

                foreach (int projectID in projectIDs)
                {
                    var project = projects.FirstOrDefault(p => p.ProjectID == projectID);

                    if (project == null)    // Projects not found
                    {
                        unfoundProjectIDs.Add(projectID);   
                    }
                    else if (!project.Active)   // Porjects already archived
                    {
                        if (project.ArchivedAt != null) 
                        {
                            ProjectsArchivedAlready[project.ProjectID] = project.ArchivedAt.Value;
                        }
                    }
                    else    // Projects to be archived
                    {
                        project.Active = false;
                        project.ArchivedAt = DateTime.UtcNow;
                        NewArchivedProjects[project.ProjectID] = project.ArchivedAt.Value;
                    }
                }
                
                // Console.WriteLine("Save the change");
                // Save the change
                await _context.SaveChangesAsync();
                return (unfoundProjectIDs, NewArchivedProjects, ProjectsArchivedAlready);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<Log>> GetArchivedProjectLogsInDb()
        {
            //TODO: only allow admin to access
            return null;
        }

        public async Task<Project> GetProjectInDb(int projectID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            var project = await _context.Projects
                .Include(p => p.ProjectTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.ProjectMemberships) // Include ProjectMemberships
                    .ThenInclude(pm => pm.User) // Include User
                .AsSplitQuery() // Use split queries instead of a single query when loading multiple collections
                .FirstOrDefaultAsync(p => p.ProjectID == projectID);

            if (project == null)
            {
                throw new DataNotFoundException($"Project {projectID} not found.");
            }
            else 
            {
                return project;
            }   
        }

        public async Task<(List<Project>, List<User>, List<ProjectMembership>)> GetAllProjectsInDb(int requesterID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            // Get projectmemberhips associated with the requester
            var requesterProjectMemberships = await _context.ProjectMemberships
                .AsNoTracking() // Disable tracking for readonly queries to improve efficiency
                .Where(pm => pm.UserID == requesterID) // Filter by requesterID first
                .Include(pm => pm.Project)
                    .ThenInclude(p => p.Assets) //  Load Assets collection associated with each loaded Project.
                .ToListAsync();
            
            if (requesterProjectMemberships.Any()) 
            {
                List<int> projectIDList = requesterProjectMemberships.Select(pm => pm.Project.ProjectID).ToList();
                // Console.WriteLine($"projects' ID: {string.Join(", ", projectIDList)}");         

                List<Project> foundProjectList = new List<Project>();
                HashSet<int> userIDSet = new HashSet<int>();
                List<ProjectMembership> projectMemberships = new List<ProjectMembership>();

                foreach (var pID in projectIDList) 
                {
                    // Eagerly load project and its collections.
                    var project = await _context.Projects
                        .Include(p => p.ProjectMemberships)
                        .FirstOrDefaultAsync(p => p.ProjectID == pID);                    
    
                    // Collect all users' IDs via project p's projectMemberships, and add these projectMemberships
                    if (project != null && !foundProjectList.Contains(project)) 
                    {
                        foreach (var pm in project.ProjectMemberships) 
                        {
                            // Console.WriteLine($"projectID: ${pID}, userID: ${pm.UserID}");
                            userIDSet.Add(pm.UserID);
                            projectMemberships.Add(pm);
                        }  
                        foundProjectList.Add(project);
                    }       
                }

                // Get all users associated with and ensure no duplicated user by checking against userIDSet
                var users = await _context.Users
                    .AsNoTracking()
                    .Where(u => userIDSet.Contains(u.UserID))
                    .ToListAsync();
                
                return (foundProjectList, users, projectMemberships);
            }
            else 
            {
                return (new List<Project>(), new List<User>(), new List<ProjectMembership>());
            }
        }

        // Get ALL assets of a project from database
        public async Task<List<Asset>> GetProjectAssetsInDb(int projectID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            var project = await _context.Projects
                .Include(p => p.Assets)
                    .ThenInclude(a => a.AssetTags)
                .Include(p => p.Assets)
                    .ThenInclude(a => a.AssetMetadata)
                .AsNoTracking() // Improve performance for Read-only operations
                .AsSplitQuery() // More roundtrips; may be removed to use single query, which has fewer trips but can lead to slow performance 
                .FirstOrDefaultAsync(p => p.ProjectID == projectID);
            
            if (project != null) 
            {
                // Filter to include assets that were submitted to project:
                List<Asset> projectAssetList = project.Assets.Where(a => a.assetState == Asset.AssetStateType.SubmittedToProject).ToList();;
                return projectAssetList;
            } 
            else 
            {
                throw new DataNotFoundException($"Project {projectID} not found");
            }
        }

        public async Task<(List<Asset>, int)> GetPaginatedProjectAssetsInDb(GetPaginatedProjectAssetsReq req, int offset, int requesterID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            // Check if the requestor is a member of the project
            bool isMember = await _context.ProjectMemberships
                .AnyAsync(pm => pm.ProjectID == req.projectID && pm.UserID == requesterID);
            
            if (isMember) 
            {
                // Retrieve matched Assets and their tags
                IQueryable<Asset> query = _context.Assets
                    .Where(a => a.ProjectID == req.projectID)
                    .Include(a => a.AssetTags)
                        .ThenInclude(at => at.Tag);
                                     
                bool isQueryEmpty = !await query.AnyAsync(); 

                if (isQueryEmpty) 
                {   
                    throw new DataNotFoundException("No results were found");
                }
                else 
                {
                    // Apply filters
                    if (req.assetType.ToLower() != "all")
                    {
                        query = query.Where(a => a.MimeType.ToLower().StartsWith(req.assetType.ToLower()));
                    }

                    if (req.postedBy.HasValue && req.postedBy.Value > 0)
                    {
                        query = query.Where(a => a.User != null && a.User.UserID == req.postedBy.Value);
                    }

                    if (req.tagID.HasValue && req.tagID.Value > 0) 
                    {
                        query = query.Where(a => a.AssetTags.Any(at => at.TagID == req.tagID.Value));
                    }


                    // Perform pagination, and do nested eager loads to include AssetMetadata for each Asset and MetadataField for each AssetMetadata.
                   
                    int totalFilteredAssetCount = query.Count(); // Count the filtered assets before paginated.
                    
                    List<Asset> assets = await query
                    .OrderBy(a => a.FileName)
                    .Skip((req.pageNumber - 1) * req.assetsPerPage)
                    .Take(req.assetsPerPage)
                    .Include(a => a.User)
                    .ToListAsync();

                    return (assets,totalFilteredAssetCount);
                }
            }
            else 
            {
                throw new DataNotFoundException("Requester not a member of the project.");
            }
        }
    }
}
