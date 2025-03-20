"use client";

import { useEffect, useState } from "react";
import ProjectCard from "./components/ProjectCard";
import { useUser } from "@/app/context/UserContext";
import GenericForm, { FormData } from "@/app/components/GenericForm";
import { fetchWithAuth } from "@/app/utils/api/api";
import { toast } from "react-toastify";
import { User, Project } from "@/app/types";

interface ProjectCardProps {
  projectID: number;
  name: string;
  creationTime: string;
  assetCount: number;
  userNames: string[];
}

interface GetAllProjectsResponse {
  projectCount: number;
  fullProjectInfos: Project[];
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
  const [projectList, setProjectList] = useState<ProjectCardProps[]>([]);

  const [query, setQuery] = useState<string>("");

  const { user } = useUser();

  const fetchProjects = async () => {
    try {
      const response = await fetchWithAuth("projects");
      if (!response.ok) {
        throw new Error(
          `Failed to fetch projects (Status: ${response.status} - ${response.statusText})`
        );
      }
      const data = (await response.json()) as GetAllProjectsResponse;

      return data.fullProjectInfos.map(
        (project: Project) =>
          ({
            projectID: project.projectID,
            name: project.projectName,
            creationTime: project.creationTime,
            assetCount: project.assetCount,
            userNames: project.admins
              .concat(project.regularUsers)
              .map((user: User) => user.name),
          }) as ProjectCardProps
      );
    } catch (error) {
      console.error("[Diagnostics] Error fetching projects: ", error);
      return [];
    }
  };

  const handleAddProject = async (formData: FormData) => {
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
      const response = await fetchWithAuth("projects", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        throw new Error("Failed to create project.");
      }

      const data = await response.json();

      const createdProject = data[0];
      if (!createdProject) {
        throw new Error("No project returned from the API.");
      }

      setNewProjectModalOpen(false);
      toast.success("Created new project successfully.");

      return await fetchProjects();
    } catch (error) {
      console.error("Error creating project:", error);
      toast.error((error as Error).message);
    }
  };

  const doSearch = async () => {
    const projects = await fetchProjects();
    if (!query.trim()) {
      setProjectList(projects);
      return;
    }

    const response = await fetchWithAuth(
      `/search?query=${encodeURIComponent(query)}`
    );

    if (!response.ok) {
      throw new Error("Failed to do search");
    }

    const data = await response.json();

    console.log(data);

    const filteredProjects = projects.filter((p: ProjectCardProps) =>
      data.projects.some(
        (project: Project) => project.projectID === p.projectID
      )
    );

    setProjectList(filteredProjects);
  };

  useEffect(() => {
    fetchProjects().then((data) => setProjectList(data));
  }, []);

  return (
    <div className="p-6 min-h-screen">
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between mb-6 space-y-2 md:space-y-0">
        <input
          type="text"
          placeholder="Search..."
          className="w-1/3 border border-gray-300 rounded-lg py-2 px-4 focus:outline-none focus:border-blue-500"
          onChange={(e) => setQuery(e.target.value)}
          onBlur={doSearch}
        />
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
          <div key={project.projectID} className="w-full h-full">
            <ProjectCard
              id={String(project.projectID)}
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

      <h1 className="text-2xl font-semibold mb-4 mt-4">Recent Assets</h1>
    </div>
  );
}
