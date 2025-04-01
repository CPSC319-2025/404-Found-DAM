import { UploadProgressCallbacks } from './types';

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
    // Get the auth token
    const token = localStorage.getItem("token");
    
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
        headers: {
          "Authorization": token ? `Bearer ${token}` : ""
        },
        body: formData,
      });
      
      if (!response.ok) {
        throw new Error(`Server responded with status: ${response.status}`);
      }
      
      const data = await response.json();
      lastResponse = data; // Save the response for later use
      console.log("Chunk upload response:", data);
      
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
    
    // Extract blobId from the last response
    const blobId = lastResponse?.blobId;
    
    onSuccess(blobId); // Pass blobId to onSuccess callback
    return blobId;
  } catch (error) {
    const errorMessage = `Error uploading chunk ${chunkNumber + 1}: ${error instanceof Error ? error.message : String(error)}`;
    console.error(errorMessage);
    onError(errorMessage);
    return undefined;
  }
} 