using DataModel;
using System;

namespace Core.Interfaces
{
    public interface IAdminRepository
    {
        // Suffixing InDb to differentiate from service operations.  
        Task<(bool, string)> ToggleMetadataCategoryActivationInDb(int projectId, int metadataFieldId, bool setEnabled);
        Task<(User, List<string>)> GetRoleDetailsInDb(int userId);
    }
}
