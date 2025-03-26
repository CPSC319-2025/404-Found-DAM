using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Core.Dtos
{
    /// <summary>
    /// Request DTO for uploading a file chunk
    /// </summary>
    public class UploadChunkRequest
    {
        /// <summary>
        /// Current chunk number (0-based)
        /// </summary>
        public int ChunkNumber { get; set; }
        
        /// <summary>
        /// Total number of chunks
        /// </summary>
        public int TotalChunks { get; set; }
        
        /// <summary>
        /// Original file name
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// User ID associated with the upload
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// The file chunk data
        /// </summary>
        public IFormFile File { get; set; }
    }

    /// <summary>
    /// Result of processing a chunk
    /// </summary>
    public class ChunkProcessResult
    {
        /// <summary>
        /// Whether processing was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Whether this is the last chunk
        /// </summary>
        public bool IsLastChunk { get; set; }
        
        /// <summary>
        /// Whether all chunks have been received
        /// </summary>
        public bool AllChunksReceived { get; set; }
        
        /// <summary>
        /// Error message if processing failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of merging chunks
    /// </summary>
    public class MergeChunksResult
    {
        /// <summary>
        /// Whether merging was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Path to the merged file
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// Error message if merging failed
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Database blob ID for the stored file
        /// </summary>
        public string BlobId { get; set; }
    }

    /// <summary>
    /// Response for file information
    /// </summary>
    public class FileInfoResponse
    {
        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// Path to the file
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// Date and time when the file was uploaded
        /// </summary>
        public DateTime UploadedAt { get; set; }
        
        /// <summary>
        /// User ID of the uploader
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// Database blob ID for the stored file
        /// </summary>
        public string BlobId { get; set; }
    }

    /// <summary>
    /// Request for downloading a file chunk
    /// </summary>
    public class DownloadChunkRequest
    {
        /// <summary>
        /// The blob ID of the file to download
        /// </summary>
        public required string BlobId { get; set; }
        
        /// <summary>
        /// Start position of the chunk (in bytes)
        /// </summary>
        public required long StartPosition { get; set; }
        
        /// <summary>
        /// Size of the chunk to download (in bytes)
        /// </summary>
        public required int ChunkSize { get; set; }
        
        /// <summary>
        /// User ID associated with the download
        /// </summary>
        public required int UserId { get; set; }
        
        /// <summary>
        /// Whether to decompress the data
        /// </summary>
        public bool Decompress { get; set; } = false;
    }

    /// <summary>
    /// Response for a downloaded file chunk
    /// </summary>
    public class DownloadChunkResponse
    {
        /// <summary>
        /// Chunk data as byte array
        /// </summary>
        public byte[] ChunkData { get; set; }
        
        /// <summary>
        /// Content type of the file
        /// </summary>
        public string ContentType { get; set; }
        
        /// <summary>
        /// Original file name
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// Total file size in bytes
        /// </summary>
        public long TotalSize { get; set; }
        
        /// <summary>
        /// Start position of this chunk
        /// </summary>
        public long StartPosition { get; set; }
        
        /// <summary>
        /// End position of this chunk
        /// </summary>
        public long EndPosition { get; set; }
        
        /// <summary>
        /// Whether this is the last chunk
        /// </summary>
        public bool IsLastChunk { get; set; }
    }

    /// <summary>
    /// Response for file metadata needed for chunked downloads
    /// </summary>
    public class FileMetadataResponse
    {
        /// <summary>
        /// The blob ID of the file
        /// </summary>
        public string BlobId { get; set; }
        
        /// <summary>
        /// Original file name
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// Content type of the file
        /// </summary>
        public string ContentType { get; set; }
        
        /// <summary>
        /// Total size of the file in bytes
        /// </summary>
        public long TotalSize { get; set; }
        
        /// <summary>
        /// Whether the file is compressed
        /// </summary>
        public bool IsCompressed { get; set; }
        
        /// <summary>
        /// Recommended chunk size for downloading in bytes
        /// </summary>
        public int RecommendedChunkSize { get; set; }
    }
} 