using Core.Entities;
using Core.Interfaces;
using Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Dtos;

namespace Core.Services

// I need the a function in ProjectController to call AddLog (either in ActivityLogController or ActivityLogService)
// However, it seems like I am unable to make this call due to missing a context that I have no access to in ProjectController
// SHould I use static dependency injection to do this? yes.
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

        public async Task<bool> AddLogAsync(CreateActivityLogDto logDto)
        {
            var log = new Log
            {
                // ChangeID = getNextLogNumber(), // removed, using Auto-incrementing identity column instead.
                Timestamp = DateTime.UtcNow,
                UserID = logDto.userID,
                // User = user, TODO
                ChangeType = logDto.changeType,
                Description = logDto.description,
                ProjectID = logDto.projID,
                AssetID = logDto.assetID,
                IsAdminAction = logDto.isAdminAction
            };

            return await _repository.AddLogAsync(log);
        }

        public async Task<IEnumerable<Log>> GetLogsAsync(int? userID, string? changeType, int? projectID, string? assetID, DateTime? fromDate, DateTime? toDate, bool isAdminAction)
        {
            return await _repository.GetLogsAsync(userID, changeType, projectID, assetID, fromDate, toDate, isAdminAction);
        }

    }
}
