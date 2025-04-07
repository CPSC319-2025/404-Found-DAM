"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { useFileContext } from "@/app/context/FileContext";
import { useState, useEffect } from "react";
import { fetchWithAuth } from "@/app/utils/api/api";

export default function EditMetadataPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const { files, updateMetadata } = useFileContext();

  const fileName = searchParams?.get("file") ?? "";

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
  const [projectTagMap, setProjectTagMap] = useState<Record<string, number>>({});
  const [isLoading, setIsLoading] = useState(false);
  const [metadataFields, setMetadataFields] = useState<Array<{
    fieldName: string;
    fieldID: number;
    isEnabled: boolean;
    fieldType: string;
  }>>([]);
  const [metadataValues, setMetadataValues] = useState<Record<number, any>>({});

  // Fetch project tags and blob details when component mounts
  useEffect(() => {
    if (fileData) {
      if (fileData.project) {
        fetchProjectTags(fileData.project);
      }
      
      // If we have a blobId, fetch its details directly
      if (fileData.blobId) {
        fetchBlobDetails(fileData.blobId);
        fetchBlobFields(fileData.blobId);
      }
    }
  }, [fileData]);

  async function fetchBlobDetails(blobId: string) {
    setIsLoading(true);
    try {
      const token = localStorage.getItem("token");
      
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/blob/${blobId}/details`, {
        headers: {
          Authorization: token ? `Bearer ${token}` : "",
        }
      });
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

      // Store metadata values if they exist
      if (data.metadata && typeof data.metadata === 'object') {
        setMetadataValues(data.metadata);
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
      const token = localStorage.getItem("token");
      
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/projects/${projectId}`, {
        headers: {
          Authorization: token ? `Bearer ${token}` : "",
        }
      });
      if (!response.ok) {
        throw new Error(`Failed to fetch project data: ${response.status}`);
      }
      const data = await response.json();
      
      // Extract tags from project data
      if (data.suggestedTags && Array.isArray(data.suggestedTags)) {
        // Create a map of tag names to tag IDs for quick lookup
        const tagMap: Record<string, number> = {};
        const tagNames: string[] = [];
        
        data.suggestedTags.forEach((tag: { name: string; tagID: number }) => {
          tagNames.push(tag.name);
          tagMap[tag.name] = tag.tagID;
        });
        
        setProjectTags(tagNames);
        setProjectTagMap(tagMap);
      }

      // Extract metadata fields
      if (data.metadataFields && Array.isArray(data.metadataFields)) {
        setMetadataFields(data.metadataFields);
      }
    } catch (error) {
      console.error("Error fetching project data:", error);
    } finally {
      setIsLoading(false);
    }
  }

  async function fetchBlobFields(blobId: string) {
    setIsLoading(true);
    try {
      const token = localStorage.getItem("token");
      
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/blob/${blobId}/fields`, {
        headers: {
          Authorization: token ? `Bearer ${token}` : "",
        }
      });
      
      if (!response.ok) {
        throw new Error(`Failed to fetch blob fields: ${response.status}`);
      }
      
      const data = await response.json();
      
      // Convert array of fields to a record object by field ID
      if (data.fields && Array.isArray(data.fields)) {
        const fieldValues: Record<number, any> = {};
        
        data.fields.forEach((field: { fieldId: number; fieldValue: string; fieldType: string }) => {
          // Convert field value based on its type
          let typedValue: any = field.fieldValue;
          
          if (field.fieldType === "Number") {
            typedValue = parseFloat(field.fieldValue);
          } else if (field.fieldType === "Boolean") {
            typedValue = field.fieldValue.toLowerCase() === "true";
          }
          
          fieldValues[field.fieldId] = typedValue;
        });
        
        setMetadataValues(fieldValues);
      }
    } catch (error) {
      console.error("Error fetching blob fields:", error);
    } finally {
      setIsLoading(false);
    }
  }

  async function handleTagSelection(tagName: string) {
    // Add the tag if it's not already included
    if (!selectedTags.includes(tagName) && fileData && fileData.blobId) {
      setIsLoading(true);
      
      try {
        // First, find the tag ID from the project tags
        const tagId = findTagIdByName(tagName);
        
        if (!tagId) {
          console.error(`Tag ID not found for tag: ${tagName}`);
          setIsLoading(false);
          return;
        }
        
        // Call API to assign the tag to the asset
        const token = localStorage.getItem("token");
        
        const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/asset/tag`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: token ? `Bearer ${token}` : "",
          },
          body: JSON.stringify({
            BlobId: fileData.blobId,
            TagId: tagId
          })
        })
        
        if (!response.ok) {
          throw new Error(`Failed to assign tag: ${response.status}`);
        }
        
        const data = await response.json();
        console.log("Tag assigned successfully:", data);
        
        // Update local state with the new tag
        setSelectedTags([...selectedTags, tagName]);
        
        // Also update the file metadata in context
        const updatedTags = [...fileData.tags, tagName];
        const updatedTagIds = [...fileData.tagIds, tagId];
        
        updateMetadata(fileIndex, {
          tags: updatedTags,
          tagIds: updatedTagIds
        });
      } catch (error) {
        console.error("Error assigning tag:", error);
      } finally {
        setIsLoading(false);
      }
    }
  }
  
  // Helper function to find a tag ID by name
  function findTagIdByName(tagName: string): number | null {
    // First check if we already have this tag in our file's tag list
    const existingTagIndex = fileData.tags.findIndex(tag => tag === tagName);
    if (existingTagIndex !== -1) {
      return fileData.tagIds[existingTagIndex];
    }
    
    // Check in our project tag map
    return projectTagMap[tagName] || null;
  }

  function handleTagRemoval(tagToRemove: string) {
    if (!fileData || !fileData.blobId) {
      console.warn("File missing blobId");
      return;
    }
    
    // Find the tag ID corresponding to the tag name
    const tagIndex = fileData.tags.indexOf(tagToRemove);
    if (tagIndex === -1) {
      console.warn(`Tag "${tagToRemove}" not found in file metadata`);
      return;
    }
    
    const tagIdToRemove = fileData.tagIds[tagIndex];
    
    // Call API to delete the tag
    async function deleteTag() {
      setIsLoading(true);
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
              BlobIds: [fileData.blobId],
              TagIds: [tagIdToRemove]
            })
          }
        );

        if (!response.ok) {
          console.error("Failed to delete tag:", response.status);
          return;
        }
        
        console.log(`Tag "${tagToRemove}" removed successfully`);
        
        // Update local state to remove the tag
        setSelectedTags(selectedTags.filter(tag => tag !== tagToRemove));
        
        // Also update the file metadata in the context
        const updatedTags = [...fileData.tags];
        const updatedTagIds = [...fileData.tagIds];
        updatedTags.splice(tagIndex, 1);
        updatedTagIds.splice(tagIndex, 1);
        
        updateMetadata(fileIndex, {
          tags: updatedTags,
          tagIds: updatedTagIds
        });
      } catch (err) {
        console.error("Error deleting tag:", err);
      } finally {
        setIsLoading(false);
      }
    }
    
    deleteTag();
  }

  if (!fileData) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-red-500 text-lg">File not found in context.</p>
      </div>
    );
  }

  function handleSave() {
    // First update context with new metadata
    updateMetadata(fileIndex, {
      description,
      location,
      tags: selectedTags,
      metadata: metadataValues,
    });

    // Only proceed with API call if we have a project and blobId
    if (fileData.project && fileData.blobId) {
      saveMetadataToAPI();
    } else {
      // If we don't have a project or blobId, just navigate back
      router.push("/palette");
    }
  }

  async function saveMetadataToAPI() {
    setIsLoading(true);
    try {
      const token = localStorage.getItem("token");
      
      // Convert metadata object to array format expected by API
      const metadataEntries = Object.entries(metadataValues).map(([fieldIdStr, value]) => {
        const fieldId = parseInt(fieldIdStr);
        return {
          FieldId: fieldId,
          FieldValue: value
        };
      });
      
      // Filter out empty entries
      const filteredMetadataEntries = metadataEntries.filter(entry => 
        entry.FieldValue !== undefined && 
        entry.FieldValue !== null && 
        entry.FieldValue !== ""
      );
      
      // Ensure projectID is properly parsed as integer
      const projectIdInt = parseInt(fileData.project!);
      
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL}/projects/${projectIdInt}/associate-assets`,
        {
          method: "PATCH",
          headers: {
            Authorization: token ? `Bearer ${token}` : "",
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            ProjectID: projectIdInt,
            BlobIDs: [fileData.blobId],
            TagIDs: [],
            MetadataEntries: filteredMetadataEntries
          }),
        }
      );
      
      if (!response.ok) {
        const errorData = await response.text();
        console.error("Server error response:", errorData);
        throw new Error(`Failed to save metadata: ${response.status}`);
      }
      
      console.log("Metadata saved successfully");
      
      // Navigate back to palette page
      router.push("/palette");
    } catch (error) {
      console.error("Error saving metadata:", error);
      alert("Failed to save metadata. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  function handleEditImage() {
    // Navigate to a new page under /palette/ for image editing
    if (!fileName) {
      console.error("File name is missing!");
      return;
    }
    router.push(`/palette/editImage?file=${encodeURIComponent(fileName)}`);
  }

  // Function to check if file is a video based on extension
  function isVideoFile(filename: string): boolean {
    const videoExtensions = ['.mp4', '.mov', '.avi', '.wmv', '.mkv', '.webm', '.flv', '.mpeg', '.mpg', '.m4v'];
    const extension = filename.substring(filename.lastIndexOf('.')).toLowerCase();
    return videoExtensions.includes(extension);
  }

  // Check if current file is a video
  const isVideo = fileData.file.name ? isVideoFile(fileData.file.name) : false;

  // Get file type icon and label
  const getFileTypeInfo = () => {
    if (isVideo) {
      return {
        icon: "🎬",
        label: "Video File"
      };
    }
    return {
      icon: "🖼️",
      label: "Image File"
    };
  };

  const fileTypeInfo = getFileTypeInfo();

  return (
    <div className="min-h-screen bg-gradient-to-b  py-10 px-4">
      <div className="w-full max-w-2xl mx-auto bg-white shadow-2xl rounded-xl overflow-hidden">
        {/* Header section with better styling */}
        <div className="bg-gradient-to-r from-blue-600 to-teal-500 px-6 py-6 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-white">
            {isVideo ? "Video" : "Image"} Metadata Editor
          </h1>
          <div className="flex items-center text-white bg-white/20 px-4 py-2 rounded-full">
            <span className="text-xl mr-2">{fileTypeInfo.icon}</span>
            <span className="text-sm font-medium">{fileTypeInfo.label}</span>
          </div>
        </div>
        
        {/* Main content */}
        <div className="p-6">
          {/* File name card */}
          <div className="mb-6 bg-gray-50 rounded-lg p-4 border border-gray-200">
            <p className="text-sm font-medium text-gray-500 mb-1">
              FILE NAME
            </p>
            <div className="flex items-center">
              <span className="text-xl mr-2">{fileTypeInfo.icon}</span>
              <p className="text-lg font-medium text-gray-800 truncate">
                {fileData.file.name}
              </p>
            </div>
          </div>

          {/* Form fields */}
          <div className="space-y-6">
            <div className="bg-white border border-gray-200 rounded-lg p-4 transition-all hover:shadow-md">
              <label className="block text-sm font-medium text-gray-500 mb-2">Description</label>
              <input
                type="text"
                className="w-full border border-gray-300 rounded-lg p-3 focus:outline-none bg-gray-100"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                readOnly={true}
              />
            </div>

            <div className="bg-white border border-gray-200 rounded-lg p-4 transition-all hover:shadow-md">
              <label className="block text-sm font-medium text-gray-500 mb-2">Location</label>
              <input
                type="text"
                className="w-full border border-gray-300 rounded-lg p-3 focus:outline-none bg-gray-100"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
                readOnly={true}
              />
            </div>

            <div className="bg-white border border-gray-200 rounded-lg p-4 transition-all hover:shadow-md">
              <label className="block text-sm font-medium text-gray-500 mb-2">Selected Tags</label>
              <div className="min-h-[50px] w-full border border-gray-300 rounded-lg p-3 flex flex-wrap gap-2 bg-white">
                {selectedTags.length > 0 ? (
                  selectedTags.map((tag, index) => (
                    <span
                      key={index}
                      className="inline-flex items-center px-3 py-1.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 border border-blue-200"
                    >
                      {tag}
                      <button
                        onClick={() => handleTagRemoval(tag)}
                        className="ml-1.5 text-blue-700 hover:text-red-600 transition-colors"
                      >
                        ×
                      </button>
                    </span>
                  ))
                ) : (
                  <span className="text-gray-400 text-sm self-center">No tags selected</span>
                )}
              </div>
            </div>

            {fileData.project && (
              <div className="bg-white border border-gray-200 rounded-lg p-4 transition-all hover:shadow-md">
                <label className="block text-sm font-medium text-gray-500 mb-2">Project Tags</label>
                {isLoading ? (
                  <div className="flex justify-center py-4">
                    <div className="animate-pulse flex space-x-2">
                      <div className="rounded-full bg-slate-200 h-3 w-3"></div>
                      <div className="rounded-full bg-slate-200 h-3 w-3"></div>
                      <div className="rounded-full bg-slate-200 h-3 w-3"></div>
                    </div>
                  </div>
                ) : projectTags.length > 0 ? (
                  <div className="flex flex-wrap gap-2 mt-2">
                    {projectTags.map((tag, index) => (
                      <button
                        key={index}
                        onClick={() => handleTagSelection(tag)}
                        className={`px-3 py-1.5 rounded-full text-sm transition-all ${
                          selectedTags.includes(tag)
                            ? "bg-blue-100 text-blue-800 cursor-default border border-blue-200"
                            : "bg-gray-100 hover:bg-gray-200 text-gray-800 hover:shadow-sm border border-gray-200"
                        }`}
                        disabled={selectedTags.includes(tag)}
                      >
                        {tag}
                      </button>
                    ))}
                  </div>
                ) : (
                  <p className="text-sm text-gray-500 py-2">No project tags available</p>
                )}
              </div>
            )}

            {/* Metadata Fields Section */}
            {fileData.project && metadataFields.length > 0 && (
              <div className="bg-white border border-gray-200 rounded-lg p-4 transition-all hover:shadow-md">
                <h2 className="text-lg font-medium text-gray-700 mb-4">Project Metadata Fields</h2>
                <div className="space-y-4">
                  {metadataFields
                    .filter(field => field.isEnabled)
                    .map((field) => (
                    <div key={field.fieldID} className="bg-gray-50 p-3 rounded-md">
                      <label className="block text-sm font-medium text-gray-500 mb-2">
                        {field.fieldName} <span className="text-xs text-gray-400">({field.fieldType})</span>
                      </label>
                      {field.fieldType === "Boolean" ? (
                        <div className="flex items-center space-x-4">
                          <div 
                            className={`px-4 py-2 rounded-md cursor-pointer border transition-colors ${
                              metadataValues[field.fieldID] === true 
                                ? "bg-blue-100 border-blue-500 text-blue-700 font-medium" 
                                : "bg-gray-100 border-gray-300 text-gray-700 hover:bg-gray-200"
                            }`}
                            onClick={() => {
                              setMetadataValues({
                                ...metadataValues,
                                [field.fieldID]: true
                              });
                            }}
                          >
                            Yes
                          </div>
                          <div 
                            className={`px-4 py-2 rounded-md cursor-pointer border transition-colors ${
                              metadataValues[field.fieldID] === false 
                                ? "bg-blue-100 border-blue-500 text-blue-700 font-medium" 
                                : "bg-gray-100 border-gray-300 text-gray-700 hover:bg-gray-200"
                            }`}
                            onClick={() => {
                              setMetadataValues({
                                ...metadataValues,
                                [field.fieldID]: false
                              });
                            }}
                          >
                            No
                          </div>
                        </div>
                      ) : (
                        <input
                          type={field.fieldType === "Number" ? "number" : "text"}
                          className="w-full border border-gray-300 rounded-lg p-3 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                          value={metadataValues[field.fieldID] || ""}
                          onChange={(e) => {
                            const value = field.fieldType === "Number" 
                              ? parseFloat(e.target.value) 
                              : e.target.value;
                            setMetadataValues({
                              ...metadataValues,
                              [field.fieldID]: value
                            });
                          }}
                        />
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Action buttons with improved styling */}
            <div className="flex justify-center space-x-4 mt-8">
              <button
                onClick={handleSave}
                disabled={isLoading}
                className="bg-gradient-to-r from-teal-500 to-teal-600 hover:from-teal-600 hover:to-teal-700 text-white font-medium py-3 px-8 rounded-lg transition-all duration-200 disabled:opacity-50 shadow-md hover:shadow-lg"
              >
                {isLoading ? (
                  <span className="flex items-center">
                    <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Saving...
                  </span>
                ) : "Save Changes"}
              </button>
              <button
                onClick={handleEditImage}
                disabled={isLoading || isVideo}
                className={`${
                  isVideo 
                    ? "bg-gray-300 cursor-not-allowed" 
                    : "bg-gradient-to-r from-blue-500 to-blue-600 hover:from-blue-600 hover:to-blue-700"
                } text-white font-medium py-3 px-8 rounded-lg transition-all duration-200 disabled:opacity-50 shadow-md hover:shadow-lg flex items-center`}
                title={isVideo ? "Video files cannot be edited" : "Edit Image"}
              >
                {isVideo ? (
                  <>
                    <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    Not Editable
                  </>
                ) : (
                  <>
                    <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                    </svg>
                    Edit Image
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
