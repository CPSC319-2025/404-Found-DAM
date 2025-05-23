using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IActivityLogRepository
    {
        Task<bool> AddLogAsync(Log log);
        Task<List<Log>> GetLogsAsync(int? userID, string? changeType, int? projectID, string? assetID, DateTime? fromDate, DateTime? toDate, bool isAdminAction);
    }
}
