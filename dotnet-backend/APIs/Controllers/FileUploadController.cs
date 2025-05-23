using Core.Interfaces;
using Core.Dtos;
using Infrastructure.Exceptions;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Controllers
{
    public static class FileUploadController
    {

        private const bool verboseLogs = false;

        private const bool logDebug = false;

        private static IServiceProvider GetServiceProvider(HttpContext context)
        {
            return context.RequestServices; // for activity log

        }

        public static void MapFileUploadEndpoints(this WebApplication app)
        {
            // Test endpoint to verify service is running
            app.MapGet("/test", () => "Server is running")
                .WithName("TestServer")
                .WithOpenApi();

            // Endpoint for receiving file chunks
            app.MapPost("/upload/chunk", async (HttpRequest request, IFileService fileService, HttpContext context) =>
            {
                return await UploadChunk(request, fileService, context);
            })
            .WithName("UploadChunk")
            .WithOpenApi();

            // Get merged file info
            app.MapGet("/files/{fileName}", async (string fileName, IFileService fileService) =>
            {
                try
                {
                    var fileInfo = await fileService.GetFileInfoAsync(fileName);
                    return Results.Ok(fileInfo);
                }
                catch (DataNotFoundException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving file: {ex.Message}");
                    return Results.StatusCode(500);
                }
            })
            .WithName("GetFileInfo")
            .WithOpenApi();
        }

        private static async Task<IResult> UploadChunk(HttpRequest request, IFileService fileService, HttpContext context)
        {
            try
            {

                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();
                var queue = context.RequestServices.GetRequiredService<IBackgroundTaskQueue>();


                // Check if request has form data
                if (!request.HasFormContentType || request.Form.Files.Count == 0)
                {
                    return Results.BadRequest("No files uploaded");
                }

                // Get form fields
                var file = request.Form.Files[0];
                
                if (!int.TryParse(request.Form["chunkNumber"], out int chunkNumber))
                {
                    return Results.BadRequest("Invalid chunk number");
                }
                
                if (!int.TryParse(request.Form["totalChunks"], out int totalChunks))
                {
                    return Results.BadRequest("Invalid total chunks");
                }
                
                string fileName = request.Form["originalname"];
                int userId = Convert.ToInt32(context.Items["userId"]);

                if (string.IsNullOrEmpty(fileName))
                {
                    return Results.BadRequest("Filename is required");
                }

                // Create a DTO for the upload request
                var uploadChunkRequest = new UploadChunkRequest
                {
                    ChunkNumber = chunkNumber,
                    TotalChunks = totalChunks,
                    FileName = fileName,
                    UserId = userId,
                    File = file
                };

                // Process the chunk
                var result = await fileService.ProcessChunkAsync(uploadChunkRequest);

                if (result.IsLastChunk && result.AllChunksReceived)
                {
                    // If this is the last chunk and all chunks are present, merge them
                    var mergeResult = await fileService.MergeChunksAsync(fileName, totalChunks, userId);
                    
                    if (mergeResult.Success)
                    {
                        Console.WriteLine("fileuploadcontroller.upload chunk mergeResult.Sucess");
                        
                        // Fire and forget activity logging to not block the response // add log in background
                        queue.QueueBackgroundWorkItem(async token =>
                        {
                            using var scope = context.RequestServices
                                                    .GetRequiredService<IServiceScopeFactory>()
                                                    .CreateScope();

                            var activityLogService = scope.ServiceProvider.GetRequiredService<IActivityLogService>();
                            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                            var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();

                            try
                            {
                                var user = await userService.GetUser(userId);
                                var assetName = await projectService.GetAssetNameByBlobIdAsync(mergeResult.BlobId);
                                string theDescription = "";
                                if (verboseLogs) {
                                    theDescription = $"User ID: {userId} uploaded '{assetName}' to their palette";                           
                                } else {
                                    theDescription = $"{user.Email} uploaded '{assetName}' to their palette";
                                }

                                if (logDebug)
                                {
                                    theDescription += "[Logged by Background Queue - Add Log called by FileUploadController.UploadChunk]";
                                    Console.WriteLine(theDescription);
                                }

                                await activityLogService.AddLogAsync(new CreateActivityLogDto
                                {
                                    userID = userId,
                                    changeType = "Uploaded",
                                    description = theDescription,
                                    projID = -1,
                                    assetID = mergeResult.BlobId,
                                    isAdminAction = false
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to add log in background: {ex.Message}");
                            }
                        });

                        return Results.Ok(new
                        {
                            message = "File uploaded and merged successfully",
                            filePath = mergeResult.FilePath, 
                            blobId = mergeResult.BlobId
                        });
                    }
                    else
                    {
                        return Results.Problem(
                            detail: mergeResult.ErrorMessage,
                            statusCode: 500,
                            title: "Error merging chunks"
                        );
                    }
                }

                // For chunk uploads
                if (result.IsLastChunk)
                {
                    Console.WriteLine("fileuploadcontroller.upload chunk result.IsLastChunk");
                    // Last chunk but not all chunks received yet
                    return Results.Ok(new
                    {
                        message = "Last chunk uploaded successfully, waiting for all chunks",
                        chunkNumber = chunkNumber,
                        totalChunks = totalChunks,
                        fileName = fileName
                    });
                }
                Console.WriteLine("fileuploadcontroller.upload chunk ELSE");
                
                return Results.Ok(new
                {
                    message = "Chunk uploaded successfully",
                    chunkNumber = chunkNumber,
                    totalChunks = totalChunks,
                    fileName = fileName
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
        }
    }
} 