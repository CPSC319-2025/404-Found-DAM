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
            app.MapPost("/tags", AddTag)
               .WithName("AddTag")
               .WithOpenApi();
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
        private static async Task<IResult> AddTag(ITagService tagService, CreateTagDto newTag)
        {
            try 
            {
                var addedTag = await tagService.AddTagAsync(newTag);
                return Results.Created($"/tags/{addedTag.TagID}", addedTag);
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
