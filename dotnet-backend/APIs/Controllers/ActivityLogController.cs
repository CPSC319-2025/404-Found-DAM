using Core.Interfaces;
using Core.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIs.Controllers
{
    // test
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
        public async Task<IActionResult> GetLogs(int? userID, string? action, DateTime? fromDate, DateTime? toDate)
        {
            var logs = await _activityLogService.GetLogsAsync(userID, action, fromDate, toDate);
            return Ok(logs);
        }
    }
}
