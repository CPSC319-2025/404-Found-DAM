using Core.Interfaces;
using Core.Dtos;
using Infrastructure.Exceptions;
using Core.Services;

namespace APIs.Controllers
{
    public static class PaletteController
    {
        private const bool AdminActionTrue = true;

        private const bool logDebug = true;

        private const bool verboseLogs = false;

        private static IServiceProvider GetServiceProvider(HttpContext context)
        {
            return context.RequestServices; // for activity log

        }

        public static void MapPaletteEndpoints(this WebApplication app)
        {
            // assets already in the pallete
        app.MapGet("/palette/assets", async (HttpRequest request, IPaletteService paletteService, HttpContext context) =>
        {
            return await GetPaletteAssets(request, paletteService, context);
        })
        .WithName("GetPaletteAssets")
        .WithOpenApi();

        // Update an existing asset by blobId
        app.MapPut("/palette/assets/{blobId}", async (string blobId, HttpRequest request, IPaletteService paletteService) =>
        {
            return await UpdateAsset(blobId, request, paletteService);
        })
        .WithName("UpdateAsset")
        .WithOpenApi();

        

        app.MapPost("/palette/upload", async (HttpRequest request, IPaletteService paletteService, HttpContext context) =>
        {
            return await UploadAssets(request, paletteService, context);
        })
        .WithName("UploadAssets")
        .WithOpenApi();

        // Delete assets in the pallete
        app.MapDelete("/palette/asset", async (HttpRequest request, IPaletteService paletteService, HttpContext context) => 
        {
            return await DeletePaletteAsset(request, paletteService, context);
        })
        .WithName("DeletePaletteAsset")
        .WithOpenApi();

        // Get project and tags by blob id
        app.MapGet("/palette/blob/{blobId}/details", async (string blobId, IPaletteService paletteService) =>
        {
            try
            {
                // int userID = Convert.ToInt32(context.Items["userId"]);
                var result = await paletteService.GetBlobProjectAndTagsAsync(blobId); //also returns tag id in the same order a s tag
                return Results.Ok(result);
            }
            catch (DataNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving blob details: {ex.Message}");
                return Results.StatusCode(500);
            }
        })
        .WithName("GetBlobProjectAndTags")
        .WithOpenApi();

        // Get all fields (field IDs and values) for a specific blob ID
        app.MapGet("/palette/blob/{blobId}/fields", async (string blobId, IPaletteService paletteService) =>
        {
            try
            {
                var result = await paletteService.GetBlobFieldsAsync(blobId);
                return Results.Ok(result);
            }
            catch (DataNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving blob fields: {ex.Message}");
                return Results.StatusCode(500);
            }
        })
        .WithName("GetBlobFields")
        .WithOpenApi();

        //     // assign assets in the palette
        //     app.MapPost("/projects/assign-assets", AssignAssetsToProjects).WithName("AssignAssetsToProjects").WithOpenApi();
 
        //    // upload assets permanently
        //     app.MapPost("/projects/upload-assets", UploadAssets).WithName("UploadAssets").WithOpenApi();

        app.MapPatch("/palette/{projectID}/submit-assets", async (int projectID, SubmitAssetsReq req, IPaletteService paletteService, HttpContext context, HttpRequest request) => 
        {
            // Read the autoNaming query parameter
            bool autoNaming = request.Query.ContainsKey("Auto");
            return await SubmitAssets(projectID, req, paletteService, context, autoNaming);
        }).WithName("SubmitAssets").WithOpenApi();

        app.MapPatch("/palette/assets/tags", RemoveTagsFromAssets)
            .WithName("RemoveTagsFromAssets")
            .WithOpenApi();

        // Add single tag to asset
        app.MapPost("/palette/asset/tag", async (AssignTagToAssetReq request, IPaletteService paletteService, HttpContext context) =>
        {

            if (logDebug) {
                Console.WriteLine("PaletteContoller pallete asset tag endpoint called - START");
            }
            
            try
            {
                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();
                AssignTagResult result = await paletteService.AssignTagToAssetAsync(request.BlobId, request.TagId);
                
                if (result.Success)
                {
                    // add log (DONE)
                    int userID = Convert.ToInt32(context.Items["userId"]);

                    // await _activityLogService.AddLogAsync(userID, "Assigned", "", request.TagId, BlobId)

                    try {
                        var tagName = await paletteService.GetTagNameByIdAsync(request.TagId);
                        var user = await userService.GetUser(userID);
                        string username = user.Name;
                        string assetName = await paletteService.GetAssetNameByBlobIdAsync(request.BlobId);

                        string theDescription = "";

                        if (verboseLogs) {
                            theDescription = $"{username} (User ID: {userID}) assigned tag {tagName} (Tag ID: {request.TagId}) to asset {await paletteService.GetAssetNameByBlobIdAsync(request.BlobId)} (Asset ID: {request.BlobId})";
                        } else {
                            theDescription = $"{user.Email} assigned tag {tagName} to {assetName}";
                        }

                        if (logDebug) {
                            theDescription += "[Add Log called by PaletteController - pallete asset tag endpoint]";
                            Console.WriteLine(theDescription);
                        }
                        await activityLogService.AddLogAsync(new CreateActivityLogDto
                        {
                            userID = userID,
                            changeType = "Assigned",
                            description = theDescription,
                            projID = 0, // Assuming no specific project is associated here
                            assetID = request.BlobId,
                            isAdminAction = AdminActionTrue
                        });
                    } catch (Exception ex) {
                        Console.WriteLine("Failed to add log - PaletteController - pallete asset tag endpoint");
                    }
                    return Results.Ok(result);
                }
                else
                {
                    return Results.BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AssignTagToAsset: {ex.Message}");
                return Results.StatusCode(500);
            }
        })
        .WithName("AssignTagToAsset")
        .WithOpenApi();

        // Assign all project tags to an asset
        app.MapPost("/palette/asset/project-tags", async (AssignProjectTagsToAssetReq request, IPaletteService paletteService, HttpContext context) =>
        {
            try
            {
                int userId = Convert.ToInt32(context.Items["userId"]);

                if (string.IsNullOrEmpty(userId.ToString()))
                {
                    return Results.BadRequest("UserId is required");
                }
                var result = await paletteService.AssignProjectTagsToAssetAsync(request);
                
                if (result.Success)
                {
                    var serviceProvider = GetServiceProvider(context);
                    var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                    var projectService = serviceProvider.GetRequiredService<IProjectService>();
                    var userService = serviceProvider.GetRequiredService<IUserService>();

                    try
                    {
                        var tagNames = new List<string>();
                        foreach (var tagId in result.AssignedTagIds)
                        {
                            var tagName = await paletteService.GetTagNameByIdAsync(tagId);
                            tagNames.Add(tagName);
                        }

                        var assetName = await paletteService.GetAssetNameByBlobIdAsync(request.BlobId);

                        var user = await userService.GetUser(userId);
                        string username = user.Name;

                        string theDescription = "";
                        if (verboseLogs)
                        {
                            theDescription = $"{username} (User ID: {userId}) assigned project tags [{string.Join(", ", tagNames)}] to Asset {assetName} (Asset ID: {request.BlobId})";
                        }
                        else
                        {
                            theDescription = $"{user.Email} assigned project tags ({string.Join(", ", tagNames)}) to {assetName}";
                        }

                        if (logDebug)
                        {
                            theDescription += "[Add Log called by PaletteController /palette/asset/project-tags]";
                            Console.WriteLine(theDescription);
                        }

                        var logDto = new CreateActivityLogDto
                        {
                            userID = userId,
                            changeType = "Assigned",
                            description = theDescription,
                            projID = request.ProjectId,
                            assetID = request.BlobId,
                            isAdminAction = AdminActionTrue
                        };

                        await activityLogService.AddLogAsync(logDto);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to add log - PaletteController /palette/asset/project-tags");
                    }
                    return Results.Ok(result);
                }
                else
                {
                    return Results.BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AssignProjectTagsToAsset: {ex.Message}");
                return Results.StatusCode(500);
            }
        })
        .WithName("AssignProjectTagsToAsset")
        .WithOpenApi();

        }

        private static async Task<IResult> GetPaletteAssets(HttpRequest request, IPaletteService paletteService, HttpContext context)
        {
            try {
                int userId = Convert.ToInt32(context.Items["userId"]);

                if (string.IsNullOrEmpty(userId.ToString()))
                {
                    return Results.BadRequest("UserId is required");
                }

                // Create your DTO
                var uploadRequest = new GetPaletteAssetsReq
                {
                    UserId = userId
                };

                // Get size limit parameter from query with default of 10MB
                int sizeLimit = 10 * 1024 * 1024; // Default 10MB
                if (request.Query.ContainsKey("sizeLimit") && 
                    int.TryParse(request.Query["sizeLimit"], out int customLimit)) {
                    sizeLimit = customLimit;
                }

                // Create a task for each file
                var files = await paletteService.GetAssets(uploadRequest);
                // If no files were found
                if (files == null || !files.BlobUris.Any())
                {
                    return Results.Ok(new { assets = Array.Empty<object>() }); 
                }
                
                // Get the metadata for all files (including their sizes)
                var fileMetadata = files.FileNames.Select((f, index) => new {
                    fileName = f,
                    size = f.Length,
                    contentType = GetMimeTypeFromFileName(f),
                    blobId = ExtractBlobIdFromUri(files.BlobUris[index])
                }).ToList();

                // Return just the metadata for all files
                return Results.Ok(new {
                    blobUris = files.BlobUris, 
                    files = fileMetadata,
                    message = "Get file metadata only. Use /palette/assets/{blobId} endpoint to download individual files."
                });
            } catch (Exception ex) {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return Results.Problem
                (
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
        }

        private static string ExtractBlobIdFromUri(string blobUri)
        {
            // Extract the part between the last / and the ? character
            int lastSlashIndex = blobUri.LastIndexOf('/');
            int questionMarkIndex = blobUri.IndexOf('?');
            
            if (lastSlashIndex != -1 && questionMarkIndex != -1)
            {
                return blobUri.Substring(lastSlashIndex + 1, questionMarkIndex - lastSlashIndex - 1);
            }
            
            // Fallback if format is different
            if (lastSlashIndex != -1 && questionMarkIndex == -1)
            {
                return blobUri.Substring(lastSlashIndex + 1);
            }
            
            return blobUri; // Return the original string if pattern doesn't match
        }

        // Helper function to determine mime type from filename
        private static string GetMimeTypeFromFileName(string filename)
        {
            var extension = Path.GetExtension(filename).ToLowerInvariant();
            
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".ogg" => "video/ogg",
                _ => "application/octet-stream"
            };
        }

        /*
            UploadAssets supports batch uploading, but FE currently will only send 1 asset per call.
            This endpoint allows partial success. That is, the result contains two lists, one for assets 
            uploaded successfully, and the other for failed ones.
        */
        private static async Task<IResult> UploadAssets(HttpRequest request, IPaletteService paletteService, HttpContext context)
        {
            // Console.WriteLine("in UploadAssets");
            try {

                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();
                // Check if the request has form data
                if (!request.HasFormContentType || request.Form.Files.Count == 0)
                {
                    return Results.BadRequest("No files uploaded");
                }

            // Get the form fields that match your DTO
            string uploadTaskName = request.Form["name"].ToString();
            string asasetMimeType = request.Form["mimeType"].ToString().ToLower();
            // int userId = int.Parse(request.Form["userId"].ToString());
            int userId = Convert.ToInt32(context.Items["userId"]);
            string? toWebpParam = request.Query["toWebp"];

            bool convertToWebp = true; // set webp conversion default to true 


            if (string.IsNullOrEmpty(uploadTaskName))
            {
                return Results.BadRequest("Name of batch upload is required");
            }

            if (string.IsNullOrEmpty(asasetMimeType))
            {
                return Results.BadRequest("mimeType is required");
            }
            else if (!asasetMimeType.Contains("/")) 
            {
                return Results.BadRequest("incorrect mimeType format");
            }
            
            // Get convertToWebp's actual value if supplied by user
            if (!string.IsNullOrEmpty(toWebpParam))
            {
                if (bool.TryParse(toWebpParam, out bool parsedToWebp))
                {
                    convertToWebp = parsedToWebp;
                }
                else 
                {
                    return Results.BadRequest("Invalid value for toWebp query param");
                }
            }

            // Create your DTO
            var uploadRequest = new UploadAssetsReq
            {
                UploadTaskName = uploadTaskName,
                AssetMimeType = asasetMimeType,
                UserId = userId
            };

                // Create a task for each file
                ProcessedAsset[] results = await paletteService.ProcessUploadsAsync(request.Form.Files.ToList(), uploadRequest, convertToWebp);


                var user = await userService.GetUser(userId);
                string username = user.Name;
                

                
            
                List<ProcessedAsset> SuccessfulUploads = new List<ProcessedAsset>();
                List<ProcessedAsset> FailedUploads = new List<ProcessedAsset>();

                foreach (var result in results)
                {
                    if (result.Success == true)
                    {
                        SuccessfulUploads.Add(result);
                    } 
                    else 
                    {
                        FailedUploads.Add(result);
                    }
                }

                UploadAssetsRes res = new UploadAssetsRes
                {
                    SuccessfulUploads = SuccessfulUploads,
                    FailedUploads = FailedUploads
                };

                if (SuccessfulUploads.Count == 0)
                {
                    // If all upload tasks failed, return status code 500 with message
                    return Results.Problem
                    (
                        detail: "Server failed to upload all assets",
                        statusCode: 500,
                        title: "Internal Server Error"
                    );
                } 
                else 
                {
                    // add log (done)
                    try 
                    {
                        foreach (var file in SuccessfulUploads)
                        {
                            string theDescription = "";
                            if (verboseLogs) {
                                theDescription = $"{username} (User ID: {userId}) uploaded asset {file.FileName} (Asset ID: {file.FileName}) to their palette";
                            } else {
                                theDescription = $"{user.Email} uploaded {file.FileName} to their palette";
                            }

                            if (logDebug) {
                                theDescription += "[Add log called by PaletteController.UploadAssets]";
                                Console.WriteLine(theDescription);
                            }
                            var logDto = new CreateActivityLogDto
                            {
                                userID = userId,
                                changeType = "Uploaded",
                                description = theDescription,
                                projID = 0, // Assuming no specific project is associated here
                                assetID = file.FileName,
                                isAdminAction = !AdminActionTrue
                            };
                            await activityLogService.AddLogAsync(logDto);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine("Failed to add log - PaletteController.UploadAssets");
                    }
                    Console.WriteLine($"res: {res}");
                    return Results.Ok(res);
                }
            } catch (Exception ex) {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return Results.Problem
                (
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
            
        }

        private static async Task<IResult> DeletePaletteAsset(HttpRequest request, IPaletteService paletteService, HttpContext context)
        {
            if (logDebug) {
                Console.WriteLine("PaletteController.DeletePaletteAsset - START");
            }
            try {

                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();
                // Get the form fields that match your DTO
                string name = request.Form["Name"].ToString();
                // int userId = int.Parse(request.Form["UserId"].ToString());
                int userId = Convert.ToInt32(context.Items["userId"]);

                

                if (string.IsNullOrEmpty(name))
                {
                    return Results.BadRequest("Name and Type are required");
                }

                // Create your DTO
                var deleteRequest = new DeletePaletteAssetReq
                {
                    Name = name,
                    UserId = userId
                };
                var assetName = await projectService.GetAssetNameByBlobIdAsync(deleteRequest.Name); // for activity log

                // Create a task for each file
                var result = await paletteService.DeleteAssetAsync(deleteRequest);

                var user = await userService.GetUser(userId);
                string username = user.Name;

                // add log (asked on Discord, unclear if Name == BlobID or not). I am assuming that Name == BlobID


                string theDescription = "";
                try {
                    if (verboseLogs) {
                        theDescription = $"{username} (User ID: {userId}) deleted asset {assetName} (Asset ID: {deleteRequest.Name}) from their palette.";
                    } else {
                        theDescription = $"{user.Email} deleted asset {assetName} from their palette";
                    }
                    if (logDebug) {
                        theDescription += "[Add log called by PaletteController.DeletePaletteAsset]";
                        Console.WriteLine(theDescription);
                    }
                    var logDto = new CreateActivityLogDto
                    {
                        userID = userId,
                        changeType = "Deleted",
                        description = theDescription,
                        projID = 0, // Assuming no specific project is associated here
                        assetID = deleteRequest.Name,
                        isAdminAction = !AdminActionTrue
                    };
                    await activityLogService.AddLogAsync(logDto);

                } catch (Exception ex) {
                    Console.WriteLine("Failed to add log - PaletteController.DeletePaletteAsset");
                }
                return Results.Ok(new {
                    fileName = deleteRequest.Name,
                });
            } catch (Exception ex) {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return Results.Problem
                (
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
            
        }

        private static async Task<IResult> SubmitAssets(int projectID, SubmitAssetsReq req, IPaletteService paletteService, HttpContext context, bool autoNaming = false)
         {
             // May need to add varification to check if client data is bad.
             if (logDebug) {
                Console.WriteLine("PaletteController.SubmitAssets - START");
                if (autoNaming) {
                    Console.WriteLine("Auto-naming is enabled for this submission");
                }
             }
             try 
             {
                 // TODO: verify submitter is in the system and retrieve the userID
                 // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();
                 int submitterID = Convert.ToInt32(context.Items["userId"]); 
                 Console.WriteLine(req.blobIDs);
                 SubmitAssetsRes result = await paletteService.SubmitAssets(projectID, req.blobIDs, submitterID, autoNaming);


                 // add log (done)
                try {

                    foreach (var blobID in req.blobIDs)
                    {
                        string assetName = await paletteService.GetAssetNameByBlobIdAsync(blobID);

                        
                        string projectName = await paletteService.GetProjectNameByIdAsync(projectID);

                        var user = await userService.GetUser(submitterID);
                        string username = user.Name;
                        string theDescription = "";

                        if (verboseLogs) {

                            theDescription = $"{username} (User ID: {submitterID}) added asset {assetName} (Asset ID: {blobID}) into project {projectName} (Project ID: {projectID}).";
                        } else {
                            theDescription = $"{user.Email} added {assetName} into project {projectName}";
                        }

                        if (logDebug) {
                            theDescription += "[Add Log called by PaletteController.SubmitAssets]";
                            Console.WriteLine(theDescription);
                        }

                        var logDto = new CreateActivityLogDto
                        {
                            userID = submitterID,
                            changeType = "Added",
                            description = theDescription,
                            projID = projectID,
                            assetID = blobID,
                            isAdminAction = !AdminActionTrue
                        };

                        await activityLogService.AddLogAsync(logDto);
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return Results.Problem
                    (
                        detail: ex.Message,
                        statusCode: 500,
                        title: "Internal Server Error"
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

        private static async Task<IResult> RemoveTagsFromAssets(RemoveTagsFromPaletteReq request, IPaletteService paletteService, HttpContext context)
        {

            if (logDebug) {
                Console.WriteLine("PaletteController.RemoveTagsFromAssets - START");
            }
            
            try
            {

                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();
                int submitterID = Convert.ToInt32(context.Items["userId"]);
                
                RemoveTagsResult result = await paletteService.RemoveTagsFromAssetsAsync(request.BlobIds, request.TagIds);
                
                if (result.RemovedAssociations.Count == 0 && result.NotFoundAssociations.Count > 0)
                {
                    return Results.BadRequest(new
                    {
                        message = "No associations were found for the specified BlobIds and TagIds. Nothing was removed.",
                        notFoundAssociations = result.NotFoundAssociations
                    });
                }
                else if (result.RemovedAssociations.Count > 0 && result.NotFoundAssociations.Count > 0)
                {


                    // add log (done)
                    try {
                        foreach (var blobId in request.BlobIds)
                        {
                            var tagNames = new List<string>();
                            foreach (var tagId in request.TagIds)
                            {
                                var tagName = await paletteService.GetTagNameByIdAsync(tagId);
                                if (verboseLogs) {
                                    tagNames.Add($"{tagName} (Tag ID: {tagId})");
                                } else {
                                    tagNames.Add($"{tagName}");
                                }
                            }
                        
                            var assetName = await paletteService.GetAssetNameByBlobIdAsync(blobId);

                            var user = await userService.GetUser(submitterID);
                            string username = user.Name;

                            string theDescription = "";
                            if (verboseLogs) {

                                theDescription = $"{username} (User ID: {submitterID}) removed tags [{string.Join(", ", tagNames)}] from Asset {assetName} (Asset ID: {blobId})";
                            } else {
                                theDescription = $"{user.Email} removed tags ({string.Join(", ", tagNames)}) from {assetName}";
                            }
                            if (logDebug) {
                                theDescription += "[Add Log called by PaletteController.RemoveTagsFromAssets - else if (result.RemovedAssociations.Count > 0 && result.NotFoundAssociations.Count > 0)]";
                                Console.WriteLine(theDescription);
                            }

                            var logDto = new CreateActivityLogDto
                            {
                                userID = submitterID,
                                changeType = "Removed",
                                description = theDescription,
                                projID = 0, // no project
                                assetID = blobId,
                                isAdminAction = !AdminActionTrue
                            };

                            await activityLogService.AddLogAsync(logDto);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine("Failed to add log - PaletteController.RemoveTagsFromAssets - else if (result.RemovedAssociations.Count > 0 && result.NotFoundAssociations.Count > 0)]");
                    }

                    return Results.Ok(new
                    {
                        message = "Some associations were removed, but some were not found.",
                        removedAssociations = result.RemovedAssociations,
                        notFoundAssociations = result.NotFoundAssociations
                    });
                }
                else if (result.RemovedAssociations.Count > 0 && result.NotFoundAssociations.Count == 0)
                {

                    // add log (done)
                    try {
                        foreach (var blobId in request.BlobIds)
                        {
                            var tagNames = new List<string>();
                            
                            foreach (var tagId in request.TagIds)
                            {
                                string tagName = await paletteService.GetTagNameByIdAsync(tagId);
                                if (verboseLogs) {
                                    tagNames.Add($"{tagName} (Tag ID: {tagId})");
                                } else {
                                    tagNames.Add($"{tagName}");
                                }
                            }

                            string tagDescription = string.Join(", ", tagNames);
                            string assetName = await paletteService.GetAssetNameByBlobIdAsync(blobId);

                            var user = await userService.GetUser(submitterID);
                            string username = user.Name;
                            string theDescription = "";

                            if (verboseLogs) {
                                theDescription = $"{username} (User ID: {submitterID}) removed tags [{string.Join(", ", tagNames)}] from Asset {assetName} (Asset ID: {blobId})";
                            } else {
                                theDescription = $"{user.Email} removed tags ({string.Join(", ", tagNames)}) from {assetName}";
                            }
                            if (logDebug) {
                                theDescription += "[Add Log called by PaletteController.RemoveTagsFromAssets] - else if (result.RemovedAssociations.Count > 0 && result.NotFoundAssociations.Count == 0)";
                                Console.WriteLine(theDescription);
                            }

                            var logDto = new CreateActivityLogDto
                            {
                                userID = submitterID,
                                changeType = "Removed",
                                description = theDescription,
                                projID = 0, // no project
                                assetID = blobId,
                                isAdminAction = !AdminActionTrue
                            };
                            
                            await activityLogService.AddLogAsync(logDto);
                        }
                        } catch (Exception ex) {
                            Console.WriteLine("Failed to add log - PaletteController.RemoveTagsFromAssets - else if (result.RemovedAssociations.Count > 0 && result.NotFoundAssociations.Count == 0)");
                        }

                        return Results.Ok(new
                        {
                            message = "All specified associations were successfully removed.",
                            removedAssociations = result.RemovedAssociations
                        });
                    }
                else
                {
                    return Results.BadRequest(new { message = "No associations were specified in the request." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveTagsFromAssets: {ex.Message}");
                return Results.StatusCode(500);
            }
        }

        private static async Task<IResult> UpdateAsset(string blobId, HttpRequest request, IPaletteService paletteService)
        {
            try
            {
                // Check if the request has form data
                if (!request.HasFormContentType || request.Form.Files.Count == 0)
                {
                    return Results.BadRequest("No files uploaded");
                }

                // Get the form fields
                string assetMimeType = request.Form["mimeType"].ToString().ToLower();
                int userId = int.Parse(request.Form["userId"].ToString());
                string? toWebpParam = request.Query["toWebp"];

                bool convertToWebp = true; // set webp conversion default to true 

                if (string.IsNullOrEmpty(assetMimeType))
                {
                    return Results.BadRequest("mimeType is required");
                }
                else if (!assetMimeType.Contains("/")) 
                {
                    return Results.BadRequest("incorrect mimeType format");
                }
                
                // Get convertToWebp's actual value if supplied by user
                if (!string.IsNullOrEmpty(toWebpParam))
                {
                    if (bool.TryParse(toWebpParam, out bool parsedToWebp))
                    {
                        convertToWebp = parsedToWebp;
                    }
                    else 
                    {
                        return Results.BadRequest("Invalid value for toWebp query param");
                    }
                }

                // Check if blob exists
                try
                {


                    // Create update request
                    var updateRequest = new UpdateAssetReq
                    {
                        BlobId = blobId,
                        AssetMimeType = assetMimeType,
                        UserId = userId
                    };

                    // Process the updated file
                    var updatedFile = request.Form.Files[0];
                    var result = await paletteService.UpdateAssetAsync(updatedFile, updateRequest, convertToWebp);
                    
                    if (result.Success)
                    {
                        return Results.Ok(new { 
                            message = $"Asset with blobId {blobId} updated successfully",
                            asset = result 
                        });
                    }
                    else
                    {
                        return Results.BadRequest(new { 
                            error = result.ErrorMessage ?? "Failed to update asset" 
                        });
                    }
                }
                catch (DataNotFoundException ex)
                {
                    return Results.NotFound(ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred updating asset: {ex.Message}");
                return Results.Problem
                (
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
        }
    }
}