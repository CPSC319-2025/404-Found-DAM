using Core.Dtos;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IActivityLogService
    {
        Task<bool> AddLogAsync(int userID, User user, string action, string detail, int projectID);
    }
}
