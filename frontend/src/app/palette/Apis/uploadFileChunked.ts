import { UploadProgressCallbacks } from './types';
import { fetchWithAuth } from "@/app/utils/api/api";

/**
 * Uploads a file in chunks for better reliability with large files
 * @param file The file to upload
 * @param callbacks Callbacks for progress updates
 * @returns Promise resolving to the blobId if successful, undefined otherwise
 */
export async function uploadFileChunked(
  file: File,
  callbacks: UploadProgressCallbacks
): Promise<string | undefined> {
  const { onProgress, onSuccess, onError } = callbacks;

  const chunkSize = 10 * 1024 * 1024; // Increased from 5MB to 10MB chunks
  const totalChunks = Math.ceil(file.size / chunkSize);
  const chunkProgress = 100 / totalChunks;
  let uploadedChunks = 0;
  let lastResponse: {
    blobId?: string;
    message?: string;
    chunkNumber?: number;
    totalChunks?: number;
    fileName?: string
  } | null = null;

  // Max concurrent uploads
  const maxConcurrentUploads = 3;

  try {
    // Prepare all chunks for upload
    const chunks: { index: number, start: number, end: number }[] = [];
    let start = 0;
    
    for (let i = 0; i < totalChunks; i++) {
      const end = Math.min(start + chunkSize, file.size);
      chunks.push({ index: i, start, end });
      start = end;
    }

    // Process chunks in batches for parallel uploads
    for (let i = 0; i < chunks.length; i += maxConcurrentUploads) {
      const batch = chunks.slice(i, i + maxConcurrentUploads);
      
      // Upload batch of chunks in parallel
      const results = await Promise.all(batch.map(chunk => uploadSingleChunk(
        file, 
        chunk.index, 
        totalChunks, 
        chunk.start, 
        chunk.end
      )));
      
      // Process results
      for (const result of results) {
        if (!result.success) {
          throw new Error(`Failed to upload chunk ${result.chunkNumber}: ${result.error}`);
        }
        
        lastResponse = result.data;
        uploadedChunks++;
        
        const progress = Math.min(100, Number(((uploadedChunks) * chunkProgress).toFixed(1)));
        onProgress(progress, `Uploading to database may take a few minutes for large videos.`);
      }
    }
    
    // All chunks uploaded
    onProgress(100, "File upload completed successfully");
    
    // Extract blobId from the last response
    const blobId = lastResponse?.blobId;
    
    onSuccess(blobId); // Pass blobId to onSuccess callback
    return blobId;
  } catch (error) {
    const errorMessage = `Error uploading file: ${error instanceof Error ? error.message : String(error)}`;
    console.error(errorMessage);
    onError(errorMessage);
    return undefined;
  }
}

/**
 * Helper function to upload a single chunk
 */
async function uploadSingleChunk(
  file: File,
  chunkNumber: number,
  totalChunks: number,
  start: number,
  end: number
): Promise<{ 
  success: boolean, 
  chunkNumber: number, 
  data?: any, 
  error?: string 
}> {
  try {
    // Slice the file to get the current chunk
    const chunk = file.slice(start, end);

    // Create form data for this chunk
    const formData = new FormData();
    formData.append("file", chunk, "chunk");
    formData.append("chunkNumber", chunkNumber.toString());
    formData.append("totalChunks", totalChunks.toString());
    formData.append("originalname", file.name);

    const response = await fetchWithAuth("upload/chunk", {
      method: "POST",
      body: formData as BodyInit,
      headers: {}
    });

    if (!response.ok) {
      throw new Error(`Server responded with status: ${response.status}`);
    }
    
    const data = await response.json();
    return { 
      success: true, 
      chunkNumber, 
      data 
    };
  } catch (error) {
    return { 
      success: false, 
      chunkNumber, 
      error: error instanceof Error ? error.message : String(error) 
    };
  }
} 