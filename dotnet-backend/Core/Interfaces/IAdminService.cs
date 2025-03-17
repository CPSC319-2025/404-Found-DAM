using Core.Dtos;

namespace Core.Interfaces
{
    public interface IAdminService
    {
        Task<ToggleMetadataStateRes> ToggleMetadataCategoryActivation(int projectID, int metadataFieldIDd, bool setEnabled);
        Task<RoleDetailsRes> GetRoleDetails(int userID);
        Task<ModifyRoleRes> ModifyRole(int projectID, int userID, bool userToAdmin);
        Task<List<AddMetadataRes>> AddMetaDataFieldsToProject(int projectID, List<AddMetadataReq> req);
        Task<List<CreateProjectsRes>> CreateProjects(List<CreateProjectsReq> req, int userID);
        Task<GetAllUsersRes> GetAllUsers(int userID);
    }
}
