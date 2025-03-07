"use client";

import { useFileContext, FileMetadata } from "@/app/context/FileContext";
import { useDropzone } from "react-dropzone";
import React, { useCallback } from "react";
import { useRouter } from "next/navigation";

export default function UploadPage() {
  const { files, setFiles } = useFileContext();
  const router = useRouter();

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

          // If it's an image, read width & height
          if (file.type.startsWith("image/")) {
            const img = new Image();
            img.onload = () => {
              fileMeta.width = img.width;
              fileMeta.height = img.height;
              setFiles((prev) => [...prev, fileMeta]);
            };
            img.src = URL.createObjectURL(file);
          }
          // If it's a video, read width/height/duration
          else if (file.type.startsWith("video/")) {
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
            // Just store file if unknown
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

  function handleUpload() {
    // When clicked, *explicitly* navigate to /palette
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

            {/* Upload to palette button */}
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
