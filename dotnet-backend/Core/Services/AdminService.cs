using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;

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
                            ? $"Metadata {metadataFieldName} is now enabled." 
                            : $"Metadata {metadataFieldName} is now disabled."
                    };
                    return result;
                }
                else 
                {
                    throw new Exception("Failed to add assets to project in database.");
                }
            }
            catch (Exception ex) 
            {
                throw;
            }
        }

        public async Task<RoleDetailsRes> GetRoleDetails(int userId) 
        {
            // TODO
            try 
            {
                (User user, List<string> userRoles) = await _repository.GetRoleDetailsInDb(userId);
                if (user == null) 
                {
                    return null;
                }
                else 
                {
                    RoleDetailsRes result = new RoleDetailsRes
                    {
                        userID = user.UserID,
                        name = user.Name,
                        email = user.Email,
                        roles = userRoles,
                    };
                    return result;
                }
            }
            catch (Exception ex) 
            {
                throw;
            }
        }
    }
}


