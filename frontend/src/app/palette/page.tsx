"use client";

import React, { useCallback, useState, useEffect, useRef } from "react";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";

import { useFileContext, FileMetadata } from "@/app/context/FileContext";
import FileTable from "./components";
import UploadModal from "./components/UploadModal";
import { 
  fetchPaletteAssets, 
  fetchBlobDetails, 
  fetchProjects, 
  removeFile as removeFileApi, 
  submitAssets,
  uploadFileChunked,
  UploadProgressCallbacks,
  Project
} from "./Apis";
import { useUser } from "@/app/context/UserContext";
import { formatFileSize } from "@/app/utils/api/formatFileSize";

// Simple Button component
const Button = ({ 
  children, 
  onClick,
  className = "",
  disabled = false
}: { 
  children: React.ReactNode; 
  onClick: () => void;
  className?: string;
  disabled?: boolean;
}) => (
  <button
    onClick={onClick}
    disabled={disabled}
    className={`px-6 py-3 rounded-lg font-medium transition-all duration-200 shadow-md hover:shadow-lg flex items-center ${className} ${
      disabled ? "opacity-50 cursor-not-allowed" : ""
    }`}
  >
    {children}
  </button>
);

// Simple Progress component
const Progress = ({ 
  value 
}: { 
  value: number 
}) => (
  <div className="w-full bg-gray-200 rounded-full h-2.5 mb-4">
    <div 
      className="bg-gradient-to-r from-blue-500 to-teal-500 h-2.5 rounded-full transition-all duration-300" 
      style={{ width: `${value}%` }}
    />
  </div>
);

export default function PalettePage() {
  const { user } = useUser();
  const router = useRouter();
  const { files, setFiles } = useFileContext();

  const [selectedIndices, setSelectedIndices] = useState<number[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const didFetchRef = useRef(false);
  const [showModal, setShowModal] = useState(false); // New upload window for modal visibility
  // Upload status for drag and drop
  const [uploadStatus, setUploadStatus] = useState<string>("");
  const [uploadProgress, setUploadProgress] = useState<number>(0);
  // Automated naming convention toggle
  const [autoNamingEnabled, setAutoNamingEnabled] = useState<boolean>(false);
  // Delete confirmation modal
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [fileToDeleteIndex, setFileToDeleteIndex] = useState<number | null>(null);

  // Helper function to get file dimensions from image/video and add to state
  const processFileMetadata = useCallback(async (fileMeta: FileMetadata): Promise<void> => {
    return new Promise((resolve) => {
      if (fileMeta.file.type.startsWith("image/")) {
        const img = new Image();
        img.onload = () => {
          fileMeta.width = img.width;
          fileMeta.height = img.height;
          setFiles((prev) => [...prev, fileMeta]);
          resolve();
        };
        img.src = URL.createObjectURL(fileMeta.file);
      } else if (fileMeta.file.type.startsWith("video/")) {
        const video = document.createElement("video");
        video.preload = "metadata";
        video.onloadedmetadata = () => {
          fileMeta.width = video.videoWidth;
          fileMeta.height = video.videoHeight;
          fileMeta.duration = Math.floor(video.duration);
          setFiles((prev) => [...prev, fileMeta]);
          resolve();
        };
        video.src = URL.createObjectURL(fileMeta.file);
      } else {
        setFiles((prev) => [...prev, fileMeta]);
        resolve();
      }
    });
  }, [setFiles]);

  // Helper function to fetch blob details
  const fetchAndUpdateBlobDetails = useCallback(async (blobId: string): Promise<void> => {
    if (!blobId) return;
    
    try {
      const details = await fetchBlobDetails(blobId);
      
      // Update the file in our state with the details
      setFiles(prevFiles => prevFiles.map(file => {
        if (file.blobId === blobId) {
          return {
            ...file,
            project: details.project,
            tags: details.tags || [],
            tagIds: details.tagIds || [],
            description: details.description || "",
            location: details.location || ""
          };
        }
        return file;
      }));
    } catch (error) {
      console.error("Error fetching blob details:", error);
    }
  }, [setFiles]);

  // Load assets on initial mount
  useEffect(() => {
    if (didFetchRef.current) return;
    didFetchRef.current = true;
    
    const loadAssets = async () => {
      setFiles([]);
      const fetchedFiles = await fetchPaletteAssets();
      
      // Process each file for additional metadata
      for (const fileMeta of fetchedFiles) {
        await processFileMetadata(fileMeta);
        if (fileMeta.blobId) {
          await fetchAndUpdateBlobDetails(fileMeta.blobId);
        }
      }
    };
    
    loadAssets();
  }, [setFiles, processFileMetadata, fetchAndUpdateBlobDetails]);

  // Fetch projects once on mount
  useEffect(() => {
    const loadProjects = async () => {
      const projectsData = await fetchProjects();
      const userProjects = projectsData.filter((project) =>
        project.admins.some((admin) => admin.userID === user?.userID) ||
        project.regularUsers.some((regularUser) => regularUser.userID === user?.userID)
      );
      setProjects(userProjects);
    };

    loadProjects();
  }, []);

  // Remove a file by index
  const removeFile = useCallback((index: number) => {
    // Only show confirmation if multiple files are selected
    if (selectedIndices.length > 1 && selectedIndices.includes(index)) {
      setFileToDeleteIndex(index);
      setShowDeleteConfirm(true);
      return;
    }

    // If not selected or only one file is selected, proceed with normal delete
    deleteFile(index);
  }, [selectedIndices]);

  // Delete a single file
  const deleteFile = useCallback((index: number) => {
    setFiles((prev) => {
      const updated = [...prev];
      const fileToRemove = updated[index];
      
      // Call the API to remove the file
      if (fileToRemove.blobId) {
        removeFileApi(fileToRemove);
      }
      
      // Remove the file from state
      updated.splice(index, 1);
      return updated;
    });
    
    // Also update selected indices to account for the removed file
    setSelectedIndices(prev => {
      return prev
        .filter(i => i !== index) // Remove the deleted index
        .map(i => i > index ? i - 1 : i); // Shift all indices above the deleted one
    });

    // Clear the file to delete index
    setFileToDeleteIndex(null);
  }, [setFiles]);

  // Delete all selected files
  const deleteAllSelected = useCallback(() => {
    // Sort indices in descending order to avoid index shifting issues
    const sortedIndices = [...selectedIndices].sort((a, b) => b - a);
    
    sortedIndices.forEach(index => {
      setFiles((prev) => {
        const updated = [...prev];
        const fileToRemove = updated[index];
        
        // Call the API to remove the file
        if (fileToRemove.blobId) {
          removeFileApi(fileToRemove);
        }
        
        // Remove the file from state
        updated.splice(index, 1);
        return updated;
      });
    });
    
    // Clear selected indices
    setSelectedIndices([]);
    setShowDeleteConfirm(false);
  }, [selectedIndices, setFiles]);

  // Prepare a file metadata object
  const createFileMetadata = useCallback((file: File): FileMetadata => {
    const fileSize = formatFileSize(file.size);
    return {
      file,
      fileSize,
      description: "",
      location: "",
      tags: [],
      tagIds: [],
    };
  }, []);

  // Create callbacks for the chunked upload
  const createUploadCallbacks = useCallback((file: File, fileMeta: FileMetadata): UploadProgressCallbacks => ({
    onProgress: (progress: number, status: string) => {
      setUploadStatus(`Uploading ${file.name}: ${status}`);
      setUploadProgress(progress);
    },
    onSuccess: async (blobId?: string) => {
      setUploadStatus(`File ${file.name} uploaded successfully`);
      setUploadProgress(100);
      
      // Clear status after a delay
      setTimeout(() => {
        setUploadStatus("");
        setUploadProgress(0);
      }, 500);
      
      if (blobId) {
        // Update the file metadata with the blobId
        setFiles(prevFiles => {
          return prevFiles.map(f => {
            if (f.file === file) {
              return { ...f, blobId };
            }
            return f;
          });
        });
        
        // Fetch and update blob details
        await fetchAndUpdateBlobDetails(blobId);
      } else {
        // Fallback to old method if no blobId is returned
        const fetchedFiles = await fetchPaletteAssets();
        
        // Find the newly uploaded file and get its blobId
        const newFile = fetchedFiles.find(f => f.file.name === file.name);
        if (newFile && newFile.blobId) {
          await fetchAndUpdateBlobDetails(newFile.blobId);
        }
      }
    },
    onError: (error: string) => {
      setUploadStatus(`Error uploading ${file.name}: ${error}`);
      
      // Clear error after a delay
      setTimeout(() => {
        setUploadStatus("");
        setUploadProgress(0);
      }, 5000);
    }
  }), [fetchAndUpdateBlobDetails, setFiles]);

  // Handle file drop with chunked upload
  const onDrop = useCallback(
    async (acceptedFiles: File[]) => {
      for (const file of acceptedFiles) {
        // Create file metadata
        const fileMeta = createFileMetadata(file);
        
        // Process file metadata (dimensions, etc.)
        await processFileMetadata(fileMeta);
        
        // Upload file in chunks
        setUploadStatus(`Starting upload of ${file.name}...`);
        setUploadProgress(0);
        
        const blobId = await uploadFileChunked(file, createUploadCallbacks(file, fileMeta));
        
        // If we got a blobId directly, update the file metadata
        if (blobId) {
          setFiles(prevFiles => {
            return prevFiles.map(f => {
              if (f.file === file) {
                return { ...f, blobId };
              }
              return f;
            });
          });
        }
      }
    },
    [createFileMetadata, processFileMetadata, createUploadCallbacks, setFiles]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { "image/*": [], "video/*": [] },
  });

  const handleUploadNewDesign = useCallback(() => {
    setShowModal(true);
  }, []);

  // Submit selected assets
  const handleSubmitAssets = useCallback(async () => {
    if (selectedIndices.length === 0) {
      alert("No files selected!");
      return;
    }

    // Check if any selected files don't have a project assigned
    const filesWithoutProject = selectedIndices.filter((index) => !files[index].project);
    if (filesWithoutProject.length > 0) {
      alert(`Warning: ${filesWithoutProject.length} selected file(s) don't have a project assigned. Please select a project for all files before submitting.`);
      return;
    }

    // Group selected files by their project
    const projectMap: Record<string, string[]> = {};
    
    selectedIndices.forEach((fileIndex) => {
      const fileMeta = files[fileIndex];

      // Make sure this file has both a blobId and a project ID
      if (!fileMeta.blobId || !fileMeta.project) {
        console.warn(`File missing blobId or project ID: ${fileMeta.file.name}`);
        return;
      }

      const projectId = fileMeta.project;
      if (!projectMap[projectId]) {
        projectMap[projectId] = [];
      }
      projectMap[projectId].push(fileMeta.blobId);
    });

    // Submit assets for each project
    for (const projectId in projectMap) {
      const blobIDs = projectMap[projectId];
      
      // Add autoNaming parameter if enabled
      const success = await submitAssets(projectId, blobIDs, autoNamingEnabled ? "?Auto" : "");
      
      if (success) {
        // Remove these files from our palette state
        setFiles((prev) =>
          prev.filter(
            (file) =>
              file.project !== projectId || // keep files from other projects
              !file.blobId ||
              !blobIDs.includes(file.blobId) // keep files not in this batch
          )
        );

        // Also remove them from the selectedIndices array
        setSelectedIndices((prev) =>
          prev.filter((i) => {
            const f = files[i];
            return (
              f.project !== projectId ||
              !f.blobId ||
              !blobIDs.includes(f.blobId)
            );
          })
        );
      }
    }
  }, [files, selectedIndices, setFiles, autoNamingEnabled]);

  // Toggle auto naming feature
  const toggleAutoNaming = useCallback(() => {
    setAutoNamingEnabled(prev => !prev);
  }, []);

  return (
    <div className="min-h-screen bg-gradient-to-b p-6">
      <div className="max-w-8xl mx-auto"> 
        <div className="mb-8 bg-gradient-to-r from-blue-600 to-teal-500 px-6 py-6 rounded-xl shadow-lg text-white">
          <h1 className="text-3xl font-bold mb-2">Asset Palette</h1>
          <p className="text-white/80">Manage and submit your digital assets</p>
        </div>
        
        <div className="bg-white rounded-xl shadow-2xl overflow-hidden mb-8">
          <FileTable
            files={files}
            removeFile={removeFile}
            selectedIndices={selectedIndices}
            setSelectedIndices={setSelectedIndices}
            projects={projects}
          />
        </div>

        {uploadStatus && (
          <div className="bg-white rounded-xl shadow-md p-6 mb-8">
            <h3 className="text-lg font-medium text-gray-700 mb-2">{uploadStatus}</h3>
            <Progress value={uploadProgress} />
          </div>
        )}

        <div className="bg-white p-8 rounded-xl shadow-2xl flex flex-col md:flex-row items-center justify-between gap-6">
          <div className="flex-1">
            <h2 className="text-xl font-semibold text-gray-800 mb-2">Ready to upload?</h2>
            <p className="text-gray-600">Add new assets to your palette or submit existing ones to projects</p>
          </div>
          
          <div className="flex flex-col sm:flex-row gap-4">
            <Button
              onClick={handleUploadNewDesign}
              className="bg-gradient-to-r from-blue-500 to-blue-600 hover:from-blue-600 hover:to-blue-700 text-white w-[220px] h-[50px] justify-center"
            >
              <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
              </svg>
              Upload Assets
            </Button>

            <div className="flex flex-col">
              <Button
                onClick={handleSubmitAssets}
                disabled={selectedIndices.length === 0}
                className={`w-[220px] h-[50px] justify-center ${
                  selectedIndices.length > 0 
                    ? "bg-gradient-to-r from-teal-500 to-teal-600 hover:from-teal-600 hover:to-teal-700 text-white" 
                    : "bg-gray-300 text-gray-500"
                }`}
              >
                <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
                Submit Selected ({selectedIndices.length})
              </Button>
              
              <div className="flex items-center mt-2 justify-end">
                <button 
                  onClick={toggleAutoNaming}
                  className={`relative w-10 h-5 rounded-full transition-colors duration-300 focus:outline-none ${autoNamingEnabled ? 'bg-gradient-to-r from-teal-500 to-blue-500' : 'bg-gray-300'}`}
                  title="Auto rename files to [Project####__File###]"
                >
                  <span 
                    className={`absolute left-0.5 top-0.5 bg-white w-4 h-4 rounded-full shadow-md transform transition-transform duration-300 ${autoNamingEnabled ? 'translate-x-5' : ''}`}
                  />
                </button>
                <span className="text-xs font-medium ml-1 text-gray-700">Auto-naming</span>
              </div>
            </div>
          </div>
        </div>
        
        {/* Dropzone Area with better styling
        <div 
          {...getRootProps()} 
          className={`mt-8 border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-all 
            ${isDragActive 
              ? "border-blue-500 bg-blue-50" 
              : "border-gray-300 hover:border-blue-400 hover:bg-blue-50"
            }`
          }
        >
          <input {...getInputProps()} />
          <div className="flex flex-col items-center justify-center space-y-4">
            <svg className="w-12 h-12 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
            </svg>
            <div>
              <p className="text-lg font-medium text-gray-700">
                {isDragActive ? "Drop the files here..." : "Drag & drop files here"}
              </p>
              <p className="text-sm text-gray-500 mt-1">
                or click to select files
              </p>
            </div>
          </div>
        </div> */}
        
        {showModal && (
          <UploadModal 
            projects={projects}
            closeModal={() => setShowModal(false)}
            createFileMetadata={createFileMetadata}
            fetchAndUpdateBlobDetails={fetchAndUpdateBlobDetails}
          />
        )}
        
        {/* Delete Confirmation Modal */}
        {showDeleteConfirm && (
          <div className="fixed inset-0 flex items-center justify-center z-50 bg-black bg-opacity-50">
            <div className="bg-white rounded-lg p-6 shadow-xl max-w-md w-full">
              <h3 className="text-xl font-semibold text-gray-800 mb-4">Confirm Deletion</h3>
              <p className="text-gray-600 mb-6">
                This item is part of your selection. Would you like to delete all {selectedIndices.length} selected items?
              </p>
              <div className="flex justify-end space-x-3">
                <button 
                  className="px-4 py-2 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 transition-colors"
                  onClick={() => {
                    if (fileToDeleteIndex !== null) {
                      deleteFile(fileToDeleteIndex);
                    }
                    setShowDeleteConfirm(false);
                    setFileToDeleteIndex(null);
                  }}
                >
                  No, Just This Item
                </button>
                <button 
                  className="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600 transition-colors"
                  onClick={deleteAllSelected}
                >
                  Yes, Delete All Selected
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
