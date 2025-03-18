"use client";

import React, { useCallback, useState, useEffect } from "react";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";

import { useFileContext, FileMetadata } from "@/app/context/FileContext";
import FileTable from "./components";
import ZSTDWorker from 'zstd-codec';
import {compressFileZstd} from "@/app/palette/compressFileZstd";

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

  // Keep track of which rows are selected in the table
  const [selectedIndices, setSelectedIndices] = useState<number[]>([]);

  // Store real project logs from the Beeceptor endpoint
  const [projects, setProjects] = useState<Project[]>([]);


  const [isModalOpen, setIsModalOpen] = useState(false);

  async function uploadFileZstd(fileMeta: FileMetadata) {
    try {
      // 3.1 Compress file with Zstandard
      const compressedFile = await compressFileZstd(fileMeta.file);

      // 3.2 Use native FormData (NOT require("form-data"))
      const FormData = require("form-data");
      const formData = new FormData();
      formData.append("userId", "001"); // or dynamic
      formData.append("name", "My Upload Batch");
      formData.append("type", "image");
      formData.append("files", compressedFile);

      // 3.3 Send the request
      const response = await fetch("https://dennis.free.beeceptor.com/palette/upload1", {
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

      if (result?.Details?.length > 0) {
        const detail = result.Details[0]; // or find by filename if needed

        // Update our FileContext so that `fileMeta` gets the blobId
        setFiles((prevFiles) =>
            prevFiles.map((f) =>
                f === fileMeta
                    ? { ...f, blobId: detail.BlobID }
                    : f
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
        const res = await fetch("https://dennis.free.beeceptor.com/projects");
        if (!res.ok) {
          console.error("Failed to fetch project logs:", res.status);
          return;
        }
        const data = await res.json();
        // data.logs is the array we want
        if (data.projects) {
          setProjects(data.projects);
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

    // 1) Group selected files by their project
    const projectMap: Record<string, number[]> = {};
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
            `https://dennis.free.beeceptor.com/palette/${projectId}/submit-assets`,
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
              return f.project !== projectId || !f.blobId || !blobIDs.includes(f.blobId);
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
            Upload Assets
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
