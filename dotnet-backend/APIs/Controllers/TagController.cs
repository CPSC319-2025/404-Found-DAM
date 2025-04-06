using Core.Interfaces;
using Infrastructure.Exceptions;
using Core.Dtos;


namespace APIs.Controllers
{
    public static class TagController
    {

        private const bool AdminActionTrue = true;

        private const bool logDebug = false;

        private const bool verboseLogs = false;
        public static void MapTagEndpoints(this WebApplication app)
        {
            app.MapGet("/tags", GetTags)
               .WithName("GetTags")
               .WithOpenApi();
            app.MapPut("/tags", ReplaceTags).WithName("ReplaceTags").WithOpenApi();
        }

        private static IServiceProvider GetServiceProvider(HttpContext context)
        {
            return context.RequestServices; // for activity log

        }

        private static async Task<IResult> GetTags(ITagService tagService)
        {
            try 
            {
                var tagNames = await tagService.GetTagNamesAsync();
                return Results.Ok(tagNames);
            }
            catch (DataNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            } 
            catch (Exception ex) 
            {
                return Results.Problem
                (
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal Server Error"
                );            
            }
        }
        private static async Task<IResult> ReplaceTags(ITagService tagService, HttpContext context)
        {
            try
            {
                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();
                // TODO: Check requester is a super admin in DB
                int requesterID = Convert.ToInt32(context.Items["userId"]);


                var tagDtos = await context.Request.ReadFromJsonAsync<IEnumerable<CreateTagDto>>();
                if (tagDtos == null) return Results.BadRequest("Invalid tag data");

                await tagService.ReplaceAllTagsAsync(tagDtos);

                // await activityLogService.AddLogAsync(new CreateActivityLogDto
                // {
                //     // Add log (done)
                //     userID = requesterID,
                //     changeType = "Update Logs",
                //     description = theDescription,
                //     projID = "",
                //     assetID = "",
                //     isAdminAction = AdminActionTrue
                // });


                return Results.Ok("Tag table replaced");
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
        }

    }
}
