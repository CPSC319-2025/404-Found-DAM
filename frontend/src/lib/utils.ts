import { FileMetadata } from '@/app/context/FileContext';

// Define BlobMetadata type
interface BlobMetadata {
  blobId: string;
  metadata: {
    [key: string]: string;
  };
}

// Optimized function to load file content with faster response times
export const loadFileContent = async (fileMeta: FileMetadata): Promise<FileMetadata | null> => {
  try {
    // If file already has a URL, return it immediately
    if (fileMeta.url && fileMeta.isLoaded) {
      return fileMeta;
    }
    
    // If there's no blobId, we can't fetch from the server
    if (!fileMeta.blobId) {
      console.error("No blobId provided for file:", fileMeta.file.name);
      return null;
    }
    
    // Create request URL with cache busting for immediate updates
    const url = `/api/palette/asset?blobId=${fileMeta.blobId}&t=${Date.now()}`;
    
    // Use fetch with priority hints for better performance
    const response = await fetch(url, {
      method: 'GET',
      cache: 'no-store', // Avoid caching delays
      priority: 'high' as RequestPriority, // Indicate high priority fetch
    });
    
    if (!response.ok) {
      throw new Error(`Error fetching file: ${response.status} ${response.statusText}`);
    }
    
    // Get the blob from the response
    const blob = await response.blob();
    
    // Create an object URL for the blob
    const objectUrl = URL.createObjectURL(blob);
    
    return {
      ...fileMeta,
      url: objectUrl,
      isLoaded: true
    };
  } catch (error) {
    console.error("Error loading file content:", error);
    return null;
  }
};

// Function to fetch and update blob details with parallel processing
export const fetchBlobDetails = async (blobId: string): Promise<BlobMetadata | null> => {
  try {
    // Create request URL with cache busting for immediate updates
    const url = `/api/palette/metadata?blobId=${blobId}&t=${Date.now()}`;
    
    // Use fetch with priority hints for better performance
    const response = await fetch(url, {
      method: 'GET',
      cache: 'no-store', // Avoid caching delays
    });
    
    if (!response.ok) {
      throw new Error(`Error fetching blob metadata: ${response.status} ${response.statusText}`);
    }
    
    const data = await response.json();
    return data;
  } catch (error) {
    console.error("Error fetching blob details:", error);
    return null;
  }
}; 