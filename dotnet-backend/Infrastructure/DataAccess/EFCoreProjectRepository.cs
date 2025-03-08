using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;
using Infrastructure.Exceptions;

namespace Infrastructure.DataAccess
{
    public class EFCoreProjectRepository : IProjectRepository
    {
        private IDbContextFactory<DAMDbContext> _contextFactory;
        public EFCoreProjectRepository(IDbContextFactory<DAMDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<bool> SubmitAssetstoDb(int projectID, List<int> blobIDs)
        {
            //TODO
            return projectID != 0 ? true : false;
        }

        public async Task<bool> ArchiveProjectsInDb(List<int> projectIDs)
         {
            // Create an empty list for storing unfound projectIDs
            List<int> unfoundProjectIDs = new List<int>();
    
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

                    if (project == null) 
                    {
                        unfoundProjectIDs.Add(projectID);   
                    }
                    else 
                    {
                        project.Active = false;

                        // TODO: Set each asset's Active to false?
                        
                        Console.WriteLine("Remove regular users from this archived project");
                        // Remove regular users from this archived project
                        List<ProjectMembership> projectMemberships = project.ProjectMemberships.ToList();
                        foreach (var pm in projectMemberships)
                        {
                            if (pm.UserRole == ProjectMembership.UserRoleType.Regular) 
                            {
                                _context.ProjectMemberships.Remove(pm);
                            }
                        }
                    }
                }
                
                Console.WriteLine("Save the change");
                // Save the change
                await _context.SaveChangesAsync();

                if (unfoundProjectIDs.Count != 0) 
                {
                    string unfoundProjects = string.Join(",", unfoundProjectIDs.Select(id => id.ToString()));
                    throw new DataNotFoundException($"Projects that do not exist in the database: {unfoundProjects}");
                }
                else 
                {
                    return true;
                }
            }
            catch (DataNotFoundException)
            {
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Log>> GetArchivedProjectLogsInDb()
        {
            //TODO
            return null;
        }

        public async Task<(Project, string, List<string>)> GetProjectInDb(int projectID)
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

            var adminMembership = project.ProjectMemberships
                .FirstOrDefault(pm => pm.UserRole == ProjectMembership.UserRoleType.Admin);

            if (adminMembership?.User == null)
            {
                throw new DataNotFoundException($"Admin not found for project {projectID}.");
            }

            List<string> tags = project.ProjectTags.Select(pt => pt.Tag.Name).ToList();

            return (project, adminMembership.User.Name, tags);
        }

        // Get ALL assets of a project from database
        public async Task<List<Asset>> GetProjectAssetsInDb(int projectID)
        {
            // TODO
            return null;
        }

        public async Task<List<Asset>> GetPaginatedProjectAssetsInDb(GetPaginatedProjectAssetsReq req, int offset)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            // Retrieve matched Assets
            IQueryable<Asset> query = _context.Assets.Where(a => a.ProjectID == req.projectID);
            
            bool isQueryEmpty = !await query.AnyAsync(); 

            if (isQueryEmpty) 
            {   
                throw new DataNotFoundException("No results wer found");
            }
            else 
            {
                // Apply filters
                if (req.assetType.ToLower() != "all")
                {
                    query = query.Where(a => a.MimeType.ToLower() == req.assetType.ToLower());
                }

                if (!string.IsNullOrEmpty(req.postedBy)) 
                {
                    query = query.Where(a => a.User != null && a.User.Name.ToLower() == req.postedBy.ToLower());
                }

                // TODO: Dateposted attribute not in datamodel

                // Perform pagination, and do nested eager loads to include AssetMetadata for each Asset and MetadataField for each AssetMetadata.
                // TODO: Tags are not included yet
                List<Asset> assets = await query
                .OrderBy(a => a.FileName)
                .Skip((req.pageNumber - 1) * req.assetsPerPage)
                .Take(req.assetsPerPage)
                .Include(a => a.User)
                .ToListAsync();

                return assets;
            }
        }
    }
}
