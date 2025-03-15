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
                            Console.WriteLine($"admin candidateID is {candidateID}");

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
                                         Console.WriteLine("candidate is a regular user of the project, but is put in the wrong list to be removed");
                                    }
                                }
                            } 
                        }

                        // Add regular users
                        foreach (int candidateID in req.removeFromRegulars)
                        {
                            Console.WriteLine($"regular candidateID is {candidateID}");

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
                                        Console.WriteLine("candidate is a regular user of the project, but is put in the wrong list to be removed");
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
                            Console.WriteLine($"admin candidateID is {candidateID}");

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
                            Console.WriteLine($"regular candidateID is {candidateID}");

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

                        await _context.AddRangeAsync(projectMemberships);
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

            using var transaction = await _context.Database.BeginTransactionAsync(); // To avoid artial data in database in case error occurs
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
                }

                // Insert and Save new projects first to allow for their projectIDs be generated
                await _context.AddRangeAsync(projectList);
                await _context.SaveChangesAsync(); // Save change in the database

                // Now create all relations

                if (req.Count != projectList.Count) 
                {
                    throw new Exception("new project count is different from input request.");
                }
        
                for (int i = 0; i < req.Count; i++)
                {
                    CreateProjectsReq data = req[i];
                    Project newProject = projectList[i];

                    Console.WriteLine($"new project ID is {newProject}");
                    ProjectMembership creatorMembership = new ProjectMembership
                    {
                        Project = newProject, // EF will assign ProjectID after saving
                        UserID = creatorUserID,
                        UserRole = ProjectMembership.UserRoleType.Admin,
                        User = creator
                    };
                    projectMembershipList.Add(creatorMembership);

                    Console.WriteLine("added creatorMembership");

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

                            Console.WriteLine($"newProjectID is {newProject.ProjectID}");
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
                Console.WriteLine("start inserting");
                await _context.AddRangeAsync(tagList);
                await _context.AddRangeAsync(projectTagList);
                await _context.AddRangeAsync(projectMembershipList);
                await _context.AddRangeAsync(projectMembershipList);
                await _context.SaveChangesAsync(); // Save change in the database
                
                await transaction.CommitAsync(); // Commit transaction for data persistence
                Console.WriteLine("done");
                return projectList;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await transaction.RollbackAsync(); // Undo any changes made within a database transaction
                throw new Exception("Failed to create projects");
            }
        }
    }
}
