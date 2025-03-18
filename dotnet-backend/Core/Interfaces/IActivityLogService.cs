using Core.Entities;
using Core.Dtos;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces
{
    public interface IActivityLogService
    {
        Task<bool> AddLogAsync(int userID, User user, string changeType, string description, int projectID, int assetID);

        Task<IEnumerable<Log>> GetLogsAsync(int? userID, string? action, DateTime? fromDate, DateTime? toDate);
    }
}