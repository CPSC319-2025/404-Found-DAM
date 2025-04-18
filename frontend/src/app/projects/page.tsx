"use client";

import React, { useEffect, useState, useRef, useMemo } from "react";
import ProjectCard from "./components/ProjectCard";
import { useUser } from "@/app/context/UserContext";
import GenericForm, { Field as FormFieldType, FormData as FormDataType, ChangeType } from "@/app/components/GenericForm";
import { fetchWithAuth } from "@/app/utils/api/api";
import { toast } from "react-toastify";
import { Project, User } from "@/app/types";
import { useDropzone } from "react-dropzone";
import { ArrowDownIcon } from "@heroicons/react/24/solid";
import { DocumentTextIcon } from "@heroicons/react/24/outline";

import LoadingSpinner from "@/app/components/LoadingSpinner";
import JSZip from "jszip";

import PopupModal from "@/app/components/ConfirmModal";
import Pagination from "@mui/material/Pagination";
import Image from "next/image";
import { ArrowDownTrayIcon, PencilIcon } from "@heroicons/react/24/outline";
import Link from "next/link";
import { formatFileSize } from "@/app/utils/api/formatFileSize";
import { convertUtcToLocal } from "@/app/utils/api/getLocalTime";
import { downloadAsset, getAssetFile } from "@/app/utils/api/getAssetFile";

interface ProjectCardProps {
  projectID: number;
  name: string;
  archived: boolean;
  creationTime: string;
  assetCount: number;
  admins: User[];
  userNames: string[];
  allUsers?: User[];
}

interface LoadingSpinnerProps {
  message?: string;
  size?: number;
  className?: string;
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
  {
    name: "description",
    label: "Description",
    type: "text",
    placeholder: "Enter project description",
    required: true,
  },
];

function Items({
  currentItems,
  openPreview,
  downloadAssetConfirm,
  showAssetMetadata,
}: {
  currentItems?: any[];
  openPreview: any;
  downloadAssetConfirm: any,
  showAssetMetadata: any,
}) {
  return (
    <div className="items min-h-[70vh] overflow-y-auto mt-4 rounded-lg p-4">
      <div className="overflow-x-auto">
        <table className="min-w-full bg-white border border-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                File Name
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Image
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Filesize
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Last Updated
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Uploaded By
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Tags
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Project
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {currentItems?.map((asset: any) => (
              <tr key={asset.blobID} className="hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">
                    {asset.filename}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="h-20 w-20 relative">
                    {!asset.src && (
                      <div className="h-full w-full flex items-center justify-center bg-gray-100 animate-pulse" style={{ animationDuration: '0.7s' }}>
                        <span className="text-gray-400 text-xs">Loading</span>
                      </div>
                    )}
                    {asset.src && asset.mimetype.includes("image") && (
                      <Image
                        src={asset.src}
                        alt={`${asset.filename}`}
                        width={120}
                        height={120}
                        className="object-cover rounded w-full h-full cursor-pointer"
                        onClick={() => openPreview(asset)}
                      />
                    )}
                    {asset.src && !asset.mimetype.includes("image") && (
                      <video
                        src={asset.src ?? ""}
                        width={120}
                        height={120}
                        className="object-cover rounded w-full h-full cursor-pointer"
                        onClick={() => openPreview(asset)}
                      />
                    )}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">
                    {formatFileSize(asset.filesizeInKB)}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {convertUtcToLocal(asset.date)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-gray-900">
                    {asset.uploadedBy?.email}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="flex gap-1">
                    {asset.tags.map((tag: any) => (
                      <span
                        key={tag}
                        className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-blue-100 text-blue-800"
                      >
                        {tag}
                      </span>
                    ))}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium">
                    <Link
                      href={`/projects/${asset.projectID}`}
                      className="hover:bg-gray-200 p-2 rounded text-blue-500"
                    >
                      {asset.projectName}
                    </Link>
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                  <div className="flex gap-3">
                    <button
                      className="text-indigo-600 hover:text-indigo-900"
                      onClick={() => showAssetMetadata(asset)}
                    >
                      <span className="flex items-center justify-center w-8 h-8 rounded-full bg-gray-200 hover:bg-gray-300 transition">
                        <DocumentTextIcon className="h-5 w-5" />
                      </span>
                    </button>
                    <button
                      className="text-indigo-600 hover:text-indigo-900"
                      onClick={() => downloadAssetConfirm(asset)}
                    >
                      <span className="flex items-center justify-center w-8 h-8 rounded-full bg-gray-200 hover:bg-gray-300 transition">
                        <ArrowDownTrayIcon className="h-5 w-5" />
                      </span>
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default function ProjectsPage() {
  const { user } = useUser();
  const [query, setQuery] = useState<string>("");

  const [aiDescription, setAIDescription] = useState("");

  const [aiLoading, setAILoading] = useState(false);

  const generateAIDescription = async (formData: Record<string, any>) => {
    const { name, location, tags } = formData;
    const prompt = `Given the following project details:
  - Project Name: ${name}
  - Project Location: ${location}
  - Tags: ${Array.isArray(tags) ? tags.join(", ") : tags}
  Generate a creative and engaging project description.`;

    try {
      const response = await fetch("/api/generate-description", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ prompt }),
      });
      const data = await response.json();
      setAIDescription(data.description);
    } catch (error) {
      console.error("Error generating AI description:", error);
    }
  };

  const [confirmDownloadPopup, setConfirmDownloadPopup] = useState(false);
  const [requestedDownloadAsset, setRequestedDownloadAsset] = useState<any>(null);

  const [confirmOverwriteDescriptionPopup, setConfirmOverwriteDescriptionPopup] = useState(false); // sean
  const [storedUpdateField, setStoredUpdateField] = useState<Function | null>(null);
  const [storedPrompt, setStoredPrompt] = useState("");
  const [storedFormData, setStoredFormData] = useState<any>(null);


  const [isPreviewAssetMetadata, setIsPreviewAssetMetadata] = useState(false);
  const [assetMetadataFields, setAssetMetadataFields] = useState<any[]>([]);
  const [projectMetadataFields, setProjectMetadataFields] = useState<any[]>([]);
  const [assetMetadataName, setAssetMetadataName] = useState<string>("");

  const showAssetMetadata = async (asset: any) => {
    const response = await fetchWithAuth(`palette/blob/${asset.blobID}/fields`);
    if (!response.ok) {
      console.error(
        `Failed to fetch asset metadata (Status: ${response.status} - ${response.statusText})`
      );
      return;
    }

    const data = await response.json();

    const responseP = await fetchWithAuth(`projects/${asset.projectID}`);

    if (!responseP.ok) {
      throw new Error("Failed to get project.");
    }

    const project = await responseP.json();

    const metadataFields = data.fields
      .filter((field: any) => {
        const foundField = project.metadataFields.find((pmf: any) => pmf.fieldID === field.fieldId);
        if (foundField && foundField.isEnabled) {
          return true;
        } else {
          return false;
        }
      })
      .map((field: any) => {
        return {
          name: field.fieldName,
          label: field.fieldName,
          type: field.fieldType,
          placeholder: "",
          value: field.fieldValue
        }
      });

    if (metadataFields.length < 1) {
      toast.warn("No custom metadata associated to this asset");
      return;
    }

    setAssetMetadataFields(metadataFields);

    setAssetMetadataName(asset.filename);

    setIsPreviewAssetMetadata(true);
  }

  const downloadAssetConfirm = async (asset: any) => {
    if (asset.mimetype.includes('image')) {
      setRequestedDownloadAsset(asset);
      setConfirmDownloadPopup(true);
    } else {
      downloadAssetWrapper(false, asset);
    }
  };

  const downloadAssetWrapper = async (addWatermark: boolean, asset: any) => {
    setConfirmDownloadPopup(false);
    try {

      try {
        const checkResponse = await fetchWithAuth(`/projects/${asset.projectID}/${asset.blobID}`, {
          method: "GET",
        });

        if (!checkResponse.ok) {

          toast.error("Asset has been deleted. Refreshing...");
          setTimeout(() => {
            window.location.reload();
          }, 1500);
          return;
          
          // throw new Error("Asset has been deleted");
        }
      } catch (error) {

        toast.error("Asset has been deleted. Refreshing...");
          setTimeout(() => {
            window.location.reload();
          }, 1500);
          return;
        // window.location.reload();
        // throw new Error("Asset has been deleted");
      }
      toast.success("Starting download...");
      await downloadAsset(
        asset,
        { projectID: asset.projectID, projectName: asset.projectName },
        user,
        addWatermark
      );
    } catch (e) {
      toast.error((e as Error).message);
    } finally {
      setRequestedDownloadAsset(null);
    }
  };

  const confirmOverwriteDescriptionWrapper = async (userConfirmsOverwriteDescription: boolean, updateFieldParam?: Function, promptParam?: string) => { // sean
    setConfirmOverwriteDescriptionPopup(false);
      if (!userConfirmsOverwriteDescription) return

      const updateField = updateFieldParam || storedUpdateField;
      const prompt = promptParam || storedPrompt;
      try {
        const response = await fetch("/api/gemini", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ prompt }),
        });
        const data = await response.json();
        console.log(data);
        const generatedDescription = data.description;
        if (generatedDescription && typeof updateField === "function") {
          // Update the description field with the AI-generated text
          updateField("description", generatedDescription);
          console.log("reached generatedDescription true branch");
        }
        console.log("end of overwritedescriptionwrapper function");
        console.log(generatedDescription);
        console.log("type of fucntion:" + updateField);
      } catch (error) {
        console.error("Error generating AI description:", error);
      }
  }

  const [allProjects, setAllProjects] = useState<ProjectCardProps[]>([]);
  const [myProjects, setMyProjects] = useState<ProjectCardProps[]>([]);
  const [newProjectModalOpen, setNewProjectModalOpen] = useState(false);
  const [addTagsModalOpen, setAddTagsModalOpen] = useState(false);

  const [isPreviewAsset, setIsPreviewAsset] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [previewType, setPreviewType] = useState<string | null>(null);

  const [formFields, setFormFields] =
    useState<FormFieldType[]>(newProjectFormFields);

  const [allUsers, setAllUsers] = useState<User[]>([]);
  const [regularUserOptions, setRegularUserOptions] = useState<User[]>([]);
  const [adminOptions, setAdminOptions] = useState<User[]>([]);
  const [tagOptions, setTagOptions] = useState<string[]>([]);

  const [configureTagsOpen, setConfigureTagsOpen] = useState(false);
  const [configuredTags, setConfiguredTags] = useState<string[]>([]);

  const [importProjectModalOpen, setImportProjectModalOpen] = useState(false);
  const [importedProjectFile, setImportedProjectFile] = useState<File | null>(
    null
  );

  const importFormRef = useRef<HTMLDivElement>(null);

  const [confirmConfigurePopup, setConfirmConfigurePopup] = useState(false);
  const [pendingConfigureFormData, setPendingConfigureFormData] =
    useState<FormDataType | null>(null);

  const [searchDone, setSearchDone] = useState<boolean>(false);

  const [currentAssets, setCurrentAssets] = useState<any[]>([]);
  const [paginatedAssets, setPaginatedAssets] = useState<any[]>([]);
  const [currentPage, setCurrentPage] = useState(1);

  function openPreview(asset: any) {
    if (asset.src) {
      setPreviewUrl(asset.src);
      setPreviewType(asset.mimetype);
      setIsPreviewAsset(true);
    }
  }

  function closeModal() {
    setIsPreviewAsset(false);
    setPreviewUrl(null);
    setPreviewType(null);
  }

  const setAssetSrcs = (assets: any[]) => {
    assets.forEach(async (asset: any) => {
      try {
        const src = (await getAssetFile(
          asset.blobSASUrl,
          asset.mimetype || ""
        )) as string;
        setPaginatedAssets((prevItems: any[]) =>
          prevItems.map((item: any) =>
            item.blobID === asset.blobID ? { ...item, src } : item
          )
        );
      } catch (error) {
        console.error(`Error loading asset ${asset.blobID}:`, error);
      }
    });
  };

  const [isLoading, setIsLoading] = useState(false);

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
            archived: !project.active,
            creationTime: project.creationTime,
            assetCount: project.assetCount,
            admins: project.admins,
            userNames: project.admins
              .concat(project.regularUsers)
              .map((user: User) => user.name),
            allUsers: project.admins.concat(project.regularUsers),
          }) as ProjectCardProps
      );
    } catch (error) {
      console.error("[Diagnostics] Error fetching projects: ", error);
      return [] as ProjectCardProps[];
    }
  };

  const handleSubmitConfigureTags = async (formData: FormDataType) => {
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

  const handleAddProject = async (formData: FormDataType) => {
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
      setMyProjects(
        projects.filter((p: ProjectCardProps) =>
          p.allUsers?.some(
            (projectUser: { userID: number }) =>
              projectUser.userID === user?.userID
          )
        )
      );
    } catch (error) {
      console.error("Error creating project:", error);
      toast.error((error as Error).message);
    }
  };

  const doSearch = async () => {
    const projects = await fetchAllProjects();
    if (!query.trim()) {
      setAllProjects(projects);
      setMyProjects(
        projects.filter((p: ProjectCardProps) =>
          p.allUsers?.some(
            (projectUser: { userID: number }) =>
              projectUser.userID === user?.userID
          )
        )
      );
      setCurrentAssets([]);
      setSearchDone(false);
      return;
    }

    const response = await fetchWithAuth(
      `/search?query=${encodeURIComponent(query)}`
    );

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
    setMyProjects(
      filteredProjects.filter((p: ProjectCardProps) =>
        p.allUsers?.some(
          (projectUser: { userID: number }) =>
            projectUser.userID === user?.userID
        )
      )
    );
    setCurrentAssets(data.assets);
    setSearchDone(true);
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
    if (currentAssets.length > 0) {
      handlePageChange(null, 1);
    }
  }, [currentAssets]);

  useEffect(() => {
    // Fetch all projects (for filtering "My Projects") AND all users
    Promise.all([fetchAllProjects(), fetchUsers()])
      .then(([projects, users]) => {
        setAllProjects(projects);
        setMyProjects(
          projects.filter((p: ProjectCardProps) =>
            p.allUsers?.some(
              (projectUser: { userID: number }) =>
                projectUser.userID === user?.userID
            )
          )
        );
        setAllUsers(users as User[]);
        setAdminOptions(users as User[]);
        setRegularUserOptions(users as User[]);
      })
      .catch((error) => {
        console.error("Error loading initial data:", error);
      });
  }, []);

  const otherProjects = allProjects.filter(
    (project) =>
      !project.allUsers?.some(
        (projectUser) => projectUser.userID === user?.userID
      )
  );

  const onSubmitConfigureTags = (formData: FormDataType) => {
    setPendingConfigureFormData(formData);
    setConfirmConfigurePopup(true);
  };

  const handlePageChange = (_: any, page: number) => {
    setCurrentPage(page);
    const startIndex = (page - 1) * 10;
    const endIndex = startIndex + 10;

    const assets = currentAssets.slice(startIndex, endIndex);
    setPaginatedAssets(assets);
    setAssetSrcs(assets);
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
  };

  const onSubmitImport = async () => {
    if (!importedProjectFile) {
      toast.error("No file selected.");
      return;
    }

    const zip = new JSZip();
    zip.file(importedProjectFile.name, importedProjectFile);

    try {
      const zipBlob = await zip.generateAsync({ type: "blob" });

      const formData = new FormData();
      formData.append("file", zipBlob, `${importedProjectFile.name}.zip`);

      toast.info("Importing project...");

      setImportedProjectFile(null);
      setImportProjectModalOpen(false);

      const response = await fetchWithAuth("/project/import", {
        method: "POST",
        body: formData as BodyInit,
        headers: {},
      });

      if (response.ok) {
        toast.success("Imported project successfully.");
        doSearch();
      } else {
        console.error("Error uploading file", response.status);

        toast.error(
          "Failed to import project. Make sure the file's content is valid."
        );
      }
    } catch (error) {
      console.error("Error:", error);
      toast.error("An error occurred while zipping the file.");
    }
  };

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    multiple: false,
    accept: {
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": [],
    },
  });

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        importFormRef.current &&
        !importFormRef.current.contains(event.target as Node)
      ) {
        setImportProjectModalOpen(false);
        setImportedProjectFile(null);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  const handleSearch = async () => {
    setIsLoading(true);
    const startTime = Date.now();
    try {
      await doSearch();
    } catch (error) {
      console.error("Search error:", error);
    } finally {
      const elapsed = Date.now() - startTime;
      const minDelay = 200;
      if (elapsed < minDelay) {
        setTimeout(() => {
          setIsLoading(false);
        }, minDelay - elapsed);
      } else {
        setIsLoading(false);
      }
    }
  };

  useEffect(() => {
    if (!query.trim()) {
      doSearch(); // This resets to the main screen (all projects)
    }
  }, [query]);
  const [showArchived, setShowArchived] = useState(true);

// Toggles at top of page.
  const toggleShowArchived = () => {
    setShowArchived((prev) => !prev);
  };

  const [showOnlyProjectsIAmAnAdminOf, setShowOnlyProjectsIAmAnAdminOf] = useState(false);
  // const [showOnlyUserProjects, setShowOnlyUserProjects] = useState(false);

  const toggleShowOnlyAdminProjects = () => {
    setShowOnlyProjectsIAmAnAdminOf((prev) => !prev);
  };

const displayedMyProjects = myProjects.filter((project) => {
  const isArchived = project.archived;
  const isAdmin = project.admins.some((admin) => admin.userID === user?.userID);

  if (!showArchived && isArchived) {
    return false; // exclude archived unless we're showing them
  }

  if (showOnlyProjectsIAmAnAdminOf && !isAdmin) {
    return false; // exclude if filtering for admin and user isn't admin
  }

  return true; // keep project
});

const displayedOtherProjects = otherProjects.filter((project) => {
  const isArchived = project.archived;
  const isAdmin = project.admins.some((admin) => admin.userID === user?.userID);

  if (!showArchived && isArchived) {
    return false;
  }

  if (showOnlyProjectsIAmAnAdminOf && !isAdmin) {
    return false;
  }

  return true;
});



  return (
    <div className="p-6 min-h-screen">
      {isLoading && (
        <div className="flex min-h-screen items-center justify-center">
          <LoadingSpinner />
        </div>
      )}
      <div className="flex flex-col md:flex-row items-stretch md:items-center justify-between mb-6 space-y-4 md:space-y-0">
        <div className="w-full md:w-1/3 flex items-center">
          <input
            id="search"
            type="text"
            placeholder="Search projects and assets..."
            className="w-full rounded-lg py-2 px-4 text-gray-700 bg-white shadow-sm border border-transparent focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition ease-in-out duration-150"
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && doSearch()}
          />
          {query.trim() !== "" && (
            <button
              onClick={handleSearch}
              className="ml-2 px-4 py-2 bg-blue-500 text-white rounded-lg transition hover:bg-blue-600 flex items-center"
            >
              {isLoading ? <LoadingSpinner className="h-5 w-5" /> : "Search"}
            </button>
          )}
        </div>
        <div className="flex items-center space-x-4">
          <label className="flex items-center cursor-pointer">
            <div className="relative">
              <input
          type="checkbox"
          checked={showOnlyProjectsIAmAnAdminOf}
          onChange={toggleShowOnlyAdminProjects}
          className="sr-only"
              />
              <div className="block bg-gray-300 w-14 h-8 rounded-full"></div>
              <div
          className={`absolute left-1 top-1 w-6 h-6 rounded-full transition ${
            showOnlyProjectsIAmAnAdminOf ? "translate-x-6 bg-blue-500" : "bg-white"
          }`}
              ></div>
            </div>
            <span className="ml-3 text-gray-700">My Admin Projects Only</span>
          </label>
        </div>
        <div>
            <label className="flex items-center cursor-pointer">
              <div className="relative">
                <input
                  type="checkbox"
                  checked={showArchived}
                  onChange={toggleShowArchived}
                  className="sr-only"
                />
                <div className="block bg-gray-300 w-14 h-8 rounded-full"></div>
                <div
                  className={`absolute left-1 top-1 w-6 h-6 rounded-full transition ${
                    showArchived ? "translate-x-6 bg-blue-500" : "bg-white"
                  }`}
                ></div>
              </div>
              <span className="ml-3 text-gray-700">
                Include Archived Projects
              </span>
            </label>
        </div>

        <div className="flex flex-col md:flex-row space-y-2 md:space-y-0 md:space-x-4">
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
                fetchTags().then(() => {
                  setNewProjectModalOpen(true);
                });
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
      {allProjects && allProjects.length < 1 && (
        <div className="flex flex-col items-center justify-center h-64">
          <p className="text-2xl text-gray-500">No projects to display!</p>
        </div>
      )}

      {allProjects && allProjects.length > 0 && (
        <>
          <div>
            <h1 className="text-2xl font-semibold mb-4">My Projects</h1>
            {myProjects.length > 0 ? (
              <div className="grid grid-cols-1 sm:grid-cols-[repeat(auto-fill,_minmax(320px,_1fr))] lg:grid-cols-[repeat(auto-fill,_minmax(320px,_420px))] gap-4">
                {displayedMyProjects.map((project) => (
                  <div key={project.projectID} className="w-full h-full">
                    <ProjectCard
                      id={String(project.projectID)}
                      name={project.name}
                      archived={project.archived}
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

          {otherProjects && otherProjects.length > 0 && (
            <div className="mt-8">
              <h1 className="text-2xl font-semibold mb-4">Other Projects</h1>
              <div className="grid grid-cols-1 sm:grid-cols-[repeat(auto-fill,_minmax(320px,_1fr))] lg:grid-cols-[repeat(auto-fill,_minmax(320px,_420px))] gap-4">
                {displayedOtherProjects.map((project) => (
                  <div key={project.projectID} className="w-full h-full">
                    <ProjectCard
                      id={String(project.projectID)}
                      name={project.name}
                      archived={project.archived}
                      creationTime={project.creationTime}
                      assetCount={project.assetCount}
                      userNames={project.userNames}
                      admins={project.admins}
                    />
                  </div>
                ))}
              </div>
            </div>
          )}
        </>
      )}

      {searchDone && (
        <div className="mt-8">
          <h1 className="text-2xl font-semibold mb-4">Searched Assets</h1>
          {currentAssets && currentAssets.length < 1 && (
            <div className="flex flex-col items-center justify-center h-64">
              <p className="text-2xl text-gray-500">No assets found!</p>
            </div>
          )}
          {currentAssets && currentAssets.length > 0 && (
            <>
              <Items
                currentItems={paginatedAssets}
                openPreview={openPreview}
                downloadAssetConfirm={downloadAssetConfirm}
                showAssetMetadata={showAssetMetadata}
              />
              <Pagination
                count={Math.ceil(currentAssets.length / 10)}
                page={currentPage}
                onChange={handlePageChange}
                shape="rounded"
                color="standard"
                className="border border-gray-300"
              />
            </>
          )}
        </div>
      )}

      {/* {!searchDone && (
        <div className="mt-8">
          <h1 className="text-2xl font-semibold mb-4">Searched Assets</h1>
          <div className="flex flex-col items-center justify-center h-64">
            <p className="text-2xl text-gray-500">
              Use the search field above to find assets!
            </p>
          </div>
        </div>
      )} */}

      {newProjectModalOpen && (
        <GenericForm
          title="Create New Project"
          fields={newProjectFormFields}
          onSubmit={handleAddProject}
          onCancel={() => setNewProjectModalOpen(false)}
          submitButtonText="Create Project"
          extraButtonText="AI Description"
          extraButtonCallback={async (currentFormData, updateField) => {
            const { name, location, tags, description } = currentFormData;
            const prompt = `Given the following project details:
            - Project Name: ${name}
            - Project Location: ${location}
            - Tags: ${Array.isArray(tags) ? tags.join(", ") : tags}
            Generate a project description aimed for Projects in a Digital Asset Management system for Field Engineers. Note that tags are metadata 
            that may be associated with assets in the project. Use tags to come up with descriptive description. Do not include any headings, titles, or extraneous text—only provide a clean description. `;

            if (typeof description === "string" && description.trim() !== "") {
              setStoredPrompt(prompt);
              setStoredUpdateField(() => updateField);
              setConfirmOverwriteDescriptionPopup(true);
            } else {
              await confirmOverwriteDescriptionWrapper(true, updateField, prompt);
            }
          }}
          showExtraHelperText={true}
          disableOutsideClose={confirmOverwriteDescriptionPopup ? true : false}
        />
      )}

      {confirmOverwriteDescriptionPopup && ( // sean
        <div onClick={(e) => e.stopPropagation()}>
          <PopupModal
            title="Are you sure you want to overwrite current description?"
            isOpen={true}
            onClose={() => setConfirmOverwriteDescriptionPopup(false)}
            onConfirm={() => confirmOverwriteDescriptionWrapper(true)}
            messages={[]}
            canCancel={false}
          />
        </div>
      )}

      {importProjectModalOpen && (
        <div className="fixed inset-0 bg-gray-500 bg-opacity-50 flex justify-center items-center">
          <div
            ref={importFormRef}
            className="bg-white p-6 rounded shadow-lg w-200 max-h-screen overflow-y-auto"
          >
            <div className="bg-white p-8 rounded shadow-md text-center w-full max-w-xl">
              <div
                {...getRootProps()}
                className="border-2 border-dashed border-gray-300 p-8 rounded-lg mb-4 cursor-pointer"
              >
                <input {...getInputProps()} />
                {isDragActive ? (
                  <p className="text-xl text-teal-600">
                    Drop the files here...
                  </p>
                ) : (
                  <>
                    <p className="text-xl mb-2">Drag and Drop here</p>
                    <p className="text-gray-500 mb-4">or</p>
                    <button className="bg-indigo-600 text-white hover:bg-indigo-700 px-4 py-2 rounded">
                      Select file
                    </button>
                    <p className="text-sm text-gray-400 mt-2">.xlsx only</p>
                  </>
                )}
              </div>
            </div>

            {importedProjectFile && (
              <div>
                <div className="py-2">
                  <p>
                    Uploaded Project: <i>{importedProjectFile.name}</i>
                  </p>
                </div>
                <div className="flex justify-end py-2">
                  <button
                    className="bg-blue-500 text-white p-2 rounded float"
                    onClick={() => onSubmitImport()}
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
              placeholder: "type and press <enter> to add new tag>"
            },
          ]}
          onSubmit={onSubmitConfigureTags}
          onCancel={() => setConfigureTagsOpen(false)}
          confirmRemoval={true}
          confirmRemovalMessage="Are you sure you want to remove this tag? Removing it will affect all projects and assets that use the tag."
          submitButtonText="Update Tags"
          disableOutsideClose={false}
          noRequired={true}
        />
      )}

      {confirmConfigurePopup && (
        <div onClick={(e) => e.stopPropagation()}>
          <PopupModal
            title="Confirm Tag Changes"
            isOpen={true}
            onClose={() => {
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

      {confirmDownloadPopup && (
        <div onClick={(e) => e.stopPropagation()}>
          <PopupModal
            title="Would you like to add a watermark to the image?"
            isOpen={true}
            onClose={() => downloadAssetWrapper(false, requestedDownloadAsset)}
            onConfirm={() => downloadAssetWrapper(true, requestedDownloadAsset)}
            messages={[]}
            canCancel={false}
          />
        </div>
      )}

      {isPreviewAsset && previewUrl && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
          <div className="relative bg-white p-4 rounded shadow-lg max-w-3xl max-h-[80vh] overflow-auto">
            <button
              onClick={closeModal}
              className="absolute top-2 right-2 text-gray-500 hover:text-gray-700"
            >
              ✕
            </button>

            {previewType?.startsWith("image/") && (
              <img
                src={previewUrl}
                alt="Full Preview"
                className="max-w-full max-h-[70vh]"
              />
            )}
            {previewType?.startsWith("video/") && (
              <video
                src={previewUrl}
                controls
                className="max-w-full max-h-[70vh]"
              />
            )}
          </div>
        </div>
      )}

      {isPreviewAssetMetadata && (
        <GenericForm
          title={"Custom Metadata: " + assetMetadataName}
          isModal={true}
          fields={assetMetadataFields}
          onSubmit={() => {}}
          onCancel={() => setIsPreviewAssetMetadata(false)}
          isEdit={false}
          noRequired={true}
          submitButtonText=""
        />
      )}
    </div>
  );
}
