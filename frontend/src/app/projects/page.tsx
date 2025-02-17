"use client";

import { useState } from "react";
import ProjectCard from "./components/ProjectCard";
import Search from "./components/Search";
import GenericForm from "@/app/components/GenericForm";

const projects = [
  { id: "1", name: "Project One" },
  { id: "2", name: "Project Two" },
  { id: "3", name: "Project Three" },
  { id: "4", name: "Project Four" },
  { id: "5", name: "Project Five" },
];

const isSuperAdmin = true;

export default function ProjectsPage() {
  const [newProjectModalOpen, setNewProjectModalOpen] = useState(false);
  const [projectList, setProjectList] = useState(projects);

  const handleAddProject = (formData: { name: string }) => {
    const newProject = { id: (projectList.length + 1).toString(), name: formData.name };
    setProjectList([...projectList, newProject]);
    setNewProjectModalOpen(false);
  };

  const fields = [
    { name: "name", label: "Project Name", type: "text", placeholder: "Enter project name", required: true },
		{ name: "location", label: "Project Location", type: "text", placeholder: "Enter project location", required: true },
		{ name: "tags", label: "Tags", type: "text", isMulti: true, placeholder: "Add tags (Press Enter to add one)" },
		{ name: "admins", label: "Admins", type: "text", isMulti: true, placeholder: "Enter Admins", required: true },
		{ name: "users", label: "Users", type: "text", isMulti: true, placeholder: "Enter Users" },
  ];

  return (
    <div className="p-6 min-h-screen">
			<div className="flex flex-col md:flex-row items-start md:items-center justify-between mb-6 space-y-2 md:space-y-0">
				<Search />
				{isSuperAdmin && (
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
      <h1 className="text-2xl font-semibold mb-4 py-6">Recent Files</h1>

      {newProjectModalOpen && (
        <GenericForm
          fields={fields}
          onSubmit={handleAddProject}
          onCancel={() => setNewProjectModalOpen(false)}
          submitButtonText="Add Project"
        />
      )}
    </div>
  );
}
