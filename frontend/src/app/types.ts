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

// What is returned from /projects
export interface Project {
  projectID: number;
  projectName: string;
  location: string;
  description: string;
  creationTime: string; // ISO
  active: boolean;
  archivedAt: string | null;
  assetCount: number;
  admins: User[];
  regularUsers: User[];
}