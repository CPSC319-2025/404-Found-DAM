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

  // 2a) Helper function: compress + upload 
  async function uploadFileZstd(fileMeta: FileMetadata) {
    try {
      // Check if file exists
      if (!fileMeta.file) {
        console.error("File object is missing in metadata");
        return;
      }

      // Compress file with Zstandard
      const compressedFile = await compressFileZstd(fileMeta.file);

      // Use native FormData
      // eslint-disable-next-line @typescript-eslint/no-require-imports
      // const FormData = require("form-data");
      const formData = new FormData();
      formData.append("userId", "1"); // or dynamic
      formData.append("name", fileMeta.file.name);
      formData.append("mimeType", fileMeta.file.type); // Use the actual file type
      formData.append("files", compressedFile);

      // Send the request
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/upload?toWebp=true`,
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
          filePath: URL.createObjectURL(file),
          fileName: file.name,
          fileSize,
          mimeType: file.type,
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
          
          // Add timeout for video metadata loading
          const timeoutId = setTimeout(() => {
            console.error(`Timed out loading video metadata for ${file.name}`);
            
            // Use default values for width/height/duration if metadata load fails
            fileMeta.width = 640;
            fileMeta.height = 480;
            fileMeta.duration = 0;
            
            // Add metadata to state
            setFiles((prev) => [...prev, fileMeta]);
            // Immediately upload
            if (fileMeta.file) {
              uploadFileZstd(fileMeta).catch(console.error);
            }
          }, 10000); // 10 second timeout
          
          video.onloadedmetadata = () => {
            clearTimeout(timeoutId);
            fileMeta.width = video.videoWidth;
            fileMeta.height = video.videoHeight;
            fileMeta.duration = Math.floor(video.duration);

            // Add metadata to state
            setFiles((prev) => [...prev, fileMeta]);
            // Immediately upload
            uploadFileZstd(fileMeta).catch(console.error);
          };
          
          video.onerror = () => {
            clearTimeout(timeoutId);
            console.error(`Failed to load video metadata for ${file.name}`);
            
            // Use default values for width/height/duration if metadata load fails
            fileMeta.width = 640;
            fileMeta.height = 480;
            fileMeta.duration = 0;
            
            // Add metadata to state
            setFiles((prev) => [...prev, fileMeta]);
            // Immediately upload
            if (fileMeta.file) {
              uploadFileZstd(fileMeta).catch(console.error);
            }
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
    accept: {
      "image/*": [],
      "video/*": [],
    },
  });

  // 3) Manual button to go to /palette
  async function handleUpload() {
    router.push("/palette"); //after push to palette, the palette page will refresh
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
                    {meta.fileName} - {meta.fileSize}
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
