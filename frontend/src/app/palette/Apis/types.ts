import { User } from "@/app/types";
import { FileMetadata } from "@/app/context/FileContext";
/**
 * Interface for project information
 */
export interface Project {
  projectID: number;
  projectName: string;
  active: boolean;
  location: string;
  description: string;
  creationTime: string;
  assetCount: number;
  admins: User[];
  regularUsers: User[];
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
 * Interface for pagination results
 */
export interface PaginatedFiles {
  files: FileMetadata[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
}

/**
 * Interface for upload progress callbacks
 */
export interface UploadProgressCallbacks {
  onProgress: (progress: number, status: string) => void;
  onSuccess: (blobId?: string) => void;
  onError: (error: string) => void;
} 