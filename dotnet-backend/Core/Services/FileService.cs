using Core.Interfaces;
using Core.Dtos;
using Microsoft.AspNetCore.Http;
using Infrastructure.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace Core.Services
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _chunksDirectory;
        private readonly string _mergedFilesDirectory;
        private readonly IPaletteRepository _paletteRepository;
        private readonly IImageService _imageService;
        
        // Add chunk tracking to improve performance
        private static readonly ConcurrentDictionary<string, HashSet<int>> _receivedChunks = new ConcurrentDictionary<string, HashSet<int>>();

        public FileService(ILogger<FileService> logger, IConfiguration configuration, IPaletteRepository paletteRepository, IImageService imageService)
        {
            _logger = logger;
            _configuration = configuration;
            _paletteRepository = paletteRepository;
            _imageService = imageService;
            
            // Create paths for temporary file storage
            string baseDirectory = _configuration["ChunkUpload:BaseDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "ChunkUploads");
            _chunksDirectory = Path.Combine(baseDirectory, "Chunks");
            _mergedFilesDirectory = Path.Combine(baseDirectory, "MergedFiles");
            
            // Ensure directories exist
            EnsureDirectoryExists(_chunksDirectory);
            EnsureDirectoryExists(_mergedFilesDirectory);
        }

        /// <summary>
        /// Process an uploaded file chunk
        /// </summary>
        public async Task<ChunkProcessResult> ProcessChunkAsync(UploadChunkRequest request)
        {
            try
            {
                // Validate request
                if (request.File == null || request.File.Length == 0)
                {
                    return new ChunkProcessResult
                    {
                        Success = false,
                        ErrorMessage = "Empty file chunk"
                    };
                }

                // Sanitize filename to prevent path traversal
                string sanitizedFileName = Path.GetFileName(request.FileName);
                
                // Determine if this is the last chunk
                bool isLastChunk = request.ChunkNumber == request.TotalChunks - 1;
                
                // Create user-specific directory for chunks
                string userChunksDir = Path.Combine(_chunksDirectory, request.UserId.ToString());
                EnsureDirectoryExists(userChunksDir);
                
                // Define chunk file path
                string chunkFilePath = Path.Combine(userChunksDir, $"{sanitizedFileName}.part_{request.ChunkNumber}");
                
                // Save the chunk to disk
                using (var stream = new FileStream(chunkFilePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }
                
                _logger.LogInformation($"Chunk {request.ChunkNumber + 1}/{request.TotalChunks} saved for {sanitizedFileName}");
                
                // Create a unique key for tracking chunks of this specific file upload
                string trackingKey = $"{request.UserId}_{sanitizedFileName}";
                
                // Update received chunks tracking
                var chunks = _receivedChunks.GetOrAdd(trackingKey, _ => new HashSet<int>());
                
                lock (chunks)
                {
                    chunks.Add(request.ChunkNumber);
                }
                
                // Check if all chunks have been received without filesystem checks
                bool allChunksReceived = false;
                
                lock (chunks)
                {
                    allChunksReceived = chunks.Count == request.TotalChunks;
                }
                
                // Only if this appears to be all chunks, verify on disk as a final check
                if (allChunksReceived)
                {
                    // Add a small delay to allow for any in-flight chunks to complete
                    await Task.Delay(100);
                    
                    // Double check the chunks count after delay
                    lock (chunks)
                    {
                        allChunksReceived = chunks.Count == request.TotalChunks;
                    }
                    
                    if (allChunksReceived)
                    {
                        for (int i = 0; i < request.TotalChunks; i++)
                        {
                            string checkPath = Path.Combine(userChunksDir, $"{sanitizedFileName}.part_{i}");
                            if (!File.Exists(checkPath))
                            {
                                allChunksReceived = false;
                                break;
                            }
                        }
                    }
                }
                
                // If this is the last chunk but we're still waiting for others
                if (isLastChunk && !allChunksReceived)
                {
                    // Wait for all chunks with a timeout
                    int maxAttempts = 1000; // 10 seconds maximum wait
                    int attempts = 0;
                    while (!allChunksReceived && attempts < maxAttempts)
                    {
                        await Task.Delay(200); // Check every second
                        attempts++;
                        
                        lock (chunks)
                        {
                            allChunksReceived = chunks.Count == request.TotalChunks;
                            // _logger.LogInformation($"Attempt {attempts}: Chunks received {chunks.Count}/{request.TotalChunks}");
                        }
                    }
                    
                    if (!allChunksReceived)
                    {
                         _logger.LogWarning($"Timeout waiting for all chunks. Only received {chunks.Count}/{request.TotalChunks}");
                    }
                }
                
                return new ChunkProcessResult
                {
                    Success = true,
                    IsLastChunk = isLastChunk,
                    AllChunksReceived = allChunksReceived
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing chunk {request.ChunkNumber} for file {request.FileName}");
                return new ChunkProcessResult
                {
                    Success = false,
                    ErrorMessage = $"Error processing chunk: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Merge all chunks of a file into a single file
        /// </summary>
        public async Task<MergeChunksResult> MergeChunksAsync(string fileName, int totalChunks, int userId)
        {
            try
            {
                // Sanitize filename to prevent path traversal
                string sanitizedFileName = Path.GetFileName(fileName);
                
                // Get user-specific directories
                string userChunksDir = Path.Combine(_chunksDirectory, userId.ToString());
                string userMergedDir = Path.Combine(_mergedFilesDirectory, userId.ToString());
                EnsureDirectoryExists(userMergedDir);
                
                // Define path for the merged file
                string mergedFilePath = Path.Combine(userMergedDir, sanitizedFileName);
                
                // Create or overwrite the merged file
                using (var outputStream = new FileStream(mergedFilePath, FileMode.Create))
                {
                    // Process each chunk in order
                    for (int i = 0; i < totalChunks; i++)
                    {
                        string chunkFilePath = Path.Combine(userChunksDir, $"{sanitizedFileName}.part_{i}");
                        
                        if (!File.Exists(chunkFilePath))
                        {
                            return new MergeChunksResult
                            {
                                Success = false,
                                ErrorMessage = $"Chunk {i} is missing. Cannot merge file."
                            };
                        }
                        
                        using (var chunkStream = new FileStream(chunkFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan))
                        {
                            await chunkStream.CopyToAsync(outputStream);
                        }
                        
                        // Delete the chunk after merging
                        File.Delete(chunkFilePath);
                    }
                    
                    // Ensure all data is written to disk
                    outputStream.Flush(true);
                }

                // Wait a moment to ensure filesystem operations complete
                await Task.Delay(100);
                
                // Clear the tracking for this file
                string trackingKey = $"{userId}_{sanitizedFileName}";
                _receivedChunks.TryRemove(trackingKey, out _);
                
                // Get MIME type based on file extension
                string mimeType = GetMimeTypeFromExtension(Path.GetExtension(mergedFilePath));
                
                // Verify file exists with proper content before attempting DB upload
                if (!File.Exists(mergedFilePath))
                {
                    return new MergeChunksResult
                    {
                        Success = false,
                        ErrorMessage = "Merged file not found after merge operation"
                    };
                }
                
                // Try to open the file to verify it's complete and accessible
                try
                {
                    using (var verifyStream = new FileStream(mergedFilePath, FileMode.Open, FileAccess.Read))
                    {
                        if (verifyStream.Length == 0)
                        {
                            return new MergeChunksResult
                            {
                                Success = false,
                                ErrorMessage = "Merged file is empty"
                            };
                        }
                    }
                    
                    // Now that we've verified the file, upload to DB
                    Asset asset = await _paletteRepository.UploadMergedChunkToDb(mergedFilePath, sanitizedFileName, mimeType, userId, true, _imageService);

                    // Delete the merged file from disk only after successful DB upload
                    if (asset != null && !string.IsNullOrEmpty(asset.BlobID))
                    {
                        File.Delete(mergedFilePath);
                        
                        return new MergeChunksResult
                        {
                            Success = true,
                            FilePath = mergedFilePath,
                            BlobId = asset.BlobID
                        };
                    }
                    else
                    {
                        return new MergeChunksResult
                        {
                            Success = false,
                            ErrorMessage = "Upload to database failed - asset was not created properly"
                        };
                    }
                }
                catch (Exception ex)
                {
                    // If file verification fails, report error
                    return new MergeChunksResult
                    {
                        Success = false,
                        ErrorMessage = $"File verification failed: {ex.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error merging chunks for file {fileName}");
                return new MergeChunksResult
                {
                    Success = false,
                    ErrorMessage = $"Error merging chunks: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get information about a merged file
        /// </summary>
        public async Task<FileInfoResponse> GetFileInfoAsync(string fileName)
        {
            // Search for the file in all user directories
            var userDirs = Directory.GetDirectories(_mergedFilesDirectory);
            
            foreach (var userDir in userDirs)
            {
                string filePath = Path.Combine(userDir, fileName);
                
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    int userId = int.Parse(Path.GetFileName(userDir));
                    
                    return new FileInfoResponse
                    {
                        FileName = fileName,
                        FilePath = filePath,
                        FileSize = fileInfo.Length,
                        UploadedAt = fileInfo.CreationTime,
                        UserId = userId
                    };
                }
            }
            
            throw new DataNotFoundException($"File {fileName} not found");
        }
        
        /// <summary>
        /// Ensures that a directory exists, creating it if necessary
        /// </summary>
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Gets the MIME type based on file extension
        /// </summary>
        private string GetMimeTypeFromExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "application/octet-stream";

            extension = extension.ToLowerInvariant();
            
            Dictionary<string, string> mimeTypes = new Dictionary<string, string>
            {
                { ".txt", "text/plain" },
                { ".pdf", "application/pdf" },
                { ".doc", "application/msword" },
                { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                { ".xls", "application/vnd.ms-excel" },
                { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                { ".png", "image/png" },
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".gif", "image/gif" },
                { ".webp", "image/webp" },
                { ".csv", "text/csv" },
                { ".xml", "application/xml" },
                { ".zip", "application/zip" },
                { ".mp3", "audio/mpeg" },
                { ".mp4", "video/mp4" },
                { ".json", "application/json" },
                { ".html", "text/html" },
                { ".htm", "text/html" }
            };

            return mimeTypes.TryGetValue(extension, out string mimeType) ? mimeType : "application/octet-stream";
        }
    }
} 