import { BlobDetails } from './types';

/**
 * Fetches project and tags for a blob
 * @param blobId The blob ID to fetch details for
 * @returns An object containing project, tags, tagIds, description, and location
 */
export async function fetchBlobDetails(blobId: string): Promise<BlobDetails> {
  try {
    const response = await fetch(
      `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/blob/${blobId}/details`
    );
    
    if (!response.ok) {
      console.error(`Failed to fetch details for blob ${blobId}: ${response.status}`);
      return {};
    }
    
    const data = await response.json();
    
    const projectId = data.project?.projectId.toString();
    
    // Check for different possible property name casings from the backend
    // The C# backend might use "location" or "Location" depending on serialization settings
    let description = data.project?.description || data.project?.Description;
    let location = data.project?.location || data.project?.Location;
    
    return {
      project: projectId,
      tags: data.tags || [],
      tagIds: data.tagIds || [],
      description: description || "",
      location: location || ""
    };
  } catch (error) {
    console.error(`Error fetching details for blob ${blobId}:`, error);
    return {};
  }
} 