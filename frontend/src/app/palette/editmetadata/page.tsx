"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { useFileContext } from "@/app/context/FileContext";
import { useState, useEffect } from "react";

export default function EditMetadataPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const { files, updateMetadata } = useFileContext();

  const fileName = searchParams.get("file");

  // Find file by comparing file.name
  const fileIndex = files.findIndex((f) => f.file.name === fileName);
  const fileData = files[fileIndex];

  // Always call hooks at the top level, even if fileData is undefined.
  const [description, setDescription] = useState(
    fileData ? fileData.description || "" : ""
  );
  const [location, setLocation] = useState(
    fileData ? fileData.location || "" : ""
  );
  // Change to array of tags instead of comma-separated string
  const [selectedTags, setSelectedTags] = useState<string[]>(
    fileData ? fileData.tags : []
  );
  const [projectTags, setProjectTags] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  // Fetch project tags and blob details when component mounts
  useEffect(() => {
    if (fileData) {
      if (fileData.project) {
        fetchProjectTags(fileData.project);
      }
      
      // If we have a blobId, fetch its details directly
      if (fileData.blobId) {
        fetchBlobDetails(fileData.blobId);
      }
    }
  }, [fileData]);

  async function fetchBlobDetails(blobId: number) {
    setIsLoading(true);
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/blob/${blobId}/details`);
      if (!response.ok) {
        throw new Error(`Failed to fetch blob details: ${response.status}`);
      }
      
      const data = await response.json();
      
      // Update local state with the fetched data
      if (data.tags && Array.isArray(data.tags)) {
        setSelectedTags(data.tags);
      }
      
      // If we have project data but no project was selected yet, select it
      if (data.project && !fileData.project) {
        // Update in context
        updateMetadata(fileIndex, {
          project: data.project.projectId.toString()
        });
        
        // Also fetch tags for this project
        fetchProjectTags(data.project.projectId.toString());
      }
    } catch (error) {
      console.error("Error fetching blob details:", error);
    } finally {
      setIsLoading(false);
    }
  }

  async function fetchProjectTags(projectId: string) {
    setIsLoading(true);
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/projects/${projectId}`);
      if (!response.ok) {
        throw new Error(`Failed to fetch project data: ${response.status}`);
      }
      const data = await response.json();
      
      // Extract tags from project data
      if (data.tags && Array.isArray(data.tags)) {
        setProjectTags(data.tags.map((tag: { name: string; tagID: number }) => tag.name));
      }
    } catch (error) {
      console.error("Error fetching project tags:", error);
    } finally {
      setIsLoading(false);
    }
  }

  function handleTagSelection(tagName: string) {
    // Add the tag if it's not already included
    if (!selectedTags.includes(tagName)) {
      setSelectedTags([...selectedTags, tagName]);
    }
  }

  function handleTagRemoval(tagToRemove: string) {
    setSelectedTags(selectedTags.filter(tag => tag !== tagToRemove));
  }

  if (!fileData) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-red-500 text-lg">File not found in context.</p>
      </div>
    );
  }

  function handleSave() {
    // Update context with new metadata
    updateMetadata(fileIndex, {
      description,
      location,
      tags: selectedTags,
    });

    // Navigate back to palette page
    router.push("/palette");
  }

  function handleEditImage() {
    // Navigate to a new page under /palette/ for image editing
    if (!fileName) {
      console.error("File name is missing!");
      return;
    }
    router.push(`/palette/editImage?file=${encodeURIComponent(fileName)}`);
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-4">
      <div className="w-full max-w-lg bg-white shadow-xl rounded-lg p-6">
        <h1 className="text-3xl font-bold text-gray-800 mb-6 text-center">
          Edit Image
        </h1>

        <div className="mb-6">
          <p className="text-lg font-medium text-gray-700 mb-2">
            Editing file:
          </p>
          <div className="bg-gray-100 p-3 rounded">
            <p className="text-gray-800 font-semibold">{fileData.file.name}</p>
          </div>
        </div>

        <div className="space-y-4">
          <div>
            <label className="block text-gray-700 mb-1">Description:</label>
            <input
              type="text"
              className="w-full border border-gray-300 rounded-lg p-2 focus:outline-none focus:ring-2 focus:ring-teal-400"
              placeholder="Enter description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>

          <div>
            <label className="block text-gray-700 mb-1">Location:</label>
            <input
              type="text"
              className="w-full border border-gray-300 rounded-lg p-2 focus:outline-none focus:ring-2 focus:ring-teal-400"
              placeholder="Where was this created?"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
            />
          </div>

          <div>
            <label className="block text-gray-700 mb-1">Selected Tags:</label>
            <div className="min-h-[38px] w-full border border-gray-300 rounded-lg p-2 flex flex-wrap gap-2">
              {selectedTags.length > 0 ? (
                selectedTags.map((tag, index) => (
                  <span
                    key={index}
                    className="inline-flex items-center px-2 py-1 rounded-full text-xs font-semibold bg-blue-100 text-blue-800"
                  >
                    {tag}
                    <button
                      onClick={() => handleTagRemoval(tag)}
                      className="ml-1 text-red-500 hover:text-red-700"
                    >
                      ×
                    </button>
                  </span>
                ))
              ) : (
                <span className="text-gray-400 text-xs self-center">No tags selected</span>
              )}
            </div>
          </div>

          {fileData.project && (
            <div>
              <label className="block text-gray-700 mb-1">Project Tags:</label>
              {isLoading ? (
                <p className="text-sm text-gray-500">Loading project tags...</p>
              ) : projectTags.length > 0 ? (
                <div className="flex flex-wrap gap-2 mt-2">
                  {projectTags.map((tag, index) => (
                    <button
                      key={index}
                      onClick={() => handleTagSelection(tag)}
                      className={`px-3 py-1 rounded-full text-sm transition-colors ${
                        selectedTags.includes(tag)
                          ? "bg-blue-100 text-blue-800 cursor-default"
                          : "bg-gray-100 hover:bg-gray-200 text-gray-800"
                      }`}
                      disabled={selectedTags.includes(tag)}
                    >
                      {tag}
                    </button>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-gray-500">No project tags available</p>
              )}
            </div>
          )}

          <div className="mt-4">
            <p className="text-gray-700 font-medium">File Size:</p>
            <p className="text-gray-800">{fileData.fileSize}</p>
          </div>
        </div>

        <div className="mt-8 flex justify-center space-x-4">
          <button
            onClick={handleSave}
            className="bg-teal-500 hover:bg-teal-600 text-white font-semibold py-2 px-6 rounded-lg transition-colors duration-200"
          >
            Save Changes
          </button>
          <button
            onClick={handleEditImage}
            className="bg-blue-500 hover:bg-blue-600 text-white font-semibold py-2 px-6 rounded-lg transition-colors duration-200"
          >
            Edit Image
          </button>
        </div>
      </div>
    </div>
  );
}
