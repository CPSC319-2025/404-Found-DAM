using Core.Interfaces;
using Core.Dtos;
using Infrastructure.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Core.Services;
using Infrastructure.DataAccess;
using System.Text;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class ProjectController
    {
        private const string DefaultAssetType = "image";
        private const int DefaultPageNumber = 1;
        private  const int DefaultPageSize = 10;
        private const int MOCKEDUSERID = 1;

        private const bool adminActionTrue = true;

        private const bool logDebug = true;
        private const bool verboseLogs = false;

        // private static IActivityLogService _activityLogService;
        // private static IProjectService _projectService;
        // private static IUserService _userService;
        // public static void Initialize(IActivityLogService activityLogService, IProjectService projectService, IUserService userService)
        // {
        //     _activityLogService = activityLogService;
        //     _projectService = projectService;
        //     _userService = userService;
        // }



        public static void MapProjectEndpoints(this WebApplication app)
        {
            // TODO: Mostly done; need to check user credentials
            app.MapPatch("/projects/archive", ArchiveProjects).WithName("ArchiveProjects").WithOpenApi();
            app.MapGet("/projects/{projectID}", GetProject).WithName("GetProject").WithOpenApi();
            app.MapGet("/projects/{projectID}/assets/pagination", GetPaginatedProjectAssets).WithName("GetPaginatedProjectAssets").WithOpenApi();
            app.MapGet("/projects/", GetAllProjects).WithName("GetAllProjects").WithOpenApi();
            app.MapPatch("/projects/{projectID}/associate-assets", AssociateAssetsWithProject).WithName("AssociateAssetsWithProject").WithOpenApi();
            app.MapGet("/project/{projectID}/asset-files/storage/{blobID}/{filename}", GetAssetFileFromStorage).WithName("GetAssetFileFromStorageReq").WithOpenApi();

            // TODO: Return mocked data currently
            // app.MapGet("/projects/logs", GetArchivedProjectLogs).WithName("GetArchivedProjectLogs").WithOpenApi();

            // Update project details endpoint
            app.MapPatch("/projects/{projectID}", UpdateProject).WithName("UpdateProject").WithOpenApi();

            app.MapGet("/projects/my", GetMyProjects)
               .WithName("GetMyProjects")
               .WithOpenApi();


        }

        private static IServiceProvider GetServiceProvider(HttpContext context)
        {
            return context.RequestServices; // for activity log

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

        private static async Task<IResult> ArchiveProjects(ArchiveProjectsReq req, IProjectService projectService, HttpContext context)
        {
            // May need to add varification to check if client data is bad.

            if (logDebug) {
                Console.WriteLine("ProjectController.ArchiveProjects - START");
            }
            try 
            {
                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                // var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();

                ArchiveProjectsRes result = await projectService.ArchiveProjects(req.projectIDs);

                int submitterID = MOCKEDUSERID;

                if (activityLogService == null) {
                    return Results.StatusCode(500);
                }
                try
                {
                    foreach (var projectID in req.projectIDs)
                    {
                        var user = await userService.GetUser(submitterID);
                        string username = user.Name;
                        var project = await projectService.GetProject(projectID);
                        var projectName = project.name;
                        string theDescription = "";
                        if (verboseLogs) {
                            theDescription = $"{username} (User ID: {submitterID}) archived project {projectName} (Project ID: {projectID})";
                        } else {
                            theDescription = $"{user.Email} archived project {projectName}";
                        }

                        if (logDebug) {
                            theDescription += "[Add Log called by ProjectController.ArchiveProjects]";
                            Console.WriteLine(theDescription);
                        }
                        await activityLogService.AddLogAsync(new CreateActivityLogDto
                        {
                            userID = submitterID, 
                            changeType = "Archived",
                            description = theDescription,
                            projID = projectID,
                            assetID = "", 
                            isAdminAction = adminActionTrue
                        });
                    }



                //     UserID = logDto.userID,
                // ChangeType = logDto.changeType,
                // Description = logDto.description,
                // ProjectID = logDto.projectID,
                // AssetID = logDto.assetID,
                // IsAdminAction = logDto.isAdminAction
                } catch (Exception ex) {
                    Console.WriteLine("Failed to add log - ProjectController.ArchiveProjects");
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
        
        private static async Task<IResult> AssociateAssetsWithProject(int projectID, AssociateAssetsWithProjectReq request, IProjectService projectService, ILogger<Program> logger, HttpContext context)
        {
            if (logDebug) {
                Console.WriteLine("ProjectController.AssociateAssetWithProject - START");
            }
            try
            {
                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                // var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();
                // Ensure the projectID in the route matches the one in the request.
                if (projectID != request.ProjectID)
                {
                    return Results.BadRequest("Project ID mismatch between route and request body.");
                }

                int submitterId = MOCKEDUSERID; // Replace with the authenticated user ID in production.

                



                AssociateAssetsWithProjectRes result = await projectService.AssociateAssetsWithProject(request, submitterId);
                var user = await userService.GetUser(submitterId);
                string username = user.Name;
                foreach (var blobID in request.BlobIDs) {
                    var blobName = await projectService.GetAssetNameByBlobIdAsync(blobID);
                    var projectName = await projectService.GetProjectNameByIdAsync(projectID);
                    var theDescription = $"{username} (User ID: {submitterId}) added {blobName} (Asset ID: {blobID}) into project {projectName} (project ID: {projectID})";
                    if (false) { // duplicate log - same as PaletteController.SubmitAssets.
                        if (logDebug) {
                            theDescription += "[Add Log called by ProjectController.AssociateAssetsWithProject]";
                            Console.WriteLine(theDescription);
                        }
                    
                        await activityLogService.AddLogAsync(new CreateActivityLogDto
                        {
                            userID = submitterId,
                            changeType = "Added",
                            description = theDescription,
                            projID = projectID,
                            assetID = blobID,
                            isAdminAction = !adminActionTrue
                        });
                    }
                }
                return Results.Ok(new
                {
                    status = "success",
                    projId = result.ProjectID,
                    updatedImages = result.UpdatedImages,
                    failedAssociations = result.FailedAssociations,
                    message = result.Message
                });
            }
            catch (DataNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in AssociateAssetsWithProject: {ex.Message}");
                return Results.StatusCode(500);
            }
        }

        private static async Task<IResult> UpdateProject(int projectID, UpdateProjectReq req, IProjectService projectService, HttpContext context)
        {

            // Get services from IServiceProvider
            var serviceProvider = GetServiceProvider(context);
            var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
            // var projectService = serviceProvider.GetRequiredService<IProjectService>();
            var userService = serviceProvider.GetRequiredService<IUserService>();
            try
            {
                var result = await projectService.UpdateProject(projectID, req);

                int submitterID = MOCKEDUSERID;

                if (activityLogService == null) {
                    return Results.StatusCode(500);
                }

                // var updateDescription = new StringBuilder();
                var user = await userService.GetUser(submitterID);
                string username = user.Name;
                string updateDescription = "";
                if (verboseLogs) {
                    updateDescription = $"{username} (User ID: {submitterID})";
                } else {
                    updateDescription = $"{user.Email} modified "
                }
                if (verboseLogs) {
                    updateDescription += $"Project ID: {projectID}";
                }

                var projectName = await projectService.GetProjectNameByIdAsync(projectID);
                updateDescription += $"project: {projectName}. ";

                if (!string.IsNullOrEmpty(req.Location))
                {
                    updateDescription += $"Location: {req.Location} ";
                }

                if (req.Memberships != null && req.Memberships.Any())
                {
                    updateDescription += "Memberships: ";
                    foreach (var membership in req.Memberships)
                    {
                        var getUserDto = await userService.GetUser(membership.UserID);
                        var memberUserName = getUserDto.Name;
                        var memberUserEmail = getUserDto.Email;

                        if (verboseLogs) {
                            updateDescription += $"- {memberUserName} (User ID: {membership.UserID}) ";
                        } else {
                            updateDescription += $"- {memberUserEmail} ";
                        }
                    }
                }

                if (req.Tags != null && req.Tags.Any())
                {
                    updateDescription += "Tags: ";
                    foreach (var tag in req.Tags)
                    {
                        updateDescription += $"- {tag.Name}";
                    }
                }

                if (req.CustomMetadata != null && req.CustomMetadata.Any())
                {
                    updateDescription += "Custom Metadata: ";
                    foreach (var customMetadataDto in req.CustomMetadata) // met
                    {
                        updateDescription += $"- {customMetadataDto.FieldName} ";
                    }
                }

                if (logDebug) {
                    updateDescription += "[Add Log called by ProjectController.UpdateProject]";
                    Console.WriteLine(updateDescription);
                }

                await activityLogService.AddLogAsync(new CreateActivityLogDto
                {
                    userID = submitterID,
                    changeType = "Updated",
                    description = updateDescription,
                    projID = projectID,
                    assetID = "", // No assetID for project update
                    isAdminAction = adminActionTrue
                });

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


        private static async Task<IResult> GetAssetFileFromStorage(int projectID, string blobID, string filename, IProjectService projectService)
        {
            int requesterID = MOCKEDUSERID;
            try 
            {
                (byte[] fileContent, string fileDownloadName) = await projectService.GetAssetFileFromStorage(projectID, blobID, filename, requesterID);
                return Results.File(
                    fileContents: fileContent,
                    contentType: "application/zstd", 
                    fileDownloadName: fileDownloadName
                );
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