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
using System.Text.Json;

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

        public async Task<(List<string>, List<string>)> AssociateAssetsWithProjectInDb(int projectID, List<string> blobIDs, int submitterID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            List<string> successfulAssociations = new List<string>();

            // check if project exists
            var isProjectFound = await _context.Projects.AnyAsync(p => p.ProjectID == projectID);
            if (!isProjectFound)
            {
                throw new DataNotFoundException($"Project {projectID} not found");
            }
            // check if submitter is member of project
            var isSubmitterMember = await _context.ProjectMemberships.AnyAsync(pm => pm.ProjectID == projectID && pm.UserID == submitterID);
            if (!isSubmitterMember)
            {
                throw new DataNotFoundException($"User {submitterID} is not a member of project {projectID}");
            }
            var assetsToBeAssociated = await _context.Assets
            .Where(a => blobIDs.Contains(a.BlobID))
            .ToListAsync();

            // check if there are any assets to be associated
            if (assetsToBeAssociated == null || assetsToBeAssociated.Count == 0)
            {
                return (successfulAssociations, blobIDs);
            }
            foreach (Asset a in assetsToBeAssociated)
            {
                // Guard: Only allow association if:
                // 1) The asset is in state "Uploaded to palette" (enum value 0), and
                // 2) The asset was uploaded by the submitter
                if (a.assetState != Asset.AssetStateType.UploadedToPalette || a.UserID != submitterID)
                {
                    // skip asset
                    continue;
                }

                a.ProjectID = projectID;
                a.LastUpdated = DateTime.UtcNow;
                successfulAssociations.Add(a.BlobID);
            } 
            await _context.SaveChangesAsync();

            return (successfulAssociations, blobIDs.Except(successfulAssociations).ToList());   

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

        public async Task<(List<Project>, List<User>, List<ProjectMembership>)> GetAllProjectsInDb()
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            var projects = await _context.Projects
                .Include(p => p.ProjectMemberships)
                .Include(p => p.Assets.Where(a => a.assetState == Asset.AssetStateType.SubmittedToProject))
                .AsNoTracking()
                .ToListAsync();

            HashSet<int> userIDSet = projects
                .SelectMany(p => p.ProjectMemberships)
                .Select(pm => pm.UserID)
                .ToHashSet();

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => userIDSet.Contains(u.UserID))
                .ToListAsync();

            var projectMemberships = projects.SelectMany(p => p.ProjectMemberships).ToList();

            return (projects, users, projectMemberships);
        }

        // Get ALL assets of a project from database
        public async Task<List<Asset>> GetProjectAndAssetsInDb(int projectID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            var project = await _context.Projects
                .Include(p => p.ProjectMetadataFields)
                .Include(p => p.Assets)
                    .ThenInclude(a => a.AssetTags)
                        .ThenInclude(at => at.Tag)
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

        public async Task<(List<Asset>, int, List<string>)> GetPaginatedProjectAssetsInDb(GetPaginatedProjectAssetsReq req, int offset, int requesterID)
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

                    if (!string.IsNullOrEmpty(req.tagName))
                    {
                        query = query.Where(a => a.AssetTags.Any(at => at.Tag.Name == req.tagName));
                    }

                    if (req.fromDate.HasValue)
                    {
                        DateTime utcFromDate = req.fromDate.Value.ToUniversalTime();
                        query = query.Where(a => a.LastUpdated >= utcFromDate);
                    }

                    if (req.toDate.HasValue)
                    {
                        DateTime utcToDate = req.toDate.Value.ToUniversalTime();
                        query = query.Where(a => a.LastUpdated <= utcToDate);
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

                    // Get asset blobSASUrl
                    List<(string, string)> assetIdNameTuples = assets.Select(a => (a.BlobID, a.FileName)).ToList();
                    string containerName = "project-" + req.projectID.ToString() + "-assets";
                    List<string> assetBlobSASUrlList = await _blobStorageService.DownloadAsync(containerName, assetIdNameTuples);

                    
                    return (assets, totalFilteredAssetCount, assetBlobSASUrlList);
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
            
            if (req.Tags != null) 
            {
                // Get current tag associations
                var currentTags = project.ProjectTags.ToList();
                //build a set of requested tag names
                var reqTagNames = req.Tags.Select(t => t.Name.ToLower()).ToHashSet();

                //remove associations for tags that are not in the request.
                foreach (var pt in currentTags) {
                    if (!reqTagNames.Contains(pt.Tag.Name.ToLower())) {
                        _context.ProjectTags.Remove(pt);
                        //remove asset tag associations for this project
                        foreach (var asset in project.Assets) {
                            var assetTag = asset.AssetTags.FirstOrDefault(at => at.TagID == pt.TagID);
                            if (assetTag != null)
                                _context.AssetTags.Remove(assetTag);
                        }
                    }
                }

                //process each tag in the request.
                foreach (var tagDto in req.Tags) {
                    //look up the tag in the Tags table 
                    var existingTag = await _context.Tags
                        .FirstOrDefaultAsync(t => t.Name.ToLower() == tagDto.Name.ToLower());
                    if (existingTag != null) {
                        //if there is not already an association add it
                        if (!currentTags.Any(pt => pt.TagID == existingTag.TagID)) {
                            _context.ProjectTags.Add(new ProjectTag {
                                ProjectID = projectID,
                                TagID = existingTag.TagID,
                                Project = project,
                                Tag = existingTag
                            });
                        }
                    }
                    // If the tag doesn't exist skip it (unlikely to happen since we are selecting from tags table)
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
                    if (req.CustomMetadata.Count(x => string.Equals(x.FieldName, cm.FieldName, StringComparison.OrdinalIgnoreCase)) > 1)
                    {
                        throw new InvalidOperationException($"Cannot have duplicate metadata field name: '{cm.FieldName}'.");
                    }
                    
                    var existing = currentMetadata.FirstOrDefault(pm => 
                        string.Equals(pm.FieldName, cm.FieldName, StringComparison.OrdinalIgnoreCase));
                    
                    if (existing != null)
                    {
                        if (Enum.TryParse(cm.FieldType, true, out ProjectMetadataField.FieldDataType newFieldType))
                        {
                            bool isFieldInUse = _context.AssetMetadata.Any(am => am.FieldID == existing.FieldID);

                            if (existing.FieldType != newFieldType && isFieldInUse)
                            {
                                throw new InvalidOperationException("Cannot change the metadata field type because it is currently in use by one or more assets.");
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
        
        public async Task<List<Project>> GetProjectsForUserInDb(int userId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Projects
                .Include(p => p.ProjectMemberships)
                .Include(p => p.ProjectTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.ProjectMetadataFields)
                .Where(p => p.ProjectMemberships.Any(pm => pm.UserID == userId))
                .ToListAsync();
        }

        public async Task<string?> GetAssetNameByBlobIdAsync(string blobID)
        {
            using var _context = _contextFactory.CreateDbContext(); // first create context
            return await _context.Assets
                .Where(a => a.BlobID == blobID)
                .Select(a => a.FileName)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetTagNameByIdAsync(int tagId)
        {
            using var context = _contextFactory.CreateDbContext();
            
            return await context.Tags
                .Where(t => t.TagID == tagId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetProjectNameByIdAsync(int projectId)
        {
            using var context = _contextFactory.CreateDbContext();
            
            return await context.Projects
                .Where(p => p.ProjectID == projectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetCustomMetadataNameByIdAsync(int fieldID) {
            using var context = _contextFactory.CreateDbContext();

            return await context.ProjectMetadataFields
                .Where(f => f.FieldID == fieldID)
                .Select(f => f.FieldName)
                .FirstOrDefaultAsync();
        }
        
        public async Task AddAssetTagAssociationAsync(string imageId, int tagId)
        {
            using var context = _contextFactory.CreateDbContext();

            // Check if the association already exists.
            bool exists = await context.AssetTags.AnyAsync(at => at.BlobID == imageId && at.TagID == tagId);
            if (!exists)
            {
                // Retrieve the required navigation properties.
                var asset = await context.Assets.FirstOrDefaultAsync(a => a.BlobID == imageId);
                var tag = await context.Tags.FirstOrDefaultAsync(t => t.TagID == tagId);

                if (asset == null || tag == null)
                {
                    throw new Exception("Either the asset or tag was not found.");
                }

                var assetTag = new AssetTag
                {
                    BlobID = imageId,
                    TagID = tagId,
                    Asset = asset, // required property
                    Tag = tag      // required property
                };

                await context.AssetTags.AddAsync(assetTag);
                await context.SaveChangesAsync();
            }
        }

        public async Task UpsertAssetMetadataAsync(string imageId, int fieldId, JsonElement fieldValueElement)
        {
            using var context = _contextFactory.CreateDbContext();

            string fieldValueString;
            // case: string
            if (fieldValueElement.ValueKind == JsonValueKind.String) {
                fieldValueString = fieldValueElement.GetString() ?? "";
            } else {
                // return raw text for numbers and bools (eg 100 or true)
                fieldValueString = fieldValueElement.GetRawText();
            }


            
            var assetMetadata = await context.AssetMetadata
                .FirstOrDefaultAsync(am => am.BlobID == imageId && am.FieldID == fieldId);

            if (assetMetadata != null)
            {
                // Update existing record.
                assetMetadata.FieldValue = fieldValueString;
            }
            else
            {
                // Retrieve the required navigation properties.
                var asset = await context.Assets.FirstOrDefaultAsync(a => a.BlobID == imageId);
                var metadataField = await context.ProjectMetadataFields.FirstOrDefaultAsync(f => f.FieldID == fieldId);

                if (asset == null || metadataField == null)
                {
                    throw new Exception("Either the asset or metadata field was not found.");
                }

                assetMetadata = new AssetMetadata
                {
                    BlobID = imageId,
                    FieldID = fieldId,
                    FieldValue = fieldValueString,
                    Asset = asset, // required navigation property
                    ProjectMetadataField = metadataField // required navigation property
                };

                await context.AssetMetadata.AddAsync(assetMetadata);
            }
            await context.SaveChangesAsync();
        }
    }
}