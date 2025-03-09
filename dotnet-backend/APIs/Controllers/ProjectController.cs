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
            app.MapGet("/projects/{projectID}", getProject).WithName("getProject").WithOpenApi();
            app.MapGet("/projects/{projectID}/assets", GetProjectAssets).WithName("GetProjectAssets").WithOpenApi();
            // app.MapGet("/projects/", RetrieveAllProjects).WithName("RetrieveAllProjects").WithOpenApi();
           
            app.MapPost("/projects/{projectID}/assets/tags", AddTagsToAssets).WithName("AddTagsToAssets").WithOpenApi();
            app.MapPost("/projects/{projectID}/export", ExportProject).WithName("ExportProject").WithOpenApi();
            app.MapPost("/projects/{projectID}/import", ImportProject).WithName("ImportProject").WithOpenApi();
        }

        private static async Task<IResult> GetProjectAssets
        (
            int projectID, 
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
                GetProjectAssetsRes result = await projectService.GetProjectAssets(projectID, type, page, limit);
                return Results.Ok(result);
            } 
            catch (Exception ex) 
            {
                return Results.StatusCode(500);
            }
        }

        private static async Task<IResult> getProject(int projectID, IProjectService projectService)
        {
            try 
            {
                GetProjectRes result = await projectService.GetProject(projectID);
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

        private static async Task<IResult> ExportProject(int projectID, IProjectService projectService)
        {
            try 
            {
                // Get binary data of the Excel file containing details of the exported project
                (string fileName, byte[] excelData) = await projectService.ExportProject(projectID);
                return excelData == null 
                    ? Results.NotFound("No project is found to be exported") 
                    : Results.File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName); // Return the Excel file's binary data
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }  
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
                ArchiveProjectsRes result = await projectService.ArchiveProjects(req.projectIDs);
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
                AddAssetsToProjectRes result = await projectService.AddAssetsToProject(req.projectID, req.blobIDs);
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
