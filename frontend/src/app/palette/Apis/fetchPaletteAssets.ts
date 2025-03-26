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
    // First, get metadata for all files
    const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets?decompress=true`, {
      method: "GET",
      headers: {
        Authorization: "Bearer MY_TOKEN",
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

    // Process each file metadata without downloading the full file
    const fileDataArray = data.files.map((fileInfo: any) => {
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
      
      // Determine the MIME type
      const mimeType = getMimeTypeFromFileName(originalFilename);
      
      // Generate file URL (will be loaded on-demand when viewed)
      // Don't use decompress=true for videos since they use range requests
      const filePath = `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets/${blobId}?decompress=true`;
      
      // Estimate file size if available, otherwise use placeholder
      const fileSize = fileInfo.size ? 
        `${(fileInfo.size / 1024).toFixed(2)} KB` : 
        "Size unknown";
      
      // Create metadata without loading the actual file
      const fileMeta: FileMetadata = {
        fileName: originalFilename,
        filePath,
        fileSize,
        description: "",
        location: "",
        tags: [],
        tagIds: [],
        blobId,
        mimeType
      };

      return fileMeta;
    });

    // Filter out null values and return
    return fileDataArray.filter((file: unknown): file is FileMetadata => 
      file !== null && typeof file === 'object'
    );
  } catch (err) {
    console.error("Error fetching palette assets:", err);
    return [];
  }
} 