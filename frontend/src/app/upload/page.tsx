"use client";

import { useFileContext, FileMetadata } from "@/app/FileContext";
import { useDropzone } from "react-dropzone";
import { useCallback } from "react";
import { useRouter } from "next/navigation";
import Search from "../projects/components/Search"; // OPTIONAL, remove if not needed

export default function UploadPage() {
    const { files, setFiles } = useFileContext();
    const router = useRouter();

    // Handle dropping files (images/videos only)
    const onDrop = useCallback((acceptedFiles: File[]) => {
        acceptedFiles.forEach((file) => {
            const fileSize = (file.size / 1024).toFixed(2) + " KB";

            // Construct initial FileMetadata
            const fileMeta: FileMetadata = {
                file,
                fileSize,
                description: "",
                location: "",
                tags: [],
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
        accept: {
            "image/*": [],
            "video/*": [],
        },
    });

    // Navigate to /palette
    function handleUpload() {
        router.push("/palette");
    }

    return (
        <div className="p-6 min-h-screen">
            {/* Optional search bar */}
            <div className="flex flex-col md:flex-row items-start md:items-center justify-between mb-6">
                <Search />
            </div>

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
                                <button className="bg-teal-500 text-white px-4 py-2 rounded hover:bg-teal-600">
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
                                {files.map((meta, i) => {
                                    return (
                                        <li key={i} className="border p-2 rounded">
                                            {meta.file.name} - {meta.fileSize}
                                        </li>
                                    );
                                })}
                            </ul>
                        </div>
                    )}

                    {/* Navigate to /palette */}
                    <button
                        onClick={handleUpload}
                        className="mt-6 px-4 py-2 rounded border border-black text-black hover:bg-gray-100"
                    >
                        Upload to Palette
                    </button>
                </div>
            </main>
        </div>
    );
}
