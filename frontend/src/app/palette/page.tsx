"use client";

import React, { useCallback, useState } from "react";
import FileTable from "./components";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";
import { useFileContext, FileMetadata } from "@/app/FileContext";

export default function PalettePage() {
  const router = useRouter();
  const { files, setFiles } = useFileContext();

  // Track dropdown open/close
  const [showDropdown, setShowDropdown] = useState(false);
  // Track which project was chosen
  const [selectedProject, setSelectedProject] = useState<string | null>(null);

  // Remove a file by index
  function removeFile(index: number) {
    setFiles((prev) => {
      const updated = [...prev];
      updated.splice(index, 1);
      return updated;
    });
  }

  // Handle dropped files
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

  // When "Upload Assets" is clicked, call the API

  async function handleUpload() {
    // Check if a project is selected (you might store this in state)
    if (!selectedProject) {
      alert("Please select a project first!");
      return;
    }

    // Map each FileMetadata into the object your API expects.
    // Here we generate a random id for each image if not already present.
    const assignedImages = files.map((fileMeta) => {
      // Generate an ID using crypto.randomUUID() if available, otherwise fallback.
      const id =
        (fileMeta as any).id ||
        (typeof crypto !== "undefined" && crypto.randomUUID
          ? crypto.randomUUID()
          : "img-" + Math.random().toString(36).substring(2, 10));

      return {
        id,
        filename: fileMeta.file.name,
        fileSize: fileMeta.fileSize,
        description: fileMeta.description,
        location: fileMeta.location,
        tags: fileMeta.tags,
        width: fileMeta.width,
        height: fileMeta.height,
        duration: fileMeta.duration,
      };
    });

    const requestBody = {
      projectId: selectedProject, // e.g., "proj123"
      assignedImages,
    };

    try {
      const res = await fetch("/projects/assign-images", {
        method: "POST",
        headers: {
          Authorization: "Bearer YOUR_TOKEN_HERE", // Replace with your actual token
          "Content-Type": "application/json",
        },
        body: JSON.stringify(requestBody),
      });

      if (!res.ok) {
        console.error("API call failed:", res.status);
        return;
      }

      const data = await res.json();
      console.log("Upload successful:", data);
      // Optionally navigate or update UI here.
      router.push("/palette");
    } catch (err) {
      console.error("Error in upload:", err);
    }
  }

  // Toggle dropdown
  function handleSelectProject() {
    setShowDropdown((prev) => !prev);
  }

  // User picks a project from dropdown
  function handleProjectChoice(projectName: string) {
    setSelectedProject(projectName);
    setShowDropdown(false);
    console.log("Selected project:", projectName);
  }

  // Optional: go back
  function handleGoBack() {
    router.push("/upload");
  }

  return (
    <div className="p-2 min-h-screen">
      <h1 className="text-2xl font-bold mb-4 text-gray-600">Palette</h1>

      {/* Table Container */}
      <div className="p-0 relative">
        <FileTable files={files} removeFile={removeFile} />

        {/* Button + Project Name Row */}
        <div className="mt-4 flex items-center w-full">
          {/* Left Column: Button + Dropdown */}
          <div className="basis-1/3 relative">
            <button
              onClick={handleSelectProject}
              className="bg-slate-200 px-4 py-2 rounded hover:bg-slate-300"
            >
              Select Project
            </button>

            {showDropdown && (
              <div className="absolute left-0 mt-2 w-40 bg-white border border-gray-300 rounded shadow">
                <div
                  className="px-3 py-2 hover:bg-gray-100 cursor-pointer"
                  onClick={() => handleProjectChoice("Project1")}
                >
                  Project1
                </div>
                <div
                  className="px-3 py-2 hover:bg-gray-100 cursor-pointer"
                  onClick={() => handleProjectChoice("Project2")}
                >
                  Project2
                </div>
                <div
                  className="px-3 py-2 hover:bg-gray-100 cursor-pointer"
                  onClick={() => handleProjectChoice("Project3")}
                >
                  Project3
                </div>
              </div>
            )}
          </div>

          {/* Middle Column: Centered Selected Project */}
          <div className="basis-1/3 text-center">
            {selectedProject ? (
              <span className="text-gray-700 font-bold">
                Selected Project: {selectedProject}
              </span>
            ) : (
              <span className="text-gray-400">No project selected</span>
            )}
          </div>

          {/* Right Column: (empty) or add another button */}
          <div className="basis-1/3 text-right" />
        </div>
      </div>

      {/* Drag-and-Drop area (optional) */}
      <div className="flex-grow p-6 flex items-center justify-center">
        <div className="bg-white p-8 rounded shadow-md text-center w-full max-w-xl">
          <div
            {...getRootProps()}
            className="border-2 border-dashed border-gray-300 p-8 rounded-lg mb-4 cursor-pointer"
          >
            <input {...getInputProps()} />
            {isDragActive ? (
              <p className="text-xl text-teal-600">Drop files here...</p>
            ) : (
              <>
                <p className="text-xl mb-2">Drag and Drop here</p>
                <p className="text-gray-500 mb-4">or</p>
                <button className="bg-teal-500 text-white px-4 py-2 rounded hover:bg-teal-600">
                  Select files
                </button>
                <p className="text-sm text-gray-400 mt-2">
                  (Images &amp; Videos Only)
                </p>
              </>
            )}
          </div>

          <button
            onClick={handleUpload}
            className="mt-6 px-4 py-2 rounded border border-black text-black hover:bg-gray-100"
          >
            Upload Assets
          </button>
        </div>
      </div>
    </div>
  );
}
