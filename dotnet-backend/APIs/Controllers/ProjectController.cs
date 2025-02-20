using Core.Interfaces;

namespace APIs.Controllers
{
    public static class ProjectController
    {
        public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/projects/{projectID}", RetrieveProject).WithName("RetrieveProject").WithOpenApi();
        }

        private static IResult RetrieveProject(string projectID, ITestService projectService)
        {
            var project = projectService.RetrieveProject();
            return Results.Ok(project);
            //if (project > 0) 
            //{
            //    return Results.Ok(project);
            //}
            //else
            //{
            //    return Results.NotFound();
            //}
        }

    }
}
