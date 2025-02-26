"use client";

import React, { useCallback, useState } from "react";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";
import FileTable from "./components"; // assumes FileTable is in a file named components.tsx (adjust the path as needed)
import { useFileContext, FileMetadata } from "@/app/FileContext";

export default function PalettePage() {
  const router = useRouter();
  const { files, setFiles } = useFileContext();

  // Track dropdown open/close & selected project
  const [showDropdown, setShowDropdown] = useState(false);
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
    if (!selectedProject) {
      alert("Please select a project first!");
      return;
    }

    const assignedImages = files.map((fileMeta) => {
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
      projectId: selectedProject,
      assignedImages,
    };

    try {
      const res = await fetch("/projects/assign-images", {
        method: "POST",
        headers: {
          Authorization: "Bearer YOUR_TOKEN_HERE",
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
      router.push("/palette");
    } catch (err) {
      console.error("Error in upload:", err);
    }
  }

  function handleSelectProject() {
    setShowDropdown((prev) => !prev);
  }

  function handleProjectChoice(projectName: string) {
    setSelectedProject(projectName);
    setShowDropdown(false);
  }

  function handleGoBack() {
    router.push("/upload");
  }

  return (
    <div className="p-6 min-h-screen">
      {/*<h1 className="text-3xl font-bold mb-6 text-gray-700">Palette</h1>*/}

      <div className="flex items-center justify-between mb-6 bg-white p-4 rounded shadow">
        {/* Centered selected project name */}
        <div className="flex-1 text-center">
          {selectedProject ? (
            <span className="text-2xl font-extrabold text-gray-700">
              {selectedProject}
            </span>
          ) : (
            <span className="text-lg text-gray-400">No project selected</span>
          )}
        </div>

        {/* Right-aligned "Select Project" button + dropdown */}
        <div className="relative w-44 ">
          <button
            onClick={handleSelectProject}
            className="inline-block h-12 w-full px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            Select Project
          </button>
          {showDropdown && (
            <div className="absolute top-full right-0 mt-1 w-full bg-white border border-gray-300 rounded shadow z-10">
              <div
                className="px-3 py-2 hover:bg-gray-100 cursor-pointer"
                onClick={() => handleProjectChoice("Project 1")}
              >
                Project 1
              </div>
              <div
                className="px-3 py-2 hover:bg-gray-100 cursor-pointer"
                onClick={() => handleProjectChoice("Project 2")}
              >
                Project 2
              </div>
              <div
                className="px-3 py-2 hover:bg-gray-100 cursor-pointer"
                onClick={() => handleProjectChoice("Project 3")}
              >
                Project 3
              </div>
            </div>
          )}
        </div>
      </div>

      {/* File list table */}
      <FileTable files={files} removeFile={removeFile} />

      {/* Drag-and-Drop area & Upload button */}
      <div className="mt-6 bg-white p-4 rounded shadow flex flex-col items-center">
        {/* Drop zone with fixed width/height in the center */}
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

        {/* Upload button at the bottom */}
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
