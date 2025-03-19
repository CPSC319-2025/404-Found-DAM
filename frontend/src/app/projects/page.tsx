"use client";

import { useState, useEffect } from "react";
import ProjectCard from "./components/ProjectCard";
import Search from "./components/Search";
import { useUser } from "@/app/context/UserContext";
import GenericForm, { FormData } from "@/app/components/GenericForm";

// const projects = [
//   { id: "1", name: "Project One" },
//   { id: "2", name: "Project Two" },
//   { id: "3", name: "Project Three" },
//   { id: "4", name: "Project Four" },
//   { id: "5", name: "Project Five" },
// ];

interface FullProjectInfo {
  projectID: number;
  projectName: string;
  location: string;
  description: string;
  creationTime: string; // ISO
  active: boolean;
  archivedAt: string | null;
  assetCount: number;
  regularUserNames: string[];
  adminNames: string[];
}

interface Project {
  id: string;
  name: string;
  creationTime: string;
  assetCount: number;
  userNames: string[];
}

interface GetAllProjectsResponse {
  projectCount: number;
  fullProjectInfos: FullProjectInfo[];
}

const newProjectFormFields = [
  {
    name: "name",
    label: "Project Name",
    type: "text",
    placeholder: "Enter project name",
    required: true,
  },
  {
    name: "location",
    label: "Project Location",
    type: "text",
    placeholder: "Enter project location",
    required: true,
  },
  {
    name: "description",
    label: "Description",
    type: "text",
    placeholder: "Enter project description",
    required: true,
  },
  {
    name: "tags",
    label: "Tags",
    type: "text",
    isMulti: true,
    placeholder: "Add tags (Press Enter to add one)",
    required: false,
  },
  {
    name: "admins",
    label: "Admins",
    type: "select",
    isMultiSelect: true,
    required: false,
    options: [
      { id: "1", name: "alice (alice@example.com)" },
      { id: "2", name: "bob (bob@example.com)" },
      { id: "3", name: "charlie (charlie@example.com)" },
    ],
  },
  {
    name: "users",
    label: "Users",
    type: "select",
    isMultiSelect: true,
    options: [
      { id: "1", name: "alice (alice@example.com)" },
      { id: "2", name: "bob (bob@example.com)" },
      { id: "3", name: "charlie (charlie@example.com)" },
    ],
  },
];

export default function ProjectsPage() {
  const [newProjectModalOpen, setNewProjectModalOpen] = useState(false);
  const [projectList, setProjectList] = useState<Project[]>([]);
  const { user } = useUser();

  useEffect(() => {
    const fetchProjects = async () => {
      const url = `${process.env.NEXT_PUBLIC_API_BASE_URL}/projects`;
      try {
        const response = await fetch(url);
        if (!response.ok) {
          throw new Error(
            `Failed to fetch projects (Status: ${response.status} - ${response.statusText})`
          );
        }
        const data = (await response.json()) as GetAllProjectsResponse;

        // Diagnostic logging: inspect API response structure
        console.log("[Diagnostics] Response from", url, ":", data);

        const projectsFromBackend = data.fullProjectInfos.map(
          (project: FullProjectInfo) => ({
            id: project.projectID.toString(),
            name: project.projectName,
            creationTime: project.creationTime,
            assetCount: project.assetCount,
            userNames: project.regularUserNames.concat(project.adminNames),
          })
        );

        console.log("[Diagnostics] Parsed projects:", projectsFromBackend);

        setProjectList(projectsFromBackend);
      } catch (error) {
        console.log(process.env.NEXT_PUBLIC_API_BASE_URL);
        console.error(
          "[Diagnostics] Error fetching projects from",
          url,
          ":",
          error
        );
      }
    };

    fetchProjects();
  }, []);

  const handleAddProject = async (formData: FormData) => {
    console.log("projectName ", formData.name);
    console.log("location ", formData.location);
    console.log("description ", formData.description);
    console.log("tags ", formData.tags);
    console.log("admins ", formData.admins);
    console.log("users ", formData.users);
    const payload = [
      {
        defaultMetadata: {
          projectName: formData.name as string,
          location: formData.location as string,
          description: formData.description as string,
          active: true,
        },
        tags: formData.tags ? (formData.tags as string[]) : [],
        admins: formData.admins
          ? (formData.admins as (string | number)[]).map(Number)
          : [],
        users: formData.users
          ? (formData.users as (string | number)[]).map(Number)
          : [],
      },
    ];
    try {
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL}/projects`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(payload),
        }
      );

      if (!response.ok) {
        throw new Error("Failed to create project.");
      }

      const data = await response.json();

      const createdProject = data[0];
      if (!createdProject) {
        throw new Error("No project returned from the API.");
      }

      setProjectList([
        ...projectList,
        {
          id: createdProject.createdProjectID.toString(),
          name: formData.name as string,
          creationTime: new Date().toISOString(),
          assetCount: 0,
          userNames: [],
        },
      ]);
      setNewProjectModalOpen(false);
    } catch (error) {
      console.log("Error creating project:", error);
    }
  };

  return (
    <div className="p-6 min-h-screen">
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between mb-6 space-y-2 md:space-y-0">
        <Search />
        {user?.superadmin && (
          <button
            onClick={() => setNewProjectModalOpen(true)}
            className="bg-blue-500 text-white p-2 rounded-md md:ml-4 sm:w-auto"
          >
            New Project
          </button>
        )}
      </div>
      <h1 className="text-2xl font-semibold mb-4">All Projects</h1>
      <div className="grid grid-cols-1 sm:grid-cols-[repeat(auto-fill,_minmax(320px,_1fr))] lg:grid-cols-[repeat(auto-fill,_minmax(320px,_420px))] gap-4">
        {projectList.map((project) => (
          <div key={project.id} className="w-full h-full">
            <ProjectCard
              id={project.id}
              name={project.name}
              creationTime={project.creationTime}
              assetCount={project.assetCount}
              userNames={project.userNames}
            />
          </div>
        ))}
      </div>

      {newProjectModalOpen && (
        <GenericForm
          title="Create New Project"
          fields={newProjectFormFields}
          onSubmit={handleAddProject}
          onCancel={() => setNewProjectModalOpen(false)}
          submitButtonText="Create Project"
        />
      )}
    </div>
  );
}
