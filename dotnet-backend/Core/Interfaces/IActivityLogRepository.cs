using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IActivityLogRepository
    {
        Task<bool> AddLogAsync(Log log);
        Task<List<Log>> GetLogsAsync(int? userID, string? action, DateTime? fromDate, DateTime? toDate);
    }
}
