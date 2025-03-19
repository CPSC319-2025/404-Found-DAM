using Core.Entities;
using Core.Interfaces;
using Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Services
{
    // test
    public class ActivityLogService : IActivityLogService
    {
        private readonly IActivityLogRepository _repository;

        private static int changeID;

        public ActivityLogService(IActivityLogRepository repository)
        {
            _repository = repository;
            changeID = 0;
        }

        public int getNextLogNumber() {
            changeID++;
            return changeID;
        }

        public async Task<bool> AddLogAsync(int userID, User user, string changeType, string description, int projectID, int assetID)
        {
            var log = new Log
            {
                ChangeID = getNextLogNumber(),
                Timestamp = DateTime.UtcNow,
                UserID = userID,
                User = user,
                ChangeType = changeType,
                Description = description,
                ProjectID = projectID,
                AssetID = assetID
            };

            return await _repository.AddLogAsync(log);
        }

        public async Task<IEnumerable<Log>> GetLogsAsync(int? userID, string? changeType, int? projectID, int? assetID, DateTime? fromDate, DateTime? toDate)
        {
            return await _repository.GetLogsAsync(userID, changeType, projectID, assetID, fromDate, toDate);
        }

    }
}
