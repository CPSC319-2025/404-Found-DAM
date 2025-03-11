using Core.Interfaces;
using Core.Dtos;

namespace APIs.Controllers
{
    public static class SearchController
    {
        public static void MapSearchEndpoints(this WebApplication app)
        {
            app.MapGet("/search", Search).WithName("Search").WithOpenApi();
        }

        private static async Task<IResult> Search(string query, ISearchService searchService) {
            if (string.IsNullOrEmpty(query)) {
                return Results.BadRequest("Search query cannot be empty.");
            }
            try {
                var result = await searchService.SearchAsync(query);

                if (result == null) {
                    return Results.NotFound("No Results found.");
                }

                return Results.Ok(result);
            }
            catch (Exception e) {
                Console.WriteLine($"An error occured during saerch: {e.Message}");
                return Results.StatusCode(500);
            }
        }

    }

}