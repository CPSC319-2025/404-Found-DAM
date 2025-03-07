"use client";

import React, { useCallback, useState, useEffect } from "react";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";

import { useFileContext, FileMetadata } from "@/app/context/FileContext";
import FileTable from "./components";

interface ProjectLog {
  projectID: number;
  projectName: string;
  archivedAt: string;
  admin: string;
}

export default function PalettePage() {
  const router = useRouter();
  const { files, setFiles } = useFileContext();

  // Keep track of which rows are selected in the table
  const [selectedIndices, setSelectedIndices] = useState<number[]>([]);

  // Store real project logs from the Beeceptor endpoint
  const [projects, setProjects] = useState<ProjectLog[]>([]);

  // 1. Fetch the project logs from Beeceptor once on mount
  useEffect(() => {
    async function fetchProjects() {
      try {
        const res = await fetch("https://dennis.free.beeceptor.com/projects/logs");
        if (!res.ok) {
          console.error("Failed to fetch project logs:", res.status);
          return;
        }
        const data = await res.json();
        // data.logs is the array we want
        if (data.logs) {
          setProjects(data.logs);
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
              setFiles((prev) => [...prev, fileMeta]);
            };
            video.src = URL.createObjectURL(file);
          } else {
            // Other file types
            setFiles((prev) => [...prev, fileMeta]);
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
  async function handleUpload() {
    if (selectedIndices.length === 0) {
      alert("No files selected!");
      return;
    }

    // Descending order so removing files doesn't break subsequent indices
    const descendingIndices = [...selectedIndices].sort((a, b) => b - a);

    for (const fileIndex of descendingIndices) {
      const fileMeta = files[fileIndex];

      // Make sure user selected a project
      if (!fileMeta.project) {
        alert(`Please select a project for "${fileMeta.file.name}" first!`);
        continue;
      }

      // Build request - just for demonstration
      const requestBody = {
        projectID: fileMeta.project, // If you store the project as an ID
        fileName: fileMeta.file.name,
      };

      try {
        const res = await fetch("https://dennis.free.beeceptor.com/projects/assign-assets", {
          method: "POST",
          headers: {
            Authorization: "Bearer MY_TOKEN",
            "Content-Type": "application/json",
          },
          body: JSON.stringify(requestBody),
        });

        if (!res.ok) {
          console.error("API call failed:", res.status);
          continue;
        }

        const data = await res.json();
        console.log("Uploaded:", data);

        // Remove from files & from selectedIndices
        removeFile(fileIndex);
        setSelectedIndices((prev) => prev.filter((i) => i !== fileIndex));
      } catch (err) {
        console.error("Error uploading file:", err);
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
              onClick={handleUpload}
              className="mt-4 px-4 py-3 border border-gray-300 rounded-md bg-indigo-600 text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            Upload Assets
          </button>
        </div>
      </div>
  );
}
