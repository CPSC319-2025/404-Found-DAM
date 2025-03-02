using Core.Interfaces;
using Core.Dtos;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class ProjectController
    {
        public const string DefaultAssetType = "image";
        public const int DefaultPageNumber = 1;
        public const int DefaultLimit = 10;


        public static void MapProjectEndpoints(this WebApplication app)
        {
            app.MapPatch("/projects/assign-assets", AddAssetsToProject).WithName("AddAssetsToProject").WithOpenApi();
            app.MapPatch("/projects/archive", ArchiveProjects).WithName("ArchiveProjects").WithOpenApi();
            app.MapGet("/projects/logs", GetArchivedProjectLogs).WithName("GetArchivedProjectLogs").WithOpenApi();
            app.MapGet("/projects/{projectID}", RetrieveProject).WithName("RetrieveProject").WithOpenApi();
            app.MapGet("/projects/{projectId}/assets", GetProjectAssets).WithName("GetProjectAssets").WithOpenApi();
            // app.MapGet("/projects/", RetrieveAllProjects).WithName("RetrieveAllProjects").WithOpenApi();
           
            app.MapPost("/projects/{projectId}/images/tags", AddTagsToAssets).WithName("AddTagsToAssets").WithOpenApi();
            app.MapPost("/projects/{projectID}/export", ExportProject).WithName("ExportProject").WithOpenApi();
            app.MapPost("/projects/{projectID}/import", ImportProject).WithName("ImportProject").WithOpenApi();
        }

        private static async Task<IResult> GetProjectAssets
        (
            string projectId, 
            IProjectService projectService, // Place required parameters before optional parameters
            string type = DefaultAssetType, 
            int page = DefaultPageNumber, 
            int limit = DefaultLimit
        )
        {
            // Validate user input
            if (page <= 0 || limit <= 0)
            {
                return Results.BadRequest("Page and limit must be positive integers.");
            }

            // Call Project Service to handle request
            try
            {
                GetProjectAssetsRes result = await projectService.GetProjectAssets(projectId, type, page, limit);
                return Results.Ok(result);
            } 
            catch (Exception ex) 
            {
                return Results.StatusCode(500);
            }
        }

        private static async Task<IResult> RetrieveProject(string projectId, IProjectService projectService)
        {
            try 
            {
                RetrieveProjectRes result = await projectService.RetrieveProject(projectId);
                return Results.Ok(result);
            }
            catch (Exception ex) 
            {
                return Results.StatusCode(500);
            }
        }

        // private static IResult RetrieveAllProjects(IProjectService projectService)
        // {
        //     return Results.NotFound("stub"); // Stub
        // }

        private static async Task<IResult> GetArchivedProjectLogs(IProjectService projectService)
        {
            // May need to add varification to check if client data is bad.
            try 
            {
                GetArchivedProjectLogsRes result = await projectService.GetArchivedProjectLogs();
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }        
        }
        
        private static async Task<IResult> AddTagsToAssets(IProjectService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static async Task<IResult> ExportProject(ExportProjectReq req, IProjectService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static async Task<IResult> ImportProject(ImportProjectReq req, IProjectService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static async Task<IResult> ArchiveProjects(ArchiveProjectsReq req, IProjectService projectService)
        {
            // May need to add varification to check if client data is bad.
            try 
            {
                ArchiveProjectsRes result = await projectService.ArchiveProjects(req.projectIds);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        }
        
        private static async Task<IResult> AddAssetsToProject(AddAssetsToProjectReq req, IProjectService projectService)
        {
            // May need to add varification to check if client data is bad.
            try 
            {
                AddAssetsToProjectRes result = await projectService.AddAssetsToProject(req.projectId, req.ImageIds);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return Results.StatusCode(500);
            }
        }
    }
}
