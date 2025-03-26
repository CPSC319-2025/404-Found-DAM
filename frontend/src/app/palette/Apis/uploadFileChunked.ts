import { UploadProgressCallbacks } from './types';

interface UploadResult {
  success: boolean;
  data?: {
    blobId: string;
    url: string;
  };
  error?: string;
}

/**
 * Uploads a file in chunks for better reliability with large files
 * @param file The file to upload
 * @param useChunks Whether to use chunked upload (for large files)
 * @param callbacks Callbacks for progress updates
 * @returns Promise resolving to upload result with success status and data
 */
export async function uploadFileChunked(
  file: File,
  useChunks: boolean = true,
  callbacks: UploadProgressCallbacks
): Promise<UploadResult> {
  const { onProgress, onSuccess, onError } = callbacks;
  
  // For small files or when chunking is disabled, use regular upload
  if (!useChunks || file.size < 1 * 1024 * 1024) { // Less than 1MB
    return regularUpload(file, callbacks);
  }
  
  const chunkSize = 5 * 1024 * 1024; // 5MB chunks
  const totalChunks = Math.ceil(file.size / chunkSize);
  const chunkProgress = 100 / totalChunks;
  let chunkNumber = 0;
  let start = 0;
  let end = chunkSize;
  let lastResponse: { 
    blobId?: string; 
    message?: string; 
    chunkNumber?: number; 
    totalChunks?: number; 
    fileName?: string 
  } | null = null;

  try {
    while (start < file.size) {
      // Adjust end to not exceed file size
      end = Math.min(start + chunkSize, file.size);
      
      // Slice the file to get the current chunk
      const chunk = file.slice(start, end);
      
      // Create form data for this chunk
      const formData = new FormData();
      formData.append("file", chunk, "chunk");
      formData.append("chunkNumber", chunkNumber.toString());
      formData.append("totalChunks", totalChunks.toString());
      formData.append("originalname", file.name);

      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/upload/chunk`, {
        method: "POST",
        body: formData,
      });
      
      if (!response.ok) {
        throw new Error(`Server responded with status: ${response.status}`);
      }
      
      const data = await response.json();
      lastResponse = data; // Save the response for later use
      
      const status = `Chunk ${chunkNumber + 1}/${totalChunks} uploaded successfully`;
      const progress = Math.min(100, Number(((chunkNumber + 1) * chunkProgress).toFixed(1)));
      
      onProgress(progress, status);
      
      // Move to next chunk
      chunkNumber++;
      start = end;
      end = start + chunkSize;
    }
    
    // All chunks uploaded
    onProgress(100, "File upload completed successfully");
    
    // If we have a response with a blobId, use it in the callback
    if (lastResponse?.blobId) {
      // All files (both images and videos) need decompress=true parameter
      const url = `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets/${lastResponse.blobId}?decompress=true`;
      
      onSuccess(lastResponse.blobId, url);
      
      return {
        success: true,
        data: {
          blobId: lastResponse.blobId,
          url
        }
      };
    } else {
      // Call without parameters if we don't have a blobId
      onSuccess();
      return {
        success: true
      };
    }
  } catch (error) {
    const errorMessage = `Error uploading chunk ${chunkNumber + 1}: ${error instanceof Error ? error.message : String(error)}`;
    console.error(errorMessage);
    onError(errorMessage);
    return {
      success: false,
      error: errorMessage
    };
  }
}

/**
 * Regular file upload for smaller files
 */
async function regularUpload(file: File, callbacks: UploadProgressCallbacks): Promise<UploadResult> {
  const { onProgress, onSuccess, onError } = callbacks;
  
  try {
    onProgress(10, "Preparing upload...");
    
    const formData = new FormData();
    formData.append("file", file);
    formData.append("name", "palette-upload");
    formData.append("mimeType", file.type);
    formData.append("userId", "1"); // Using the mocked user ID
    
    onProgress(30, "Sending to server...");
    
    const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/upload`, {
      method: "POST",
      body: formData,
    });
    
    if (!response.ok) {
      throw new Error(`Server responded with status: ${response.status}`);
    }
    
    onProgress(90, "Processing response...");
    
    const data = await response.json();
    
    if (data.SuccessfulUploads && data.SuccessfulUploads.length > 0) {
      const { BlobId } = data.SuccessfulUploads[0];
      
      // All files (both images and videos) need decompress=true parameter
      const url = `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets/${BlobId}?decompress=true`;
      
      onProgress(100, "Upload complete!");
      onSuccess(BlobId, url);
      
      return {
        success: true,
        data: {
          blobId: BlobId,
          url
        }
      };
    } else {
      throw new Error("Upload failed on server");
    }
  } catch (error) {
    const errorMessage = `Error uploading file: ${error instanceof Error ? error.message : String(error)}`;
    console.error(errorMessage);
    onError(errorMessage);
    return {
      success: false,
      error: errorMessage
    };
  }
} 