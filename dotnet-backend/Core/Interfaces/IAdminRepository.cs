using Core.Entities;
using Core.Dtos;

namespace Core.Interfaces
{
    public interface IAdminRepository
    {
        // Suffixing InDb to differentiate from service operations.  
        Task<(bool, string)> ToggleMetadataCategoryActivationInDb(int projectID, int metadataFieldID, bool setEnabled);
        Task<(User, List<ProjectMembership>)> GetRoleDetailsInDb(int userID);
        Task<DateTime> ModifyRoleInDb(int projectID, int metadataFieldID, ProjectMembership.UserRoleType roleChangeTo);
        Task<List<ProjectMetadataField>> AddMetaDataFieldsToProjectInDb(int projectID, List<AddMetadataReq> req);
        Task<List<Project>> CreateProjectsInDb(List<CreateProjectsReq> req, int userID);
        Task<List<User>> GetAllUsers();
        Task<(HashSet<int>, HashSet<int>, HashSet<int>, HashSet<int>)> AddUsersToProjectInDb(int reqeusterID, int projectID, AddUsersToProjectReq req);
        Task<(HashSet<int>, HashSet<int>, HashSet<int>, HashSet<int>)> DeleteUsersFromProjectInDb(int reqeusterID, int projectID, DeleteUsersFromProjectReq req);
        Task<(int, List<UserCustomInfo>)> ImportProjectInDB(List<Project> projectList, List<ProjectTag> projectTagList, List<Tag> tagList, List<ImportUserProfile> importUserProfileList, int requesterID);
    }
}
