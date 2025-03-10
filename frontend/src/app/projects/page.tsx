"use client";

import { useState } from "react";
import ProjectCard from "./components/ProjectCard";
import Search from "./components/Search";
import { useUser } from "@/app/context/UserContext";
import GenericForm, { FormData } from "@/app/components/GenericForm";

const projects = [
  { id: "1", name: "Project One" },
  { id: "2", name: "Project Two" },
  { id: "3", name: "Project Three" },
  { id: "4", name: "Project Four" },
  { id: "5", name: "Project Five" },
];

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
    name: "tags",
    label: "Tags",
    type: "text",
    isMulti: true,
    placeholder: "Add tags (Press Enter to add one)",
    required: true,
  },
  {
    name: "admins",
    label: "Admins",
    type: "select",
    isMultiSelect: true,
    required: true,
    options: [
      { id: "0", name: "dave" },
      { id: "1", name: "nehemiah" },
      { id: "2", name: "susan" },
    ],
  },
  {
    name: "users",
    label: "Users",
    type: "select",
    isMultiSelect: true,
    options: [
      { id: "0", name: "alice" },
      { id: "1", name: "bob" },
      { id: "2", name: "charlie" },
    ],
  },
];

export default function ProjectsPage() {
  const [newProjectModalOpen, setNewProjectModalOpen] = useState(false);
  const [projectList, setProjectList] = useState(projects);
  const { user } = useUser();

  const handleAddProject = (formData: FormData) => {
    const newProject = {
      id: (projectList.length + 1).toString(),
      name: formData.name as string,
    };
    setProjectList([...projectList, newProject]);
    setNewProjectModalOpen(false);
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
            <ProjectCard id={`project-${project.id}`} name={project.name} />
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
