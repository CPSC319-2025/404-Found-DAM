import { FileMetadata } from "@/app/context/FileContext";

// Function to get MIME type from filename
export function getMimeTypeFromFileName(filename: string): string {
  const extension = filename.split(".").pop()?.toLowerCase();
  if (!extension) return "unknown";
  const imageExtensions = ["jpg", "jpeg", "png", "gif", "bmp", "webp"];
  const videoExtensions = ["mp4", "webm", "ogg"];
  if (imageExtensions.includes(extension)) {
    return extension === "jpg" ? "image/jpeg" : `image/${extension}`;
  } else if (videoExtensions.includes(extension)) {
    return extension === "mp4" ? "video/mp4" : `video/${extension}`;
  }
  return "unknown";
}

// Extract the original filename from the server format (BlobID.OriginalFilename.zst)
export function extractOriginalFilename(filename: string): string {
  if (!filename) return "";
  
  // Format: BlobID.OriginalFilename.zst
  const parts = filename.split('.');
  if (parts.length < 3) {
    return filename; // Return as is if not in expected format
  }
  
  // Remove the first part (BlobID) and the last part (.zst)
  parts.shift(); // Remove BlobID
  
  // Check if the last part is "zst" and remove it
  if (parts[parts.length - 1] === "zst") {
    parts.pop(); // Remove .zst extension
  }
  
  // Join the remaining parts to handle filenames with dots
  return parts.join('.');
}

// Extract BlobID from the server format (BlobID.OriginalFilename.zst)
export function extractBlobId(filename: string): string | undefined {
  if (!filename) return undefined;
  
  const parts = filename.split('.');
  if (parts.length < 2) {
    return undefined;
  }
  
  // The first part should be the blobId
  const blobIdStr = parts[0];
  return blobIdStr;
}

// Main function to fetch palette assets
export async function fetchPaletteAssets(): Promise<FileMetadata[]> {
  const formData = new FormData();
  formData.append("UserId", "1"); // Fixed requirement: UserId=1
  const files: FileMetadata[] = [];
  
  try {
    // Get the auth token
    const token = localStorage.getItem("token");
    
    // First, get metadata for all files
    const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets?decompress=true`, {
      method: "GET",
      headers: {
        Authorization: token ? `Bearer ${token}` : "",
      },
    });

    if (!response.ok) {
      throw new Error(`Fetch failed with status ${response.status}`);
    }

    const data = await response.json();
    
    console.log("API Response - data.files:", data.files);
    if (data.files.length > 0) {
      console.log("First file properties:", Object.keys(data.files[0]));
    }

    if (!data.files || data.files.length === 0) {
      console.log("No files in palette");
      return [];
    }

    const filePromises = data.files.map(async (fileInfo: any) => {
      // Handle case-insensitive property names
      console.log("Processing file:", fileInfo);
      const blobId = fileInfo.blobId;
      const fileName = fileInfo.fileName;
      
      if (!blobId) {
        console.warn("Missing blobId in file metadata:", fileInfo);
        return null;
      }
      
      // Extract the original filename
      const originalFilename = extractOriginalFilename(fileName);
      
      // Download each file individually with decompression done on the server
      const fileResponse = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets/${blobId}?decompress=true`, 
        {
          method: "GET",
          headers: {
            Authorization: token ? `Bearer ${token}` : "",
          }
        }
      );
      
      if (!fileResponse.ok) {
        console.error(`Failed to fetch file ${blobId}:`, fileResponse.status);
        return null;
      }
      
      // Get the file content
      const blob = await fileResponse.blob();

      
      // Create a File object
      const file = new File(
        [blob],
        originalFilename,
        { type: getMimeTypeFromFileName(originalFilename) }
      );

      const fileSize = (file.size / 1024).toFixed(2) + " KB";
      const fileMeta: FileMetadata = {
        file,
        fileSize,
        description: "",
        location: "",
        tags: [],
        tagIds: [],
        blobId
      };

      return fileMeta;
    });

    // Wait for all promises to resolve
    const fetchedFiles = await Promise.all(filePromises);
    
    // Filter out null values and return
    return fetchedFiles.filter(file => file !== null) as FileMetadata[];
  } catch (err) {
    console.log("The Palette is empty");
    return [];
  }
} 