using Core.Interfaces;
using Core.Dtos;
using Microsoft.Extensions.Logging.Abstractions;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class ProjectController
    {
        public const string DefaultAssetType = "image";
        public const int DefaultPageNumber = 1;
        public const int DefaultPageSize = 10;


        public static void MapProjectEndpoints(this WebApplication app)
        {
            app.MapPatch("/projects/{projectID}/submit-assets", SubmitAssets).WithName("SubmitAssets").WithOpenApi();
            app.MapPatch("/projects/archive", ArchiveProjects).WithName("ArchiveProjects").WithOpenApi();
            app.MapGet("/projects/logs", GetArchivedProjectLogs).WithName("GetArchivedProjectLogs").WithOpenApi();
            app.MapGet("/projects/{projectID}", getProject).WithName("getProject").WithOpenApi();
            app.MapGet("/projects/{projectID}/assets/pagination", GetPaginatedProjectAssets).WithName("GetPaginatedProjectAssets").WithOpenApi();
            // app.MapGet("/projects/", RetrieveAllProjects).WithName("RetrieveAllProjects").WithOpenApi();
           
            app.MapPost("/projects/{projectID}/export", ExportProject).WithName("ExportProject").WithOpenApi();
            // app.MapPost("/projects/{projectID}/import", ImportProject).WithName("ImportProject").WithOpenApi();
        }

        private static async Task<IResult> GetPaginatedProjectAssets
        (
            int projectID, 
            string? status,
            string? postedBy,
            string? datePosted,
            IProjectService projectService,
            string assetType = DefaultAssetType,
            int pageNumber = DefaultPageNumber, 
            int assetsPerPage = DefaultPageSize
        )
        {
            // Validate user input
            if (pageNumber <= 0 || assetsPerPage <= 0)
            {
                return Results.BadRequest("Page and limit must be positive integers.");
            }

            // Call Project Service to handle request
            try
            {
                if (status == null) 
                {
                    Console.WriteLine("status is null");
                }

                if (postedBy == null) 
                {
                    Console.WriteLine("postedBy is null");
                }
                else 
                {
                    Console.WriteLine($"posted by {postedBy}");
   
                }

                if (datePosted == null) 
                {
                    Console.WriteLine("datePosted is null");

                }

                GetPaginatedProjectAssetsReq req = new GetPaginatedProjectAssetsReq
                {
                    projectID = projectID,
                    assetType = assetType,
                    pageNumber = pageNumber,
                    assetsPerPage = assetsPerPage,
                    status = status,
                    postedBy = postedBy,
                    datePosted = datePosted
                };

                GetPaginatedProjectAssetsRes result = await projectService.GetPaginatedProjectAssets(req);
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
        
        private static async Task<IResult> SubmitAssets(int projectID, SubmitAssetsReq req, IProjectService projectService)
        {
            // May need to add varification to check if client data is bad.
            try 
            {
                SubmitAssetsRes result = await projectService.SubmitAssets(projectID, req.blobIDs);
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
