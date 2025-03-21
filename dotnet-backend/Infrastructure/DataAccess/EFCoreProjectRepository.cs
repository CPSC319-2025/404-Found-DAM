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

                    return (assets,totalFilteredAssetCount);
                }
            }
            else 
            {
                throw new DataNotFoundException("Requester not a member of the project.");
            }
        }

        public async Task<UpdateProjectRes> UpdateProjectInDb(int projectID, UpdateProjectReq req) {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            var project = await _context.Projects
                .Include(p => p.ProjectMemberships)
                .Include(p => p.ProjectTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.ProjectMetadataFields)
                    .ThenInclude(pm => pm.MetadataField)
                .FirstOrDefaultAsync(p => p.ProjectID == projectID);
            
            if (project == null) {
                throw new DataNotFoundException($"Project with ID {projectID} not found.");
            }

            if (!string.IsNullOrEmpty(req.Location)) {
                project.Location = req.Location;
            }

            if (req.Memberships != null) {
                var currentMemberships = project.ProjectMemberships.ToList();
                var reqUserIds = req.Memberships.Select(m => m.UserID).ToHashSet();

                foreach (var membership in currentMemberships) {
                    if (!reqUserIds.Contains(membership.UserID)) {
                        _context.ProjectMemberships.Remove(membership);
                    }
                }

                foreach (var membershipDto in req.Memberships) {
                    var existingMembership = await _context.ProjectMemberships.FirstOrDefaultAsync(m => m.ProjectID == projectID && m.UserID == membershipDto.UserID);
                    
                    if (existingMembership != null) {
                        if (!string.Equals(existingMembership!.UserRole.ToString(), membershipDto.Role, StringComparison.OrdinalIgnoreCase))
                        {
                            if (Enum.TryParse<ProjectMembership.UserRoleType>(membershipDto.Role, true, out var Role))
                            {
                                existingMembership.UserRole = Role;
                            }
                        }
                    }
                    else {
                        if (Enum.TryParse<ProjectMembership.UserRoleType>(membershipDto.Role, true, out var role))
                        {
                            var user = await _context.Users.FindAsync(membershipDto.UserID);
                            if (user == null) {
                                  throw new DataNotFoundException($"User with ID {membershipDto.UserID} not found.");
                            }
                            var newMembership = new ProjectMembership {
                                ProjectID = projectID,
                                UserID = membershipDto.UserID,
                                UserRole = role,
                                Project = project,
                                User = user
                            };
                            _context.ProjectMemberships.Add(newMembership);
                        }
                    }
                }
            }


            // handle project tag updates

            if (req.Tags != null) {
                var currentProjectTags = project.ProjectTags.ToList();
                var reqTagIds = req.Tags.Where(t => t.TagID.HasValue).Select(t => t.TagID.Value).ToHashSet();

                // Remove the tags that aren't included in the req
                foreach (var projectTag in currentProjectTags)
                {
                    if (!reqTagIds.Contains(projectTag.TagID))
                    {
                        _context.ProjectTags.Remove(projectTag);
                    }
                }

                // process the tags in the request
                foreach (var tagDto in req.Tags)
                {
                    if (tagDto.TagID.HasValue)
                    {
                        // If not already associated, add the tag.
                        if (!currentProjectTags.Any(pt => pt.TagID == tagDto.TagID.Value))
                        {
                            var existingTag = await _context.Tags.FindAsync(tagDto.TagID.Value);
                            // If the tag is not found, you might create it or throw an exception.
                            if (existingTag == null)
                            {
                                existingTag = new Core.Entities.Tag { TagID = tagDto.TagID.Value, Name = tagDto.Name };
                                _context.Tags.Add(existingTag);
                                await _context.SaveChangesAsync();
                            }

                            var newAssociation = new ProjectTag
                            {
                                ProjectID = projectID,
                                TagID = existingTag.TagID,
                                Project = project,
                                Tag = existingTag
                            };
                            _context.ProjectTags.Add(newAssociation);
                        }
                    }
                    else
                    {
                        // For new tags (without TagID), check by name.
                        var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagDto.Name);
                        if (existingTag == null)
                        {
                            existingTag = new Core.Entities.Tag { Name = tagDto.Name };
                            _context.Tags.Add(existingTag);
                            await _context.SaveChangesAsync();
                        }

                        if (!currentProjectTags.Any(pt => pt.TagID == existingTag.TagID))
                        {
                            var newAssociation = new ProjectTag
                            {
                                ProjectID = projectID,
                                TagID = existingTag.TagID,
                                Project = project,
                                Tag = existingTag
                            };
                            _context.ProjectTags.Add(newAssociation);
                        }
                    }
                }
            }

            if (req.CustomMetadata != null) {
                var currentMetadata = project.ProjectMetadataFields.ToList();
                var reqMetadataIds = req.CustomMetadata.Where(cm => cm.FieldID.HasValue)
                                                       .Select(cm => cm.FieldID.Value)
                                                       .ToHashSet();
                
                // metadata entries that are not in the request but in the project metadata 
                foreach (var pm in currentMetadata) {

                    if (!reqMetadataIds.Contains(pm.MetadataField.FieldID))
                    {
                        _context.ProjectMetadataFields.Remove(pm);
                    }

                }

                foreach (var cm in req.CustomMetadata) {

                    if (cm.FieldID.HasValue) {

                        // update existing metadata
                        var existingPm = currentMetadata.FirstOrDefault(pm => pm.MetadataField.FieldID == cm.FieldID.Value);
                        if (existingPm != null)
                        {
                            existingPm.FieldValue = cm.FieldValue;
                            existingPm.IsEnabled = cm.IsEnabled;
                            existingPm.MetadataField.FieldName = cm.FieldName;
                            if (Enum.TryParse<MetadataField.FieldDataType>(cm.FieldType, true, out var fieldType))
                            {
                                existingPm.MetadataField.FieldType = fieldType;
                            }
                        }
                    } else {
                         if (Enum.TryParse<MetadataField.FieldDataType>(cm.FieldType, true, out var fieldType)) {
                            
                            var newField = new MetadataField {
                                FieldName = cm.FieldName,
                                FieldType = fieldType
                            };

                            _context.MetadataFields.Add(newField);

                            await _context.SaveChangesAsync();

                            var newProjectMetadata = new ProjectMetadataField
                            {
                                ProjectID = projectID,
                                FieldID = newField.FieldID,
                                FieldValue = cm.FieldValue,
                                IsEnabled = cm.IsEnabled,
                                Project = project,
                                MetadataField = newField
                            };

                            _context.ProjectMetadataFields.Add(newProjectMetadata);
                         }
                    }
                }

            }

            await _context.SaveChangesAsync();

            return new UpdateProjectRes
            {
                Success = true,
                Message = "Project updated successfully."
            };

        }
        
    }
}