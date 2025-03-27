using Core.Interfaces;
using Infrastructure.Exceptions;

namespace APIs.Controllers
{
    public static class TagController
    {
        public static void MapTagEndpoints(this WebApplication app)
        {
            app.MapGet("/tags", GetTags)
               .WithName("GetTags")
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
    }
}
