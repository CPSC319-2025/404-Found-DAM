using Core.Interfaces;
using Core.Dtos;
using Infrastructure.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Core.Services;
using Infrastructure.DataAccess;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class ProjectController
    {
        private const string DefaultAssetType = "image";
        private const int DefaultPageNumber = 1;
        private  const int DefaultPageSize = 10;
        private const int MOCKEDUSERID = 2;

        private static IActivityLogService _activityLogService;
        public static void Initialize(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;

        }

        public static void MapProjectEndpoints(this WebApplication app)
        {
            // TODO: Mostly done; need to check user credentials
            app.MapPatch("/projects/archive", ArchiveProjects).WithName("ArchiveProjects").WithOpenApi();
            app.MapGet("/projects/{projectID}", GetProject).WithName("GetProject").WithOpenApi();
            app.MapGet("/projects/{projectID}/assets/pagination", GetPaginatedProjectAssets).WithName("GetPaginatedProjectAssets").WithOpenApi();
            app.MapGet("/projects/", GetAllProjects).WithName("GetAllProjects").WithOpenApi();
            app.MapPatch("/projects/{projectID}/associate-assets", AssociateAssetsWithProject).WithName("AssociateAssetsWithProject").WithOpenApi();

            // TODO: Return mocked data currently
            app.MapGet("/projects/logs", GetArchivedProjectLogs).WithName("GetArchivedProjectLogs").WithOpenApi();

            // Update project details endpoint
            app.MapPatch("/projects/{projectID}", UpdateProject).WithName("UpdateProject").WithOpenApi();

            app.MapGet("/projects/my", GetMyProjects)
               .WithName("GetMyProjects")
               .WithOpenApi();


        }

        private static async Task<IResult> GetPaginatedProjectAssets
        (
            int projectID, 
            int? postedBy,
            int? tagID,

            IProjectService projectService,
            string assetType = DefaultAssetType,
            int pageNumber = DefaultPageNumber, 
            int assetsPerPage = DefaultPageSize
        )
        {
            // TODO: Get requester's ID and replace
            int requesterID = MOCKEDUSERID; 
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
                    tagID = tagID
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

                int submitterID = MOCKEDUSERID;

                if (_activityLogService == null) {
                    return Results.StatusCode(500);
                }


                foreach (var projectID in req.projectIDs) {
                    await _activityLogService.AddLogAsync(submitterID, "Archived", "", projectID, "") // assetID is empty string, but it should be ignored.
                }
                return Results.Ok(result);

            }
            catch (PartialSuccessException ex) // sean todo: what to do here?
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
        
        private static async Task<IResult> AssociateAssetsWithProject(int projectID, AssociateAssetsReq req, IProjectService projectService)
        {
            try 
            {
                // TODO: verify submitter is in the DB and retrieve the userID; replace the following MOCKEDUSERID
                int submitterID = MOCKEDUSERID;
                // int? submittedID = AuthorizationHelper.GetUserIdFromToken(req);
                if (submitterID == null) {
                    return Results.StatusCode(500);
                }
                AssociateAssetsRes result = await projectService.AssociateAssetsWithProject(projectID, req.blobIDs, submitterID);

                if (_activityLogService == null) {
                    return Results.StatusCode(500);
                }
                foreach (var blobID in req.blobIDs) {
                    await _activityLogService.AddLogAsync(
                        submitterID,
                        "Added",
                        "",
                        projectID,
                        int.Parse(blobID)
                    );
                }
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

        private static async Task<IResult> UpdateProject(int projectID, UpdateProjectReq req, IProjectService projectService)
        {
            try
            {
                var result = await projectService.UpdateProject(projectID, req);

                int submitterID = MOCKEDUSERID;

                if (_activityLogService == null) {
                    return Results.StatusCode(500);
                }

                var updateDescription = new StringBuilder();
                if (!string.IsNullOrEmpty(req.Location))
                {
                    updateDescription.AppendLine($"Location: {req.Location}");
                }

                if (req.Memberships != null && req.Memberships.Any())
                {
                    updateDescription.AppendLine("Memberships: ");
                    foreach (var membership in req.Memberships)
                    {
                        updateDescription.AppendLine($"- {membership.MemberName} ({membership.Role})");
                    }
                }

                if (req.Tags != null && req.Tags.Any())
                {
                    updateDescription.AppendLine("Tags: ");
                    foreach (var tag in req.Tags)
                    {
                        updateDescription.AppendLine($"- {tag.Name}");
                    }
                }

                if (req.CustomMetadata != null && req.CustomMetadata.Any())
                {
                    updateDescription.AppendLine("Custom Metadata: ");
                    foreach (var metadata in req.CustomMetadata)
                    {
                        updateDescription.AppendLine($"- {metadata.Key}: {metadata.Value}");
                    }
                }

                await _activityLogService.AddLogAsync(
                    submitterID,
                    "Updated",
                    updateDescription.ToString(),
                    projectID,
                    "" // Project ID is empty string, but it should be ignored. No assetID for project update
                );

                return Results.Ok(result);
            }
            catch (DataNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
        }


        private static async Task<IResult> GetMyProjects(IProjectService projectService)
        {
            int userId = MOCKEDUSERID;
            List<GetProjectRes> result = await projectService.GetMyProjects(userId);
            return Results.Ok(result);
        }
        
    }
}