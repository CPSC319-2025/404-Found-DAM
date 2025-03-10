import { SignJWT, jwtVerify } from "jose";

// TODO: this will all be done by backend
const secret = new TextEncoder().encode("your-secret-key");

// enum Role {
//   Admin,
//   User,
// }

interface ProjectMembership {
  project: string;
  role: string;
}

export interface User {
  email: string;
  superadmin: boolean;
  projectMemberships: ProjectMembership[];
}

export const users: User[] = [
  {
    email: "superadmin@example.com",
    superadmin: true,
    projectMemberships: [],
  },
  {
    email: "admin@example.com",
    superadmin: false,
    projectMemberships: [
      { project: "project-1", role: "admin" },
      { project: "project-2", role: "admin" },
      { project: "project-3", role: "admin" },
    ],
  },
  {
    email: "user@example.com",
    superadmin: false,
    projectMemberships: [
      { project: "project-1", role: "user" },
      { project: "project-2", role: "user" },
      { project: "project-3", role: "user" },
    ],
  },
];

export async function login(email: string): Promise<string | null> {
  return new Promise((resolve, reject) => {
    const user = users.find((u) => u.email === email);
    if (!user) {
      resolve(null);
      return;
    }

    setTimeout(async () => {
      try {
        const token = await new SignJWT({
          email: user.email,
          superadmin: user.superadmin,
          projectMemberships: user.projectMemberships,
        })
          .setProtectedHeader({ alg: "HS256" })
          .sign(secret);

        localStorage.setItem("token", token);
        resolve(token);
      } catch (error) {
        console.error("Error signing the token:", error);
        resolve(null);
      }
    }, 500);
  });
}

export async function getUserFromToken() {
  const token = localStorage.getItem("token");
  if (!token) return null;

  try {
    const { payload } = await jwtVerify(token, secret);
    return payload;
  } catch (error) {
    console.error("Invalid token:", error);
    return null;
  }
}

export function logout() {
  localStorage.removeItem("token");
}
