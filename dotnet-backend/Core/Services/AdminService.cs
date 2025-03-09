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
            //TODO
            try 
            {
                (bool isSuccessul, string metadataCatagory) = await _repository.ToggleMetadataCategoryActivationInDb(projectID, metadataFieldID, setEnabled);
                if (isSuccessul)
                {
                    ToggleMetadataStateRes result = new ToggleMetadataStateRes
                    {
                        fieldID = metadataFieldID,
                        enabled = setEnabled,
                        message = $"Metadata category {metadataCatagory} is now enabled."
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
                        lastModified = user.LastUpdated
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


