using Core.Interfaces;
using Core.Dtos;
using Infrastructure.Exceptions;

namespace APIs.Controllers
{
    public static class PaletteController
    {
        private const int MOCKEDUSERID = 1;

        private const bool AdminActionTrue = true;

        // PUT /palette/assets/{assetId} edit asset in the pallete
        // DELETE /projects/assign-assets  delete an asset from palette

        private static IActivityLogService _activityLogService;
        public static void Initialize(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;

        }

        public static void MapPaletteEndpoints(this WebApplication app)
        {
            // assets already in the pallete
        app.MapGet("/palette/assets", async (HttpRequest request, IPaletteService paletteService) =>
        {
            return await GetPaletteAssets(request, paletteService);
        })
        .WithName("GetPaletteAssets")
        .WithOpenApi();

        app.MapPost("/palette/upload", async (HttpRequest request, IPaletteService paletteService) =>
        {
            return await UploadAssets(request, paletteService);
        })
        .WithName("UploadAssets")
        .WithOpenApi();

        // Delete assets in the pallete
        app.MapDelete("/palette/asset", async (HttpRequest request, IPaletteService paletteService) => 
        {
            return await DeletePaletteAsset(request, paletteService);
        })
        .WithName("DeletePaletteAsset")
        .WithOpenApi();
        
        // update the images in the palette with the selected project tags
        app.MapPatch("/palette/images/tags", async (AssignTagsToPaletteReq request, IPaletteService paletteService, ILogger<Program> logger) => 
        {
            var result = await paletteService.AddTagsToPaletteImagesAsync(request.ImageIds, request.ProjectId);
            if (result) {

                // // add log (todo, half done): NO LONGER BEING DONE AS PER CONVERSATION OVER DISCORD
                // int userID = MOCKEDUSERID;



                // foreach (imageID in request.ImageIds) {
                //     await _activityLogService.AddLogAsync(userID, "Added", "DESCRIPTION TO BE UPDATED TODO - Tags", request.projectId, imageID) 
                // }

                return Results.Ok(new {
                    status = "success",
                    projectId = request.ProjectId,
                    updatedImages = request.ImageIds,
                    message = "Tags successfully added to selected images in the palette."
                });
            } else {
                Console.WriteLine($"Failed to assign project tags to images for ProjectId {result}.");
                return Results.NotFound("Failed to assign project tags to images");
            }
        })
        .WithName("ModifyTags")
        .WithOpenApi();

        // Get project and tags by blob id
        app.MapGet("/palette/blob/{blobId}/details", async (string blobId, IPaletteService paletteService) =>
        {
            try
            {
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

        //     // assign assets in the palette
        //     app.MapPost("/projects/assign-assets", AssignAssetsToProjects).WithName("AssignAssetsToProjects").WithOpenApi();
 
        //    // upload assets permanently
        //     app.MapPost("/projects/upload-assets", UploadAssets).WithName("UploadAssets").WithOpenApi();

        app.MapPatch("/palette/{projectID}/submit-assets", SubmitAssets).WithName("SubmitAssets").WithOpenApi();

         app.MapPatch("/palette/assets/tags", RemoveTagsFromAssets)
               .WithName("RemoveTagsFromAssets")
               .WithOpenApi();

        // Add single tag to asset
        app.MapPost("/palette/asset/tag", async (AssignTagToAssetReq request, IPaletteService paletteService) =>
        {
            try
            {
                AssignTagResult result = await paletteService.AssignTagToAssetAsync(request.BlobId, request.TagId);
                
                if (result.Success)
                {
                    // add log (DONE)
                    int userID = MOCKEDUSERID;

                    // await _activityLogService.AddLogAsync(userID, "Assigned", "", request.TagId, BlobId)

                    var tagName = await paletteService.GetTagNameByIdAsync(request.TagId);
                    await _activityLogService.AddLogAsync(new CreateActivityLogDto
                    {
                        userID = userID,
                        changeType = "Assigned",
                        description = $"User {userID} assigned tag {tagName} (Tag ID: {request.TagId}) to asset {await paletteService.GetAssetNameByBlobIdAsync(request.BlobId)} (Blob ID: {request.BlobId}).",
                        projectID = 0, // Assuming no specific project is associated here
                        assetID = request.BlobId,
                        isAdminAction = AdminActionTrue
                    });
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

        }

        private static async Task<IResult> GetPaletteAssets(HttpRequest request, IPaletteService paletteService)
        {
            // Console.WriteLine("Request received. Form data: " + string.Join(", ", request.Form.Keys));
            try {
                int userId = MOCKEDUSERID;

                if (string.IsNullOrEmpty(userId.ToString()))
                {
                    return Results.BadRequest("UserId is required");
                }

                // Create your DTO
                var uploadRequest = new GetPaletteAssetsReq
                {
                    UserId = userId
                };

                // Create a task for each file
                var files = await paletteService.GetAssets(uploadRequest);
                // If no files were found
                if (files == null || !files.Any())
                {
                    return Results.Ok(new { assets = Array.Empty<object>() }); 
                }
                
                // If there's only one file, return it directly
                if (files.Count == 1)
                {
                    var file = files[0];
                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        return Results.File(
                            fileContents: memoryStream.ToArray(),
                            contentType: "application/zstd",  // Use appropriate MIME type for zstd
                            fileDownloadName: file.FileName
                        );
                    }
                }
                
                // If multiple files, create a zip archive containing the already-compressed .zst files
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        foreach (var file in files)
                        {
                            // Create a zip entry with the original filename
                            var zipEntry = archive.CreateEntry(file.FileName, System.IO.Compression.CompressionLevel.NoCompression); // Use NoCompression since files are already compressed
                            
                            // Write the .zst file content to the zip entry
                            using (var entryStream = zipEntry.Open())
                            using (var fileStream = file.OpenReadStream())
                            {
                                await fileStream.CopyToAsync(entryStream);
                            }
                        }
                    }
                    
                    memoryStream.Position = 0;
                    return Results.File(
                        fileContents: memoryStream.ToArray(),
                        contentType: "application/zip",
                        fileDownloadName: $"zst-files-{DateTime.Now:yyyyMMddHHmmss}.zip"
                    );
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


        // TODO: Bring back ITestService projectService
        private static async Task<IResult> UploadAssets(HttpRequest request, IPaletteService paletteService)
        {
            try {
                // Check if the request has form data
                if (!request.HasFormContentType || request.Form.Files.Count == 0)
                {
                    return Results.BadRequest("No files uploaded");
                }

            // Get the form fields that match your DTO
            string uploadTaskName = request.Form["name"].ToString();
            string asasetMimeType = request.Form["mimeType"].ToString().ToLower();
            int userId = int.Parse(request.Form["userId"].ToString());
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
                var results = await paletteService.ProcessUploadsAsync(request.Form.Files.ToList(), uploadRequest, convertToWebp);

                // add log (todo)
                foreach (var file in request.Form.Files)
                {
                    var logDto = new CreateActivityLogDto
                    {
                        userID = MOCKEDUSERID,
                        changeType = "Uploaded",
                        description = $"User {MOCKEDUSERID} uploaded asset {file.FileName} (Asset ID: {file.FileName})",
                        projectID = 0, // Assuming no specific project is associated here
                        assetID = file.FileName,
                        isAdminAction = !AdminActionTrue
                    };
                    await _activityLogService.AddLogAsync(logDto);
                }
            
                // Return combined results
                return Results.Ok(results);
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

        private static async Task<IResult> DeletePaletteAsset(HttpRequest request, IPaletteService paletteService)
        {
            try {
                // Get the form fields that match your DTO
                string name = request.Form["Name"].ToString();
                int userId = int.Parse(request.Form["UserId"].ToString());

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

                // Create a task for each file
                var result = await paletteService.DeleteAssetAsync(deleteRequest);

                // add log (asked on Discord, unclear if Name == BlobID or not). I am assuming that Name == BlobID
                var logDto = new CreateActivityLogDto
                {
                    userID = MOCKEDUSERID,
                    changeType = "Deleted",
                    description = $"User {MOCKEDUSERID} deleted asset {deleteRequest.Name} (Asset ID: {deleteRequest.Name}).",
                    projectID = 0, // Assuming no specific project is associated here
                    assetID = deleteRequest.Name,
                    isAdminAction = !AdminActionTrue
                };
                await _activityLogService.AddLogAsync(logDto);



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

        private static async Task<IResult> SubmitAssets(int theProjectID, SubmitAssetsReq req, IPaletteService paletteService)
         {
             // May need to add varification to check if client data is bad.
             try 
             {
                 // TODO: verify submitter is in the system and retrieve the userID; replace the following MOCKEDUSERID
                 int submitterID = MOCKEDUSERID; 
                 Console.WriteLine(req.blobIDs);
                 SubmitAssetsRes result = await paletteService.SubmitAssets(theProjectID, req.blobIDs, submitterID);


                 // add log (done)
                 foreach (var blobID in req.blobIDs)
                {
                    string assetName = await paletteService.GetAssetNameByBlobIdAsync(blobID);

                    
                    string projectName = await paletteService.GetProjectNameByIdAsync(theProjectID);

                    var logDto = new CreateActivityLogDto
                    {
                        userID = submitterID,
                        changeType = "Added",
                        description = $"User {submitterID} added blob {assetName} (Blob ID: {blobID}) into project {projectName} (Project ID: {theProjectID}).",
                        projectID = theProjectID,
                        assetID = blobID,
                        isAdminAction = !AdminActionTrue
                    };

                    await _activityLogService.AddLogAsync(logDto);
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

        private static async Task<IResult> RemoveTagsFromAssets(RemoveTagsFromPaletteReq request, IPaletteService paletteService)
        {
            try
            {
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
                    foreach (var blobId in request.BlobIds)
                    {
                        var tagNames = new List<string>();
                        foreach (var tagId in request.TagIds)
                        {
                            var tagName = await paletteService.GetTagNameByIdAsync(tagId);
                            tagNames.Add($"{tagName} (Tag ID: {tagId})");
                        }
                    
                        var assetName = await paletteService.GetAssetNameByBlobIdAsync(blobId);

                        var logDto = new CreateActivityLogDto
                        {
                            userID = MOCKEDUSERID,
                            changeType = "Removed",
                            description = $"User {MOCKEDUSERID} removed tags [{string.Join(", ", tagNames)}] from Blob {assetName} (Blob ID: {blobId}).",
                            projectID = 0, // no project
                            assetID = blobId,
                            isAdminAction = !AdminActionTrue
                        };

                        await _activityLogService.AddLogAsync(logDto);
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
                    foreach (var blobId in request.BlobIds)
                    {
                        var tagNames = new List<string>();
                        
                        foreach (var tagId in request.TagIds)
                        {
                            string tagName = await paletteService.GetTagNameByIdAsync(tagId);
                            tagNames.Add($"{tagName} (Tag ID: {tagId})");
                        }

                        string tagDescription = string.Join(", ", tagNames);
                        string assetName = await paletteService.GetAssetNameByBlobIdAsync(blobId);

                        var logDto = new CreateActivityLogDto
                        {
                            userID = MOCKEDUSERID,
                            changeType = "Removed",
                            description = $"User {MOCKEDUSERID} removed tags [{tagDescription}] from Blob {assetName} (Blob ID: {blobId}).",
                            projectID = 0, // no project
                            assetID = blobId,
                            isAdminAction = !AdminActionTrue
                        };
                        
                        await _activityLogService.AddLogAsync(logDto);
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
        
    }
}