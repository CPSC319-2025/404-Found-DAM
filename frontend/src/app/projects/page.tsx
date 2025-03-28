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

import PopupModal from "@/app/components/ConfirmModal";

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

const addTagFormFields: FormFieldType[] = [
  {
    name: "newTag",
    label: "New Tag",
    type: "text",
    placeholder: "Enter new tag name",
    required: true,
  },
];

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
    type: "select",
    isMultiSelect: true,
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

  const [query, setQuery] = useState<string>("");

  const [newProjectModalOpen, setNewProjectModalOpen] = useState(false);
  const [addTagsModalOpen, setAddTagsModalOpen] = useState(false);
  const [projectList, setProjectList] = useState<ProjectCardProps[]>([]);

  const [formFields, setFormFields] =
    useState<FormFieldType[]>(newProjectFormFields);

  const [allUsers, setAllUsers] = useState<User[]>([]);
  const [regularUserOptions, setRegularUserOptions] = useState<User[]>([]);
  const [adminOptions, setAdminOptions] = useState<User[]>([]);

  const [tagOptions, setTagOptions] = useState<string[]>([]);

  const [configureTagsOpen, setConfigureTagsOpen] = useState(false);
  const [configuredTags, setConfiguredTags] = useState<string[]>([]);

  const [confirmConfigurePopup, setConfirmConfigurePopup] = useState(false);
  const [pendingConfigureFormData, setPendingConfigureFormData] =
    useState<FormData | null>(null);

  // Global Tags
  const fetchTags = async () => {
    try {
      const response = await fetchWithAuth("tags");
      if (!response.ok) {
        throw new Error(`Failed to fetch tags: ${response.statusText}`);
      }
      // returns an array of tag names (strings)
      const data = await response.json();
      setTagOptions(data);
    } catch (error) {
      console.error("Error fetching tags:", error);
    }
  };

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
      return [] as ProjectCardProps[];
    }
  };

  const handleSubmitConfigureTags = async (formData: FormData) => {
    const updatedTags = (formData.tags as string[])
      .map((t) => t.trim())
      .filter((t) => t.length > 0);

    try {
      const response = await fetchWithAuth("tags", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(updatedTags.map((name) => ({ name }))),
      });

      if (!response.ok) throw new Error("Failed to update tags");

      toast.success("Tags updated");
      setConfigureTagsOpen(false);
      fetchTags(); // refresh local tag options
    } catch (error) {
      console.error("Error replacing tags", error);
      toast.error("Failed to update tags");
    }
  };

  useEffect(() => {
    if (addTagsModalOpen) {
      fetchTags();
    }
  }, [addTagsModalOpen]);

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

      const projects = await fetchProjects();
      setProjectList(projects);
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

  useEffect(() => {
    fetchProjects().then((projects) => setProjectList(projects));
    fetchUsers().then((users) => {
      setAllUsers(users);
      setAdminOptions(users);
      setRegularUserOptions(users);
    });
  }, []);

  useEffect(() => {
    console.log("Updated tag options:", tagOptions);
  }, [tagOptions]);

  const onSubmitConfigureTags = (formData: FormData) => {
    setPendingConfigureFormData(formData);
    setConfirmConfigurePopup(true);
  };

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
      if (field.name === "tags") {
        field.options = tagOptions.map((tag: string) => ({
          id: tag,
          name: tag,
        }));
      }
    });

    setFormFields(updatedFormFields);
  }, [adminOptions, regularUserOptions, tagOptions]);

  return (
    <div className="p-6 min-h-screen">
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between mb-6 space-y-2 md:space-y-0">
        <input
          type="text"
          placeholder="Search... <press enter or click outside>"
          className="w-1/3 border border-gray-300 rounded-lg py-2 px-4 focus:outline-none focus:border-blue-500"
          onChange={(e) => setQuery(e.target.value)}
          onBlur={doSearch}
          onKeyDown={(e) => e.key === "Enter" && doSearch()}
        />
        <div>
          <button
            onClick={async () => {
              const response = await fetchWithAuth("tags");
              const tags = await response.json();
              setConfiguredTags(tags);
              setConfigureTagsOpen(true);
            }}
            className="bg-blue-500 text-white p-2 rounded-md md:ml-4 sm:w-auto"
          >
            Configure Tags
          </button>
          {user?.superadmin && (
            <button
              onClick={() => {
                fetchTags();
                console.log("line 316", tagOptions);
                setNewProjectModalOpen(true);
              }}
              className="bg-blue-500 text-white p-2 rounded-md md:ml-4 sm:w-auto"
            >
              New Project
            </button>
          )}
        </div>
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
          fields={formFields}
          onSubmit={handleAddProject}
          onCancel={() => setNewProjectModalOpen(false)}
          submitButtonText="Create Project"
        />
      )}
      {configureTagsOpen && (
        <GenericForm
          title="Configure Tags"
          fields={[
            {
              name: "tags",
              label: "Tag List",
              type: "text",
              isMulti: true,
              required: false,
              value: configuredTags,
            },
          ]}
          onSubmit={onSubmitConfigureTags} // use our new handler here
          onCancel={() => setConfigureTagsOpen(false)}
          confirmRemoval={true}
          confirmRemovalMessage="Are you sure you want to remove this tag? Removing it will affect all projects and assets that use the tag."
          submitButtonText="Update Tags"
          disableOutsideClose={confirmConfigurePopup}
        />
      )}

      {confirmConfigurePopup && (
        <div onClick={(e) => e.stopPropagation()}>
          <PopupModal
            title="Confirm Tag Changes"
            isOpen={true}
            onClose={() => {
              // Only clear the confirmation popup state, leaving the GenericForm open.
              setConfirmConfigurePopup(false);
            }}
            onConfirm={async () => {
              if (pendingConfigureFormData) {
                await handleSubmitConfigureTags(pendingConfigureFormData);
              }
              setConfirmConfigurePopup(false);
            }}
            messages={[
              "Are you sure you would like to make these changes? This may cause unexpected project and asset changes across the system.",
            ]}
          />
        </div>
      )}
    </div>
  );
}
