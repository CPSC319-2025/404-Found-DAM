import { FileMetadata } from "@/app/context/FileContext";

/**
 * Removes a file from the palette
 * @param fileMeta File metadata object
 * @returns True if successful, false otherwise
 */
export async function removeFile(fileMeta: FileMetadata): Promise<boolean> {
  // Prepare form data
  const formData = new FormData();
  formData.append("UserId", "1");

  // Use whatever property holds the "blobId" or "name" you need to pass
  if (fileMeta.blobId !== undefined) {
    formData.append("Name", fileMeta.blobId);
  } else {
    console.warn("No blobId found for file:", fileMeta.file.name);
    return false;
  }

  try {
    // Make the DELETE request with form data
    const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/asset`, {
      method: "DELETE",
      body: formData,
      // No need to set 'Content-Type'; fetch does it automatically for FormData
    });

    if (!response.ok) {
      throw new Error(`Delete failed with status ${response.status}`);
    }
    
    const data = await response.json();
    console.log("Delete successful:", data);
    
    return true;
  } catch (error) {
    console.error("Error deleting:", error);
    return false;
  }
} 