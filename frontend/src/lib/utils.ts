import { FileMetadata } from '@/app/context/FileContext';

interface BlobMetadata {
  blobId: string;
  metadata: {
    [key: string]: string;
  };
}

export const loadFileContent = async (fileMeta: FileMetadata): Promise<FileMetadata | null> => {
  try {
    if (fileMeta.url && fileMeta.isLoaded) {
      return fileMeta;
    }
    
    if (!fileMeta.blobId) {
      console.error("No blobId provided for file:", fileMeta.file.name);
      return null;
    }
    
    const url = `/api/palette/asset?blobId=${fileMeta.blobId}&t=${Date.now()}`;
    
    const response = await fetch(url, {
      method: 'GET',
      cache: 'no-store',
      priority: 'high' as RequestPriority,
    });
    
    if (!response.ok) {
      throw new Error(`Error fetching file: ${response.status} ${response.statusText}`);
    }
    
    const blob = await response.blob();
    
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

// function to fetch and update blob details with parallel processing
export const fetchBlobDetails = async (blobId: string): Promise<BlobMetadata | null> => {
  try {
    const url = `/api/palette/metadata?blobId=${blobId}&t=${Date.now()}`;
    
    const response = await fetch(url, {
      method: 'GET',
      cache: 'no-store',
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
