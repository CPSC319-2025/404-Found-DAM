"use client";

import { useEffect, useState } from "react";
import ProjectCard from "./components/ProjectCard";
import { useUser } from "@/app/context/UserContext";
import GenericForm, {
  Field as FormFieldType,
  FormData,
  ChangeType,
} from "@/app/components/GenericForm";
import { fetchWithAuth } from "@/app/utils/api/api";
import { toast } from "react-toastify";
import { Project, User } from "@/app/types";

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

const newProjectFormFields: FormFieldType[] = [
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
  },
  {
    name: "users",
    label: "Users",
    type: "select",
    isMultiSelect: true,
  },
];

export default function ProjectsPage() {
  const { user } = useUser();

  // state variables related to projects

  const [allProjects, setAllProjects] = useState<ProjectCardProps[]>([]);
  const [newProjectModalOpen, setNewProjectModalOpen] = useState(false);
  const [query, setQuery] = useState<string>("");

  // state variables for the form

  const [formFields, setFormFields] =
    useState<FormFieldType[]>(newProjectFormFields);
  const [allUsers, setAllUsers] = useState<User[]>([]);
  const [regularUserOptions, setRegularUserOptions] = useState<User[]>([]);
  const [adminOptions, setAdminOptions] = useState<User[]>([]);

  const onUserChange = (
    changeItem: { id: number; name: string },
    fieldName: string,
    changeType: ChangeType
  ) => {
    if (fieldName === "admins") {
      if (changeType === "select") {
        setRegularUserOptions((prev) =>
          prev.filter((user) => user.userID !== changeItem.id)
        );
      } else {
        const userToAddBack = allUsers.find(
          (user) => user.userID === changeItem.id
        );
        setRegularUserOptions((prev) => [...prev, userToAddBack!]);
      }
    } else {
      if (changeType === "select") {
        setAdminOptions((prev) =>
          prev.filter((admin) => admin.userID !== changeItem.id)
        );
      } else {
        const userToAddBack = allUsers.find(
          (user) => user.userID === changeItem.id
        );
        setAdminOptions((prev) => [...prev, userToAddBack!]);
      }
    }
  };

  const fetchAllProjects = async () => {
    try {
      const response = await fetchWithAuth("projects");
      if (!response.ok) {
        throw new Error(
          `Failed to fetch projects (Status: ${response.status} - ${response.statusText})`
        );
      }
      const data = (await response.json()) as GetAllProjectsResponse;
      console.log("All Projects", data);
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
      return [] as ProjectCardProps[];
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

      const projects = await fetchAllProjects();
      setAllProjects(projects);
    } catch (error) {
      console.error("Error creating project:", error);
      toast.error((error as Error).message);
    }
  };

  const doSearch = async () => {
    const projects = await fetchAllProjects();
    if (!query.trim()) {
      setAllProjects(projects);
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

    setAllProjects(filteredProjects);
  };

  const fetchUsers = async () => {
    try {
      const response = await fetchWithAuth("users");
      if (!response.ok) {
        throw new Error(
          `Failed to fetch users (Status: ${response.status} - ${response.statusText})`
        );
      }
      return (await response.json()).users as User[];
    } catch (error) {
      console.error("[Diagnostics] Error fetching users: ", error);
      return [] as User[];
    }
  };

  const loadProjects = async () => {
    const projects = await fetchAllProjects();
    setAllProjects(projects);
  };

  useEffect(() => {
    loadProjects();
    fetchUsers().then((users) => {
      setAllUsers(users);
      setAdminOptions(users);
      setRegularUserOptions(users);
    });
  }, []);

  useEffect(() => {
    fetchAllProjects().then((projects) => setAllProjects(projects));
    fetchUsers().then((users) => {
      setAllUsers(users);
      setAdminOptions(users);
      setRegularUserOptions(users);
    });
  }, []);

  // whenever a user selects an admin/regular user we need to update the form (filter options)
  useEffect(() => {
    const updatedFormFields = [...newProjectFormFields];

    updatedFormFields.forEach((field) => {
      if (field.name === "admins") {
        field.options = adminOptions.map((user) => ({
          id: user.userID,
          name: `${user.name} (${user.email})`,
        }));
        field.onChange = onUserChange;
      }

      if (field.name === "users") {
        field.options = regularUserOptions.map((user) => ({
          id: user.userID,
          name: `${user.name} (${user.email})`,
        }));
        field.onChange = onUserChange;
      }
    });

    setFormFields(updatedFormFields);
  }, [adminOptions, regularUserOptions]);

  const myProjects = allProjects.filter(
    (project) =>
      user &&
      user.projectMemberships.some(
        (membership) => membership.project === project.projectID
      )
  );

  const filteredAllProjects = allProjects.filter(
    (project) =>
      user &&
      !user.projectMemberships.some(
        (membership) => membership.project === project.projectID
      )
  );

  const overallProjectsExist = allProjects.length > 0 || myProjects.length > 0;

  return (
    <div className="p-6 min-h-screen">
      {/* header: Always display search bar for all users and "New Project" button if superadmin */}
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

      {/* case 1: there are no projects overall */}
      {!overallProjectsExist && (
        <div className="flex flex-col items-center justify-center h-64">
          <p className="text-2xl text-gray-500">No projects to display!</p>
        </div>
      )}

      {/* else display the project sections */}
      {overallProjectsExist && (
        <>
          {/* My Projects */}
          <div>
            <h1 className="text-2xl font-semibold mb-4">My Projects</h1>
            {myProjects.length > 0 ? (
              <div className="grid grid-cols-1 sm:grid-cols-[repeat(auto-fill,_minmax(320px,_1fr))] lg:grid-cols-[repeat(auto-fill,_minmax(320px,_420px))] gap-4">
                {myProjects.map((project) => (
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
            ) : (
              <p className="text-lg text-gray-500">
                No projects assigned to you
              </p>
            )}
          </div>

          {/* All Projects Section filtering projects not belonging to me */}
          {filteredAllProjects.length > 0 && (
            <div className="mt-8">
              <h1 className="text-2xl font-semibold mb-4">All Projects</h1>
              <div className="grid grid-cols-1 sm:grid-cols-[repeat(auto-fill,_minmax(320px,_1fr))] lg:grid-cols-[repeat(auto-fill,_minmax(320px,_420px))] gap-4">
                {filteredAllProjects.map((project) => (
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
            </div>
          )}
        </>
      )}

      {newProjectModalOpen && (
        <GenericForm
          title="Create New Project"
          fields={formFields}
          onSubmit={handleAddProject}
          onCancel={() => setNewProjectModalOpen(false)}
          submitButtonText="Create Project"
        />
      )}
    </div>
  );
}
