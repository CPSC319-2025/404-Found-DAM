using Core.Interfaces;
using Core.Dtos;

namespace APIs.Controllers
{
    public static class PaletteController
    {

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


        //     // assign assets in the palette
        //     app.MapPost("/projects/assign-assets", AssignAssetsToProjects).WithName("AssignAssetsToProjects").WithOpenApi();
 
        //    // upload assets permanently
        //     app.MapPost("/projects/upload-assets", UploadAssets).WithName("UploadAssets").WithOpenApi();

        }

        private static async Task<IResult> GetPaletteAssets(HttpRequest request, IPaletteService paletteService)
        {

            int userId = int.Parse(request.Form["UserId"].ToString());

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
                return Results.NotFound("No files found for this user.");
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
        }


        // TODO: Bring back ITestService projectService
        private static async Task<IResult> UploadAssets(HttpRequest request, IPaletteService paletteService)
        {
            // Check if the request has form data
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                return Results.BadRequest("No files uploaded");
            }

            // Get the form fields that match your DTO
            string name = request.Form["Name"].ToString();
            string type = request.Form["Type"].ToString();
            int userId = int.Parse(request.Form["UserId"].ToString());

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
            {
                return Results.BadRequest("Name and Type are required");
            }

            // Create your DTO
            var uploadRequest = new UploadAssetsReq
            {
                Name = name,
                Type = type,
                UserId = userId
            };

            // Create a task for each file
            var results = await paletteService.ProcessUploadsAsync(request.Form.Files.ToList(), uploadRequest);
        
            // Return combined results
            return Results.Ok(results);
        }

        private static async Task<IResult> DeletePaletteAsset(HttpRequest request, IPaletteService paletteService)
        {

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

            if (result) {
                return Results.Ok(new {
                    fileName = deleteRequest.Name,
                });
            } else {
                Console.WriteLine($"Failed to delete asset {deleteRequest.Name}.");
                return Results.NotFound("Failed to delete asset");
            }
        }
        
    }
}