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
            app.MapGet("/projects/{projectId}/assets", GetProjectAssets).WithName("GetProjectAssets").WithOpenApi();
            app.MapGet("/projects/{projectID}", RetrieveProject).WithName("RetrieveProject").WithOpenApi();
            app.MapGet("/projects/", RetrieveAllProjects).WithName("RetrieveAllProjects").WithOpenApi();
            app.MapGet("/projects/logs", GetArchivedProjectLogs).WithName("GetArchivedProjectLogs").WithOpenApi();
           
            app.MapPost("/projects/{projectId}/images/tags", AddTagsToAssets).WithName("AddTagsToAssets").WithOpenApi();
            app.MapPost("/projects/{projectID}/export", ExportProject).WithName("ExportProject").WithOpenApi();
            app.MapPost("/projects/{projectID}/import", ImportProject).WithName("ImportProject").WithOpenApi();

            app.MapPatch("/projects/archive", ArchiveProjects).WithName("ArchiveProjects").WithOpenApi();
            app.MapPatch("/projects/assign-images", AddAssetsToProject).WithName("AddAssetsToProject").WithOpenApi();
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
            } catch (Exception ex) 
            {
                return Results.StatusCode(500);
            }
        }

        private static IResult RetrieveProject(string projectID, IProjectService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult RetrieveAllProjects(IProjectService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult GetArchivedProjectLogs(IProjectRepository projectRepository)
        {
            // May need to add varification to check if client data is bad.
            try 
            {
                GetArchivedProjectLogsRes result = projectRepository.GetArchivedProjectLogs();
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }        
        }
        
        private static IResult AddTagsToAssets(IProjectService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult ExportProject(ExportProjectReq req, IProjectService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult ImportProject(ImportProjectReq req, IProjectService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult ArchiveProjects(ArchiveProjectsReq req, IProjectRepository projectRepository)
        {
            // May need to add varification to check if client data is bad.
            try 
            {
                ArchiveProjectsRes result = projectRepository.ArchiveProjects(req.projectIds);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        }
        
        private static IResult AddAssetsToProject(AddAssetsToProjectReq req, IProjectRepository projectRepository)
        {
            // May need to add varification to check if client data is bad.
            try 
            {
                AddAssetsToProjectRes result = projectRepository.AddAssetsToProject(req.projectId, req.ImageIds);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        }
    }
}
