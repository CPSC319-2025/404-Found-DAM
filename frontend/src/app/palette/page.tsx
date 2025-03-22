"use client";

import React, { useCallback, useState, useEffect, useRef } from "react";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";

import { useFileContext, FileMetadata } from "@/app/context/FileContext";
import FileTable from "./components";
import { ZstdCodec } from "zstd-codec";
import { compressFileZstd } from "@/app/palette/compressFileZstd";
import JSZip from "jszip";
import { decompressSync } from "zstd.ts";

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
    // Format: BlobID.OriginalFilename.zst
    const parts = filename.split('.');
    if (parts.length < 3) {
      return filename; // Return as is if not in expected format
    }
    
    // Remove the first part (BlobID) and the last part (.zst)
    parts.shift(); // Remove BlobID
    parts.pop(); // Remove .zst extension
    
    // Join the remaining parts to handle filenames with dots
    return parts.join('.');
  }

  // Function to extract BlobID from the server format (BlobID.OriginalFilename.zst)
  function extractBlobId(filename: string): string | undefined {
    const parts = filename.split('.');
    if (parts.length < 2) {
      return undefined;
    }
    
    const blobIdStr = parts[0];
    return blobIdStr && blobIdStr.length > 0 ? blobIdStr : undefined;
  }

  // Fetch project and tags for a blob
  async function fetchBlobDetails(blobId: string): Promise<{project?: any, tags?: string[], description?: string, location?: string}> {
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
      
      return {
        project: projectId,
        tags: data.tags || [],
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
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    // const FormData = require("form-data");
    const formData = new FormData();
    formData.append("UserId", "1"); // Fixed requirement: UserId=1

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets`, {
      method: "GET",
      headers: {
        Authorization: "Bearer MY_TOKEN",
      },
      // body: formData,
    });

    if (!response.ok) {
      throw new Error(`Fetch failed with status ${response.status}`);
    }

    console.log(response);

    const blob = await response.blob();
    const contentType = response.headers.get("content-type");

    // Helper to decompress using ZstdCodec
    const decompressZstd = async (data: Uint8Array): Promise<Uint8Array> => {
      return new Promise((resolve, reject) => {
        ZstdCodec.run((zstd: any) => {
          try {
            const simple = new zstd.Simple();
            const result = simple.decompress(data);
            resolve(result);
          } catch (error) {
            reject(error);
          }
        });
      });
    };

    if (contentType === "application/zstd") {
      const fileContent = new Uint8Array(await blob.arrayBuffer());
      const decompressed = await decompressZstd(fileContent);

      const contentDisposition = response.headers.get("Content-Disposition");
      let filename = "defaultFilename.zst";
      if (contentDisposition) {
        filename = getFilenameFromContentDisposition(contentDisposition);
      }

      // Extract the original filename and blobId from the server format
      const originalFilename = extractOriginalFilename(filename);
      const blobId = extractBlobId(filename);
      
      const file = new File(
        [decompressed],
        originalFilename, // Use the original filename without BlobID and .zst
        { type: getMimeTypeFromFileName(originalFilename) }
      );

      const fileSize = (file.size / 1024).toFixed(2) + " KB";
      const fileMeta: FileMetadata = {
        file,
        fileSize,
        description: "",
        location: "",
        tags: [],
        blobId // Store the blobId extracted from the filename
      };

      // Fetch project and tags information if we have a blobId
      if (blobId) {
        const details = await fetchBlobDetails(blobId);
        if (details.project) {
          fileMeta.project = details.project;
        }
        if (details.tags && details.tags.length > 0) {
          fileMeta.tags = details.tags;
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
    } else if (contentType === "application/zip") {
      const zip = await JSZip.loadAsync(await blob.arrayBuffer());
      // const fileContents: FileMetadata[] = [];
      // console.log("zip");
      await Promise.all(
        Object.keys(zip.files).map(async (filename) => {
          const fileData = await zip.files[filename].async("uint8array");
          const decompressedData = await decompressZstd(fileData);

          // Extract the original filename and blobId from the server format
          const originalFilename = extractOriginalFilename(filename);
          const blobId = extractBlobId(filename);
          
          const file = new File(
            [decompressedData],
            originalFilename, // Use the original filename without BlobID and .zst
            { type: getMimeTypeFromFileName(originalFilename) }
          );

          const fileSize = (file.size / 1024).toFixed(2) + " KB";
          const fileMeta: FileMetadata = {
            file,
            fileSize,
            description: "",
            location: "",
            tags: [],
            blobId // Store the blobId extracted from the filename
          };

          // Fetch project and tags information if we have a blobId
          if (blobId) {
            const details = await fetchBlobDetails(blobId);
            if (details.project) {
              fileMeta.project = details.project;
            }
            if (details.tags && details.tags.length > 0) {
              fileMeta.tags = details.tags;
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
        })
      );
    } else {
      console.error("Unexpected content type:", contentType);
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
      formData.append("name", "My Upload Batch");
      formData.append("type", file.type);
      formData.append("files", compressedFile);

      // 3.3 Send the request
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/upload`, {
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

      if (result?.length > 0) {
        const detail = result[0]; // or find by filename if needed

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

  // 1. Fetch the project logs from Beeceptor once on mount
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
        formData.append("Name", fileToRemove.blobId.toString());
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

    for (let fileIndex of selectedIndices) {
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
    }

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
            body: JSON.stringify({ blobIDs }), // e.g. { "blobIDs": ["123", "456"] }
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

        {/*TODO*/}
        {/*<button*/}
        {/*    onClick={handleUpload}*/}
        {/*    className="mt-4 px-4 py-3 border border-gray-300 rounded-md bg-indigo-600 text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-blue-500"*/}
        {/*>*/}
        {/*  Select Files to Upload*/}
        {/*</button>*/}
      </div>
    </div>
  );
}
