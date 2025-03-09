using Core.Entities;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IActivityLogRepository _repository;

        public ActivityLogService(IActivityLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> AddLogAsync(int userID, User user, string action, string detail, int projectID)
        {
            var log = new Log
            {
                UserID = userID,
                User = user,
                Action = action,
                Detail = detail,
                Timestamp = DateTime.UtcNow
            };

            return await _repository.AddLogAsync(log);
        }

        public async Task<List<Log>> GetLogsAsync(int? userID, string? action, DateTime? fromDate, DateTime? toDate)
        {
            return await _repository.GetLogsAsync(userID, action, fromDate, toDate);
        }
    }
}
