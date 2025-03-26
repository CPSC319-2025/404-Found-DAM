using Core.Interfaces;
using Core.Dtos;
using Infrastructure.Exceptions;

namespace APIs.Controllers
{
    public static class PaletteController
    {
        private const int MOCKEDUSERID = 1;

        // PUT /palette/assets/{assetId} edit asset in the pallete
        // DELETE /projects/assign-assets  delete an asset from palette

        public static void MapPaletteEndpoints(this WebApplication app)
        {
            // assets already in the pallete
        app.MapGet("/palette/assets", async (HttpRequest request, IPaletteService paletteService) =>
        {
            return await GetPaletteAssets(request, paletteService);
        })
        .WithName("GetPaletteAssets")
        .WithOpenApi();

        // Get a specific asset by blobId
        app.MapGet("/palette/assets/{blobId}", async (string blobId, HttpRequest request, IPaletteService paletteService) =>
        {
            return await GetSingleAsset(blobId, request, paletteService);
        })
        .WithName("GetSingleAsset")
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

                // Check if client prefers decompressed files
                bool decompressFiles = request.Query.ContainsKey("decompress") && 
                    bool.TryParse(request.Query["decompress"], out bool decompressValue) && decompressValue;

                // Get size limit parameter from query with default of 10MB
                int sizeLimit = 10 * 1024 * 1024; // Default 10MB
                if (request.Query.ContainsKey("sizeLimit") && 
                    int.TryParse(request.Query["sizeLimit"], out int customLimit)) {
                    sizeLimit = customLimit;
                }

                // Create a task for each file
                var files = await paletteService.GetAssets(uploadRequest);
                // If no files were found
                if (files == null || !files.Any())
                {
                    return Results.Ok(new { assets = Array.Empty<object>() }); 
                }
                
                // Get the metadata for all files (including their sizes)
                var fileMetadata = files.Select(f => new {
                    fileName = f.FileName,
                    size = f.Length,
                    contentType = decompressFiles ? 
                        GetMimeTypeFromFileName(f.FileName.Replace(".zst", "")) : 
                        "application/zstd",
                    blobId = ExtractBlobId(f.FileName)
                }).ToList();

                // Return just the metadata for all files
                return Results.Ok(new { 
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

        // Helper function to extract the blobId from a filename
        private static string ExtractBlobId(string filename)
        {
            // Format: BlobID.OriginalFilename.zst
            var parts = filename.Split('.');
            if (parts.Length < 2)
            {
                return string.Empty;
            }
            
            return parts[0];
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
                ProcessedAsset[] results = await paletteService.ProcessUploadsAsync(request.Form.Files.ToList(), uploadRequest, convertToWebp);
            
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

        private static async Task<IResult> SubmitAssets(int projectID, SubmitAssetsReq req, IPaletteService paletteService)
         {
             // May need to add varification to check if client data is bad.
             try 
             {
                 // TODO: verify submitter is in the system and retrieve the userID; replace the following MOCKEDUSERID
                 int submitterID = MOCKEDUSERID; 
                 Console.WriteLine(req.blobIDs);
                 SubmitAssetsRes result = await paletteService.SubmitAssets(projectID, req.blobIDs, submitterID);
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
                    return Results.Ok(new
                    {
                        message = "Some associations were removed, but some were not found.",
                        removedAssociations = result.RemovedAssociations,
                        notFoundAssociations = result.NotFoundAssociations
                    });
                }
                else if (result.RemovedAssociations.Count > 0 && result.NotFoundAssociations.Count == 0)
                {
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
        
        private static async Task<IResult> GetSingleAsset(string blobId, HttpRequest request, IPaletteService paletteService)
        {
            try 
            {
                int userId = MOCKEDUSERID;

                // Check if we should decompress the file
                bool decompress = request.Query.ContainsKey("decompress") && 
                    bool.TryParse(request.Query["decompress"], out bool decompressValue) && decompressValue;

                // Get the specific file by blobId
                var file = await paletteService.GetAssetByBlobIdAsync(blobId, userId);
                
                if (file == null)
                {
                    return Results.NotFound($"File with blobId {blobId} not found");
                }

                // Get file information
                var fileSize = file.Length;
                var fileName = file.FileName;
                
                // Extract original filename from BlobId.OriginalFilename.zst format
                string originalFileName = fileName;
                if (fileName.EndsWith(".zst"))
                {
                    originalFileName = fileName.Substring(0, fileName.Length - 4); // Remove .zst
                    
                    var parts = originalFileName.Split('.');
                    if (parts.Length > 1)
                    {
                        // Remove blobId prefix
                        originalFileName = string.Join('.', parts.Skip(1));
                    }
                }

                // Determine content type based on original filename and decompress option
                var contentType = decompress
                    ? GetMimeTypeFromFileName(originalFileName)
                    : "application/zstd";

                // For videos, always deliver the entire file when decompression is requested
                var isVideo = contentType.StartsWith("video/");
                
                // Return the whole file for video files when decompress=true
                if (decompress && isVideo)
                {
                    using (var fileStream = file.OpenReadStream())
                    {
                        var memoryStream = new MemoryStream();
                        await fileStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        
                        byte[] fileContents = memoryStream.ToArray();
                        
                        // If decompress is requested and this is a .zst file
                        if (fileName.EndsWith(".zst"))
                        {
                            fileContents = await paletteService.DecompressZstdAsync(fileContents);
                        }
                        
                        return Results.File(
                            fileContents: fileContents,
                            contentType: contentType,
                            fileDownloadName: decompress ? originalFileName : fileName,
                            enableRangeProcessing: true,
                            lastModified: DateTimeOffset.UtcNow,
                            entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"{blobId}\"")
                        );
                    }
                }
                
                // Proceed with normal processing including range requests for other cases
                // Get range headers for chunked download
                var rangeHeader = request.Headers.Range.FirstOrDefault();
                long? startByte = null;
                long? endByte = null;

                if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes="))
                {
                    var rangeValue = rangeHeader.Substring("bytes=".Length);
                    var rangeParts = rangeValue.Split('-');
                    
                    if (rangeParts.Length == 2)
                    {
                        if (!string.IsNullOrEmpty(rangeParts[0]))
                            startByte = Convert.ToInt64(rangeParts[0]);
                        
                        if (!string.IsNullOrEmpty(rangeParts[1]))
                            endByte = Convert.ToInt64(rangeParts[1]);
                    }
                }

                // If range is specified, return just that chunk
                if (startByte.HasValue)
                {
                    // Set default end byte if not specified
                    if (!endByte.HasValue || endByte.Value >= fileSize)
                        endByte = fileSize - 1;
                    
                    var length = endByte.Value - startByte.Value + 1;
                    
                    using (var fileStream = file.OpenReadStream())
                    {
                        fileStream.Seek(startByte.Value, SeekOrigin.Begin);
                        
                        byte[] buffer = new byte[length];
                        await fileStream.ReadAsync(buffer, 0, (int)length);
                        
                        // If decompress is requested and this is a .zst file
                        if (decompress && fileName.EndsWith(".zst"))
                        {
                            // Note: This approach only works for complete files, not for partial chunks
                            // For partial chunks, you'd need a more sophisticated approach
                            // This is why we're only decompressing if it's the full file
                            if (startByte == 0 && endByte == fileSize - 1)
                            {
                                buffer = await paletteService.DecompressZstdAsync(buffer);
                            }
                            else
                            {
                                // We can't decompress partial chunks, so return an error
                                return Results.BadRequest("Cannot decompress partial file chunks. Request the whole file or set decompress=false.");
                            }
                        }
                        
                        return Results.Bytes(
                            contents: buffer,
                            contentType: contentType,
                            fileDownloadName: decompress ? originalFileName : fileName,
                            enableRangeProcessing: true,
                            lastModified: DateTimeOffset.UtcNow,
                            entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"{blobId}\"")
                        );
                    }
                }
                else
                {
                    // Return the whole file
                    using (var fileStream = file.OpenReadStream())
                    {
                        var memoryStream = new MemoryStream();
                        await fileStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        
                        byte[] fileContents = memoryStream.ToArray();
                        
                        // If decompress is requested and this is a .zst file
                        if (decompress && fileName.EndsWith(".zst"))
                        {
                            fileContents = await paletteService.DecompressZstdAsync(fileContents);
                        }
                        
                        return Results.File(
                            fileContents: fileContents,
                            contentType: contentType,
                            fileDownloadName: decompress ? originalFileName : fileName,
                            enableRangeProcessing: true,
                            lastModified: DateTimeOffset.UtcNow,
                            entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"{blobId}\"")
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred getting asset: {ex.Message}");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
        }
    }
}