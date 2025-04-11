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

        private const bool adminActionTrue = true;

        private const bool logDebug = false;
        private const bool verboseLogs = false;

        public static void MapProjectEndpoints(this WebApplication app)
        {
            // TODO: Mostly done; need to check user credentials
            app.MapPatch("/projects/archive", ArchiveProjects).WithName("ArchiveProjects").WithOpenApi();
            app.MapGet("/projects/{projectID}", GetProject).WithName("GetProject").WithOpenApi();
            app.MapGet("/projects/{projectID}/assets/pagination", GetPaginatedProjectAssets).WithName("GetPaginatedProjectAssets").WithOpenApi();
            app.MapGet("/projects/", GetAllProjects).WithName("GetAllProjects").WithOpenApi();
            app.MapPatch("/projects/{projectID}/associate-assets", AssociateAssetsWithProject).WithName("AssociateAssetsWithProject").WithOpenApi();

            // TODO: Return mocked data currently
            // app.MapGet("/projects/logs", GetArchivedProjectLogs).WithName("GetArchivedProjectLogs").WithOpenApi();

            // Update project details endpoint
            app.MapPatch("/projects/{projectID}", UpdateProject).WithName("UpdateProject").WithOpenApi();

            app.MapGet("/projects/my", GetMyProjects)
               .WithName("GetMyProjects")
               .WithOpenApi();

            app.MapDelete("/projects/{projectID}/assets/{blobId}", DeleteAsset)
               .WithName("DeleteAsset")
               .WithOpenApi();


        }

        private static IServiceProvider GetServiceProvider(HttpContext context)
        {
            return context.RequestServices; // for activity log

        }

        private static async Task<IResult> GetPaginatedProjectAssets
        (
            int projectID, 
            HttpContext context,
            int? postedBy,
            string? tagName,
            DateTime? fromDate,
            DateTime? toDate,

            IProjectService projectService,
            string assetType = DefaultAssetType,
            int pageNumber = DefaultPageNumber, 
            int assetsPerPage = DefaultPageSize
        )
        {
            // TODO: Get requester's ID and replace
            int requesterID = Convert.ToInt32(context.Items["userId"]); 
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
                    tagName = tagName,
                    fromDate = fromDate,
                    toDate = toDate
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

        private static async Task<IResult> GetProject(int projectID, IProjectService projectService, ITagRepository tagRepository)
        {
            try 
            {
                GetProjectRes result = await projectService.GetProject(projectID);
                
                // Get all tags to include as suggested tags
                var allTags = await tagRepository.GetTagsAsync();
                result.suggestedTags = allTags.Select(t => new TagCustomInfo
                {
                    tagID = t.TagID,
                    name = t.Name
                }).ToList();
                
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
                GetAllProjectsRes result = await projectService.GetAllProjects(); 
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

        private static async Task<IResult> ArchiveProjects(ArchiveProjectReq req, IProjectService projectService, HttpContext context)
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

                ArchiveProjectRes result = await projectService.ArchiveProject(req.projectID);

                int submitterID = Convert.ToInt32(context.Items["userId"]);

                if (activityLogService == null) {
                    return Results.StatusCode(500);
                }
                try
                {
                    var user = await userService.GetUser(submitterID);
                    string username = user.Name;
                    var project = await projectService.GetProject(req.projectID);
                    var projectName = project.name;
                    string theDescription = "";
                    if (verboseLogs) {
                        theDescription = $"{username} (User ID: {submitterID}) archived project '{projectName}' (Project ID: {req.projectID})";
                    } else {
                        theDescription = $"{user.Email} archived project '{projectName}'";
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
                        projID = req.projectID,
                        assetID = "", 
                        isAdminAction = adminActionTrue
                    });

                } catch (Exception ex) {
                    Console.WriteLine("Failed to add log - ProjectController.ArchiveProjects");
                }
                return Results.Ok(result);

            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
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

                int submitterId = Convert.ToInt32(context.Items["userId"]);

                AssociateAssetsWithProjectRes result = await projectService.AssociateAssetsWithProject(request, submitterId);
                var user = await userService.GetUser(submitterId);
                string username = user.Name;
                foreach (var blobID in request.BlobIDs) {
                    var blobName = await projectService.GetAssetNameByBlobIdAsync(blobID);
                    var projectName = await projectService.GetProjectNameByIdAsync(projectID);
                    string theDescription = "";
                    if (verboseLogs) {
                        theDescription = $"{username} (User ID: {submitterId}) added '{blobName}' (Asset ID: {blobID}) into project '{projectName}' (project ID: {projectID})";
                    } else {
                        theDescription = $"{user.Email} added '{blobName}' into project '{projectName}'";
                    }
                    // if (false) { // duplicate log - same as PaletteController.SubmitAssets. update: removed the add log in SubmitAssets update. note: no longer the same.
                    if (logDebug) {
                        theDescription += "[Add Log called by ProjectController.AssociateAssetsWithProject]";
                        Console.WriteLine(theDescription);
                    }
                
                    // await activityLogService.AddLogAsync(new CreateActivityLogDto DO NOT ADD LOG - This endpoint is called when the user is still on the palette page and has changed which project to potentially add the asset into, but hasn't submitted the asset yet. Therefore, there should not be a log saying "user added asset into project"
                    // {
                    //     userID = submitterId,
                    //     changeType = "Added",
                    //     description = theDescription,
                    //     projID = projectID,
                    //     assetID = blobID,
                    //     isAdminAction = !adminActionTrue
                    // });

                
                    // Assumption: all tags are added to all blobs
                    if (request.TagIDs != null && request.TagIDs.Any())
                    {
                        var tagNames = new List<string>();
                        foreach (var tagID in request.TagIDs)
                        {
                            var tagName = await projectService.GetTagNameByIdAsync(tagID);
                            tagNames.Add(tagName);
                        }

                        string tagList = string.Join(", ", tagNames);
                        string theDescription2 = "";

                        if (verboseLogs)
                        {
                            theDescription2 = $"{username} (User ID: {submitterId}) assigned tags ({tagList}) to '{blobName}' (Asset ID: {blobID})";
                        }
                        else
                        {
                            theDescription2 = $"{user.Email} assigned tags ({tagList}) to '{blobName}'";
                        }

                        if (logDebug)
                        {
                            theDescription2 += "[Add Log called by ProjectController.AssociateAssetsWithProject - add tag part]";
                            Console.WriteLine(theDescription2);
                        }

                        await activityLogService.AddLogAsync(new CreateActivityLogDto
                        {
                            userID = submitterId,
                            changeType = "Assigned",
                            description = theDescription2,
                            projID = projectID,
                            assetID = blobID,
                            isAdminAction = !adminActionTrue
                        });
                    }

                    
                    // }
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

                int submitterID = Convert.ToInt32(context.Items["userId"]);

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
                    updateDescription = $"{user.Email} modified ";
                }
                if (verboseLogs) {
                    updateDescription += $"Project ID: {projectID}";
                }

                var projectName = await projectService.GetProjectNameByIdAsync(projectID);
                updateDescription += $"project: {projectName}";

                if (!string.IsNullOrEmpty(req.Location))
                {
                    updateDescription += $"; Location: {req.Location}";
                }

                if (req.Memberships != null && req.Memberships.Count == 0) { // optional
                    updateDescription += "; Memberships: None";
                }
                else if (req.Memberships != null && req.Memberships.Any())
                {
                    updateDescription += "; Memberships: ";

                    int totalMemberships = req.Memberships.Count();
                    
                    foreach (var (membership, index) in req.Memberships.Select((membership, index) => (membership, index)))
                    {
                        var getUserDto = await userService.GetUser(membership.UserID);
                        var memberUserName = getUserDto.Name;
                        var memberUserEmail = getUserDto.Email;

                        bool isLast = index == totalMemberships - 1;

                        if (verboseLogs) {
                            updateDescription += $"{memberUserName} (User ID: {membership.UserID}) ";
                            if (membership.Role.Equals("Admin")) {
                                updateDescription += $"(Admin)";
                            } else {
                                updateDescription += $"(User)";
                            }
                        } else {
                            if (membership.Role.Equals("Admin")) {
                                updateDescription += $"{memberUserEmail} (Admin)";
                            } else { // User
                                updateDescription += $"{memberUserEmail} (User)";
                            }
                        }

                        if (!isLast)
                        {
                            updateDescription += ", ";
                        }
                    }
                }

                if (req.Tags != null && req.Tags.Count == 0) { // optional
                    
                    updateDescription += "; Tags: None";
                }
                else if (req.Tags != null && req.Tags.Any())
                {
                    updateDescription += "; Tags: ";
                    updateDescription += string.Join(", ", req.Tags.Select(tag => tag.Name));
                }

                if (req.CustomMetadata != null && req.CustomMetadata.Count == 0) { // optional
                    updateDescription += "; Custom Metadata: None";
                }
                else if (req.CustomMetadata != null && req.CustomMetadata.Any())
                {
                    updateDescription += "; Custom Metadata: ";
                    updateDescription += string.Join(", ", req.CustomMetadata.Select(metadata => metadata.FieldName));
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

        private static async Task<IResult> GetMyProjects(IProjectService projectService, HttpContext context)
        {
            int userId = Convert.ToInt32(context.Items["userId"]);
            List<GetProjectRes> result = await projectService.GetMyProjects(userId);
            return Results.Ok(result);
        }
        
        private static async Task<IResult> DeleteAsset(int projectID, string blobId, IProjectService projectService, HttpContext context)
        {
            try
            {
                var assetName = await projectService.GetAssetNameByBlobIdAsync(blobId); // for activity log. Fix Bug: must call this before calling DeleteAssetFromProject in order to save the assetName before deleting it
                await projectService.DeleteAssetFromProject(projectID, blobId);
                try { // add log
                    var serviceProvider = GetServiceProvider(context);
                    var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                    var userService = serviceProvider.GetRequiredService<IUserService>();

                    int submitterID = Convert.ToInt32(context.Items["userId"]);
                    var user = await userService.GetUser(submitterID);
                    string username = user.Name;
                    var projectName = await projectService.GetProjectNameByIdAsync(projectID);
                    string theDescription = "";


                    

                    if (verboseLogs) {

                        theDescription = $"{username} (User ID: {submitterID}) deleted (Blob ID: {blobId}) from project {projectName} (Project ID: {projectID})";
                    } else {
                        theDescription = $"{user.Email} deleted '{assetName}' from project '{projectName}'";
                    }

                    if (string.IsNullOrEmpty(assetName)) {
                        Console.WriteLine("debug - error: asset name not found for blobId: {blobId}");
                    }

                    if (logDebug) {
                        theDescription += "[Add Log called by ProjectController.DeleteAsset]";
                        Console.WriteLine(theDescription);
                    }

                    await activityLogService.AddLogAsync(new CreateActivityLogDto
                    {
                        userID = submitterID,
                        changeType = "Deleted",
                        description = theDescription,
                        projID = projectID,
                        assetID = blobId,
                        isAdminAction = adminActionTrue
                    });
                } catch (Exception ex) { // error in add log
                    Console.WriteLine("Failed to add log - ProjectController.DeleteAsset");

                }
                return Results.Ok(new { message = "Asset deleted successfully."});
            }
            catch (DataNotFoundException ex) {
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
        
    }
}