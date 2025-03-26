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

// Utility to format file size
const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
  if (bytes < 1024 * 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  return (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB';
};

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
      if (fileMeta.mimeType.startsWith("image/")) {
        const img = new Image();
        img.onload = () => {
          fileMeta.width = img.width;
          fileMeta.height = img.height;
          setFiles((prev) => [...prev, fileMeta]);
          resolve();
        };
        img.onerror = () => {
          console.error(`Failed to load image metadata for ${fileMeta.fileName}`);
          setFiles((prev) => [...prev, fileMeta]);
          resolve();
        };
        img.src = fileMeta.filePath;
      } else if (fileMeta.mimeType.startsWith("video/")) {
        const video = document.createElement("video");
        video.preload = "metadata";
        
        // Add timeout for video metadata loading
        const timeoutId = setTimeout(() => {
          console.error(`Timed out loading video metadata for ${fileMeta.fileName}`);
          setFiles((prev) => [...prev, fileMeta]);
          resolve();
        }, 10000); // 10 second timeout
        
        video.onloadedmetadata = () => {
          clearTimeout(timeoutId);
          fileMeta.width = video.videoWidth;
          fileMeta.height = video.videoHeight;
          fileMeta.duration = Math.floor(video.duration);
          setFiles((prev) => [...prev, fileMeta]);
          resolve();
        };
        
        video.onerror = () => {
          clearTimeout(timeoutId);
          console.error(`Failed to load video metadata for ${fileMeta.fileName}`);
          setFiles((prev) => [...prev, fileMeta]);
          resolve();
        };
        
        video.src = fileMeta.filePath;
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

  // Load assets on initial mount and clean up on unmount
  useEffect(() => {
    if (didFetchRef.current) return;
    didFetchRef.current = true;
    
    const loadAssets = async () => {
      setFiles([]);
      const fetchedFiles = await fetchPaletteAssets();
      
      // Add each file to the state directly
      for (const fileMeta of fetchedFiles) {
        // No need to process file metadata separately - the server URLs are already set up
        setFiles(prev => [...prev, fileMeta]);
        
        // Fetch additional details if needed
        if (fileMeta.blobId) {
          await fetchAndUpdateBlobDetails(fileMeta.blobId);
        }
      }
    };
    
    loadAssets();

    // Cleanup function to revoke any object URLs created for files
    return () => {
      // Get the current files at cleanup time
      const currentFiles = files; // Capture current value of files
      currentFiles.forEach(fileMeta => {
        if (fileMeta.file && fileMeta.filePath) {
          try {
            URL.revokeObjectURL(fileMeta.filePath);
          } catch (err) {
            console.error(`Failed to revoke object URL for ${fileMeta.fileName}:`, err);
          }
        }
      });
    };
  }, [setFiles, fetchAndUpdateBlobDetails]);

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
      file, // Keep temporarily for upload
      filePath: URL.createObjectURL(file), // Temporary URL during upload
      fileName: file.name,
      fileSize,
      description: "",
      location: "",
      tags: [],
      tagIds: [],
      mimeType: file.type,
    };
  }, []);

  // Create callbacks for the chunked upload
  const createUploadCallbacks = useCallback((file: File, fileMeta: FileMetadata): UploadProgressCallbacks => ({
    onProgress: (progress: number, status: string) => {
      setUploadStatus(status || `Uploading ${file.name}: ${progress}%`);
      setUploadProgress(progress);
    },
    onSuccess: (blobId?: string, url?: string) => {
      setUploadStatus(`File ${file.name} uploaded successfully`);
      setUploadProgress(100);
      
      // Update file metadata with permanent URL and blobId
      const updatedMeta: FileMetadata = {
        ...fileMeta,
        file: undefined, // Remove the file object to save memory
        filePath: url || fileMeta.filePath, // Use permanent URL from server or keep existing
        blobId: blobId || fileMeta.blobId // Use new blobId or keep existing
      };
      
      // If we had a temporary object URL, revoke it
      if (fileMeta.file) {
        try {
          URL.revokeObjectURL(fileMeta.filePath);
        } catch (err) {
          console.error("Failed to revoke object URL:", err);
        }
      }
      
      // Now process the file metadata to get dimensions, etc.
      processFileMetadata(updatedMeta)
        .then(() => {
          // After processing and adding to state, fetch additional blob details
          if (blobId) {
            return fetchAndUpdateBlobDetails(blobId);
          }
        })
        .catch(err => {
          console.error("Error processing file metadata:", err);
        })
        .finally(() => {
          // Clear status after a delay
          setTimeout(() => {
            setUploadStatus("");
            setUploadProgress(0);
          }, 50);
        });
    },
    onError: (error: string) => {
      setUploadStatus(`Error uploading ${file.name}: ${error}`);
      
      // Clear error after a delay
      setTimeout(() => {
        setUploadStatus("");
        setUploadProgress(0);
      }, 5000);
    }
  }), [setUploadStatus, setUploadProgress, processFileMetadata, fetchAndUpdateBlobDetails]);

  // Handle file drop with chunked upload
  const onDropFiles = useCallback(async (acceptedFiles: File[]) => {
    setUploadStatus("Uploading files...");
    setUploadProgress(0);
    
    // Create an array to store successfully uploaded files
    const uploadedFiles: FileMetadata[] = [];
    
    // Process each file in sequence to avoid overwhelming the server
    for (let i = 0; i < acceptedFiles.length; i++) {
      const file = acceptedFiles[i];
      const fileNumber = i + 1;
      const totalFiles = acceptedFiles.length;
      
      try {
        setUploadStatus(`Uploading file ${fileNumber} of ${totalFiles}: ${file.name}`);
        
        // Calculate file size string
        const fileSizeString = formatFileSize(file.size);
        
        // Create a temporary object URL for the file (for preview during upload)
        const objectUrl = URL.createObjectURL(file);
        
        // Prepare the file metadata (temporary, will be updated after upload)
        const tempMetadata: FileMetadata = {
          file: file,
          filePath: objectUrl,
          fileName: file.name,
          fileSize: fileSizeString,
          description: "",
          location: "",
          tags: [],
          tagIds: [],
          mimeType: file.type || "application/octet-stream"
        };
        
        // For large files, use chunked upload
        const isLargeFile = file.size > 5 * 1024 * 1024; // 5MB threshold
        
        // Setup progress callbacks
        const progressCallbacks: UploadProgressCallbacks = {
          onProgress: (progress: number, status: string) => {
            setUploadProgress(progress);
            setUploadStatus(status || `Uploading ${file.name}: ${progress}%`);
          },
          onSuccess: (blobId?: string, url?: string) => {
            setUploadStatus(`File ${file.name} uploaded successfully`);
            setUploadProgress(100);
          },
          onError: (error: string) => {
            console.error(`Upload error: ${error}`);
            setUploadStatus(`Error uploading ${file.name}: ${error}`);
          }
        };
        
        // Upload the file
        const uploadResult = await uploadFileChunked(
          file, 
          isLargeFile,
          progressCallbacks
        );
        
        // Check upload success
        if (uploadResult.success && uploadResult.data) {
          const { blobId, url } = uploadResult.data;
          
          // Update metadata with the server response
          const updatedMetadata: FileMetadata = {
            ...tempMetadata,
            blobId: blobId,
            filePath: url || objectUrl,
          };
          
          // Add to our array of uploaded files
          uploadedFiles.push(updatedMetadata);
          
          // Process metadata like dimensions if it's a media file
          await processFileMetadata(updatedMetadata);
        } else {
          console.error("Upload failed:", uploadResult.error || "Unknown error");
          setUploadStatus(`Failed to upload ${file.name}`);
        }
      } catch (err) {
        console.error(`Error uploading ${file.name}:`, err);
        setUploadStatus(`Error uploading ${file.name}`);
      }
      
      // Update progress for the whole batch
      setUploadProgress((fileNumber / totalFiles) * 100);
    }
    
    // All uploads completed
    if (uploadedFiles.length > 0) {
      setUploadStatus(`Successfully uploaded ${uploadedFiles.length} file(s)`);
    } else {
      setUploadStatus("No files were uploaded successfully");
    }
    
    // Reset progress after a delay
    setTimeout(() => {
      setUploadStatus("");
      setUploadProgress(0);
    }, 3000);
  }, [processFileMetadata]);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop: onDropFiles,
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
        console.warn(`File missing blobId or project ID: ${fileMeta.fileName}`);
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
