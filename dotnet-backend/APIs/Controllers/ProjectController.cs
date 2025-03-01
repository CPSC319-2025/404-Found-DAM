using Core.Interfaces;
using Core.Dtos;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class ProjectController
    {
        public static void MapProjectEndpoints(this WebApplication app)
        {
            app.MapGet("/projects/{projectId}/images", GetImages).WithName("GetImages").WithOpenApi();
            app.MapGet("/projects/{projectID}", RetrieveProject).WithName("RetrieveProject").WithOpenApi();
            app.MapGet("/projects/", RetrieveAllProjects).WithName("RetrieveAllProjects").WithOpenApi();
            app.MapPost("/projects/{projectId}/assign-images", AddImagesToProject).WithName("AddImagesToProject").WithOpenApi();
            app.MapPost("/projects/{projectId}/images/tags", AddTagsToAssets).WithName("AddTagsToAssets").WithOpenApi();
            app.MapPost("/projects/logs", ArchiveProjects).WithName("ArchiveProjects").WithOpenApi();
            app.MapPost("/projects/{projectID}/export", ExportProject).WithName("ExportProject").WithOpenApi();
            app.MapPost("/projects/{projectID}/import", ImportProject).WithName("ImportProject").WithOpenApi();
        }

        private static IResult GetImages(string projectID, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }
        private static IResult RetrieveProject(string projectID, ITestService projectService)
        {
            var project = projectService.RetrieveProject();
            return Results.Ok(project); // Return a 200 OK with response body being project
            //if (project > 0) 
            //{
            //    return Results.Ok(project);
            //}
            //else
            //{
            //    return Results.NotFound();
            //}
        }

        private static IResult RetrieveAllProjects(ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult AddImagesToProject(ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult AddTagsToAssets(ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult ArchiveProjects(ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

                private static IResult ExportProject(ExportProjectReq req, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

                private static IResult ImportProject(ImportProjectReq req, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }
    }
}
