/**
 * Interface for project information
 */
export interface Project {
  projectID: number;
  projectName: string;
  location: string;
  description: string;
  creationTime: string;
  assetCount: number;
  adminNames: string[];
  regularUserNames: string[];
}

/**
 * Interface for blob details response
 */
export interface BlobDetails {
  project?: string;
  tags?: string[];
  tagIds?: number[];
  description?: string;
  location?: string;
}

/**
 * Interface for upload progress callbacks
 */
export interface UploadProgressCallbacks {
  onProgress: (progress: number, status: string) => void;
  onSuccess: (blobId?: string) => void;
  onError: (error: string) => void;
} 