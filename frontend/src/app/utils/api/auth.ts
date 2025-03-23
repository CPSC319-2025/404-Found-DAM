import { fetchWithAuth } from "./api";

interface ProjectMembership {
  project: number;
  role: string;
}

export interface User {
  userID: number;
  email: string;
  superadmin: boolean;
  projectMemberships: ProjectMembership[];
}

export async function login(email: string, password: string) {
  const response = await fetchWithAuth("auth/login", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    throw new Error("Failed to login. Invalid password or email.");
  }

  const data = await response.json();

  console.log(data);

  localStorage.setItem("token", data.token);
}

export async function getUserFromToken() {
  const token = localStorage.getItem("token");
  if (!token) return null;

  try {
    const payload = JSON.parse(atob(token.split(".")[1]));

    const currentTime = Math.floor(Date.now() / 1000);
    if (payload.exp && payload.exp < currentTime) {
      console.warn("Token has expired, removing...");
      logout();
      return null;
    }

    console.log(payload);

    const parsedProjectMemberships: ProjectMembership[] = JSON.parse(payload.projectMemberships).map(
      (membership: { ProjectID: number; Role: string }) => ({
        project: membership.ProjectID,
        role: membership.Role,
      })
    );

    return {
      userID: Number(payload.userId),
      email: payload.email,
      superadmin: payload.isSuperAdmin === "True",
      projectMemberships: parsedProjectMemberships
    } as User;
  } catch (error) {
    console.error("Invalid token:", error);
    logout();
    return null;
  }
}

export function logout() {
  localStorage.removeItem("token");
}