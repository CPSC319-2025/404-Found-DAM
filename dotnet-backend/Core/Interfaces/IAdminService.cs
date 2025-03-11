using Core.Dtos;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IAdminService
    {
        Task<ToggleMetadataStateRes> ToggleMetadataCategoryActivation(int projectID, int metadataFieldIDd, bool setEnabled);
        Task<RoleDetailsRes> GetRoleDetails(int userID);
        Task<ModifyRoleRes> ModifyRole(int projectID, int userID, string normalizedRoleString);
        Task<List<AddMetadataRes>> AddMetaDataFieldsToProject(int projectID, List<AddMetadataReq> req);
        Task<List<CreateProjectsRes>> CreateProjects(List<CreateProjectsReq> req);
    }
}
