using Core.Entities;
using Core.Dtos;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces
{
    public interface IActivityLogService
    {
        Task<bool> AddLogAsync(CreateActivityLogDto dto);

        Task<IEnumerable<Log>> GetLogsAsync(int? userID, string? changeType, int? projectID, string? assetID, DateTime? fromDate, DateTime? toDate, bool isAdminAction);
    }
}