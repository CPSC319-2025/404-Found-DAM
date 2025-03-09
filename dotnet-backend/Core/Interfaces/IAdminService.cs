using Core.Dtos;

namespace Core.Interfaces
{
    public interface IAdminService
    {
        Task<ToggleMetadataStateRes> ToggleMetadataCategoryActivation(int projectID, int metadataFieldIDd, bool setEnabled);
        Task<RoleDetailsRes> GetRoleDetails(int userID);
        Task<ModifyRoleRes> ModifyRole(int projectID, int userID, bool userToAdmin);
        Task<AddMetadataRes> AddMetaDataToProject(int projectID, string fieldName, string fieldType);
    }
}
