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
            app.MapGet("/projects/logs", GetArchivedProjectLogs).WithName("GetArchivedProjectLogs").WithOpenApi();
           
            app.MapPost("/projects/{projectId}/images/tags", AddTagsToAssets).WithName("AddTagsToAssets").WithOpenApi();
            app.MapPost("/projects/{projectID}/export", ExportProject).WithName("ExportProject").WithOpenApi();
            app.MapPost("/projects/{projectID}/import", ImportProject).WithName("ImportProject").WithOpenApi();

            app.MapPatch("/projects/archive", ArchiveProjects).WithName("ArchiveProjects").WithOpenApi();
            app.MapPatch("/projects/assign-images", AddAssetsToProject).WithName("AddAssetsToProject").WithOpenApi();
        }

        private static IResult GetImages(string projectID, IProjectService projectService)
        {
            return Results.NotFound("stub"); // Stub
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
