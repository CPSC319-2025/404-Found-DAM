"use client";

import React, { useCallback, useState, useEffect, useRef } from "react";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";

import { useFileContext, FileMetadata } from "@/app/context/FileContext";
import FileTable from "./components";
import { compressFileZstd } from "@/app/palette/compressFileZstd";
import { ZstdCodec } from "zstd-codec";

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

export default function PalettePage() {
  const router = useRouter();
  const { files, setFiles } = useFileContext();

  const [selectedIndices, setSelectedIndices] = useState<number[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const didFetchRef = useRef(false);

  useEffect(() => {
    if (didFetchRef.current) return;
    didFetchRef.current = true;
    fetchPaletteAssets();
  }, []);

  function getMimeTypeFromFileName(filename: string): string {
    const extension = filename.split(".").pop()?.toLowerCase();
    //console.log(filename)
    if (!extension) return "unknown";
    const imageExtensions = ["jpg", "jpeg", "png", "gif", "bmp", "webp"];
    const videoExtensions = ["mp4", "webm", "ogg"];
    if (imageExtensions.includes(extension)) {
      return extension === "jpg" ? "image/jpeg" : `image/${extension}`;
    } else if (videoExtensions.includes(extension)) {
      return extension === "mp4" ? "video/mp4" : `video/${extension}`;
    }
    return "unknown";
  }

  function getFilenameFromContentDisposition(disposition: string): string {
    // Example disposition:
    // 'attachment; filename=11.Screenshot 2025-03-18 200722.png.zst; filename*=UTF-8\'\'11.Screenshot 2025-03-18 200722.png.zst'

    // Split on semicolons
    const parts = disposition.split(";");

    for (const part of parts) {
      const trimmed = part.trim();

      // Look for filename=
      if (trimmed.toLowerCase().startsWith("filename=")) {
        // e.g. filename=11.Screenshot 2025-03-18 200722.png.zst
        const val = trimmed
          .substring("filename=".length)
          .trim()
          .replace(/^"|"$/g, "");
        return val;
      }

      // Look for filename*= (UTF-8)
      if (trimmed.toLowerCase().startsWith("filename*=")) {
        // e.g. filename*=UTF-8''11.Screenshot 2025-03-18 200722.png.zst
        const val = trimmed
          .substring("filename*=".length)
          .trim()
          .replace(/^"|"$/g, "");
        // If it starts with UTF-8'', remove that and decode
        if (val.toLowerCase().startsWith("utf-8''")) {
          return decodeURIComponent(val.substring(7));
        }
        return val;
      }
    }

    // Fallback if we don't find anything
    return "defaultFilename.zst";
  }

  // Function to extract the original filename from the server format (BlobID.OriginalFilename.zst)
  function extractOriginalFilename(filename: string): string {
    if (!filename) return "";
    
    // Format: BlobID.OriginalFilename.zst
    const parts = filename.split('.');
    if (parts.length < 3) {
      return filename; // Return as is if not in expected format
    }
    
    // Remove the first part (BlobID) and the last part (.zst)
    parts.shift(); // Remove BlobID
    
    // Check if the last part is "zst" and remove it
    if (parts[parts.length - 1] === "zst") {
      parts.pop(); // Remove .zst extension
    }
    
    // Join the remaining parts to handle filenames with dots
    return parts.join('.');
  }

  // Function to extract BlobID from the server format (BlobID.OriginalFilename.zst)
  function extractBlobId(filename: string): string | undefined {
    if (!filename) return undefined;
    
    const parts = filename.split('.');
    if (parts.length < 2) {
      return undefined;
    }
    
    // The first part should be the blobId
    const blobIdStr = parts[0];
    return blobIdStr;
  }

  // Fetch project and tags for a blob
  async function fetchBlobDetails(blobId: string): Promise<{project?: any, tags?: string[], tagIds?: number[], description?: string, location?: string}> {
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/blob/${blobId}/details`);
      if (!response.ok) {
        console.error(`Failed to fetch details for blob ${blobId}: ${response.status}`);
        return {};
      }
      const data = await response.json();
      
      const projectId = data.project?.projectId.toString();
      
      // Check for different possible property name casings from the backend
      // The C# backend might use "location" or "Location" depending on serialization settings
      let description = data.project?.description || data.project?.Description;
      let location = data.project?.location || data.project?.Location;
      // console.log(data.tagIds);
      return {
        project: projectId,
        tags: data.tags || [],
        tagIds: data.tagIds || [],
        description: description || "",
        location: location || ""
      };
    } catch (error) {
      console.error(`Error fetching details for blob ${blobId}:`, error);
      return {};
    }
  }

  async function fetchPaletteAssets() {
    setFiles([]);
    const formData = new FormData();
    formData.append("UserId", "1"); // Fixed requirement: UserId=1

    try {
      // First, get metadata for all files
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets?decompress=true`, {
        method: "GET",
        headers: {
          Authorization: "Bearer MY_TOKEN",
        },
      });

      if (!response.ok) {
        throw new Error(`Fetch failed with status ${response.status}`);
      }

      const data = await response.json();
      
      console.log("API Response - data.files:", data.files);
      if (data.files.length > 0) {
        console.log("First file properties:", Object.keys(data.files[0]));
      }

      if (!data.files || data.files.length === 0) {
        console.log("No files in palette");
        return;
      }

      // Process each file from metadata
      for (const fileInfo of data.files) {
        // Handle case-insensitive property names
        console.log("Processing file:", fileInfo);
        const blobId = fileInfo.blobId;
        const fileName = fileInfo.fileName;
        
        if (!blobId) {
          console.warn("Missing blobId in file metadata:", fileInfo);
          continue;
        }
        
        // Extract the original filename
        const originalFilename = extractOriginalFilename(fileName);
        
        // Download each file individually with decompression done on the server
        const fileResponse = await fetch(
          `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets/${blobId}?decompress=true`, 
          {
            method: "GET",
            headers: {
              Authorization: "Bearer MY_TOKEN",
            }
          }
        );
        
        if (!fileResponse.ok) {
          console.error(`Failed to fetch file ${blobId}:`, fileResponse.status);
          continue;
        }
        
        // Get the file content
        const blob = await fileResponse.blob();
        
        // Create a File object
        const file = new File(
          [blob],
          originalFilename,
          { type: getMimeTypeFromFileName(originalFilename) }
        );

        const fileSize = (file.size / 1024).toFixed(2) + " KB";
        const fileMeta: FileMetadata = {
          file,
          fileSize,
          description: "",
          location: "",
          tags: [],
          tagIds: [],
          blobId
        };

        // Fetch project and tags information if we have a blobId
        if (blobId) {
          const details = await fetchBlobDetails(blobId);
          if (details.project) {
            fileMeta.project = details.project;
          }
          if (details.tags && details.tags.length > 0) {
            fileMeta.tags = details.tags;
            fileMeta.tagIds = details.tagIds || [];
            fileMeta.description = details.description || "";
            fileMeta.location = details.location || "";
          }
        }

        if (file.type.startsWith("image/")) {
          const img = new Image();
          img.onload = () => {
            fileMeta.width = img.width;
            fileMeta.height = img.height;
            setFiles((prev) => [...prev, fileMeta]);
          };
          img.src = URL.createObjectURL(file);
        } else if (file.type.startsWith("video/")) {
          const video = document.createElement("video");
          video.preload = "metadata";
          video.onloadedmetadata = () => {
            fileMeta.width = video.videoWidth;
            fileMeta.height = video.videoHeight;
            fileMeta.duration = Math.floor(video.duration);
            // Add metadata to state
            setFiles((prev) => [...prev, fileMeta]);
          };
          video.src = URL.createObjectURL(file);
        } else {
          // Other file types:
          setFiles((prev) => [...prev, fileMeta]);
        }
      }
    } catch (err) {
      console.error("Error fetching palette assets:", err);
    }
  }

  async function uploadFileZstd(fileMeta: FileMetadata) {
    try {
      // 3.1 Compress file with Zstandard
      const compressedFile = await compressFileZstd(fileMeta.file);

      // 3.2 Use native FormData (NOT require("form-data"))
      // eslint-disable-next-line @typescript-eslint/no-require-imports
      // const FormData = require("form-data");
      const formData = new FormData();
      formData.append("userId", "001"); // or dynamic
      formData.append("name", fileMeta.file.name);
      formData.append("mimeType", fileMeta.file.type); // Use the file's actual MIME type dynamically
      formData.append("files", compressedFile);

      // 3.3 Send the request
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/upload?toWebp=true`, {
        method: "POST",
        headers: {
          Authorization: "Bearer MY_TOKEN",
        },
        body: formData,
      });

      if (!response.ok) {
        throw new Error(`Upload failed with status ${response.status}`);
      }

      const result = await response.json();
      console.log("Upload result:", result);

      if (result.successfulUploads?.length > 0) {
        const detail = result.successfulUploads[0]; // or find by filename if needed

        console.log(detail.blobID);

        // Update our FileContext so that `fileMeta` gets the blobId
        setFiles((prevFiles) =>
          prevFiles.map((f) =>
            f === fileMeta ? { ...f, blobId: detail.blobID } : f
          )
        );
      }
    } catch (err) {
      console.error("Error uploading file:", err);
    }
  }

  // 1. Fetch the project logs  once on mount
  useEffect(() => {
    async function fetchProjects() {
      try {
        const res = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/projects`);
        if (!res.ok) {
          console.error("Failed to fetch project logs:", res.status);
          return;
        }
        const data = await res.json();
        // data.logs is the array we want
        if (data.fullProjectInfos) {
          setProjects(data.fullProjectInfos);
        } else {
          console.warn("No 'logs' found in response:", data);
        }
      } catch (err) {
        console.error("Error fetching project logs:", err);
      }
    }

    fetchProjects();
  }, []);

  // 2. Remove a file by index
  function removeFile(index: number) {
    setFiles((prev) => {
      const updated = [...prev];

      // Grab the file object we are removing
      const fileToRemove = updated[index];

      // Prepare form data
      // eslint-disable-next-line @typescript-eslint/no-require-imports
      // const FormData = require("form-data");
      const formData = new FormData();
      formData.append("UserId", "1");

      // Use whatever property holds the "blobId" or "name" you need to pass
      // For example, if your file object has "blobId":
      if (fileToRemove.blobId !== undefined) {
        formData.append("Name", fileToRemove.blobId);
      } else {
        console.warn("No blobId found for file:", fileToRemove.file.name);
        // Continue with deletion from UI even if we can't delete from server
      }

      //Make the DELETE request with form data
      fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/asset`, {
        method: "DELETE",
        body: formData,
        // No need to set 'Content-Type'; fetch does it automatically for FormData
      })
        .then((response) => {
          if (!response.ok) {
            throw new Error("Network response was not ok");
          }
          return response.json(); // or response.text() depending on your API
        })
        .then((data) => {
          console.log("Delete successful:", data);
        })
        .catch((error) => {
          console.error("Error deleting:", error);
        });

      // Remove the file from state
      updated.splice(index, 1);
      return updated;
    });
  }

  // 3. Drag-and-drop for images/videos
  const onDrop = useCallback(
    (acceptedFiles: File[]) => {
      acceptedFiles.forEach((file) => {
        const fileSize = (file.size / 1024).toFixed(2) + " KB";
        const fileMeta: FileMetadata = {
          file,
          fileSize,
          description: "",
          location: "",
          tags: [],
          tagIds: [],
        };

        if (file.type.startsWith("image/")) {
          const img = new Image();
          img.onload = () => {
            fileMeta.width = img.width;
            fileMeta.height = img.height;

            // Add metadata to state
            setFiles((prev) => [...prev, fileMeta]);
            // Immediately upload
            uploadFileZstd(fileMeta).catch(console.error);
          };
          img.src = URL.createObjectURL(file);
        } else if (file.type.startsWith("video/")) {
          const video = document.createElement("video");
          video.preload = "metadata";
          video.onloadedmetadata = () => {
            fileMeta.width = video.videoWidth;
            fileMeta.height = video.videoHeight;
            fileMeta.duration = Math.floor(video.duration);

            // Add metadata to state
            setFiles((prev) => [...prev, fileMeta]);
            // Immediately upload
            uploadFileZstd(fileMeta).catch(console.error);
          };
          video.src = URL.createObjectURL(file);
        } else {
          // Other file types:
          setFiles((prev) => [...prev, fileMeta]);
          // Immediately upload
          uploadFileZstd(fileMeta).catch(console.error);
        }
      });
    },
    [setFiles]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { "image/*": [], "video/*": [] },
  });

  // 4. Example: One call per selected file
  async function handleSubmitAssets() {
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

    // 1) Group selected files by their project
    const projectMap: Record<string, string[]> = {};
    //   projectMap[projectID] => [blobIDs]

    selectedIndices.forEach((fileIndex) => {
      const fileMeta = files[fileIndex];

      // Make sure this file has both a blobId and a project ID
      if (!fileMeta.blobId) {
        console.warn("File missing blobId:", fileMeta.file.name);
        return;
      }
      if (!fileMeta.project) {
        console.warn("File missing project ID:", fileMeta.file.name);
        return;
      }

      const projectId = fileMeta.project;
      if (!projectMap[projectId]) {
        projectMap[projectId] = [];
      }
      projectMap[projectId].push(fileMeta.blobId);
    });

    // 2) For each project, hit the PATCH endpoint with the array of blobIDs
    for (const projectId in projectMap) {
      const blobIDs = projectMap[projectId];


      try {
        const response = await fetch(
          `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/${projectId}/submit-assets`,
          {
            method: "PATCH",
            headers: {
              Authorization: "Bearer MY_TOKEN",
              "Content-Type": "application/json",
            },
            body: JSON.stringify({ blobIDs }), // e.g. { "blobIDs": [123, 456] }
          }
        );

        if (!response.ok) {
          console.error("Submit assets failed:", response.status);
          continue;
        }

        

        const data = await response.json();
        console.log("Submission success:", data);

        // 3) Remove these files from our palette state
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
      } catch (err) {
        console.error("Error submitting assets:", err);
      }
    }
  }

  const FileUpload = () => {
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [status, setStatus] = useState("");
    const [progress, setProgress] = useState(0);
  
    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
      const file = event.target.files?.[0] || null;
      setSelectedFile(file);
    };
  
    const handleFileUpload = async () => {
      if (!selectedFile) {
        alert("Please select a file to upload.");
        return;
      }
  
      const chunkSize = 5 * 1024 * 1024; // 5MB chunks
      const totalChunks = Math.ceil(selectedFile.size / chunkSize);
      const chunkProgress = 100 / totalChunks;
      let chunkNumber = 0;
      let start = 0;
      let end = chunkSize;
      let lastResponse: { blobId?: string; message?: string; chunkNumber?: number; totalChunks?: number; fileName?: string } | null = null;
  
      const uploadNextChunk = async () => {
        if (start < selectedFile.size) {
          // Adjust end to not exceed file size
          end = Math.min(start + chunkSize, selectedFile.size);
          
          // Slice the file to get the current chunk
          const chunk = selectedFile.slice(start, end);
          
          // Create form data for this chunk
          const formData = new FormData();
          formData.append("file", chunk, "chunk");
          formData.append("chunkNumber", chunkNumber.toString());
          formData.append("totalChunks", totalChunks.toString());
          formData.append("originalname", selectedFile.name);
  
          try {
            const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/upload/chunk`, {
              method: "POST",
              body: formData,
            });
            
            if (!response.ok) {
              throw new Error(`Server responded with status: ${response.status}`);
            }
            
            const data = await response.json();
            lastResponse = data; // Save the response for later use
            console.log("Chunk upload response:", data);
            
            const temp = `Chunk ${chunkNumber + 1}/${totalChunks} uploaded successfully`;
            setStatus(temp);
            setProgress(Math.min(100, Number(((chunkNumber + 1) * chunkProgress).toFixed(1))));
            
            // Move to next chunk
            chunkNumber++;
            start = end;
            end = start + chunkSize;
            
            // Process next chunk
            await uploadNextChunk();
          } catch (error) {
            console.error("Error uploading chunk:", error);
            setStatus(`Error uploading chunk ${chunkNumber + 1}: ${error instanceof Error ? error.message : String(error)}`);
          }
        } else {
          // All chunks uploaded
          setProgress(100);
          setStatus("File upload completed successfully");
          
          // Reset file selection
          setSelectedFile(null);
          
          // Refresh the palette assets list to show the newly uploaded file
          fetchPaletteAssets();
        }
      };
  
      // Start the upload process
      setStatus("Starting upload...");
      setProgress(0);
      await uploadNextChunk();

      console.log("File upload completed successfully");
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
        projects={projects} // Pass in the array from Beeceptor
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

        {/* Button that uploads each selected file individually */}
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
