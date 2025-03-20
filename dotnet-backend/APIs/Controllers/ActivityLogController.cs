using Core.Interfaces;
using Core.Entities;
using Microsoft.AspNetCore.Mvc;

using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.DataAccess;
using Core.Services;

namespace APIs.Controllers
{
    public static class ActivityLogController
    {

        public static void MapActivityLogEndpoints(this WebApplication app)
        {
            app.MapGet("/logs", async (HttpRequest request, [FromServices] IActivityLogService activityLogService) =>
            {
                return await GetActivityLog(request, activityLogService);
            })
            .WithName("GetActivityLog")
            .WithOpenApi();

            app.MapPost("/logs", async (HttpRequest request, [FromServices] IActivityLogService activityLogService) =>
            {
                return await AddActivityLog(request, activityLogService);
            })
            .WithName("AddActivityLog")
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
        public static async Task<IResult> GetActivityLog(HttpRequest request, [FromServices] IActivityLogService activityService)
        {

            // // AddLogAsync(int userID, User user, string changeType, string description, int projectID, int assetID)
            // User user = new User { UserID = 5, Email = "test_for_logs@example.com", Name = "Test Log" };

            // await activityService.AddLogAsync(1, user, "created", "image1", 1, 2);
            // await activityService.AddLogAsync(1, user, "created", "image2", 1, 2);
            // await activityService.AddLogAsync(1, user, "created", "image3", 1, 2);
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

        // public static async Task<IResult> AddActivityLog([FromBody] Log log, IActivityLogService activityLogService)
        // {
        //     // var log = await request.ReadFromJsonAsync<Log>();

        //     if (log == null)
        //     {
        //         return Results.BadRequest("Invalid log data.");
        //     }
            
        //     if (string.IsNullOrEmpty(log.Description) || log.UserID == 0 || log.AssetID == 0 || log.ProjectID == 0)
        //     {
        //         return Results.BadRequest("Missing required fields.");
        //     }

        //     User user = new User { UserID = 5, Email = "test_for_logs@example.com", Name = "Test Log"}; 

        //     bool result = await activityLogService.AddLogAsync(
        //         log.UserID, 
        //         user, 
        //         log.ChangeType, 
        //         log.Description, 
        //         log.ProjectID, 
        //         log.AssetID
        //     );

        //     if (result)
        //     {
        //         return Results.Created($"/logs/{log.ChangeID}", log);
        //     }
        //     else
        //     {
        //         return Results.BadRequest("Failed to add log.");
        //     }
        // }

        public static async Task<IResult> AddActivityLog(HttpRequest request, IActivityLogService activityLogService)
{
    try
    {
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        
        // Console.WriteLine($"Request Body: {body}");


        // var log = JsonSerializer.Deserialize<Log>(body);

        // if (log == null)
        // {
        //     return Results.BadRequest("Failed to deserialize log object.");
        // }

        // if (string.IsNullOrEmpty(log.Description) || log.UserID == 0 || log.AssetID == 0 || log.ProjectID == 0)
        // {
        //     return Results.BadRequest("Missing required fields.");
        // }

        User user = new User { UserID = 5, Email = "test_for_logs@example.com", Name = "Test Log" };

        bool result = await activityLogService.AddLogAsync( // mock data
            5,
            user,
            "Created",
            "Image1",
            2,
            2
        );

        // var log2 = new Log
        // {
        //     ChangeType = "Create",
        //     Description = "Image 1",
        //     ProjectID = 101,
        //     AssetID = 202,
        //     UserID = 5,
        //     Timestamp = DateTime.UtcNow,
        //     User = user,
        // };

        if (result)
        {
            // return Results.Created($"/logs/{log.ChangeID}", log);
            return Results.Created("success", "success");
        }
        else
        {
            return Results.BadRequest("Failed to add log.");
        }
    }
    catch (JsonException ex)
    {
        return Results.BadRequest($"Deserialization error: {ex.Message}");
    }
}






    }
}
