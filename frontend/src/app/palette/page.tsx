"use client";

import React, { useCallback, useState, useEffect, useRef } from "react";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";

import { useFileContext, FileMetadata } from "@/app/context/FileContext";
import FileTable from "./components";
import { 
  fetchPaletteAssets, 
  fetchBlobDetails, 
  uploadFileZstd, 
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

  // Helper function to upload file and update blobId
  const uploadAndUpdateBlobId = useCallback(async (fileMeta: FileMetadata): Promise<void> => {
    try {
      const blobId = await uploadFileZstd(fileMeta);
      if (blobId) {
        setFiles((prevFiles) =>
          prevFiles.map((f) =>
            f === fileMeta ? { ...f, blobId } : f
          )
        );
      }
    } catch (error) {
      console.error("Error uploading file:", error);
    }
  }, [setFiles]);

  // Helper function to fetch blob details
  const fetchAndUpdateBlobDetails = useCallback(async (fileMeta: FileMetadata): Promise<void> => {
    if (!fileMeta.blobId) return;
    
    try {
      const details = await fetchBlobDetails(fileMeta.blobId);
      if (details.project) {
        fileMeta.project = details.project;
      }
      if (details.tags && details.tags.length > 0) {
        fileMeta.tags = details.tags;
        fileMeta.tagIds = details.tagIds || [];
        fileMeta.description = details.description || "";
        fileMeta.location = details.location || "";
      }
    } catch (error) {
      console.error("Error fetching blob details:", error);
    }
  }, []);

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
        await fetchAndUpdateBlobDetails(fileMeta);
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

  // Handle file drop
  const onDrop = useCallback(
    async (acceptedFiles: File[]) => {
      for (const file of acceptedFiles) {
        const fileMeta = createFileMetadata(file);
        await processFileMetadata(fileMeta);
        await uploadAndUpdateBlobId(fileMeta);
      }
    },
    [createFileMetadata, processFileMetadata, uploadAndUpdateBlobId]
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

  // File upload component for large files
  const FileUpload = () => {
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [status, setStatus] = useState("");
    const [progress, setProgress] = useState(0);
  
    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
      const file = event.target.files?.[0] || null;
      setSelectedFile(file);
    };
  
    // Create callbacks for the chunked upload
    const createUploadCallbacks = (): UploadProgressCallbacks => ({
      onProgress: (progress: number, status: string) => {
        setStatus(status);
        setProgress(progress);
      },
      onSuccess: () => {
        setStatus("File upload completed successfully");
        setProgress(100);
        
        // Reset file selection
        setSelectedFile(null);
        
        // Refresh the palette assets list to show the newly uploaded file
        const loadAssets = async () => {
          setFiles([]);
          const fetchedFiles = await fetchPaletteAssets();
          setFiles(fetchedFiles);
        };
        loadAssets();
      },
      onError: (error: string) => {
        setStatus(`Error: ${error}`);
      }
    });
    
    const handleFileUpload = async () => {
      if (!selectedFile) {
        alert("Please select a file to upload.");
        return;
      }
  
      // Start the upload process
      setStatus("Starting upload...");
      setProgress(0);
      await uploadFileChunked(selectedFile, createUploadCallbacks());
    };

    return (
      <div className="mt-6 p-4 border rounded bg-gray-50">
        <h2 className="text-lg font-semibold mb-2">Large File Upload</h2>
        <p className="text-sm text-gray-600 mb-3">Upload large files in chunks for better reliability</p>
        
        {status && (
          <div className="mb-3">
            <p className="text-sm text-gray-700">{status}</p>
            {progress > 0 && <Progress value={progress} />}
          </div>
        )}
        
        <div className="flex items-center space-x-4">
          <input 
            type="file" 
            onChange={handleFileChange} 
            className="block w-full text-sm text-gray-500
                      file:mr-4 file:py-2 file:px-4
                      file:rounded-md file:border-0
                      file:text-sm file:font-semibold
                      file:bg-indigo-50 file:text-indigo-700
                      hover:file:bg-indigo-100"
          />
          <Button onClick={handleFileUpload}>
            Upload File
          </Button>
        </div>
      </div>
    );
  };

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

        <button
          onClick={handleSubmitAssets}
          className="mt-4 px-4 py-3 border border-gray-300 rounded-md bg-indigo-600 text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          Submit Assets
        </button>

        <FileUpload />
      </div>
    </div>
  );
}
