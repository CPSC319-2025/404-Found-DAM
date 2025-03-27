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
using ZstdSharp;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Core.Entities;

namespace Core.Services
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _chunksDirectory;
        private readonly string _mergedFilesDirectory;
        private readonly IPaletteRepository _paletteRepository;

        public FileService(ILogger<FileService> logger, IConfiguration configuration, IPaletteRepository paletteRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _paletteRepository = paletteRepository;
            
            // Get directory paths from configuration or use defaults
            string baseDirectory = _configuration["FileStorage:BaseDirectory"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileStorage");
            Console.WriteLine($"Base directory: {baseDirectory}");
            _chunksDirectory = Path.Combine(baseDirectory, "chunks");
            _mergedFilesDirectory = Path.Combine(baseDirectory, "merged_files");
            
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
                
                // Check if all chunks have been received
                bool allChunksReceived = true;
                for (int i = 0; i < request.TotalChunks; i++)
                {
                    string checkPath = Path.Combine(userChunksDir, $"{sanitizedFileName}.part_{i}");
                    if (!File.Exists(checkPath))
                    {
                        allChunksReceived = false;
                        break;
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
                        
                        using (var chunkStream = new FileStream(chunkFilePath, FileMode.Open))
                        {
                            await chunkStream.CopyToAsync(outputStream);
                        }
                        
                        // Delete the chunk after merging
                        File.Delete(chunkFilePath);
                    }
                }
                
                Console.WriteLine($"File Name:{sanitizedFileName}");
                Console.WriteLine($"File path:{mergedFilePath}");
                Console.WriteLine($"User ID:{userId}");
                
                // Get MIME type based on file extension
                string mimeType = GetMimeTypeFromExtension(Path.GetExtension(mergedFilePath));
                Console.WriteLine($"File MimeType:{mimeType}");

                Asset asset = await _paletteRepository.UploadMergedChunkToDb(mergedFilePath, sanitizedFileName, mimeType, userId);

                // Delete the merged file from disk
                File.Delete(mergedFilePath);
                
                return new MergeChunksResult
                {
                    Success = true,
                    FilePath = mergedFilePath,
                    BlobId = asset.BlobID
                };
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