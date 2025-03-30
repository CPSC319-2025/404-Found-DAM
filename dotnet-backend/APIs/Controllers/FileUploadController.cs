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
        private const int MOCKEDUSERID = 1;

        public static void MapFileUploadEndpoints(this WebApplication app)
        {
            // Test endpoint to verify service is running
            app.MapGet("/test", () => "Server is running")
                .WithName("TestServer")
                .WithOpenApi();

            // Endpoint for receiving file chunks
            app.MapPost("/upload/chunk", async (HttpRequest request, IFileService fileService) =>
            {
                return await UploadChunk(request, fileService);
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

        private static async Task<IResult> UploadChunk(HttpRequest request, IFileService fileService)
        {
            try
            {
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
                int userId = MOCKEDUSERID; // Use actual user ID in production

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

                // Add log (done)

                foreach (var file in request.Form.Files)
                {
                    var log = new CreateActivityLogDto
                    {
                        userID = MOCKEDUSERID,
                        changeType = "Uploaded",
                        description = $"Uploaded chunk {chunkNumber} of {formFile.FileName}",
                        projID = 0, // no project
                        assetID = file.
                        isAdminAction = false
                    };

                    activityLogService.AddLogAsync(log);
                }

                // For chunk uploads
                if (result.IsLastChunk)
                {
                    // Last chunk but not all chunks received yet
                    return Results.Ok(new
                    {
                        message = "Last chunk uploaded successfully, waiting for all chunks",
                        chunkNumber = chunkNumber,
                        totalChunks = totalChunks,
                        fileName = fileName
                    });
                }
                
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