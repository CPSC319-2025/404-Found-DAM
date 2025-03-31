"use client";

import React, { useEffect, useState, useRef } from "react";
import ProjectCard from "./components/ProjectCard";
import { useUser } from "@/app/context/UserContext";
import GenericForm, { Field as FormFieldType, FormData as FormDataType, ChangeType } from "@/app/components/GenericForm";
import { fetchWithAuth } from "@/app/utils/api/api";
import { toast } from "react-toastify";
import { Project, User } from "@/app/types";
import { useDropzone } from "react-dropzone";

import LoadingSpinner from "@/app/components/LoadingSpinner";

import PopupModal from "@/app/components/ConfirmModal";

interface ProjectCardProps {
  projectID: number;
  name: string;
  creationTime: string;
  assetCount: number;
  admins: User[];
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
  const [loading, setLoading] = useState<boolean>(true);
  const [query, setQuery] = useState<string>("");

  const [allProjects, setAllProjects] = useState<ProjectCardProps[]>([]);
  const [newProjectModalOpen, setNewProjectModalOpen] = useState(false);
  const [addTagsModalOpen, setAddTagsModalOpen] = useState(false);

  const [formFields, setFormFields] =
    useState<FormFieldType[]>(newProjectFormFields);

  const [allUsers, setAllUsers] = useState<User[]>([]);
  const [regularUserOptions, setRegularUserOptions] = useState<User[]>([]);
  const [adminOptions, setAdminOptions] = useState<User[]>([]);
  const [tagOptions, setTagOptions] = useState<string[]>([]);

  const [configureTagsOpen, setConfigureTagsOpen] = useState(false);
  const [configuredTags, setConfiguredTags] = useState<string[]>([]);

  const [importProjectModalOpen, setImportProjectModalOpen] = useState(false);
  const [importedProjectFile, setImportedProjectFile] = useState<File | null>(null);

  const importFormRef = useRef<HTMLDivElement>(null);

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

  const fetchAllProjects = async () => {
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
            admins: project.admins,
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
      const response = await 
      ("projects", {
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

    const response = await fetchWithAuth(`/search?query=${encodeURIComponent(query)}`);

    if (!response.ok) {
      throw new Error("Failed to do search");
    }

    const data = await response.json();

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
  useEffect(() => {
    setLoading(true);

    // Fetch all projects (for filtering "My Projects") AND all users
    Promise.all([fetchAllProjects(), fetchUsers()])
      .then(([projects, users]) => {
        setAllProjects(projects);
        setAllUsers(users);
        setAdminOptions(users);
        setRegularUserOptions(users);
      })
      .catch((error) => {
        console.error("Error loading initial data:", error);
      })
      .finally(() => setLoading(false));
  }, []);

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

  const onDrop = (acceptedFiles: File[]) => {
    setImportedProjectFile(acceptedFiles[0]);
  }

  const onSubmitZip = async () => {
    const formData = new FormData();
    formData.append("file", importedProjectFile!);

    try {
      const response = await fetchWithAuth("/project/import", {
        method: "POST",
        body: formData as BodyInit,
        headers: {}
      });

      if (response.ok) {
        setImportedProjectFile(null);
        setImportProjectModalOpen(false);
        toast.success("Imported project successfully.");

        doSearch();
      } else {
        console.log("Error uploading file", response.status);
        toast.error("Failed to import project. Make sure zip's content is valid.");
      }
    } catch (error) {
      console.error("Error:", error);
    }
  }

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    multiple: false,
    accept: {
      "application/x-zip-compressed": []
    }
  });

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (importFormRef.current && !importFormRef.current.contains(event.target as Node)) {
        setImportProjectModalOpen(false);
        setImportedProjectFile(null);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);
      
//   const myProjects = allProjects.filter((project) =>
//     project.userNames.includes("Isabella Sanchez")
//   );

//   const filteredAllProjects = allProjects.filter(
//     (project) => !project.userNames.includes("Isabella Sanchez")
//   );
//   const myProjects = allProjects; // TODO

//   const filteredAllProjects = []; // TODO

//   const overallProjectsExist = allProjects.length > 0 || myProjects.length > 0;

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
          {user?.superadmin && (
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
            )}
          {user?.superadmin && (
            <button
              onClick={() => {
                fetchTags();
                setNewProjectModalOpen(true);
              }}
              className="bg-blue-500 text-white p-2 rounded-md md:ml-4 sm:w-auto"
            >
              New Project
            </button>
          )}
          {user?.superadmin && (
            <button
              onClick={() => setImportProjectModalOpen(true)}
              className="bg-blue-500 text-white p-2 rounded-md md:ml-4 sm:w-auto"
            >
              Import Project
            </button>
          )}
        </div>
      </div>
      {/* case 1: there are no projects overall */}
      {allProjects && allProjects.length < 1 && (
        <div className="flex flex-col items-center justify-center h-64">
          <p className="text-2xl text-gray-500">No projects to display!</p>
        </div>
      )}

      {/* else display the project sections */}
      {allProjects && allProjects.length > 0 && (
        <>
          {/* My Projects */}
          <div>
            <h1 className="text-2xl font-semibold mb-4">My Projects</h1>
            {allProjects.length > 0 ? (
              <div className="grid grid-cols-1 sm:grid-cols-[repeat(auto-fill,_minmax(320px,_1fr))] lg:grid-cols-[repeat(auto-fill,_minmax(320px,_420px))] gap-4">
                {allProjects.map((project) => (
                  <div key={project.projectID} className="w-full h-full">
                    <ProjectCard
                      id={String(project.projectID)}
                      name={project.name}
                      creationTime={project.creationTime}
                      assetCount={project.assetCount}
                      userNames={project.userNames}
                      admins={project.admins}
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
          {/* {filteredAllProjects.length > 0 && (*/}
          {/*  <div className="mt-8">*/}
          {/*    <h1 className="text-2xl font-semibold mb-4">All Projects</h1>*/}
          {/*    <div className="grid grid-cols-1 sm:grid-cols-[repeat(auto-fill,_minmax(320px,_1fr))] lg:grid-cols-[repeat(auto-fill,_minmax(320px,_420px))] gap-4">*/}
          {/*      {filteredAllProjects.map((project) => (*/}
          {/*        <div key={project.projectID} className="w-full h-full">*/}
          {/*          <ProjectCard*/}
          {/*            id={String(project.projectID)}*/}
          {/*            name={project.name}*/}
          {/*            creationTime={project.creationTime}*/}
          {/*            assetCount={project.assetCount}*/}
          {/*            userNames={project.userNames}*/}
          {/*          />*/}
          {/*        </div>*/}
          {/*      ))}*/}
          {/*    </div>*/}
          {/*  </div>*/}
          {/*)}*/}
        </>
      )}
              
      <h1 className="text-2xl font-semibold mb-4 mt-4">Recent Assets</h1>

      {newProjectModalOpen && (
        <GenericForm
          title="Create New Project"
          fields={formFields}
          onSubmit={handleAddProject}
          onCancel={() => setNewProjectModalOpen(false)}
          submitButtonText="Create Project"
        />
      )}

      {importProjectModalOpen && (
        <div className="fixed inset-0 bg-gray-500 bg-opacity-50 flex justify-center items-center">
          <div ref={importFormRef} className="bg-white p-6 rounded shadow-lg w-200 max-h-screen overflow-y-auto">
            <div className="bg-white p-8 rounded shadow-md text-center w-full max-w-xl">
              <div
                {...getRootProps()}
                className="border-2 border-dashed border-gray-300 p-8 rounded-lg mb-4 cursor-pointer"
              >
                <input {...getInputProps()} />
                {isDragActive ? (
                  <p className="text-xl text-teal-600">Drop the files here...</p>
                ) : (
                  <>
                    <p className="text-xl mb-2">Drag and Drop here</p>
                    <p className="text-gray-500 mb-4">or</p>
                    <button className="bg-indigo-600 text-white hover:bg-indigo-700 px-4 py-2 rounded">
                      Select file
                    </button>
                    <p className="text-sm text-gray-400 mt-2">
                      Zip only
                    </p>
                  </>
                )}
              </div>
            </div>

            {importedProjectFile && (
              <div>
                <div className="py-2">
                  <p>Uploaded Project Zip: <i>{importedProjectFile.name}</i></p>
                </div>
                <div className="flex justify-end py-2">
                  <button
                    className="bg-blue-500 text-white p-2 rounded float"
                    onClick={() => onSubmitZip()}
                  >
                    Add Project
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
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
