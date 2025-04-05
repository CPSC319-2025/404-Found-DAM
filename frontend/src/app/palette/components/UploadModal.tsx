"use client";

import React, { useCallback, useState, useEffect } from "react";
import { useDropzone } from "react-dropzone";
import { 
  fetchBlobDetails, 
  uploadFileChunked,
  UploadProgressCallbacks,
  Project
} from "../Apis";
import { FileMetadata, useFileContext } from "@/app/context/FileContext";

// Simple Progress component
const Progress = ({ 
  value 
}: { 
  value: number 
}) => (
  <div className="w-full bg-gray-200 rounded-full h-2.5 mb-4">
    <div 
      className="bg-indigo-600 h-2.5 rounded-full" 
      style={{ width: `${value}%` }}
    />
  </div>
);

interface UploadModalProps {
  projects: Project[];
  closeModal: () => void;
  createFileMetadata: (file: File) => FileMetadata;
  fetchAndUpdateBlobDetails: (blobId: string) => Promise<void>;
}

export default function UploadModal({
  projects,
  closeModal,
  createFileMetadata,
  fetchAndUpdateBlobDetails
}: UploadModalProps) {
  const { setFiles } = useFileContext();
  const [currentStep, setCurrentStep] = useState(1);
  const [uploadedFiles, setUploadedFiles] = useState<File[]>([]);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadingProgress, setUploadingProgress] = useState(0);
  const [uploadingFileName, setUploadingFileName] = useState("");
  const [selectedProject, setSelectedProject] = useState("");
  const [description, setDescription] = useState("");
  const [location, setLocation] = useState("");
  const [processingFiles, setProcessingFiles] = useState<{name: string, progress: number}[]>([]);
  const [processedFiles, setProcessedFiles] = useState<{name: string}[]>([]);
  const [projectTags, setProjectTags] = useState<{id: number, name: string}[]>([]);
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);
  const [isLoadingTags, setIsLoadingTags] = useState(false);
  const [uploadedCount, setUploadedCount] = useState(0);
  
  // Auto-populate description and location when project is selected
  useEffect(() => {
    if (selectedProject) {
      // Clear selected tags when project changes
      setSelectedTags([]);
      
      const project = projects.find(p => p.projectID.toString() === selectedProject);
      if (project) {
        setDescription(project.description || "");
        setLocation(project.location || "");
        // Fetch project tags when project is selected
        fetchProjectTags(selectedProject);
      }
    }
  }, [selectedProject, projects]);
  
  // Function to fetch project tags
  const fetchProjectTags = async (projectId: string) => {
    setIsLoadingTags(true);
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
      
      // Extract suggested tags for display in the UI
      if (data.suggestedTags && Array.isArray(data.suggestedTags)) {
        const tagList = data.suggestedTags.map((tag: { tagID: number, name: string }) => ({
          id: tag.tagID,
          name: tag.name
        }));
        setProjectTags(tagList);
      }
      
      // Auto-add only the project's actual tags (not all suggested tags)
      if (data.tags && Array.isArray(data.tags)) {
        // Extract project tags from data
        const projectTagNames: string[] = data.tags.map((tag: { name: string }) => tag.name);
        const projectTagIds: number[] = data.tags.map((tag: { tagID: number }) => tag.tagID);
        
        // Add project tags to selected tags (prevent duplicates)
        setSelectedTags(prevTags => {
          const newTags = [...prevTags];
          
          // Add each project tag if not already in selectedTags
          projectTagNames.forEach(tag => {
            if (!newTags.includes(tag)) {
              newTags.push(tag);
            }
          });
          
          return newTags;
        });

        // Add project tag IDs to selectedTagIds
        setSelectedTagIds(prevIds => {
          const newIds = [...prevIds];
          
          // Add each project tag ID if not already in selectedTagIds
          projectTagIds.forEach(id => {
            if (!newIds.includes(id)) {
              newIds.push(id);
            }
          });
          
          return newIds;
        });
      }
    } catch (error) {
      console.error("Error fetching project tags:", error);
    } finally {
      setIsLoadingTags(false);
    }
  };

  // Handle tag selection
  const handleTagSelection = (tag: string) => {
    if (!selectedTags.includes(tag)) {
      setSelectedTags([...selectedTags, tag]);
      
      // Find and add the corresponding tag ID
      const tagObject = projectTags.find(t => t.name === tag);
      if (tagObject && !selectedTagIds.includes(tagObject.id)) {
        setSelectedTagIds([...selectedTagIds, tagObject.id]);
      }
    }
  };

  // Handle tag removal
  const handleTagRemoval = (tagToRemove: string) => {
    setSelectedTags(selectedTags.filter(tag => tag !== tagToRemove));
    
    // Remove the corresponding tag ID
    const tagObject = projectTags.find(t => t.name === tagToRemove);
    if (tagObject) {
      setSelectedTagIds(selectedTagIds.filter(id => id !== tagObject.id));
    }
  };
  
  // Handle dropping files
  const onModalDrop = useCallback((acceptedFiles: File[]) => {
    // Check for duplicate filenames before adding
    const uniqueFiles = acceptedFiles.filter(newFile => {
      // Check if file with same name exists in either uploaded or processed files
      const isDuplicate = uploadedFiles.some(existingFile => existingFile.name === newFile.name);
      
      if (isDuplicate) {
        alert(`File "${newFile.name}" already exists in upload list. Please rename the file before uploading.`);
        return false;
      }
      return true;
    });
    
    const newFiles = [...uploadedFiles, ...uniqueFiles];
    setUploadedFiles(newFiles);
    
    // Initialize processing files (only for unique files)
    setProcessingFiles(prev => [
      ...prev,
      ...uniqueFiles.map(file => ({
        name: file.name, 
        progress: 0
      }))
    ]);
  }, [uploadedFiles]);
  
  const { getRootProps: getModalRootProps, getInputProps: getModalInputProps, isDragActive: isModalDragActive } = useDropzone({
    onDrop: onModalDrop,
    accept: { "image/*": [], "video/*": [], "application/pdf": [], "application/vnd.ms-powerpoint": [], 
             "application/vnd.openxmlformats-officedocument.presentationml.presentation": [], 
             "application/msword": [], "application/vnd.openxmlformats-officedocument.wordprocessingml.document": [] }
  });
  
  // Simulate progress for demo purposes
  useEffect(() => {
    if (processingFiles.length > 0) {
      // Simulate file processing
      const timer = setInterval(() => {
        setProcessingFiles(prev => {
          const updated = [...prev];
          const firstPending = updated.findIndex(f => f.progress < 100);
          
          if (firstPending >= 0) {
            const newProgress = Math.min(updated[firstPending].progress + 20, 100);
            updated[firstPending] = { ...updated[firstPending], progress: newProgress };
            
            // If file is complete, move to processedFiles but check for duplicates first
            if (newProgress === 100) {
              const fileName = updated[firstPending].name;
              setProcessedFiles(prev => {
                // Check if file is already in processedFiles to avoid duplicates
                if (!prev.some(f => f.name === fileName)) {
                  return [...prev, { name: fileName }];
                }
                return prev;
              });
            }
          } else {
            clearInterval(timer);
          }
          
          return updated;
        });
      }, 20); // Slower timer to reduce race conditions
      
      return () => clearInterval(timer);
    }
  }, [processingFiles.length]);
  
  // Remove a file from the upload list
  const removeUploadFile = useCallback((index: number) => {
    const fileToRemove = processingFiles[index];
    
    // Ensure we have a valid file to remove
    if (!fileToRemove) return;
    
    setUploadedFiles(prev => {
      return prev.filter(file => file.name !== fileToRemove.name);
    });
    
    setProcessingFiles(prev => {
      const newFiles = [...prev];
      newFiles.splice(index, 1);
      return newFiles;
    });
    
    // Also remove from processed files if it exists there
    setProcessedFiles(prev => {
      return prev.filter(file => file.name !== fileToRemove.name);
    });
  }, [processingFiles]);
  
  // Remove a processed file
  const removeProcessedFile = useCallback((index: number) => {
    const fileToRemove = processedFiles[index];
    
    // Ensure we have a valid file to remove
    if (!fileToRemove) return;
    
    setProcessedFiles(prev => {
      const newFiles = [...prev];
      newFiles.splice(index, 1);
      return newFiles;
    });
    
    // Also remove from uploaded files
    setUploadedFiles(prev => {
      return prev.filter(file => file.name !== fileToRemove.name);
    });
  }, [processedFiles]);
  
  // Handle confirming upload
  const handleConfirmUpload = useCallback(async () => {
    if (!selectedProject) {
      const confirmUpload = window.confirm("No project selected. Assets will be uploaded without being associated with a project. Continue?");
      if (!confirmUpload) {
        return;
      }
    }
    
    setIsUploading(true);
    const uploadedBlobIds: string[] = [];
    let successCount = 0;
    
    for (const file of uploadedFiles) {
      // Create file metadata
      const fileMeta = createFileMetadata(file);
      fileMeta.description = description;
      fileMeta.location = location;
      fileMeta.project = selectedProject;
      fileMeta.tags = selectedTags;
      fileMeta.tagIds = selectedTagIds;
      
      // Upload file in chunks without adding to files state yet
      setUploadingFileName(file.name);
      setUploadingProgress(0);
      
      const callbacks: UploadProgressCallbacks = {
        onProgress: (progress: number, status: string) => {
          setUploadingProgress(progress);
        },
        onSuccess: async (blobId?: string) => {
          setUploadingProgress(20);
          
          if (blobId) {
            // Add blobId to our tracking array
            uploadedBlobIds.push(blobId);
            
            // Instead of using processFileMetadata (which adds to files state),
            // manually add the file with all metadata at once
            fileMeta.blobId = blobId;
            
            // Don't add to files state directly - we'll reload the page instead
            // to respect pagination
            successCount++;
            setUploadedCount(prev => prev + 1);
            
            // Fetch and update blob details
            await fetchAndUpdateBlobDetails(blobId);
          }
        },
        onError: (error: string) => {
          alert(`Error uploading ${file.name}: ${error}`);
        }
      };
      
      await uploadFileChunked(file, callbacks);
    }
    
    // After all files have been uploaded, associate them with the project and tags
    if (uploadedBlobIds.length > 0) {
      try {
        // Only associate files with project if a project is selected
        if (selectedProject) {
          const token = localStorage.getItem("token");
          const projectId = parseInt(selectedProject);
          
          // Call the associate-assets API
          const response = await fetch(
            `${process.env.NEXT_PUBLIC_API_BASE_URL}/projects/${projectId}/associate-assets`,
            {
              method: "PATCH",
              headers: {
                Authorization: token ? `Bearer ${token}` : "",
                "Content-Type": "application/json",
              },
              body: JSON.stringify({
                ProjectID: projectId,
                BlobIDs: uploadedBlobIds,
                TagIDs: selectedTagIds,
                MetadataEntries: []
              }),
            }
          );
          
          if (!response.ok) {
            throw new Error(`Failed to associate assets with project: ${response.status}`);
          }
        }
      } catch (error) {
        console.error("Error associating assets with project:", error);
        alert("Files were uploaded but there was an issue linking them to the project. Please check your project dashboard.");
      }
    }
    
    // Store a flag indicating that we need to refresh and go to page 1
    if (successCount > 0) {
      // Set a flag in localStorage so the main page knows to reload page 1
      localStorage.setItem('paletteRefreshPage', '1');
      
      // Use a small delay to avoid race conditions
      setTimeout(() => {
        setIsUploading(false);
        closeModal();
        
        // Clear state
        setUploadedFiles([]);
        setCurrentStep(1);
        setProcessingFiles([]);
        setProcessedFiles([]);
        setSelectedTags([]);
        setSelectedTagIds([]);
        setUploadedCount(0);
        
        // Display a temporary message to user
        alert(`Successfully uploaded ${successCount} file(s). Refreshing to display new files.`);
        
        // Force a page refresh to pick up the new files with correct pagination
        window.location.reload();
      }, 500);
    } else {
      setIsUploading(false);
      closeModal();
      setUploadedFiles([]);
      setCurrentStep(1);
      setProcessingFiles([]);
      setProcessedFiles([]);
      setSelectedTags([]);
      setSelectedTagIds([]);
      setUploadedCount(0);
    }
  }, [uploadedFiles, selectedProject, description, location, selectedTags, selectedTagIds, createFileMetadata, fetchAndUpdateBlobDetails, closeModal]);
  
  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl overflow-hidden">
        <div className="flex justify-between items-center p-4 border-b">
          <h2 className="text-xl font-semibold">Upload</h2>
          <button 
            onClick={closeModal}
            className="text-gray-500 hover:text-gray-700 text-2xl"
          >
            &times;
          </button>
        </div>
        
        <div className="p-6">
          {currentStep === 1 && (
            <div>
              <div 
                {...getModalRootProps()} 
                className="border-2 border-dashed border-gray-300 rounded-lg p-8 text-center cursor-pointer"
              >
                <input {...getModalInputProps()} />
                <div className="flex flex-col items-center justify-center">
                  <div className="mb-4">
                    <svg className="w-12 h-12 text-indigo-600" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path d="M7 16a4 4 0 0 0 4 4h2a4 4 0 0 0 4-4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                      <path d="M12 12V3M12 3L9 6M12 3L15 6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                  </div>
                  <h3 className="text-lg font-medium mb-2">Drag & drop files or <span className="text-indigo-600">Browse</span></h3>
                  <p className="text-sm text-gray-500">Supported formats: JPEG, PNG, GIF, MP4, PDF, PSD, AI, Word, PPT</p>
                </div>
              </div>
              
              <div className="mt-6 max-h-60 overflow-y-auto">
                {/* If there are files, show a header */}
                {(processingFiles.length > 0 || processedFiles.length > 0) && (
                  <h3 className="font-medium mb-4">Uploading - {uploadedFiles.length} files</h3>
                )}
                
                {/* Processing Files */}
                {processingFiles.filter(f => f.progress < 100).map((file, index) => (
                  <div key={`processing-${index}`} className="mb-4">
                    <div className="py-3 px-4 border rounded-lg mb-2 flex justify-between items-center">
                      <span>{file.name}</span>
                      <button 
                        onClick={() => removeUploadFile(index)}
                        className="text-gray-500 hover:text-red-500"
                      >
                        &times;
                      </button>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2.5">
                      <div 
                        className="bg-indigo-600 h-2.5 rounded-full" 
                        style={{ width: `${file.progress}%` }}
                      />
                    </div>
                  </div>
                ))}
                
                {/* Uploaded Files */}
                {processedFiles.length > 0 && (
                  <div className="mt-4">
                    <h3 className="font-medium mb-2">Uploaded</h3>
                    {processedFiles.map((file, index) => (
                      <div key={`uploaded-${index}`} className="py-3 px-4 border border-green-500 rounded-lg mb-2 flex justify-between items-center">
                        <span>{file.name}</span>
                        <button
                          onClick={() => removeProcessedFile(index)}
                          className="text-red-500 hover:text-red-700"
                        >
                          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                            <path fillRule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clipRule="evenodd" />
                          </svg>
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          )}
          
          {currentStep === 2 && (
            <div>
              <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Project</label>
                <select 
                  className="w-full px-3 py-2 border rounded-md"
                  value={selectedProject}
                  onChange={(e) => setSelectedProject(e.target.value)}
                >
                  <option value="">Select Project</option>
                  {projects.map((project) => (
                    <option key={project.projectID} value={project.projectID.toString()}>
                      {project.projectName}
                    </option>
                  ))}
                </select>
              </div>
              
              <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Description</label>
                <textarea 
                  className="w-full px-3 py-2 border rounded-md bg-gray-100"
                  placeholder="Enter description..."
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  readOnly={true}
                />
                <p className="mt-1 text-xs text-gray-500">Auto-populated from project settings</p>
              </div>
              
              <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Location</label>
                <input 
                  type="text"
                  className="w-full px-3 py-2 border rounded-md bg-gray-100"
                  placeholder="Enter Location..."
                  value={location}
                  onChange={(e) => setLocation(e.target.value)}
                  readOnly={true}
                />
                <p className="mt-1 text-xs text-gray-500">Auto-populated from project settings</p>
              </div>
              
              <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Selected Tags:</label>
                <div className="min-h-[38px] w-full border border-gray-300 rounded-md p-2 flex flex-wrap gap-2">
                  {selectedTags.length > 0 ? (
                    selectedTags.map((tag, index) => {
                      // Find corresponding tag ID for tooltip
                      const tagObject = projectTags.find(t => t.name === tag);
                      const tagId = tagObject ? tagObject.id : 'unknown';
                      
                      return (
                        <span
                          key={index}
                          className="inline-flex items-center px-2 py-1 rounded-full text-xs font-semibold bg-blue-100 text-blue-800"
                          title={`Tag ID: ${tagId}`}
                        >
                          {tag}
                          <button
                            onClick={() => handleTagRemoval(tag)}
                            className="ml-1 text-red-500 hover:text-red-700"
                            title="Remove tag"
                          >
                            ×
                          </button>
                        </span>
                      );
                    })
                  ) : (
                    <span className="text-gray-400 text-xs self-center">No tags selected</span>
                  )}
                </div>
              </div>
              
              {selectedProject && (
                <div className="mb-4">
                  <label className="block text-sm font-medium mb-1">Suggested Tags:</label>
                  {isLoadingTags ? (
                    <p className="text-sm text-gray-500">Loading suggested tags...</p>
                  ) : projectTags.length > 0 ? (
                    <div className="flex flex-wrap gap-2 mt-2">
                      {projectTags.map((tag, index) => (
                        <button
                          key={index}
                          onClick={() => handleTagSelection(tag.name)}
                          className={`px-3 py-1 rounded-full text-sm transition-colors ${
                            selectedTags.includes(tag.name)
                              ? "bg-blue-100 text-blue-800 cursor-default"
                              : "bg-gray-100 hover:bg-gray-200 text-gray-800"
                          }`}
                          disabled={selectedTags.includes(tag.name)}
                        >
                          {tag.name}
                        </button>
                      ))}
                    </div>
                  ) : (
                    <p className="text-sm text-gray-500">No suggested tags available</p>
                  )}
                </div>
              )}
            </div>
          )}
          
          {isUploading && (
            <div className="mt-4">
              <p className="text-sm">Uploading {uploadingFileName}</p>
              <Progress value={uploadingProgress} />
            </div>
          )}
          
          <div className="mt-6 flex justify-center">
            <button
              onClick={() => {
                if (currentStep === 1) {
                  // Only allow proceeding if there are files and they're all processed
                  if (uploadedFiles.length > 0 && processingFiles.every(f => f.progress === 100)) {
                    setCurrentStep(2);
                  }
                } else {
                  handleConfirmUpload();
                }
              }}
              disabled={isUploading || 
                       (currentStep === 1 && (uploadedFiles.length === 0 || !processingFiles.every(f => f.progress === 100)))}
              className="w-full py-3 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isUploading ? "Uploading..." : 
               currentStep === 2 ? "UPLOAD" : "CONTINUE"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
} 