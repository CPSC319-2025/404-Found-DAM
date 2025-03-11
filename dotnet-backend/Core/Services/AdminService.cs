using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;
using Infrastructure.Exceptions;

namespace Core.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _repository;
        public AdminService(IAdminRepository repository)
        {
            _repository = repository;
        }

        public async Task<ToggleMetadataStateRes> ToggleMetadataCategoryActivation(int projectID, int metadataFieldID, bool setEnabled)
        {
            try 
            {
                (bool isSuccessul, string metadataFieldName) = await _repository.ToggleMetadataCategoryActivationInDb(projectID, metadataFieldID, setEnabled);
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
                (User user, List<ProjectMembership> projectMemberships) = await _repository.GetRoleDetailsInDb(userId);
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
                DateTime timeUpdated = await _repository.ModifyRoleInDb(projectID, userID, roleChangeTo);
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
                List<MetadataField> addedMetadataFields = await _repository.AddMetaDataFieldsToProjectInDb(projectID, req);

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

        public async Task<List<CreateProjectsRes>> CreateProjects(List<CreateProjectsReq> req)
        {
            try 
            {
                List<Project> createdProjects = await _repository.CreateProjectsInDb(req);

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


