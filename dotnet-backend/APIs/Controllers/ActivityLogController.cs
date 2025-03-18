using Core.Interfaces;
using Core.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/activity-log")]
    public class ActivityLogController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;

        public ActivityLogController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        [HttpGet("retrieve")]
        public async Task<IActionResult> GetLogs(int userID, User user, string changeType, int projectID, int assetID, DateTime? fromDate, DateTime? toDate)
        {
            var logs = await _activityLogService.GetLogsAsync(userID, changeType, projectID, assetID, fromDate, toDate);
            return Ok(logs);
        }
    }
}
