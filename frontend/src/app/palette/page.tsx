"use client";

import React, { useCallback } from "react";
import FileTable from "./components";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";
import { useFileContext, FileMetadata } from "@/app/FileContext";

export default function PalettePage() {
    const router = useRouter();
    const { files, setFiles } = useFileContext();

    // Remove by index
    function removeFile(index: number) {
        setFiles((prev) => {
            const updated = [...prev];
            updated.splice(index, 1);
            return updated;
        });
    }

    // Accept dropped files (optional)
    const onDrop = useCallback((acceptedFiles: File[]) => {
        acceptedFiles.forEach((file) => {
            const fileSize = (file.size / 1024).toFixed(2) + " KB";

            // Construct initial FileMetadata
            const fileMeta: FileMetadata = {
                file,
                fileSize,
                description: "",
                location: "",
                tags: ["red", 'blue'],
            };

            if (file.type.startsWith("image/")) {
                // Load image to get width & height
                const img = new Image();
                img.onload = () => {
                    fileMeta.width = img.width;
                    fileMeta.height = img.height;
                    setFiles((prev) => [...prev, fileMeta]);
                };
                img.src = URL.createObjectURL(file);
            } else if (file.type.startsWith("video/")) {
                // Load video to get duration, width & height
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
                // Fallback if neither image nor video
                setFiles((prev) => [...prev, fileMeta]);
            }
        });
    }, [setFiles]);


    const { getRootProps, getInputProps, isDragActive } = useDropzone({
        onDrop,
        accept: { "image/*": [], "video/*": [] },
    });

    function handleUpload() {
        // Example: call some /api route
        fetch("/api/upload-all", { method: "POST" })
            .then((res) => {
                if (!res.ok) console.error("API call failed");
                else console.log("API call successful");
            })
            .catch((err) => console.error("Error:", err))
            .finally(() => router.push("/palette"));
    }

    function handleGoBack() {
        router.push("/upload");
    }

    return (
        <div className="p-2 min-h-screen">
            <h1 className="text-2xl font-bold mb-4 text-gray-600">Palette</h1>

            {/* File Table */}
            <div className="p-0">
                <FileTable files={files} removeFile={removeFile} />
            </div>

            {/* Optional drag-and-drop area */}
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
                                <p className="text-sm text-gray-400 mt-2">(Images &amp; Videos Only)</p>
                            </>
                        )}
                    </div>

                    <button
                        onClick={handleUpload}
                        className="mt-6 px-4 py-2 rounded border border-black text-black hover:bg-gray-100"
                    >
                        Upload Assets
                    </button>
                    {/*<button*/}
                    {/*    onClick={handleGoBack}*/}
                    {/*    className="mt-6 ml-4 px-4 py-2 rounded border border-black text-black hover:bg-gray-100"*/}
                    {/*>*/}
                    {/*    Back to Upload*/}
                    {/*</button>*/}
                </div>
            </div>
        </div>
    );
}
