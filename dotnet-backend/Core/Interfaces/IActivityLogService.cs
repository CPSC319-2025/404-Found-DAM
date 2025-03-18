using Core.Dtos;
using System.Threading.Tasks;
using Core.Entities;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IActivityLogService
    {
        Task<bool> AddLogAsync(int userID, User user, string changeType, string description, int projectID, int assetID);
    }
}
