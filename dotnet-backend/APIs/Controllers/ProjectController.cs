using Core.Interfaces;
using Core.Dtos;
using Infrastructure.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class ProjectController
    {
        private const string DefaultAssetType = "image";
        private const int DefaultPageNumber = 1;
        private  const int DefaultPageSize = 10;
        private const int MOCKEDUSERID = 1;

        public static void MapProjectEndpoints(this WebApplication app)
        {
            // TODO: Mostly done; need to check user credentials
            app.MapPatch("/projects/archive", ArchiveProjects).WithName("ArchiveProjects").WithOpenApi();
            app.MapGet("/projects/{projectID}", GetProject).WithName("GetProject").WithOpenApi();
            app.MapGet("/projects/{projectID}/assets/pagination", GetPaginatedProjectAssets).WithName("GetPaginatedProjectAssets").WithOpenApi();
            app.MapGet("/projects/", GetAllProjects).WithName("GetAllProjects").WithOpenApi();
            app.MapPatch("/projects/{projectID}/submit-assets", SubmitAssets).WithName("SubmitAssets").WithOpenApi();

            // TODO: Return mocked data currently
            app.MapGet("/projects/logs", GetArchivedProjectLogs).WithName("GetArchivedProjectLogs").WithOpenApi();
        }

        private static async Task<IResult> GetPaginatedProjectAssets
        (
            int projectID, 
            string? postedBy,
            string? datePosted,
            IProjectService projectService,
            string assetType = DefaultAssetType,
            int pageNumber = DefaultPageNumber, 
            int assetsPerPage = DefaultPageSize
        )
        {
            // TODO: Get requester's ID and replace
            int requesterID = MockedUserID; 
            // Validate user input
            if (pageNumber <= 0 || assetsPerPage <= 0)
            {
                return Results.BadRequest("Page and limit must be positive integers.");
            }

            // Call Project Service to handle request
            try
            {
                GetPaginatedProjectAssetsReq req = new GetPaginatedProjectAssetsReq
                {
                    projectID = projectID,
                    assetType = assetType,
                    pageNumber = pageNumber,
                    assetsPerPage = assetsPerPage,
                    postedBy = postedBy,
                    datePosted = datePosted
                };

                GetPaginatedProjectAssetsRes result = await projectService.GetPaginatedProjectAssets(req, requesterID);
                return Results.Ok(result);
            } 
            catch (DataNotFoundException ex) 
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception) 
            {
                return Results.StatusCode(500);
            }  
        }

        private static async Task<IResult> GetProject(int projectID, IProjectService projectService)
        {
            try 
            {
                GetProjectRes result = await projectService.GetProject(projectID);
                return Results.Ok(result);
            }
            catch (DataNotFoundException ex) 
            {
                return Results.NotFound(ex.Message);
            }
            catch (ArchivedException ex) 
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception) 
            {
                return Results.StatusCode(500);
            } 
        }

        private static async Task<IResult> GetAllProjects(IProjectService projectService)
        {
            try 
            {
                // TODO: replace MOCKEDUSERID with authenticated userID
                int userID = MOCKEDUSERID;
                GetAllProjectsRes result = await projectService.GetAllProjects(userID);
                return Results.Ok(result);
            }
            catch (DataNotFoundException ex) 
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception) 
            {
                return Results.StatusCode(500);
            } 
        }

        private static async Task<IResult> GetArchivedProjectLogs(IProjectService projectService)
        {
            // May need to add varification to check if client data is bad.
            try 
            {
                GetArchivedProjectLogsRes result = await projectService.GetArchivedProjectLogs();
                return Results.Ok(result);
            }
            catch (Exception)
            {
                return Results.StatusCode(500);
            }        
        }

        private static async Task<IResult> ArchiveProjects(ArchiveProjectsReq req, IProjectService projectService)
        {
            // May need to add varification to check if client data is bad.
            try 
            {
                ArchiveProjectsRes result = await projectService.ArchiveProjects(req.projectIDs);
                return Results.Ok(result);
            }
            catch (PartialSuccessException ex)
            {
                return Results.Ok(ex.Message);
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
        
        private static async Task<IResult> SubmitAssets(int projectID, SubmitAssetsReq req, IProjectService projectService)
        {
            try 
            {
                // TODO: verify submitter is in the DB and retrieve the userID; replace the following MOCKEDUSERID
                int submitterID = MOCKEDUSERID; 
                SubmitAssetsRes result = await projectService.SubmitAssets(projectID, req.blobIDs, submitterID);
                return Results.Ok(result);
            }
            catch (DataNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return Results.StatusCode(500);
            }
        }
    }
}
