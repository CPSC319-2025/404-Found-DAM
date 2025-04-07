import { FileMetadata } from "@/app/context/FileContext";
import { fetchWithAuth } from "@/app/utils/api/api";
import { formatFileSize } from "@/app/utils/api/formatFileSize";

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

// Interface for pagination results
export interface PaginatedFiles {
  files: FileMetadata[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
}

// Main function to fetch palette assets with pagination
export async function fetchPaletteAssets(page: number = 1, pageSize: number = 10): Promise<PaginatedFiles> {
  const formData = new FormData();
  try {
    // First, get metadata for files with pagination parameters (even if server doesn't support them yet)
    const response = await fetchWithAuth(`palette/assets?decompress=true&page=${page}&pageSize=${pageSize}`, {
      method: "GET"
    });

    if (!response.ok) {
      throw new Error(`Fetch failed with status ${response.status}`);
    }

    const data = await response.json();
    
    console.log("API Response:", data);
    console.log("API Response - data.files:", data.files);
    console.log("API Response - data.blobUris:", data.blobUris);
    
    if (data.files && data.files.length > 0) {
      console.log("First file properties:", Object.keys(data.files[0]));
    }

    if (!data.files || data.files.length === 0) {
      console.log("No files in palette");
      return { files: [], totalCount: 0, currentPage: 1, totalPages: 1 };
    }

    // Make sure data.blobUris exists
    if (!data.blobUris || data.blobUris.length === 0 || data.blobUris.length !== data.files.length) {
      console.warn("Missing or mismatched blobUris in API response");
      return { files: [], totalCount: 0, currentPage: 1, totalPages: 1 };
    }

    // Get the total count of files
    const totalCount = data.files.length;
    
    // Calculate total pages based on the pageSize
    const totalPages = Math.ceil(totalCount / pageSize);
    
    // Create file metadata for all files first
    const allFiles: FileMetadata[] = data.files.map((fileInfo: any, index: number) => {
      const blobId = fileInfo.blobId;
      const fileName = fileInfo.fileName;
      const contentType = fileInfo.contentType || "image/webp";
      
      // Create a placeholder File object
      const placeholderFile = new File(
        [new Blob([], { type: contentType })], // Empty blob with correct content type
        fileName,
        { type: contentType }
      );
      
      // Create a placeholder file metadata
      const fileMeta: FileMetadata = {
        file: placeholderFile,
        fileSize: fileInfo.fileSize || "Loading...",
        fileName: fileName,
        description: "",
        location: "",
        tags: [],
        tagIds: [],
        blobId,
        blobUri: data.blobUris[index], // Store the blobUri for later loading
        isLoaded: false // Flag to track if the actual file is loaded
      };
      
      return fileMeta;
    });
    
    // Apply pagination manually (client-side) since server may not support it
    const startIdx = (page - 1) * pageSize;
    const endIdx = Math.min(startIdx + pageSize, allFiles.length);
    const paginatedFiles = allFiles.slice(startIdx, endIdx);
    
    console.log(`Showing files ${startIdx+1}-${endIdx} of ${totalCount} (Page ${page} of ${totalPages})`);

    return {
      files: paginatedFiles,
      totalCount,
      currentPage: page,
      totalPages
    };
  } catch (err) {
    console.error("Error fetching palette assets:", err);
    return { files: [], totalCount: 0, currentPage: 1, totalPages: 1 };
  }
}

// Function to load a specific file when needed
export async function loadFileContent(fileMeta: FileMetadata): Promise<FileMetadata | null> {
  if (!fileMeta.blobUri || fileMeta.isLoaded) {
    return fileMeta; // Already loaded or no URI available
  }
  
  try {
    // Download the file using the blob URI
    console.log("Fetching from blobUri:", fileMeta.blobUri);
    const fileResponse = await fetch(fileMeta.blobUri);
    
    if (!fileResponse.ok) {
      console.error(`Failed to fetch file ${fileMeta.blobId}:`, fileResponse.status);
      return null;
    }
    
    // Get the file content as blob
    const blob = await fileResponse.blob();
    
    // Create a direct object URL from the blob
    const objectUrl = URL.createObjectURL(blob);
    
    // Create a proper File object with the correct content type
    const file = new File(
      [blob],
      fileMeta.fileName || fileMeta.file.name,
      { type: fileMeta.file.type }
    );

    const fileSize = formatFileSize(file.size / 1024);
    
    // Return updated file metadata
    return {
      ...fileMeta,
      file,
      fileSize,
      url: objectUrl,
      isLoaded: true
    };
  } catch (error) {
    console.error("Error loading file content:", error);
    return null;
  }
} 
