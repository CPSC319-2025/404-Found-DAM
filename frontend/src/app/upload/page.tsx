"use client";

import React, { useCallback, useEffect } from "react";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";

import { useFileContext, FileMetadata } from "@/app/context/FileContext";
// Import your Zstandard compression helper
import { compressFileZstd } from "@/app/palette/compressFileZstd";

export default function UploadPage() {
  const { files, setFiles } = useFileContext();
  const router = useRouter();

  // 1) Automatically redirect to /palette if we already have files
  useEffect(() => {
    if (files.length > 0) {
      router.push("/palette");
    }
  }, [files, router]);

  // 2a) Helper function: compress + upload to your Beeceptor endpoint
  async function uploadFileZstd(fileMeta: FileMetadata) {
    try {
      // Compress file with Zstandard
      const compressedFile = await compressFileZstd(fileMeta.file);

      // Use native FormData
      // eslint-disable-next-line @typescript-eslint/no-require-imports
      // const FormData = require("form-data");
      const formData = new FormData();
      formData.append("userId", "001"); // or dynamic
      formData.append("name", "My Upload Batch");
      formData.append("type", "image");
      formData.append("files", compressedFile);

      // Send the request
      const response = await fetch(
        "https://dennis.free.beeceptor.com/palette/upload1",
        {
          method: "POST",
          headers: {
            Authorization: "Bearer MY_TOKEN",
          },
          body: formData,
        }
      );

      if (!response.ok) {
        throw new Error(`Upload failed with status ${response.status}`);
      }

      const result = await response.json();
      console.log("Upload result:", result);

      // If success: store blobId in that file’s metadata
      if (result?.Details?.length > 0) {
        const detail = result.Details[0]; // or match by filename if needed

        setFiles((prevFiles) =>
          prevFiles.map((f) =>
            f === fileMeta
              ? {
                  ...f,
                  blobId: detail.BlobID, // store the ID returned by the API
                }
              : f
          )
        );
      }
    } catch (err) {
      console.error("Error uploading file:", err);
      // Optionally remove file from context or show error state
    }
  }

  // 2b) Drag-and-drop handling
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

        // For images, read width & height
        if (file.type.startsWith("image/")) {
          const img = new Image();
          img.onload = () => {
            fileMeta.width = img.width;
            fileMeta.height = img.height;

            // Add to context immediately
            setFiles((prev) => [...prev, fileMeta]);

            // Then compress & upload
            uploadFileZstd(fileMeta).catch(console.error);
          };
          img.src = URL.createObjectURL(file);
        }
        // For videos, read width, height, duration
        else if (file.type.startsWith("video/")) {
          const video = document.createElement("video");
          video.preload = "metadata";
          video.onloadedmetadata = () => {
            fileMeta.width = video.videoWidth;
            fileMeta.height = video.videoHeight;
            fileMeta.duration = Math.floor(video.duration);

            setFiles((prev) => [...prev, fileMeta]);
            uploadFileZstd(fileMeta).catch(console.error);
          };
          video.src = URL.createObjectURL(file);
        } else {
          // For other types, just store and then upload
          setFiles((prev) => [...prev, fileMeta]);
          uploadFileZstd(fileMeta).catch(console.error);
        }
      });
    },
    [setFiles]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      "image/*": [],
      "video/*": [],
    },
  });

  // 3) Manual button to go to /palette
  function handleUpload() {
    router.push("/palette");
  }

  return (
    <div className="p-6 min-h-screen">
      <main className="flex-grow p-6 flex items-center justify-center">
        <div className="bg-white p-8 rounded shadow-md text-center w-full max-w-xl">
          <div
            {...getRootProps()}
            className="border-2 border-dashed border-gray-300 p-8 rounded-lg mb-4 cursor-pointer"
          >
            <input {...getInputProps()} />
            {isDragActive ? (
              <p className="text-xl text-teal-600">Drop the files here...</p>
            ) : (
              <>
                <p className="text-xl mb-2">Drag and Drop here</p>
                <p className="text-gray-500 mb-4">or</p>
                <button className="bg-indigo-600 text-white hover:bg-indigo-700 px-4 py-2 rounded">
                  Select files
                </button>
                <p className="text-sm text-gray-400 mt-2">
                  (Images &amp; Videos Only)
                </p>
              </>
            )}
          </div>

          {/* Display chosen files */}
          {files.length > 0 && (
            <div className="mt-4 text-left">
              <h3 className="font-semibold mb-2">Files:</h3>
              <ul className="space-y-2">
                {files.map((meta, i) => (
                  <li key={i} className="border p-2 rounded">
                    {meta.file.name} - {meta.fileSize}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {/* Manual button to jump to /palette */}
          <button
            onClick={handleUpload}
            className="mt-6 px-4 py-2 rounded border border-black bg-indigo-600 text-white hover:bg-indigo-700"
          >
            Upload to Palette
          </button>
        </div>
      </main>
    </div>
  );
}
