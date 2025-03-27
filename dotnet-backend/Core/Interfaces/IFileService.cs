using Microsoft.AspNetCore.Http;
using Core.Dtos;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IFileService
    {
        /// <summary>
        /// Process an uploaded file chunk
        /// </summary>
        /// <param name="request">The request containing chunk information</param>
        /// <returns>Result of processing the chunk</returns>
        Task<ChunkProcessResult> ProcessChunkAsync(UploadChunkRequest request);

        /// <summary>
        /// Merge all chunks of a file into a single file
        /// </summary>
        /// <param name="fileName">The original file name</param>
        /// <param name="totalChunks">Total number of chunks to merge</param>
        /// <param name="userId">User ID associated with the upload</param>
        /// <returns>Result of the merge operation</returns>
        Task<MergeChunksResult> MergeChunksAsync(string fileName, int totalChunks, int userId);

        /// <summary>
        /// Get information about a merged file
        /// </summary>
        /// <param name="fileName">The file name to retrieve information for</param>
        /// <returns>File information</returns>
        Task<FileInfoResponse> GetFileInfoAsync(string fileName);
    }
} 