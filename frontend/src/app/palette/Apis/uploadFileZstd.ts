import { FileMetadata } from "@/app/context/FileContext";
import { compressFileZstd } from "@/app/palette/compressFileZstd";

/**
 * Uploads a file after compressing it with Zstandard
 * @param fileMeta File metadata object containing the file to upload
 * @returns The blob ID if upload is successful
 */
export async function uploadFileZstd(fileMeta: FileMetadata): Promise<string | undefined> {
  try {
    // Check if file exists
    if (!fileMeta.file) {
      console.error("File object is missing in metadata");
      return undefined;
    }
    
    // Compress file with Zstandard
    const compressedFile = await compressFileZstd(fileMeta.file);

    // Use native FormData
    const formData = new FormData();
    formData.append("userId", "001"); // or dynamic
    formData.append("name", fileMeta.fileName);
    formData.append("mimeType", fileMeta.mimeType);
    formData.append("files", compressedFile);

    // Send the request
    const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/upload?toWebp=true`, {
      method: "POST",
      headers: {
        Authorization: "Bearer MY_TOKEN",
      },
      body: formData,
    });

    if (!response.ok) {
      throw new Error(`Upload failed with status ${response.status}`);
    }

    const result = await response.json();
    console.log("Upload result:", result);

    if (result.successfulUploads?.length > 0) {
      const detail = result.successfulUploads[0]; // or find by filename if needed
      console.log("Blob ID:", detail.blobID);
      return detail.blobID;
    }
    
    return undefined;
  } catch (err) {
    console.error("Error uploading file:", err);
    return undefined;
  }
} 