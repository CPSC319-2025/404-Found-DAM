using Core.Dtos;

namespace Core.Interfaces
{
    public interface IAdminService
    {
        Task<ToggleMetadataStateRes> ToggleMetadataCategoryActivation(int projectId, int metadataFieldId, bool setEnabled);
        Task<RoleDetailsRes> GetRoleDetails(int userID);
    }
}
