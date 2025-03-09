using Core.Entities;
using System;

namespace Core.Interfaces
{
    public interface IAdminRepository
    {
        // Suffixing InDb to differentiate from service operations.  
        Task<(bool, string)> ToggleMetadataCategoryActivationInDb(int projectID, int metadataFieldID, bool setEnabled);
        Task<(User, List<ProjectMembership>)> GetRoleDetailsInDb(int userID);
        Task<DateTime> ModifyRoleInDb(int projectID, int metadataFieldID, bool userToAdmin);

        Task<int> AddMetaDataToProjectInDb(int projectID, string fieldName, string fieldType);
    }
}
