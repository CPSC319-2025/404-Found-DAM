"use client";

import { useFileContext, FileMetadata } from "@/app/FileContext";
import { useDropzone } from "react-dropzone";
import { useCallback } from "react";
import { useRouter } from "next/navigation";
import Search from "../projects/components/Search"; // OPTIONAL, remove if not needed

export default function UploadPage() {
    const { files, setFiles } = useFileContext();
    const router = useRouter();

    // Debug: log current files on every render
    console.log("Current files in context:", files);

    // Handle dropping files (images/videos only) with debugging
    const onDrop = useCallback(
        (acceptedFiles: File[]) => {
            console.log("Files dropped:", acceptedFiles);
            acceptedFiles.forEach((file) => {
                console.log("Processing file:", file.name, file.type);
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
                        console.log(
                            "Image loaded:",
                            file.name,
                            "width:",
                            img.width,
                            "height:",
                            img.height
                        );
                        fileMeta.width = img.width;
                        fileMeta.height = img.height;
                        setFiles((prev) => {
                            const updated = [...prev, fileMeta];
                            console.log("Updated files after image:", updated);
                            return updated;
                        });
                    };
                    img.onerror = (err) => {
                        console.error("Error loading image:", file.name, err);
                    };
                    img.src = URL.createObjectURL(file);
                    console.log("Image src set for:", file.name);
                } else if (file.type.startsWith("video/")) {
                    const video = document.createElement("video");
                    video.preload = "metadata";
                    video.onloadedmetadata = () => {
                        console.log(
                            "Video metadata loaded:",
                            file.name,
                            "width:",
                            video.videoWidth,
                            "height:",
                            video.videoHeight,
                            "duration:",
                            video.duration
                        );
                        fileMeta.width = video.videoWidth;
                        fileMeta.height = video.videoHeight;
                        fileMeta.duration = Math.floor(video.duration);
                        setFiles((prev) => {
                            const updated = [...prev, fileMeta];
                            console.log("Updated files after video:", updated);
                            return updated;
                        });
                    };
                    video.onerror = (err) => {
                        console.error("Error loading video metadata:", file.name, err);
                    };
                    video.src = URL.createObjectURL(file);
                    console.log("Video src set for:", file.name);
                } else {
                    console.warn("File type not supported:", file.type);
                    setFiles((prev) => {
                        const updated = [...prev, fileMeta];
                        console.log("Updated files after fallback:", updated);
                        return updated;
                    });
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

    // Navigate to /palette
    function handleUpload() {
        console.log("Navigating to palette with files:", files);
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
                            <p className="text-xl text-teal-600">
                                Drop the files here...
                            </p>
                        ) : (
                            <>
                                <p className="text-xl mb-2">Drag and Drop here</p>
                                <p className="text-gray-500 mb-4">or</p>
                                <button className="bg-indigo-600 text-white hover:bg-indigo-700 px-4 py-2 rounded ">
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

                    {/* Navigate to /palette */}
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
