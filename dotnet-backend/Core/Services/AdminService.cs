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


namespace Core.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;

        private readonly IProjectService _projectService;
        private readonly IProjectRepository _projRepository;

        public AdminService(IAdminRepository adminRepository, IProjectService projectService, IProjectRepository projRepository)
        {
            _adminRepository = adminRepository;
            _projectService = projectService;
            _projRepository = projRepository;
        }

        /*
            ImportProject Assumes: see Note of ImportProject in AdminController
        */
        public async Task<ImportProjectRes> ImportProject(FileStream stream)
        {
            // Create project and establish collection relations
            // Create Users and establish ProjectMemberships
            using var workbook = new XLWorkbook(stream); // Create an Excel workbook instance
            var wsProject = workbook.Worksheet(1); 
            var wsMembers = workbook.Worksheet(2);
            var nonEmptyProjectSheetRows = wsProject.RowsUsed(); 
            var nonEmptyMemberSheetRows = wsMembers.RowsUsed(); 
            
            List<Project> projectList = new List<Project>();
            List<ProjectTag> projectTagList = new List<ProjectTag>();
            List<Tag> tagList = new List<Tag>();
            List<Asset> assetList = new List<Asset>();
            List<AssetTag> assetTagList = new List<AssetTag>();
            List<User> userList = new List<User>();
            List<ProjectMembership> projectMembershipList = new List<ProjectMembership>();

            // TODO: May add metadatafield and AssetMetadata if needed later. 

            int projectSheetRowCount = 1;

            foreach (var projectSheetRow in nonEmptyProjectSheetRows) 
            {
                if (projectSheetRowCount == 2) 
                {
                    // Create project
                    Project p = new Project 
                    {
                        Name = projectSheetRow.Cell(2).GetValue<string>(),
                        Version = projectSheetRow.Cell(3).GetValue<string>(),
                        Location = projectSheetRow.Cell(4).GetValue<string>(),
                        Description = projectSheetRow.Cell(5).GetValue<string>(),
                        CreationTime =  DateTimeOffset.Parse(projectSheetRow.Cell(6).GetValue<string>()).UtcDateTime,
                        Active = projectSheetRow.Cell(7).GetValue<bool>(),
                        ArchivedAt = projectSheetRow.Cell(7).GetValue<bool>() ? null : DateTimeOffset.Parse(projectSheetRow.Cell(8).GetValue<string>()).UtcDateTime
                    };
                    projectList.Add(p);

                    // Create ProjectTags and Tags
                    string extractedProjectTagString = projectSheetRow.Cell(9).GetValue<string>();
                    List<string> tagNames = extractedProjectTagString.Split(',').Select(tag => tag.Trim()).ToList();
                    foreach (string tagName in tagNames)
                    {
                        Tag t = new Tag { Name = tagName };
                        ProjectTag pt = new ProjectTag 
                        {
                            Project = p,
                            Tag = t
                        };
                        projectTagList.Add(pt);
                        tagList.Add(t);
                    }
                }
                else if (projectSheetRowCount >= 4) 
                {
                    // Create assets (asasume they exist in blob already for now!)
                    Asset a = new Asset
                    {
                        FileName = projectSheetRow.Cell(2).GetValue<string>(),
                        MimeType = projectSheetRow.Cell(3).GetValue<string>(),
                        FileSizeInKB = projectSheetRow.Cell(4).GetValue<double>(),
                        LastUpdated =  DateTimeOffset.Parse(projectSheetRow.Cell(5).GetValue<string>()).UtcDateTime,
                        assetState = Asset.AssetStateType.SubmittedToProject
                    };

                    // Create AssetTags and Tags
                    string extractedAssetTagString = projectSheetRow.Cell(6).GetValue<string>();
                    List<string> tagNames = extractedAssetTagString.Split(',').Select(tag => tag.Trim()).ToList();
                    foreach (string tagName in tagNames)
                    {
                        Tag t = new Tag { Name = tagName };
                        AssetTag at = new AssetTag 
                        {
                            Asset = a,
                            Tag = t
                        };
                        assetTagList.Add(at);
                        tagList.Add(t);
                    }

                    // TODO: May create metadatafield and AssetMetadata if needed later.
                }
                projectSheetRowCount++;
            }

            // Create users & relations
            int memberSheetRowCount = 1;
            foreach (var memberSheetRow in nonEmptyMemberSheetRows)
            {
                if (memberSheetRowCount >= 2) {
                    User u = new User 
                    {
                        Name = memberSheetRow.Cell(2).GetValue<string>(),
                        Email = memberSheetRow.Cell(3).GetValue<string>(),
                        IsSuperAdmin = memberSheetRow.Cell(4).GetValue<bool>(),
                        LastUpdated = DateTimeOffset.Parse(memberSheetRow.Cell(5).GetValue<string>()).UtcDateTime,
                    };
                    userList.Add(u);

                    // Create ProjectMembership
                    ProjectMembership pm = new ProjectMembership {
                        Project = projectList[0],
                        User = u,
                        UserRole = memberSheetRow.Cell(6).GetValue<string>() == "admin" ? ProjectMembership.UserRoleType.Admin : ProjectMembership.UserRoleType.Regular
                    };
                    projectMembershipList.Add(pm);
                }
                memberSheetRowCount++;
            } 

            int importedProjectID = await _adminRepository.ImportProjectInDB
                (
                    projectList,
                    projectTagList,
                    tagList,
                    assetList,
                    assetTagList,
                    userList,
                    projectMembershipList
                );

            GetProjectRes importedProjectInfo = await _projectService.GetProject(importedProjectID);
            ImportProjectRes res = new ImportProjectRes { importedDate = DateTime.UtcNow, importedProjectInfo = importedProjectInfo };
            return res;
        }

        public async Task<(string, byte[])> ExportProject(int projectID, int requesterID)
        {
            try
            {
                // Fetch project and assets
                Project project = await _projRepository.GetProjectInDb(projectID);

                // Check if this requester is an admin who owns the project:
                bool isRequesterProjectAdmin = project.ProjectMemberships.Any(pm => pm.UserID == requesterID && pm.UserRole == ProjectMembership.UserRoleType.Admin);

                if (isRequesterProjectAdmin)
                {
                    List<Asset> assets = await _projRepository.GetProjectAssetsInDb(projectID);
                    (string fileName, byte[] excelByteArray) = AdminServiceHelpers.GenerateProjectExportExcel(project, assets);
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
                List<MetadataField> addedMetadataFields = await _adminRepository.AddMetaDataFieldsToProjectInDb(projectID, req);

                List<AddMetadataRes> result = new List<AddMetadataRes>();

                foreach (MetadataField mf in addedMetadataFields)
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
    }
}


