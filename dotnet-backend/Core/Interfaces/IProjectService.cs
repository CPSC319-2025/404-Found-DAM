using Core.Dtos;

namespace Core.Interfaces
{
    public interface IProjectService
    {
        Task<GetProjectAssetsRes> GetProjectAssets(string projectId, string type, int pageNumber, int pageSize);
    }
}
