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
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;


namespace Infrastructure.DataAccess
{
    public class EFCoreProjectRepository : IProjectRepository
    {
        private IDbContextFactory<DAMDbContext> _contextFactory;
        private readonly IBlobStorageService _blobStorageService;

        public EFCoreProjectRepository(IDbContextFactory<DAMDbContext> contextFactory, IBlobStorageService blobStorageService)
        {
            _contextFactory = contextFactory;
            _blobStorageService = blobStorageService;
        }

        public async Task<(List<string>, List<string>)> AssociateAssetsWithProjectinDb(int projectID, List<string> blobIDs, int submitterID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            List<string> successfulAssociations = new List<string>();

            // Get the project to be associated with & check if submitter is a member
            var projectToBeAssociated = await _context.Projects
                .Where(p => p.ProjectID == projectID)
                .Include(p => p.ProjectTags)
                    .ThenInclude(pt => pt.Tag) // Eagerly load the Tag entities
                .Include(p => p.ProjectMetadataFields)
                .FirstOrDefaultAsync();

            if (projectToBeAssociated != null) 
            {
                var isSubmitterMember = await _context.ProjectMemberships.AnyAsync(pm => pm.ProjectID == projectID && pm.UserID == submitterID);
                if (isSubmitterMember) 
                {
                    // Retrieve assets using blobIDs
                    var assetsToBeAssociated = await _context.Assets
                        .Where(a => blobIDs.Contains(a.BlobID) && a.ProjectID != projectID) // Avoid including assets already in the projectToBeAssociated.
                        .Include(a => a.AssetTags)
                        .Include(a => a.AssetMetadata)
                        .ToListAsync();
                    
                    if (assetsToBeAssociated == null || assetsToBeAssociated.Count == 0) 
                    {
                        // No assets to be associated, return empty successfulAssociations, and blobIDs = failedAssociations
                        return (successfulAssociations, blobIDs);
                    }
                    else 
                    {
                        // Take away association with the current project, assign new association with the new project, and add to successfulAssociations
                        foreach (Asset a in assetsToBeAssociated)
                        {
                            // Remove current assoication
                            _context.AssetTags.RemoveRange(a.AssetTags);
                            _context.AssetMetadata.RemoveRange(a.AssetMetadata);

                            // Create new association
                            a.LastUpdated = DateTime.UtcNow;
                            a.Project = projectToBeAssociated;

                            foreach (ProjectTag pt in projectToBeAssociated.ProjectTags)
                            {
                                AssetTag at = new AssetTag { Asset = a, Tag = pt.Tag };
                                _context.AssetTags.Add(at);
                            }

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
                .Include(p => p.ProjectMetadataFields)
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
                    .Where(a => a.ProjectID == req.projectID && a.assetState == Asset.AssetStateType.SubmittedToProject)
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


                    // number of total assets
                    int totalAssetCount = await query.CountAsync();

                    // Perform pagination, and do nested eager loads to include AssetMetadata for each Asset and MetadataField for each AssetMetadata.
                   
                    int totalFilteredAssetCount = query.Count(); // Count the filtered assets before paginated.
                    
                    List<Asset> assets = await query
                    .OrderBy(a => a.FileName)
                    .Skip((req.pageNumber - 1) * req.assetsPerPage)
                    .Take(req.assetsPerPage)
                    .Include(a => a.User)
                    .ToListAsync();
                    
                    return (assets, totalFilteredAssetCount);
                }
            }
            else 
            {
                throw new DataNotFoundException("Requester not a member of the project.");
            }
        }

        public async Task<UpdateProjectRes> UpdateProjectInDb(int projectID, UpdateProjectReq req) {
            using var _context = _contextFactory.CreateDbContext();

            var project = await _context.Projects
                .Include(p => p.ProjectMemberships)
                .Include(p => p.ProjectTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.ProjectMetadataFields)
                .FirstOrDefaultAsync(p => p.ProjectID == projectID);
            
            if (project == null) {
                throw new DataNotFoundException($"Project with ID {projectID} not found.");
            }

            if (!string.IsNullOrEmpty(req.Location)) project.Location = req.Location;

            if (req.Memberships != null) {
                var currentMemberships = project.ProjectMemberships.ToList();
                var reqUserIds = req.Memberships.Select(m => m.UserID).ToHashSet();

                foreach (var membership in currentMemberships) {
                    if (!reqUserIds.Contains(membership.UserID)) _context.ProjectMemberships.Remove(membership);
                }

                foreach (var dto in req.Memberships) {
                    var existing = currentMemberships.FirstOrDefault(m => m.UserID == dto.UserID);
                    if (existing != null) {
                        if (Enum.TryParse(dto.Role, true, out ProjectMembership.UserRoleType newRole)) existing.UserRole = newRole;
                    }
                    else {
                        var user = await _context.Users.FindAsync(dto.UserID);
                         if (user != null && Enum.TryParse(dto.Role, true, out ProjectMembership.UserRoleType role)) {
                            _context.ProjectMemberships.Add(new ProjectMembership {
                                ProjectID = projectID,
                                UserID = user.UserID,
                                UserRole = role,
                                User = user,
                                Project = project
                            });
                         }
                    }
                }
            }

            if (req.Tags != null) {
                var currentTags = project.ProjectTags.ToList();
                var reqTagNames = req.Tags.Select(t => t.Name.ToLower()).ToHashSet();


                foreach (var pt in currentTags) {
                    if (!reqTagNames.Contains(pt.Tag.Name.ToLower())) {
                        _context.ProjectTags.Remove(pt);
                        foreach (var asset in project.Assets) {
                            var assetTag = asset.AssetTags.FirstOrDefault(at => at.TagID == pt.TagID);
                            if (assetTag != null)
                                _context.AssetTags.Remove(assetTag);
                        }
                        _context.Tags.Remove(pt.Tag);
                    }
                }

                foreach (var tagDto in req.Tags) {
                    var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == tagDto.Name.ToLower());
                    if (existingTag == null) {
                        existingTag = new Core.Entities.Tag { Name = tagDto.Name };
                        _context.Tags.Add(existingTag);
                        await _context.SaveChangesAsync();
                    }

                    if (!currentTags.Any(pt => pt.TagID == existingTag.TagID)) {
                        _context.ProjectTags.Add(new ProjectTag {
                            ProjectID = projectID,
                            TagID = existingTag.TagID,
                            Project = project,
                            Tag = existingTag
                        });
                    }
                }

            }

            if (req.CustomMetadata != null) {
                var currentMetadata = project.ProjectMetadataFields.ToList();
                var reqMetadataNames = req.CustomMetadata
                                        .Select(cm => cm.FieldName.ToLower())
                                        .ToHashSet();

                // Remove metadata that are not in the request.
                foreach (var pm in currentMetadata) {
                    if (!reqMetadataNames.Contains(pm.FieldName.ToLower())) {
                        foreach (var asset in project.Assets) {
                            var am = asset.AssetMetadata.FirstOrDefault(a => a.FieldID == pm.FieldID);
                            if (am != null)
                                _context.AssetMetadata.Remove(am);
                        }
                        _context.ProjectMetadataFields.Remove(pm);
                    }
                }

                // Process each custom metadata from the request.
                foreach (var cm in req.CustomMetadata) {
                    // Check if the metadata already exists (update if found).
                    var existing = currentMetadata.FirstOrDefault(pm => pm.FieldName.ToLower() == cm.FieldName.ToLower());
                    if (existing != null) {
                        if (Enum.TryParse(cm.FieldType, true, out ProjectMetadataField.FieldDataType newFieldType)) {
                            if (existing.FieldType != newFieldType && existing.AssetMetadata.Any()) {
                                throw new InvalidOperationException("Cannot change the metadatafield type because it is currently in use by one or more assets.");
                            }
                            existing.IsEnabled = cm.IsEnabled;
                            existing.FieldType = newFieldType;
                        }
                    } else {
                        // Add new metadata field.
                        if (Enum.TryParse(cm.FieldType, true, out ProjectMetadataField.FieldDataType fieldType)) {
                            var newField = new ProjectMetadataField {
                                ProjectID = projectID,
                                FieldName = cm.FieldName,
                                FieldType = fieldType,
                                IsEnabled = cm.IsEnabled,
                                Project = project
                            };
                            _context.ProjectMetadataFields.Add(newField);
                            // Add related AssetMetadata for each asset.
                            foreach (var asset in project.Assets) {
                                _context.AssetMetadata.Add(new AssetMetadata {
                                    BlobID = asset.BlobID,
                                    FieldID = newField.FieldID,
                                    FieldValue = null,
                                    Asset = asset,
                                    ProjectMetadataField = newField
                                });
                            }
                            // Optionally update the local collection to avoid duplicates.
                            currentMetadata.Add(newField);
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();

            await _context.SaveChangesAsync();

            return new UpdateProjectRes {
                Success = true,
                Message = "Project updated successfully."
            };
            
        }

        public async Task<bool> CheckProjectAssetExistence(int projectID, string blobID, int userID)
        {
            try
            {
                using var _context = _contextFactory.CreateDbContext();

                return await _context.Projects.AnyAsync(project =>
                    project.ProjectID == projectID &&
                    project.ProjectMemberships.Any(membership => membership.UserID == userID) &&
                    project.Assets.Any(asset => asset.BlobID == blobID)
                );
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}