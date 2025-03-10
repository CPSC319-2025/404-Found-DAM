﻿using System;
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
                        project.ArchivedAt = DateTime.UtcNow;

                        // TODO: Set each asset's Active to false?
                        
                        // Console.WriteLine("Remove regular users from this archived project");
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
                
                // Console.WriteLine("Save the change");
                // Save the change
                await _context.SaveChangesAsync();

                if (unfoundProjectIDs.Count != 0) 
                {
                    string unfoundProjects = string.Join(",", unfoundProjectIDs.Select(id => id.ToString()));
                    throw new PartialSuccessException($"Partial success. Unfound and not archived: {unfoundProjects}");
                }
                else 
                {
                    return true;
                }
            }
            catch (PartialSuccessException)
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

        public async Task<(List<Project>, List<User>, List<ProjectMembership>)> GetAllProjectsInDb(int userID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            var projectMemberships = await _context.ProjectMemberships
                .AsNoTracking() // Disable tracking for readonly queries to improve efficiency
                .Where(pm => pm.UserID == userID) // Filter by userID first
                .Include(pm => pm.Project)
                .ThenInclude(p => p.Assets) //  Load Assets collection associated with each loaded Project.
                .ToListAsync();
            
            if (projectMemberships.Any()) 
            {
                List<Project> projects = projectMemberships.Select(pm => pm.Project).ToList();

                HashSet<int> userIDSet = new HashSet<int>();

                foreach (var p in projects) 
                {
                    // Collect all users' IDs via project p's projectMemberships
                    foreach (var pm in p.ProjectMemberships) 
                    {
                        userIDSet.Add(pm.UserID);
                    }                    
                }

                // Get all users associated with and ensure no duplicated user by checking against userIDSet
                var users = await _context.Users
                    .AsNoTracking()
                    .Where(u => userIDSet.Contains(u.UserID))
                    .ToListAsync();
                
                return (projects, users, projectMemberships);
            }
            else 
            {
                throw new DataNotFoundException($"No Projects associted with the user found in the database");
            }
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
