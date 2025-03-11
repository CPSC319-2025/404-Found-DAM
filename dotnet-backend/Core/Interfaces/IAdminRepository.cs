using Core.Entities;
using Core.Dtos;

namespace Core.Interfaces
{
    public interface IAdminRepository
    {
        // Suffixing InDb to differentiate from service operations.  
        Task<(bool, string)> ToggleMetadataCategoryActivationInDb(int projectID, int metadataFieldID, bool setEnabled);
        Task<(User, List<ProjectMembership>)> GetRoleDetailsInDb(int userID);
        Task<DateTime> ModifyRoleInDb(int projectID, int metadataFieldID, bool userToAdmin);
        Task<List<MetadataField>> AddMetaDataFieldsToProjectInDb(int projectID, List<AddMetadataReq> req);
        Task<List<Project>> CreateProjectsInDb(List<CreateProjectsReq> req, int userID);
    }
}
