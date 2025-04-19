"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { useFileContext } from "@/app/context/FileContext";
import { useState, useEffect } from "react";
import { fetchWithAuth } from "@/app/utils/api/api";
import { toast } from "react-toastify";
import { loadFileContent } from "../Apis";

export default function EditMetadataPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const { files, updateMetadata } = useFileContext();

  const fileName = searchParams?.get("file") ?? "";

  const fileIndex = files.findIndex((f) => f.file.name === fileName);
  const fileData = files[fileIndex];

  const [description, setDescription] = useState(
    fileData ? fileData.description || "" : ""
  );
  const [location, setLocation] = useState(
    fileData ? fileData.location || "" : ""
  );
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
  const [result, setResult] = useState<string[]>([]); // sean - suggest tags using AI

  useEffect(() => {
    if (fileData) {
      // Only fetch project tags if we don't already have them
      if (fileData.project && projectTags.length === 0) {
        fetchProjectTags(fileData.project);
      }
      
      // If we have a blobId and no metadata values, fetch its details directly
      if (fileData.blobId && Object.keys(metadataValues).length === 0) {
        fetchBlobDetails(fileData.blobId);
        // Only fetch fields once on component mount
        if (!isLoading) {
          fetchBlobFields(fileData.blobId);
        }
      }
    }
  }, [fileData, projectTags.length]); // Remove metadataValues from dependencies

  async function fetchBlobDetails(blobId: string) {
    // Only fetch if we don't already have the data
    if (selectedTags.length > 0) {
      return;
    }
    
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
      
      if (data.tags && Array.isArray(data.tags)) {
        setSelectedTags(data.tags);
      }
      
      if (data.project && !fileData.project) {
        updateMetadata(fileIndex, {
          project: data.project.projectId.toString()
        });
        
        fetchProjectTags(data.project.projectId.toString());
      }

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
      
      if (data.suggestedTags && Array.isArray(data.suggestedTags)) {
        const tagMap: Record<string, number> = {};
        const tagNames: string[] = [];
        
        data.suggestedTags.forEach((tag: { name: string; tagID: number }) => {
          tagNames.push(tag.name);
          tagMap[tag.name] = tag.tagID;
        });
        
        setProjectTags(tagNames);
        setProjectTagMap(tagMap);
      }

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
      
      if (data.fields && Array.isArray(data.fields)) {
        const fieldValues: Record<number, any> = {};
        
        data.fields.forEach((field: { fieldId: number; fieldValue: string; fieldType: string }) => {
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
    if (!selectedTags.includes(tagName) && fileData && fileData.blobId) {
      setIsLoading(true);
      
      try {
        const tagId = findTagIdByName(tagName);
        
        if (!tagId) {
          console.error(`Tag ID not found for tag: ${tagName}`);
          setIsLoading(false);
          return;
        }
        
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
        
        // No need to wait for data response, just log success
        console.log("Tag assigned successfully");
        
        setSelectedTags([...selectedTags, tagName]);
        
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
  
  function findTagIdByName(tagName: string): number | null {
    const existingTagIndex = fileData.tags.findIndex(tag => tag === tagName);
    if (existingTagIndex !== -1) {
      return fileData.tagIds[existingTagIndex];
    }
    
    return projectTagMap[tagName] || null;
  }

  function handleTagRemoval(tagToRemove: string) {
    if (!fileData || !fileData.blobId) {
      console.warn("File missing blobId");
      return;
    }
    
    const tagIndex = fileData.tags.indexOf(tagToRemove);
    if (tagIndex === -1) {
      console.warn(`Tag "${tagToRemove}" not found in file metadata`);
      return;
    }
    
    const tagIdToRemove = fileData.tagIds[tagIndex];
    
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
        
        setSelectedTags(selectedTags.filter(tag => tag !== tagToRemove));
        
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
        <p className="text-red-500 text-lg">Please go back to palette after refresh.</p>
      </div>
    );
  }

  function handleSave() {
    updateMetadata(fileIndex, {
      description,
      location,
      tags: selectedTags,
      metadata: metadataValues,
    });

    if (fileData.project && fileData.blobId) {
      saveMetadataToAPI();
    } else {
      router.push("/palette");
    }
  }

  async function saveMetadataToAPI() {
    setIsLoading(true);
    try {
      const token = localStorage.getItem("token");
      
      const metadataEntries = Object.entries(metadataValues).map(([fieldIdStr, value]) => {
        const fieldId = parseInt(fieldIdStr);
        return {
          FieldId: fieldId,
          FieldValue: value
        };
      });
      
      const filteredMetadataEntries = metadataEntries.filter(entry => 
        entry.FieldValue !== undefined && 
        entry.FieldValue !== null && 
        entry.FieldValue !== ""
      );
      
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

      if (response.status === 403) {
        const errorData = await response.json();
        toast.error(errorData.detail); // eg. "You cannot submit assets to archived project {projectName}"
        // console.log("sean3");
        throw new Error();
      }
      
      if (!response.ok) {
        const errorData = await response.text();
        console.error("Server error response:", errorData);
        throw new Error(`Failed to save metadata: ${response.status}`);
      }
      
      console.log("Metadata saved successfully");
      
      router.push("/palette");
    } catch (error) {
      console.error("Error saving metadata:", error);
      toast.error("Failed to save metadata. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleEditImage() {
    // Navigate to a new page under /palette/ for image editing
    if (!fileName) {
      console.error("File name is missing!");
      return;
    }

    setIsLoading(true);
    try {
      // Check if we have the raw file content
      if (!fileData.isLoaded && fileData.blobId) {
        // Need to load the file content first
        const blob = await loadFileContent(fileData.blobId);
        if (!blob) {
          throw new Error("Failed to load file content");
        }
        
        // Create a new File object from the blob
        const file = new File([blob], fileData.file.name, { type: blob.type });
        
        // Create an object URL for preview
        const objectUrl = URL.createObjectURL(blob);
        
        // Update the file metadata in context with the loaded content
        updateMetadata(fileIndex, {
          file: file,
          url: objectUrl,
          isLoaded: true
        });
      }

      // Now navigate to the edit image page
      router.push(`/palette/editImage?file=${encodeURIComponent(fileName)}`);
    } catch (error) {
      console.error("Error preparing file for editing:", error);
      alert("Failed to prepare file for editing. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  function readFileAsBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onloadend = () => {
        if (reader.result) resolve(reader.result as string);
        else reject("Failed to read file");
      };
      reader.onerror = reject;
      reader.readAsDataURL(file);
    });
  }
  

  async function handleSuggestTagsUsingAI() {
    console.log("starting suggest tags using AI");
    if (!fileName) {
      console.error("File name is missing!");
      return;
    }

    if (!fileData.file) return;

    setIsLoading(true);
    try {
      // const handleSubmit = async 
      // (e: React.FormEvent) => {
        // e.preventDefault();

        const base64 = await readFileAsBase64(fileData.file); // sean 2
    
          const tags = projectTags.join(", ");
          const prompt = `You are an image tagging assistant. From the list: [${tags}], return only those relevant to this image. Output as a comma-separated list.`;
      
          const response = await fetch("/api/gemini", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              prompt,
              imageBase64: base64,
            }),
          });

          console.log("after post sent to gemini");
      
          const data = await response.json();
          console.log(data);

          data.relevantTags = "data, cloud";

          // for loop that calls handleTagSelection(tagName: string)
          // setResult(data.relevantTags);

          const userConfirmed = window.confirm(
            `AI suggests these tags: ${data.relevantTags}. Press OK to accept all, Cancel to decline.`
          );

          if (userConfirmed) {
            const suggestedTags = data.relevantTags
              .split(",")
              .map((tag: string) => tag.trim())
              .filter(Boolean);

            for (const tag of suggestedTags) {
              handleTagSelection(tag);
            }
          }

    } catch (error) {
      console.error("suggest tags with AI", error);
      alert("Failed to prepare file for editing. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  function isVideoFile(filename: string): boolean {
    const videoExtensions = ['.mp4', '.mov', '.avi', '.wmv', '.mkv', '.webm', '.flv', '.mpeg', '.mpg', '.m4v'];
    const extension = filename.substring(filename.lastIndexOf('.')).toLowerCase();
    return videoExtensions.includes(extension);
  }

  const isVideo = fileData.file.name ? isVideoFile(fileData.file.name) : false;

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
        <div className="bg-gradient-to-r from-blue-600 to-teal-500 px-6 py-6 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-white">
            {isVideo ? "Video" : "Image"} Metadata Editor
          </h1>
          <div className="flex items-center text-white bg-white/20 px-4 py-2 rounded-full">
            <span className="text-xl mr-2">{fileTypeInfo.icon}</span>
            <span className="text-sm font-medium">{fileTypeInfo.label}</span>
          </div>
        </div>
        
        <div className="p-6">
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
                {isLoading ? (
                  <>
                    <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Preparing Image...
                  </>
                ) : isVideo ? (
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
              <button
                onClick={handleSuggestTagsUsingAI}
                disabled={isLoading || isVideo}
                className={`${
                  isVideo 
                    ? "bg-gray-300 cursor-not-allowed" 
                    : "bg-gradient-to-r from-blue-500 to-blue-600 hover:from-blue-600 hover:to-blue-700"
                } text-white font-medium py-3 px-8 rounded-lg transition-all duration-200 disabled:opacity-50 shadow-md hover:shadow-lg flex items-center`}
                title={isVideo ? "AI Cannot Suggest Tags for Video Files" : "Suggest Tags Using AI"}
              >
                {isLoading ? (
                  <>
                    <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Preparing AI Suggest Tags Feature...
                  </>
                ) : isVideo ? (
                  <>
                    <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    AI Cannot Suggest Tags for Video Files
                  </>
                ) : (
                  <>
                    <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                    </svg>
                    Suggest Tags Using AI
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
