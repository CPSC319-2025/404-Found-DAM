import { FileMetadata } from "@/app/context/FileContext";
import { loadFileContent as originalLoadFileContent } from './fetchPaletteAssets';

// Export all API functions
export * from './fetchPaletteAssets';
export * from './fetchBlobDetails';
export * from './uploadFileZstd';
export * from './fetchProjects';
export * from './removeFile';
export * from './submitAssets';
export * from './uploadFileChunked';
export * from './types';

// Add a new overload function for loadFileContent that accepts blobId string
export async function loadFileContent(blobIdOrFileMeta: string | FileMetadata): Promise<Blob> {
  if (typeof blobIdOrFileMeta === 'string') {
    // It's a blobId, fetch the content
    const token = localStorage.getItem("token");
    const response = await fetch(
      `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/asset/display/${blobIdOrFileMeta}`, 
      {
        headers: {
          Authorization: token ? `Bearer ${token}` : "",
        }
      }
    );
    
    if (!response.ok) {
      throw new Error(`Failed to fetch file content for blobId ${blobIdOrFileMeta}`);
    }
    
    return await response.blob();
  } else {
    // It's a FileMetadata object, use the existing function
    // This is needed to maintain compatibility with existing code
    const result = await originalLoadFileContent(blobIdOrFileMeta);
    if (!result) {
      throw new Error(`Failed to fetch file content for FileMetadata`);
    }
    return new Blob([result.file], { type: result.file.type });
  }
}

// Export all API functions except loadFileContent which we've overridden
export * from './fetchBlobDetails';
export * from './uploadFileZstd';
export * from './fetchProjects';
export * from './removeFile';
export * from './submitAssets';
export * from './uploadFileChunked';
export * from './types';

// Re-export everything from fetchPaletteAssets except loadFileContent
export { fetchPaletteAssets } from './fetchPaletteAssets';
export type { PaginatedFiles } from './fetchPaletteAssets'; 