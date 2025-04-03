// Use for types that are reused!!!
// try to align with backend types returned

export interface User {
  userID: number;
  name: string;
  email: string;
}

export interface Tag {
  tagID: number;
  name: string;
}

export interface Log {
  change_id: number;
  user: number;
  description: string;
  change_type: string;
  asset_id: string;
  project_id: number;
  isAdminAction: boolean;
  date_time: string;
}

export interface ProjectMetadataField {
  fieldID: number;
  fieldName: string;
  isEnabled: boolean;
  fieldType: string;
}

// What is returned from /projects
export interface Project {
  projectID: number;
  projectName: string;
  name?: string; // /projects/{id} returns this
  location: string;
  description: string;
  creationTime: string; // ISO
  active: boolean;
  archivedAt: string | null;
  assetCount: number;
  admins: User[];
  regularUsers: User[];
}

export interface Asset {
  blobID: string;
  filename: string;
  uploadedBy: {
    userID: number;
    name: string;
    email: string;
  };
  date: string;
  filesizeInKB: 0;
  tags: string[];
  mimetype?: string;
}

export interface Pagination {
  pageNumber: number;
  assetsPerPage?: number;
  logsPerPage?: number;
  totalAssetsReturned?: number;
  totalPages: number;
}
