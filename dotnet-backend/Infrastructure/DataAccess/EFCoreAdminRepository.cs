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

        public async Task<(int, List<UserCustomInfo>)> ImportProjectInDB
        (
            List<Project> projectList, 
            List<ProjectTag> projectTagList, 
            List<Tag> tagList, 
            List<ImportUserProfile> importUserProfileList
        )
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            List<(User, ProjectMembership.UserRoleType)> existentUserAndRoleList = new List<(User, ProjectMembership.UserRoleType)>();
            List<UserCustomInfo> nonExistentUsers = new List<UserCustomInfo>();

            // Get users if in the DB, or put into nonexistent list
            foreach (ImportUserProfile profile in importUserProfileList)
            {
                User? user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.UserID == profile.userID &&
                    u.Name == profile.userName &&
                    u.Email == profile.userEmail
                    // u.IsSuperAdmin == profile.userIsSuperAdmin // TODO: How to handle inconsistency here?
                );

                if (user != null)
                {
                    existentUserAndRoleList.Add((user, profile.userRole)); 
                }
                else 
                {
                    UserCustomInfo nonExistentUser = new UserCustomInfo
                    {
                        name = profile.userName,
                        email = profile.userEmail,
                        userID = profile.userID
                    };
                    nonExistentUsers.Add(nonExistentUser);
                }
            }
                
        
            // Create projectMembershipList
            List<ProjectMembership> projectMembershipList = new List<ProjectMembership>();
            foreach ((User, ProjectMembership.UserRoleType) ur in existentUserAndRoleList)
            {
                ProjectMembership pm = new ProjectMembership
                {
                    Project = projectList[0],
                    User = ur.Item1,
                    UserRole = ur.Item2
                };
                projectMembershipList.Add(pm);
            };

            // Store
            await _context.Tags.AddRangeAsync(tagList);
            await _context.Projects.AddRangeAsync(projectList);
            await _context.ProjectTags.AddRangeAsync(projectTagList);
            await _context.ProjectMemberships.AddRangeAsync(projectMembershipList);

            await _context.SaveChangesAsync();
            // Console.WriteLine("imported");

            // Retrieve new Project's ID
            List<int> newProjectIDs = projectList.Select(p => p.ProjectID).ToList();
            return (newProjectIDs[0], nonExistentUsers);
        }

        public async Task<(HashSet<int>, HashSet<int>, HashSet<int>, HashSet<int>)> DeleteUsersFromProjectInDb(int reqeusterID, int projectID, DeleteUsersFromProjectReq req)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();
            
            // Check if project exists and retrieve it if so.
            var project = await _context.Projects.FindAsync(projectID);
            if (project == null) 
            {
                throw new DataNotFoundException($"Project {projectID} not found.");
            }
            else 
            {
                // Check if the requester is an admin of the prokect w/o getting that admin
                bool isRequesterAdmin = await _context.ProjectMemberships
                    .AnyAsync(pm => pm.ProjectID == projectID && pm.UserID == reqeusterID && pm.UserRole == ProjectMembership.UserRoleType.Admin);
                
                if (isRequesterAdmin) 
                {
                    // Create empty sets for storing processed results
                    HashSet<int> removedAdmins = new HashSet<int>();
                    HashSet<int> removedRegularUsers = new HashSet<int>();
                    HashSet<int> failedToRemoveFromAdmins = new HashSet<int>();
                    HashSet<int> failedToRemoveFromRegulars = new HashSet<int>();
                    HashSet<ProjectMembership> pmTobeRemoved = new HashSet<ProjectMembership>();

                    using var transaction = await _context.Database.BeginTransactionAsync(); // To avoid partial data
                    try 
                    {
                        // remove admins first
                        foreach (int candidateID in req.removeFromAdmins)
                        {
                            // Check if the admin is non-existent
                            User? candidate = await _context.Users.FindAsync(candidateID);
                            if (candidate == null)
                            {
                                failedToRemoveFromAdmins.Add(candidateID); // Candidate does not exist in user pool.
                            } 
                            else
                            {
                                // Check if the to-be-removed admin is in the project
                                ProjectMembership? projectMembership = await _context.ProjectMemberships.FindAsync(projectID, candidateID);
                                if (projectMembership == null) // to-be-removed admin candidate is not in the project.
                                {
                                    failedToRemoveFromAdmins.Add(candidateID); 
                                } 
                                else // to-be-removed admin candidate is in the project.
                                {
                                    // Check if the to-be-removed admin candidate is an admin in the project.
                                    if (projectMembership.UserRole == ProjectMembership.UserRoleType.Admin) 
                                    {
                                        // Admin should not remove themselves
                                        if (candidateID == reqeusterID) 
                                        {
                                            failedToRemoveFromAdmins.Add(candidateID); // to-be-removed admin candidate is the requester themselves!
                                        }
                                        else if (candidate.IsSuperAdmin) 
                                        {
                                            // Admin should not remove super admin.
                                            failedToRemoveFromAdmins.Add(candidateID); // to-be-removed admin candidate is the requester themselves!
                                        }
                                        else 
                                        {
                                            // Check if this admin is already in the pmTobeRemoved set
                                            if (!pmTobeRemoved.Contains(projectMembership))
                                            {
                                                pmTobeRemoved.Add(projectMembership);
                                                removedAdmins.Add(candidateID);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // FE should guard this already, so in BE this case will be ignored for now.
                                        //  Console.WriteLine("candidate is a regular user of the project, but is put in the wrong list to be removed");
                                    }
                                }
                            } 
                        }

                        // Add regular users
                        foreach (int candidateID in req.removeFromRegulars)
                        {
                            // Check if the regular user is non-existent
                            User? candidate = await _context.Users.FindAsync(candidateID);
                            if (candidate == null)
                            {
                                failedToRemoveFromRegulars.Add(candidateID); // Candidate does not exist in user pool.
                            } 
                            else
                            {
                                // Check if the to-be-removed candidate is in the project
                                ProjectMembership? projectMembership = await _context.ProjectMemberships.FindAsync(projectID, candidateID);
                                if (projectMembership == null) // to-be-removed candidate is not in the project
                                {
                                    failedToRemoveFromRegulars.Add(candidateID);
                                } 
                                else 
                                {
                                    // Check if the to-be-removed candidate in the project is actually a regular user of this project
                                    if (projectMembership.UserRole == ProjectMembership.UserRoleType.Regular) 
                                    {
                                        pmTobeRemoved.Add(projectMembership);
                                        removedRegularUsers.Add(candidateID);
                                    }
                                    else 
                                    {
                                        // FE should guard this already, so in BE this case will be ignored for now.
                                        // Console.WriteLine("candidate is a regular user of the project, but is put in the wrong list to be removed");
                                    }
                                }
                            } 
                        }

                        _context.RemoveRange(pmTobeRemoved);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync(); // Commit transaction
                        return (removedAdmins, removedRegularUsers, failedToRemoveFromAdmins, failedToRemoveFromRegulars);
                    } 
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(); // Undo any changes made within a database transaction
                        throw new Exception($"Error adding users: {ex.Message}");
                    }
                }
                else 
                {
                    throw new DataNotFoundException($"Request issued from non-admin of project {projectID}.");
                }
            }
        }

        public async Task<(HashSet<int>, HashSet<int>, HashSet<int>, HashSet<int>)> AddUsersToProjectInDb(int reqeusterID, int projectID, AddUsersToProjectReq req)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();
            
            // Check if project exists and retrieve it if so.
            var project = await _context.Projects.FindAsync(projectID); // Lazy load (some navigation properties may NOT loaded immediately).

            
            if (project == null) 
            {
                throw new DataNotFoundException($"Project {projectID} not found.");
            }
            else 
            {
                // Check if the requester is an admin of the prokect w/o getting that admin
                bool isRequesterAdmin = await _context.ProjectMemberships
                    .AnyAsync(pm => pm.ProjectID == projectID && pm.UserID == reqeusterID && pm.UserRole == ProjectMembership.UserRoleType.Admin);
                
                if (isRequesterAdmin) 
                {
                    // Create empty sets for storing processed results
                    HashSet<int> newAdmins = new HashSet<int>();
                    HashSet<int> newRegularUsers = new HashSet<int>();
                    HashSet<int> failedToAddAsAdmin = new HashSet<int>();
                    HashSet<int> failedToAddAsRegular = new HashSet<int>();
                    HashSet<ProjectMembership> projectMemberships = new HashSet<ProjectMembership>();

                    using var transaction = await _context.Database.BeginTransactionAsync(); // To avoid partial data
                    try 
                    {
                        // Add admins first
                        foreach (int candidateID in req.addAsAdmin)
                        {
                            User? candidate = await _context.Users.FindAsync(candidateID);
                            if (candidate == null)
                            {
                                failedToAddAsAdmin.Add(candidateID); // Candidate does not exist in user pool.
                            } 
                            else
                            {
                                bool isAddedAlready = await _context.ProjectMemberships
                                    .AnyAsync(pm => pm.ProjectID == projectID && pm.UserID == candidateID);
                                if (isAddedAlready)
                                {
                                    failedToAddAsAdmin.Add(candidateID); // Candidate is in project aready.
                                }
                                else 
                                {
                                    ProjectMembership projectMembership = new ProjectMembership
                                    {
                                        Project = project,
                                        User = candidate,
                                        UserRole = ProjectMembership.UserRoleType.Admin
                                    };
                                    
                                    newAdmins.Add(candidateID);
                                    projectMemberships.Add(projectMembership);
                                }
                            } 
                        }

                        // Add regular users
                        foreach (int candidateID in req.addAsRegular)
                        {
                            User? candidate = await _context.Users.FindAsync(candidateID);
                            if (candidate == null)
                            {
                                failedToAddAsRegular.Add(candidateID); // Candidate does not exist in user pool.
                            } 
                            else
                            {
                                bool isAddedAlready = await _context.ProjectMemberships
                                    .AnyAsync(pm => pm.ProjectID == projectID && pm.UserID == candidateID);
                                if (isAddedAlready)
                                {
                                    failedToAddAsRegular.Add(candidateID); // Candidate is in project aready.
                                }
                                else 
                                {
                                    ProjectMembership projectMembership = new ProjectMembership
                                    {
                                        Project = project,
                                        User = candidate,
                                        UserRole = ProjectMembership.UserRoleType.Regular
                                    };

                                    newRegularUsers.Add(candidateID);
                                    projectMemberships.Add(projectMembership);
                                }
                            } 
                        }

                        await _context.ProjectMemberships.AddRangeAsync(projectMemberships);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync(); // Commit transaction
                    
                        return (newAdmins, newRegularUsers, failedToAddAsAdmin, failedToAddAsRegular);
                    } 
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(); // Undo any changes made within a database transaction
                        throw new Exception($"Error adding users: {ex.Message}");
                    }
                }
                else 
                {
                    throw new DataNotFoundException($"Request issued from non-admin of project {projectID}.");
                }
            }
        }

        public async Task<(bool, string)> ToggleMetadataCategoryActivationInDb(int projectID, int metadataFieldID, bool setEnabled)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            // Check if project exists
            var project = await _context.Projects.FindAsync(projectID);
            if (project == null)
            {
                throw new DataNotFoundException($"Project {projectID} not found.");
            }

            // Access metadata and update the specific field by checking both projectID and metadataFieldID.
            var projectMetadataField = await _context.ProjectMetadataFields
                .FirstOrDefaultAsync(pmf => pmf.FieldID == metadataFieldID && pmf.ProjectID == projectID);

            if (projectMetadataField == null)
            {
                throw new DataNotFoundException($"No such metadata field found for Project {projectID}.");
            }

            projectMetadataField.IsEnabled = setEnabled;
            await _context.SaveChangesAsync();

            // Return the updated status along with the FieldName directly from ProjectMetadataField.
            return (true, projectMetadataField.FieldName);
        }


        public async Task<(User, List<ProjectMembership>)> GetRoleDetailsInDb(int userID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            // get User and associated ProjectMemberships
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.ProjectMemberships)
                .FirstOrDefaultAsync(u => u.UserID == userID);

            return user != null 
                ? (user, user.ProjectMemberships.ToList()) 
                : throw new DataNotFoundException("No user found.");
        }

        public async Task<DateTime> ModifyRoleInDb(int projectID, int userID, ProjectMembership.UserRoleType roleChangeTo)
        {
            // TODO: Can't demote a SuperAdmin to regular user.

            using DAMDbContext _context = _contextFactory.CreateDbContext();
            
            // Get the ProjectMembership and update the user role if found
            var projectMembership = await _context.ProjectMemberships
                .FirstOrDefaultAsync(pm => pm.ProjectID == projectID && pm.UserID == userID);

            if (projectMembership != null)
            {
                projectMembership.UserRole = roleChangeTo;
                await _context.SaveChangesAsync();
                return DateTime.UtcNow;
            }
            else 
            {
                throw new DataNotFoundException("No record found");
            }
        }


        public async Task<List<ProjectMetadataField>> AddMetaDataFieldsToProjectInDb(int projectID, List<AddMetadataReq> req)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            // Get the project
            var project = await _context.Projects.FindAsync(projectID);
            if (project == null)
            {
                throw new DataNotFoundException("Project not found");
            }

            List<ProjectMetadataField> projectMetadataFieldsToAdd = new List<ProjectMetadataField>();

            foreach (AddMetadataReq amreq in req)
            {
                // Parse the field type to the enum defined in ProjectMetadataField.
                if (Enum.TryParse(amreq.fieldType, true, out ProjectMetadataField.FieldDataType dataType))
                {
                    // Create a new ProjectMetadataField directly.
                    ProjectMetadataField pmf = new ProjectMetadataField
                    {
                        FieldName = amreq.fieldName,   // Required property
                        FieldType = dataType,           // Required property
                        IsEnabled = false,              // Default value; adjust as needed
                        Project = project               // Associate this field with the project
                    };

                    projectMetadataFieldsToAdd.Add(pmf);
                }
                else
                {
                    throw new ArgumentException($"Invalid field type: {amreq.fieldType}");
                }
            }

            // Add all ProjectMetadataField entities and save
            await _context.ProjectMetadataFields.AddRangeAsync(projectMetadataFieldsToAdd);
            await _context.SaveChangesAsync();

            return projectMetadataFieldsToAdd;
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
                        CreationTime = DateTime.UtcNow,
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
                // Console.WriteLine("start inserting");
                await _context.Projects.AddRangeAsync(projectList);
                await _context.Tags.AddRangeAsync(tagList);
                await _context.ProjectTags.AddRangeAsync(projectTagList);
                await _context.ProjectMemberships.AddRangeAsync(projectMembershipList);
                await _context.SaveChangesAsync(); // Save change in the database
                
                await transaction.CommitAsync(); // Commit transaction for data persistence
                // Console.WriteLine("done");
                return projectList;
            }
            catch (Exception)
            {
                // Console.WriteLine(ex.Message);
                await transaction.RollbackAsync(); // Undo any changes made within a database transaction
                throw new Exception("Failed to create projects");
            }
        }

        public async Task<List<User>> GetAllUsers()
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();
            // Using AsNoTracking for read-only queries
            return await _context.Users.AsNoTracking().ToListAsync();
        }

    }
}
