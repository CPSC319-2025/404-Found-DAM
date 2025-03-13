using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;
using Infrastructure.Exceptions;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace Infrastructure.DataAccess
{
    public class EFCoreAdminRepository : IAdminRepository
    {
        private IDbContextFactory<DAMDbContext> _contextFactory;
        public EFCoreAdminRepository(IDbContextFactory<DAMDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<(bool, string)> ToggleMetadataCategoryActivationInDb(int projectID, int metadataFieldID, bool setEnabled)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();
           
            // Check if project exists
            var project = await _context.Projects.FindAsync(projectID);

            if (project != null) 
            {
                // Access metadata and update the specific field
                var projectMetadataField = await _context.ProjectMetadataFields
                    .Include(pmf => pmf.MetadataField) // Eager loading
                    .FirstOrDefaultAsync(pmf => pmf.FieldID == metadataFieldID && pmf.FieldID == metadataFieldID);                
                    
                if (projectMetadataField != null) 
                {
                    projectMetadataField.IsEnabled = setEnabled;
                    await _context.SaveChangesAsync();
                    return (true, projectMetadataField.MetadataField.FieldName);
                } 
                else 
                {
                    throw new DataNotFoundException($"No such metadata field found for Project {projectID}.");
                }
            } 
            else 
            {
                throw new DataNotFoundException($"Project {projectID} not found.");
            }
        }

        public async Task<(User, List<ProjectMembership>)> GetRoleDetailsInDb(int userID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            // get User and associated ProjectMemberships
            var user = await _context.Users
                .Include(u => u.ProjectMemberships)
                .FirstOrDefaultAsync(u => u.UserID == userID);

            return user != null 
                ? (user, user.ProjectMemberships.ToList()) 
                : throw new DataNotFoundException("No user found.");
        }

        public async Task<DateTime> ModifyRoleInDb(int projectID, int userID, bool userToAdmin)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();
            
            // Get the ProjectMembership and update the user role if found
            var projectMembership = await _context.ProjectMemberships
                .FirstOrDefaultAsync(pm => pm.ProjectID == projectID && pm.UserID == userID);

            if (projectMembership != null)
            {
                projectMembership.UserRole = userToAdmin 
                    ? ProjectMembership.UserRoleType.Admin 
                    : ProjectMembership.UserRoleType.Regular;
                return DateTime.UtcNow;
            }
            else 
            {
                throw new DataNotFoundException("No record found");
            }
        }


        public async Task<List<MetadataField>> AddMetaDataFieldsToProjectInDb(int projectID, List<AddMetadataReq> req)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            // Get the project; 
            var project = await _context.Projects.FindAsync(projectID);

            if (project != null) 
            {
                // Create two lists for processing newly added metadatafields and projectMetadataFields
                List<MetadataField> metadataFieldsToAdd = new List<MetadataField>();
                List<ProjectMetadataField> projectMetadataFieldsToAdd = new List<ProjectMetadataField>();

                // project was found, so insert all new metadata fields to the metadatafield table
                foreach (AddMetadataReq amreq in req)
                {
                    if (Enum.TryParse(amreq.fieldType, true, out MetadataField.FieldDataType dataType))
                    {
                        MetadataField mf = new MetadataField { FieldName = amreq.fieldName, FieldType = dataType };
                        metadataFieldsToAdd.Add(mf); // Add to the list; don't save yet
                    } 
                    else 
                    {
                        throw new ArgumentException($"Invalid field type: {amreq.fieldType}");
                    }
                }

                // Add all MetadataField entities and save
                await _context.MetadataFields.AddRangeAsync(metadataFieldsToAdd);
                await _context.SaveChangesAsync();

                foreach (MetadataField mf in metadataFieldsToAdd)
                {
                    ProjectMetadataField pmf = new ProjectMetadataField
                    {
                        IsEnabled = false, // Explicitly set IsEnabled (default)
                        FieldValue = "", // Empty string 
                        
                        // Do NOT set projectID and fieldID. Below will let EF Core takes care of everything.
                        Project = project,
                        MetadataField = mf
                    };
                    projectMetadataFieldsToAdd.Add(pmf); // Add to the list; don't save yet
                }

                // Add all ProjectMetadataField entities and save
                await _context.ProjectMetadataFields.AddRangeAsync(projectMetadataFieldsToAdd);
                await _context.SaveChangesAsync();
                
                return metadataFieldsToAdd;
            }
            else 
            {
                throw new DataNotFoundException("Project not found");
            }
        }

        public async Task<List<Project>> CreateProjectsInDb(List<CreateProjectsReq> req, int creatorUserID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            User? creator = await _context.Users.FindAsync(creatorUserID);

            if (creator == null) 
            {
                throw new Exception("Failed to create project. User not found.");
            }

            if (!creator.IsSuperAdmin)
            {
                throw new UnauthorizedAccessException("Must be a super admin to create a project.");
            }  

            List<Project> projectList = new List<Project>(); // For storing created Projects
            List<Tag> tagList = new List<Tag>(); // For storing created Tags
            List<ProjectTag> projectTagList = new List<ProjectTag>(); // For storing created ProjectTags
            List<ProjectMembership> projectMembershipList = new List<ProjectMembership>(); // For storing project memberships
            
            using var transaction = await _context.Database.BeginTransactionAsync(); // To avoid partial data in database in case error occurs
            try 
            {
                foreach (CreateProjectsReq data in req) 
                {
                    Project newProject = new Project
                    {
                        Name = data.defaultMetadata.projectName,
                        Version = "0",
                        Location = data.defaultMetadata.location == null ? "" : data.defaultMetadata.location,
                        Description = data.defaultMetadata.description == null ? "" : data.defaultMetadata.description,
                        Active = data.defaultMetadata.active
                    };

                    projectList.Add(newProject);


                    ProjectMembership creatorMembership = new ProjectMembership
                    {
                        Project = newProject, // EF will assign ProjectID after saving
                        UserID = creatorUserID,
                        UserRole = ProjectMembership.UserRoleType.Admin,
                        User = creator
                    };
                    projectMembershipList.Add(creatorMembership);

                    // add any specified admins by the project creator to the project
                    if (data.admins != null && data.admins.Any()) {
                        foreach (var adminID in data.admins) {
                            if (adminID == creatorUserID) {
                                continue;
                            }
                            if (projectMembershipList.Any(pm => pm.UserID == adminID && pm.Project == newProject)) {
                                continue;
                            }
                            User? adminUser = await _context.Users.FindAsync(adminID);
                            if (adminUser == null) {
                                throw new Exception($"Admin user with ID {adminID} does not exist!");
                            }
                            ProjectMembership adminMembership = new ProjectMembership
                            {
                                Project = newProject,
                                UserID = adminID,
                                UserRole = ProjectMembership.UserRoleType.Admin,
                                User = adminUser
                            };
                            projectMembershipList.Add(adminMembership);
                        }
                    }
                    // add any specified regular users by the project creator to the project
                    if (data.users != null && data.users.Any()) {
                        foreach (var userID in data.users) {
                            if (data.admins != null && data.admins.Contains(userID)) {
                                continue; //skip, already added
                            }
                            if (projectMembershipList.Any(pm => pm.UserID == userID && pm.Project == newProject)) {
                                continue;
                            }
                            User? user = await _context.Users.FindAsync(userID);
                            if (user == null) {
                                throw new Exception($"User with ID {userID} not found");
                            }
                            ProjectMembership userMembership = new ProjectMembership
                            {
                                Project = newProject,
                                UserID = userID,
                                UserRole = ProjectMembership.UserRoleType.Regular,
                                User = user
                            };
                            projectMembershipList.Add(userMembership);
                        }
                    }
                    if (data.tags != null && data.tags.Any()) 
                    {
                        foreach (string tagName in data.tags) 
                        {
                            Tag newTag = new Tag{ Name = tagName };
                            tagList.Add(newTag);

                            ProjectTag newProjectTag = new ProjectTag
                            {
                                Project = newProject,
                                Tag = newTag
                            };     
                            projectTagList.Add(newProjectTag);                   
                        }
                    }
                }

                // Insert in batch
                await _context.AddRangeAsync(projectList);
                await _context.AddRangeAsync(tagList);
                await _context.AddRangeAsync(projectTagList);
                await _context.AddRangeAsync(projectMembershipList);
                await _context.SaveChangesAsync(); // Save change in the database
                
                await transaction.CommitAsync(); // Commit transaction for data persistence
                Console.WriteLine("Save to database");
                return projectList;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(); // Undo any changes made within a database transaction
                throw new Exception("Failed to create projects");
            }
        }
    }
}
