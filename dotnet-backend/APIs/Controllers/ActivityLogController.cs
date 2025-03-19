using Core.Interfaces;
using Core.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIs.Controllers
{
    public static class ActivityLogController
    {

        public static void MapActivityLogEndpoints(this WebApplication app)
        {
            app.MapGet("/logs", async (HttpRequest request, IActivityLogService activityLogService) =>
            {
                return await GetActivityLog(request, activityLogService);
            })
            .WithName("GetActivityLog")
            .WithOpenApi();
        }
        // private readonly IActivityLogService _activityLogService;

        // public ActivityLogController(IActivityLogService activityLogService)
        // {
        //     _activityLogService = activityLogService;
        // }

        // [HttpGet("retrieve")]
        // public async Task<IActionResult> GetLogs(int userID, User user, string changeType, int projectID, int assetID, DateTime? fromDate, DateTime? toDate)
        // {
        //     var logs = await _activityLogService.GetLogsAsync(userID, changeType, projectID, assetID, fromDate, toDate);
        //     return Ok(logs);
        // }
        public static async Task<IResult> GetActivityLog(HttpRequest request, IActivityLogService activityService)
        {
            var query = request.Query;

            int? projectID = query.ContainsKey("projectID") ? int.Parse(query["projectID"]) : null;
            int? assetID = query.ContainsKey("assetID") ? int.Parse(query["assetID"]) : null;
            int? userID = query.ContainsKey("userID") ? int.Parse(query["userID"]) : null;
            string? changeType = query.ContainsKey("changeType") ? query["changeType"].ToString() : null;
            DateTime? start = query.ContainsKey("start") ? DateTime.Parse(query["start"]) : null;
            DateTime? end = query.ContainsKey("end") ? DateTime.Parse(query["end"]) : null;
            
            int pageNumber = query.ContainsKey("pageNumber") ? int.Parse(query["pageNumber"]) : 1;
            int pageSize = query.ContainsKey("pageSize") ? int.Parse(query["pageSize"]) : 10;
            
            if (pageNumber <= 0 || pageSize <= 0)
            {
                return Results.BadRequest("Page number and page size must be positive integers.");
            }
            
            var logs = await activityService.GetLogsAsync(userID, changeType, projectID, assetID, start, end);
            
            var paginatedLogs = logs.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            
            var response = paginatedLogs.GroupBy(log => new { log.AssetID, log.ProjectID })
                .Select(group => new
                {
                    item_id = group.Key.AssetID.ToString(),
                    item_type = "image", // Assuming all logs relate to images
                    audit_log = group.Select(log => new
                    {
                        change_id = log.ChangeID,
                        date_time = log.Timestamp.ToString("o"),
                        user = log.UserID,
                        description = log.Description,
                        change_type = log.ChangeType,
                        asset_id = log.AssetID,
                        project_id = log.ProjectID
                    }).ToList()
                });
            
            var totalRecords = logs.Count();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            
            return Results.Ok(new
            {
                currentPage = pageNumber,
                totalPages = totalPages,
                pageSize = pageSize,
                totalRecords = totalRecords,
                data = response
            });
        }

    }
}
