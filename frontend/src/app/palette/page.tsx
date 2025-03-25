"use client";

import React, { useCallback, useState, useEffect, useRef } from "react";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";

import { useFileContext, FileMetadata } from "@/app/context/FileContext";
import FileTable from "./components";
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

// Simple Button component
const Button = ({ 
  children, 
  onClick 
}: { 
  children: React.ReactNode; 
  onClick: () => void 
}) => (
  <button
    onClick={onClick}
    className="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700"
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
      className="bg-indigo-600 h-2.5 rounded-full" 
      style={{ width: `${value}%` }}
    />
  </div>
);

export default function PalettePage() {
  const router = useRouter();
  const { files, setFiles } = useFileContext();

  const [selectedIndices, setSelectedIndices] = useState<number[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const didFetchRef = useRef(false);
  
  // Upload status for drag and drop
  const [uploadStatus, setUploadStatus] = useState<string>("");
  const [uploadProgress, setUploadProgress] = useState<number>(0);

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
      setProjects(projectsData);
    };

    loadProjects();
  }, []);

  // Remove a file by index
  const removeFile = useCallback((index: number) => {
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
  }, [setFiles]);

  // Prepare a file metadata object
  const createFileMetadata = useCallback((file: File): FileMetadata => {
    const fileSize = (file.size / 1024).toFixed(2) + " KB";
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
  const createUploadCallbacks = useCallback((file: File): UploadProgressCallbacks => ({
    onProgress: (progress: number, status: string) => {
      setUploadStatus(`Uploading ${file.name}: ${status}`);
      setUploadProgress(progress);
    },
    onSuccess: async () => {
      setUploadStatus(`File ${file.name} uploaded successfully`);
      setUploadProgress(100);
      
      // Clear status after a delay
      setTimeout(() => {
        setUploadStatus("");
        setUploadProgress(0);
      }, 3000);
      
      // Refresh the palette assets list to show the newly uploaded file
      const fetchedFiles = await fetchPaletteAssets();
      
      // Find the newly uploaded file and get its blobId
      const newFile = fetchedFiles.find(f => f.file.name === file.name);
      if (newFile && newFile.blobId) {
        await fetchAndUpdateBlobDetails(newFile.blobId);
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
  }), [fetchAndUpdateBlobDetails]);

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
        
        await uploadFileChunked(file, createUploadCallbacks(file));
      }
    },
    [createFileMetadata, processFileMetadata, createUploadCallbacks]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { "image/*": [], "video/*": [] },
  });

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
      
      const success = await submitAssets(projectId, blobIDs);
      
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
  }, [files, selectedIndices, setFiles]);

  return (
    <div className="p-6 min-h-screen">
      <FileTable
        files={files}
        removeFile={removeFile}
        selectedIndices={selectedIndices}
        setSelectedIndices={setSelectedIndices}
        projects={projects}
      />

      <div className="mt-6 bg-white p-4 rounded shadow flex flex-col items-center">
        <div
          {...getRootProps()}
          className="w-96 h-48 border-2 border-dashed border-gray-300 p-4 rounded-lg text-center cursor-pointer flex flex-col items-center justify-center"
        >
          <input {...getInputProps()} />
          {isDragActive ? (
            <p className="text-xl text-teal-600">Drop files here...</p>
          ) : (
            <>
              <p className="text-xl font-semibold text-gray-700 mb-1">
                Drag and Drop here
              </p>
              <p className="text-gray-500 mb-2">or</p>
              <button
                type="button"
                className="px-4 py-2 rounded bg-indigo-600 text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-teal-400"
              >
                Select files
              </button>
              <p className="text-sm text-gray-400 mt-2">
                (Images &amp; Videos Only)
              </p>
            </>
          )}
        </div>

        {/* Display upload progress and status */}
        {uploadStatus && (
          <div className="mt-4 w-full max-w-md">
            <p className="text-sm text-gray-700">{uploadStatus}</p>
            {uploadProgress > 0 && <Progress value={uploadProgress} />}
          </div>
        )}

        <button
          onClick={handleSubmitAssets}
          className="mt-4 px-4 py-3 border border-gray-300 rounded-md bg-indigo-600 text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          Submit Assets
        </button>
      </div>
    </div>
  );
}
