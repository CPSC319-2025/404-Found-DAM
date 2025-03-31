using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;
using Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore.Query;
using Core.Services.Utils;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using System.IO.Compression;



namespace Core.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;

        private readonly IProjectService _projectService;
        private readonly IProjectRepository _projRepository;
        private readonly IUserRepository _userRepository;

        public AdminService(IAdminRepository adminRepository, IProjectService projectService, IProjectRepository projRepository, IUserRepository userRepository)
        {
            _adminRepository = adminRepository;
            _projectService = projectService;
            _projRepository = projRepository;
            _userRepository = userRepository;
        }

        /*
            ImportProject Assumes: see Note of ImportProject in AdminController
        */
        public async Task<ImportProjectRes> ImportProject(Stream stream)
        {
            using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);
            
            // Get the xlsx file 
            var xlsxEntry = archive.Entries
                .FirstOrDefault(entry => entry.FullName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));

            // // Get the collection of assets
            // var assetEntries = archive.Entries
            //     .Where(entry => entry.FullName.StartsWith("assets/"))
            //     .ToList();            

            // Create Project-relavant elements for inserting into DB
            if (xlsxEntry != null) 
            {
                (
                    List<Project> projectList,
                    List<ProjectTag> projectTagList,
                    List<Tag> tagList,
                    List<ImportUserProfile> importUserProfileList
                ) = AdminServiceHelpers.CreateProjectForImport(xlsxEntry);

                (int importedProjectID, List<UserCustomInfo> nonExistentUsers) 
                    = await _adminRepository.ImportProjectInDB(projectList, projectTagList, tagList, importUserProfileList);

               
                GetProjectRes importedProjectInfo = await _projectService.GetProject(importedProjectID);
                ImportProjectRes res = new ImportProjectRes { importedDate = DateTime.UtcNow, importedProjectInfo = importedProjectInfo, nonExistentUsers = nonExistentUsers };
                return res;
            }
            else 
            {
                throw new InvalidDataException("Empty zip content");
            }
        }

        public async Task<(string, byte[])> ExportProject(int projectID, int requesterID)
        {
            try
            {
                // Fetch project and assets
                Project project = await _projRepository.GetProjectInDb(projectID);
                
                User requester = await _userRepository.GetUserById(requesterID);

                // Check if this requester is an admin who owns the project:
                bool isRequesterProjectAdmin = project.ProjectMemberships.Any(pm => pm.UserID == requesterID && pm.UserRole == ProjectMembership.UserRoleType.Admin);

                bool isRequesterSuperAdmin = requester.IsSuperAdmin;

                if (isRequesterProjectAdmin || isRequesterSuperAdmin)
                {
                    List<Asset> assets = await _projRepository.GetProjectAndAssetsInDb(projectID);
                    (string fileName, byte[] excelByteArray) = AdminServiceHelpers.GenerateProjectExportExcel(project, assets);
                    

                    // TODO: get the asset files to be retunr together
                    return (fileName, excelByteArray);
                }
                else
                {
                    throw new DataNotFoundException($"No project found for user {requesterID}");
                }
            }
             catch (DataNotFoundException)
            {
                throw;
            }           
            catch (Exception)
            {
                throw;
            }
        }



        public async Task<DeleteUsersFromProjectRes> DeleteUsersFromProject(int reqeusterID, int projectID, DeleteUsersFromProjectReq req)
        {
            try 
            {
                (HashSet<int> removedAdmins, HashSet<int> removedRegularUsers, HashSet<int> failedToRemoveFromAdmins, HashSet<int> failedToRemoveFromRegulars) = 
                    await _adminRepository.DeleteUsersFromProjectInDb(reqeusterID, projectID, req);
                
                DeleteUsersFromProjectRes res = new DeleteUsersFromProjectRes
                { 
                    projectID = projectID,
                    removedAdmins = removedAdmins,
                    removedRegularUsers = removedRegularUsers,
                    failedToRemoveFromAdmins = failedToRemoveFromAdmins,
                    failedToRemoveFromRegulars = failedToRemoveFromRegulars
                };

                return res;
            }
            catch (DataNotFoundException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<AddUsersToProjectRes> AddUsersToProject(int reqeusterID, int projectID, AddUsersToProjectReq req)
        {
            try 
            {
                (HashSet<int> newAdmins, HashSet<int> newRegularUsers, HashSet<int> failedToAddAsAdmin, HashSet<int> failedToAddAsRegular) = 
                    await _adminRepository.AddUsersToProjectInDb(reqeusterID, projectID, req);
                
                AddUsersToProjectRes res = new AddUsersToProjectRes
                { 
                    projectID = projectID,
                    newAdmins = newAdmins,
                    newRegularUsers = newRegularUsers,
                    failedToAddAsAdmin = failedToAddAsAdmin,
                    failedToAddAsRegular = failedToAddAsRegular
                };

                return res;
 
            }
            catch (DataNotFoundException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ToggleMetadataStateRes> ToggleMetadataCategoryActivation(int projectID, int metadataFieldID, bool setEnabled)
        {
            try 
            {
                (bool isSuccessul, string metadataFieldName) = await _adminRepository.ToggleMetadataCategoryActivationInDb(projectID, metadataFieldID, setEnabled);
                if (isSuccessul)
                {
                    ToggleMetadataStateRes result = new ToggleMetadataStateRes
                    {
                        fieldID = metadataFieldID,
                        enabled = setEnabled,
                        message = setEnabled 
                            ? $"Metadata field {metadataFieldName} is now enabled." 
                            : $"Metadata field {metadataFieldName} is now disabled."
                    };
                    return result;
                }
                else 
                {
                    throw new Exception("Failed to add assets to project in database.");
                }
            }
            catch (DataNotFoundException) 
            {
                throw;
            }
            catch (Exception) 
            {
                throw;
            }
        }

        public async Task<RoleDetailsRes> GetRoleDetails(int userId) 
        {
            try 
            {
                (User user, List<ProjectMembership> projectMemberships) = await _adminRepository.GetRoleDetailsInDb(userId);
                HashSet<string> userRoles = projectMemberships
                    .Select(pm => pm.UserRole == ProjectMembership.UserRoleType.Regular ? "user" : "admin")
                    .ToHashSet();

                RoleDetailsRes result = new RoleDetailsRes
                {
                    userID = user.UserID,
                    name = user.Name,
                    email = user.Email,
                    roles = userRoles,
                    lastUpdated = user.LastUpdated
                };
                return result;
            }
            catch (DataNotFoundException) 
            {
                throw;
            }
            catch (Exception) 
            {
                throw;
            }
        }

        public async Task<ModifyRoleRes> ModifyRole(int projectID, int userID, string normalizedRoleString)
        {
            try 
            {
                ProjectMembership.UserRoleType roleChangeTo = normalizedRoleString == "admin" ? ProjectMembership.UserRoleType.Admin : ProjectMembership.UserRoleType.Regular;
                DateTime timeUpdated = await _adminRepository.ModifyRoleInDb(projectID, userID, roleChangeTo);
                return new ModifyRoleRes
                {
                        projectID = projectID,
                        userID = userID,
                        updatedRole = normalizedRoleString,
                        updatedAt = timeUpdated
                };
            }
            catch (DataNotFoundException) 
            {
                throw;
            }
            catch (Exception) 
            {
                throw;
            }
        }

        public async Task<List<AddMetadataRes>> AddMetaDataFieldsToProject(int projectID, List<AddMetadataReq> req)
        {
            try 
            {
                List<ProjectMetadataField> addedMetadataFields = await _adminRepository.AddMetaDataFieldsToProjectInDb(projectID, req);

                List<AddMetadataRes> result = new List<AddMetadataRes>();

                foreach (ProjectMetadataField mf in addedMetadataFields)
                {
                    AddMetadataRes res = new AddMetadataRes
                    {
                        fieldID = mf.FieldID,
                        message = $"Medatada field {mf.FieldName} created; field disabled by default"
                    };
                    result.Add(res);
                }
                return result;
            }
            catch (ArgumentException) 
            {
                throw;
            }
            catch (DataNotFoundException) 
            {
                throw;
            }
            catch (Exception) 
            {
                throw;
            }
        }

        public async Task<List<CreateProjectsRes>> CreateProjects(List<CreateProjectsReq> req, int userID)
        {
            try 
            {
                List<Project> createdProjects = await _adminRepository.CreateProjectsInDb(req, userID);

                List<CreateProjectsRes> res = new List<CreateProjectsRes>();

                foreach (Project createdProject in createdProjects) 
                {
                    CreateProjectsRes r = new CreateProjectsRes
                    {
                        createdProjectID = createdProject.ProjectID
                    };

                    res.Add(r);
                }
                return res;
            }
            catch (Exception) 
            {
                throw;
            }
        }

        public async Task<GetAllUsersRes> GetAllUsers(int userID)
        {
            var users = await _adminRepository.GetAllUsers();

            var userDtos = users?.Select(u => new UserDto
            {
                UserID = u.UserID,
                Name = u.Name,
                Email = u.Email
            }).ToList() ?? [];

            return new GetAllUsersRes
            { 
                UserCount = userDtos.Count, // 0 if userDtos is empty
                Users = userDtos // [] if userDtos is empty
            };
        }
    }
}


