"use client";

import React, { useState, ChangeEvent } from "react";
import { useRouter } from "next/navigation";
import { PencilIcon, TrashIcon } from "@heroicons/react/24/outline";
import { useFileContext, FileMetadata } from "@/app/context/FileContext";

interface Project {
  projectID: number;
  projectName: string;
  location: string;
  description: string;
  creationTime: string;
  assetCount: number;
  adminNames: string[];
  regularUserNames: string[];
}

type FileTableProps = {
  files: FileMetadata[];
  removeFile: (index: number) => void;

  // Row selection from parent
  selectedIndices: number[];
  setSelectedIndices: React.Dispatch<React.SetStateAction<number[]>>;
  projects: Project[];
};

export default function FileTable({
  files,
  removeFile,
  selectedIndices,
  setSelectedIndices,
  projects,
}: FileTableProps) {
  const router = useRouter();
  const { setFiles } = useFileContext();

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [previewType, setPreviewType] = useState<string | null>(null);

  function handleSelectAll(e: ChangeEvent<HTMLInputElement>) {
    if (e.target.checked) {
      setSelectedIndices(files.map((_, idx) => idx));
    } else {
      setSelectedIndices([]);
    }
  }
  function handleSelectRow(index: number) {
    setSelectedIndices((prev) =>
      prev.includes(index) ? prev.filter((i) => i !== index) : [...prev, index]
    );
  }

  function handleRemoveTag(fileIndex: number, tagIndex: number) {
    const fileMeta = files[fileIndex];
    const tagToRemove = fileMeta.tags[tagIndex];
    const tagIdToRemove = fileMeta.tagIds[tagIndex];
    
    if (!fileMeta.blobId) {
      console.warn("File missing blobId:", fileMeta.file.name);
      return;
    }

    // Call API to delete the tag
    async function deleteTag() {
      try {
        console.log(`Deleting tag "${tagToRemove}" with ID ${tagIdToRemove}`);
        
        const token = localStorage.getItem("token");
        
        const response = await fetch(
          `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets/tags`,
          {
            method: "PATCH",
            headers: {
              "Content-Type": "application/json",
              Authorization: token ? `Bearer ${token}` : "",
            },
            body: JSON.stringify({
              BlobIds: [fileMeta.blobId],
              TagIds: [tagIdToRemove]
            })
          }
        );

        if (!response.ok) {
          console.error("Failed to delete tag:", response.status);
          return;
        }

        // remove that tagid and corresponding tag from the fileMeta
        if (response.ok) {
          setFiles((prev) => {
            const updated = [...prev];
            if (updated[fileIndex]) {
              // Find the current index of the tagId we're removing
              // This is important because UI might have changed since API call was made
              const currentTagIdIndex = updated[fileIndex].tagIds.indexOf(tagIdToRemove);
              
              if (currentTagIdIndex !== -1) {
                const updatedTags = [...updated[fileIndex].tags];
                const updatedTagIds = [...updated[fileIndex].tagIds];
                
                // Remove the tag and tagId at the current index
                updatedTags.splice(currentTagIdIndex, 1);
                updatedTagIds.splice(currentTagIdIndex, 1);
                
                updated[fileIndex] = {
                  ...updated[fileIndex],
                  tags: updatedTags,
                  tagIds: updatedTagIds
                };
              }
            }
            return updated;
          });
          
          console.log(`Tag "${tagToRemove}" removed successfully`);
        }
        
      } catch (err) {
        console.error("Error deleting tag:", err);
      }
    }

    deleteTag();
  }

  // ----- Project Dropdown -----
  async function handleProjectChange(index: number, newProjectID: string) {
    if (!newProjectID) return; // Don't do anything if no project selected
    
    const fileMeta = files[index];
    if (!fileMeta.blobId) {
      console.warn("File missing blobId:", fileMeta.file.name);
      return;
    }

    // Clear existing tags first
    setFiles((prev) => {
      const updated = [...prev];
      updated[index] = {
        ...updated[index],
        tags: [] // Clear tags
      };
      return updated;
    });
    
    // Call the API to delete all tags from the backend if there are any
    if (fileMeta.tags.length > 0 && fileMeta.blobId) {
      try {
        console.log(fileMeta.tags);
        const token = localStorage.getItem("token");
        
        const deleteTagsResponse = fetch(
          `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets/tags`,
          {
            method: "PATCH",
            headers: {
              "Content-Type": "application/json",
              Authorization: token ? `Bearer ${token}` : "",
            },
            body: JSON.stringify({
              BlobIds: [fileMeta.blobId],
              TagIds: fileMeta.tagIds
            })
          }
        );
        console.log("Cleared all existing tags");
      } catch (err) {
        console.error("Error clearing tags:", err);
      }
    }
    
    // Update the UI state first for responsiveness
    setFiles((prev) => {
      const updated = [...prev];
      updated[index] = {
        ...updated[index],
        project: newProjectID,
      };
      return updated;
    });
    
    // Get any existing tags from the blob
    
    
    // Call API to update tags for the image
    try {
      const token = localStorage.getItem("token");
      
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/asset/project-tags`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: token ? `Bearer ${token}` : "",
          },
          body: JSON.stringify({
            blobId: fileMeta.blobId,
            projectId: newProjectID
          })
        }
      );

      if (!response.ok) {
        console.error("Failed to update image tags:", response.status);
      }
    } catch (err) {
      console.error("Error updating image tags:", err); 
    }

    // Call the API to associate the asset with the project
    try {
      const token = localStorage.getItem("token");
      
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL}/projects/${newProjectID}/associate-assets`,
        {
          method: "PATCH",
          headers: {
            Authorization: token ? `Bearer ${token}` : "",
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            projectID: parseInt(newProjectID),
            blobIDs: [fileMeta.blobId],
            tagIDs: [],
            metadataEntries: []
          }),
        }
      );
      
      if (!response.ok) {
        console.error("Associate asset failed:", response.status);
        return;
      }
      
      const data = await response.json();
      console.log("Association success:", data);
      
      // Remove the file from the table if successfully associated
      if (data.successfulSubmissions?.includes(fileMeta.blobId)) {
        removeFile(index);
      }
    } catch (err) {
      console.error("Error associating asset with project:", err);
    }

    // Call the API to get asset details from the blobId
    try {
      const token = localStorage.getItem("token");
      
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/blob/${fileMeta.blobId}/details`,
        {
          headers: {
            Authorization: token ? `Bearer ${token}` : "",
          }
        }
      );
      
      if (response.ok) {
        const data = await response.json();
        // If the blob has tags, keep them
        // console.log(data.tagIds);
        if (data.tags && data.tags.length > 0) {
          setFiles((prev) => {
            const updated = [...prev];
            updated[index] = {
              ...updated[index],
              tags: data.tags,
              tagIds: data.tagIds,
              description: projects.find(p => p.projectID.toString() === newProjectID)?.description || "",
              location: projects.find(p => p.projectID.toString() === newProjectID)?.location || "",
            };
            return updated;
          });
        }
      }
    } catch (err) {
      console.error("Error fetching blob details:", err);
    }
  }

  // ----- Edit Metadata -----
  function handleEditMetadata(index: number) {
    const fileMeta = files[index];
    
    // Check if project is selected
    if (!fileMeta.project) {
      alert("Please select a project before editing metadata.");
      return;
    }
    
    // Navigate to /palette/editmetadata?file=<filename>
    router.push(
      `/palette/editmetadata?file=${encodeURIComponent(fileMeta.file.name)}`
    );
  }

  // ----- Modal Preview Logic -----
  function openPreview(url: string, fileType: string) {
    setPreviewUrl(url);
    setPreviewType(fileType);
    setIsModalOpen(true);
  }
  function closeModal() {
    setIsModalOpen(false);
    setPreviewUrl(null);
    setPreviewType(null);
  }

  return (
    <div className="overflow-auto">
      <table className="min-w-full bg-white border border-gray-200">
        <thead className="bg-gray-50">
          <tr>
            <th className="px-6 py-3 text-center">
              <div className="flex items-center justify-center">
                <input
                  type="checkbox"
                  checked={
                    selectedIndices.length === files.length && files.length > 0
                  }
                  onChange={handleSelectAll}
                />
                <span className="ml-2 text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Select All
                </span>
              </div>
            </th>
            {/* Preview */}
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Preview
            </th>
            {/* File Name */}
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              File Name
            </th>
            {/* File Type */}
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              File Type
            </th>
            {/* File Size */}
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              File Size
            </th>
            {/* Project dropdown */}
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Project
            </th>
            {/* Tags */}
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Tags
            </th>
            {/* Edit */}
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Edit
            </th>
            {/* Remove */}
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Remove
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {files.map((fileMeta, index) => {
            const rawFile = fileMeta.file;
            const displayName = rawFile.name;

            // detect if image or video
            const isImage = rawFile.type.startsWith("image/");
            const isVideo = rawFile.type.startsWith("video/");

            // create object URL for preview
            let previewUrlObj: string | null = null;
            try {
              previewUrlObj = URL.createObjectURL(rawFile);
            } catch (error) {
              console.error("Failed to create object URL:", error);
            }

            return (
              <tr
                key={index}
                className="hover:bg-gray-50 cursor-pointer"
                onClick={() => handleSelectRow(index)}
              >
                {/* Checkbox */}
                <td className="px-6 py-4 text-center">
                  <input
                    type="checkbox"
                    checked={selectedIndices.includes(index)}
                    onChange={(e) => e.stopPropagation()}
                    onClick={(e) => {
                      e.stopPropagation();
                      handleSelectRow(index);
                    }}
                  />
                </td>

                {/* Preview cell */}
                <td className="px-6 py-4">
                  {isImage && previewUrlObj && (
                    <div
                      className="h-20 w-20 relative"
                      onClick={(e) => {
                        e.stopPropagation();
                        openPreview(previewUrlObj!, rawFile.type);
                      }}
                    >
                      <img
                        src={previewUrlObj}
                        alt="Preview"
                        className="object-cover rounded w-full h-full"
                      />
                    </div>
                  )}
                  {isVideo && previewUrlObj && (
                    <div
                      className="h-20 w-20 relative"
                      onClick={(e) => {
                        e.stopPropagation();
                        openPreview(previewUrlObj!, rawFile.type);
                      }}
                    >
                      <video
                        src={previewUrlObj}
                        className="object-cover rounded w-full h-full"
                      />
                    </div>
                  )}
                </td>

                {/* File name */}
                <td className="px-6 py-4">{displayName}</td>

                {/* File Type */}
                <td className="px-6 py-4">{rawFile.type}</td>

                {/* File Size */}
                <td className="px-6 py-4">{fileMeta.fileSize}</td>

                {/* Project dropdown */}
                <td className="px-6 py-4">
                  <select
                    className="border border-gray-300 rounded p-1"
                    value={fileMeta.project || ""}
                    onClick={(e) => e.stopPropagation()}
                    onChange={(e) => handleProjectChange(index, e.target.value)}
                  >
                    <option value="">Select project...</option>
                    {projects.map((p) => (
                      <option key={p.projectID} value={p.projectID.toString()}>
                        {p.projectName}
                      </option>
                    ))}
                  </select>
                </td>

                {/* Tags */}
                <td className="px-6 py-4">
                  {fileMeta.tags.length > 0 ? (
                    fileMeta.tags.map((tag, tagIndex) => (
                      <span
                        key={tagIndex}
                        className="inline-flex items-center px-2 py-1 mr-1 rounded-full text-xs font-semibold bg-blue-100 text-blue-800"
                      >
                        {tag}
                        <button
                          onClick={(evt) => {
                            evt.stopPropagation();
                            handleRemoveTag(index, tagIndex);
                          }}
                          className="ml-1 text-red-500 hover:text-red-700"
                        >
                          ×
                        </button>
                      </span>
                    ))
                  ) : (
                    <span className="text-gray-400 text-xs">No tags</span>
                  )}
                </td>

                {/* Edit button */}
                <td className="px-6 py-4">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      handleEditMetadata(index);
                    }}
                    className="text-indigo-600 hover:text-indigo-900"
                  >
                    <PencilIcon className="h-5 w-5" />
                  </button>
                </td>

                {/* Remove button */}
                <td className="px-6 py-4">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      removeFile(index);
                    }}
                    className="text-red-600 hover:text-red-900"
                  >
                    <TrashIcon className="h-5 w-5" />
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>

      {/* Modal for full preview */}
      {isModalOpen && previewUrl && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
          <div className="relative bg-white p-4 rounded shadow-lg max-w-3xl max-h-[80vh] overflow-auto">
            {/* Close Button */}
            <button
              onClick={closeModal}
              className="absolute top-2 right-2 text-gray-500 hover:text-gray-700"
            >
              ✕
            </button>

            {/* Image or Video Preview */}
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
    </div>
  );
}
