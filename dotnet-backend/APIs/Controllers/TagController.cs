using Core.Interfaces;
using Infrastructure.Exceptions;
using Core.Dtos;


namespace APIs.Controllers
{
    public static class TagController
    {
        public static void MapTagEndpoints(this WebApplication app)
        {
            app.MapGet("/tags", GetTags)
               .WithName("GetTags")
               .WithOpenApi();
            app.MapPut("/tags", ReplaceTags).WithName("ReplaceTags").WithOpenApi();
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
                var tagDtos = await context.Request.ReadFromJsonAsync<IEnumerable<CreateTagDto>>();
                if (tagDtos == null) return Results.BadRequest("Invalid tag data");

                await tagService.ReplaceAllTagsAsync(tagDtos);
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

        private static async Task<IResult> AddTags(HttpContext context, ITagService tagService)
        {
            try
            {
                var newTags = await context.Request.ReadFromJsonAsync<IEnumerable<CreateTagDto>>();
                if (newTags == null || !newTags.Any())
                {
                    return Results.BadRequest("No tags provided.");
                }

                var addedTags = await tagService.AddTagsAsync(newTags);
                return Results.Created("/tags", addedTags);
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
