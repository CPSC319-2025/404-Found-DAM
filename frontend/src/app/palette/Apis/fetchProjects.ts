import { Project } from './types';

/**
 * Fetches all projects
 * @returns Array of project objects
 */
export async function fetchProjects(): Promise<Project[]> {
  try {
    const res = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/projects`);
    
    if (!res.ok) {
      console.error("Failed to fetch project logs:", res.status);
      return [];
    }
    
    const data = await res.json();
    
    // Return the projects array if it exists, otherwise empty array
    if (data.fullProjectInfos) {
      return data.fullProjectInfos;
    } else {
      console.warn("No 'logs' found in response:", data);
      return [];
    }
  } catch (err) {
    console.error("Error fetching project logs:", err);
    return [];
  }
} 