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

        public static async Task MapActivityLogEndpoints(this WebApplication app)
        {
            app.MapGet("/logs", async (HttpRequest request, [FromServices] IActivityLogService activityLogService) =>
            {
                return await GetActivityLog(request, activityLogService);
            })
            .WithName("GetActivityLog")
            .WithOpenApi();

            // app.MapPost("/addLog", async (HttpRequest request, [FromServices] IActivityLogService activityLogServuice) => )
            // {
            //     return await AddLogAsync(request, activityLogService);
            // }

        }
        
        public static async Task<IResult> GetActivityLog(HttpRequest request, [FromServices] IActivityLogService activityService)
        {
            var query = request.Query;

            int? projectID = query.ContainsKey("projectID") ? int.Parse(query["projectID"]) : null;
            int? userID = query.ContainsKey("userID") ? int.Parse(query["userID"]) : null;
            string? assetID = query.ContainsKey("assetID") ? query["assetID"].ToString() : null;
            string? changeType = query.ContainsKey("changeType") ? query["changeType"].ToString() : null;
            DateTime? start = query.ContainsKey("start") ? DateTime.Parse(query["start"]) : null;
            DateTime? end = query.ContainsKey("end") ? DateTime.Parse(query["end"]) : null;
            bool? isAdminAction = query.ContainsKey("isAdminAction") ? bool.Parse(query["isAdminAction"]) : null;

            int pageNumber = query.ContainsKey("pageNumber") ? int.Parse(query["pageNumber"]) : 1;
            int pageSize = query.ContainsKey("pageSize") ? int.Parse(query["pageSize"]) : 10;
            

            if (pageNumber <= 0 || pageSize <= 0)
            {
                return Results.BadRequest("Page number and page size must be positive integers.");
            }

            var logs = await activityService.GetLogsAsync(userID, changeType, projectID, assetID, start, end, isAdminAction ?? false);

            var paginatedLogs = logs
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new
                {
                    change_id = log.ChangeID,
                    date_time = log.Timestamp.ToString("o"),
                    user = log.UserID,
                    description = log.Description,
                    change_type = log.ChangeType,
                    asset_id = log.AssetID,
                    project_id = log.ProjectID,
                    isAdminAction = log.IsAdminAction
                })
                .ToList();

            var totalRecords = logs.Count();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            return Results.Ok(new
            {
                pageNumber,
                totalPages,
                logsPerPage = pageSize,
                totalLogs = totalRecords,
                data = paginatedLogs
            });
        }

        // public static async Task<bool> AddLogAsync(int userID, string changeType, string description, int projectID, int assetID, IActivityLogService service) {
        //     // IActivityLogService service = new ActivityLogService();
        //     return await service.AddLogAsync(userID, changeType, description, projectID, assetID);
        // }

    }
}