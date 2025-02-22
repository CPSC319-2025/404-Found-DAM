"use client";

import { useState, useCallback } from "react";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";
import Search from "../projects/components/Search";
import { useFileContext } from "../FileContext";

export default function UploadPage() {
    const { files, setFiles } = useFileContext();
    const router = useRouter();

    // Accept images & videos only
    const onDrop = useCallback((acceptedFiles: File[]) => {
        setFiles((prevFiles) => [...prevFiles, ...acceptedFiles]);
    }, [setFiles]);

    const { getRootProps, getInputProps, isDragActive } = useDropzone({
        onDrop,
        accept: {
            "image/*": [],
            "video/*": [],
        },
    });

    // Remove a file by its index
    function removeFile(index: number) {
        setFiles((prevFiles) => {
            const updated = [...prevFiles];
            updated.splice(index, 1);
            return updated;
        });
    }

    // Navigate to /palette (could also do uploads here)
    function handleUpload() {
        router.push("/palette");
    }

    return (
        <div className="p-6 min-h-screen">
            {/* Top section with search, etc. */}
            <div className="flex flex-col md:flex-row items-start md:items-center justify-between mb-6">
                <Search />
            </div>

            {/* Main Content */}
            <main className="flex-grow p-6 flex items-center justify-center">
                <div className="bg-white p-8 rounded shadow-md text-center w-full max-w-xl">
                    {/* Drag-and-drop container */}
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

                    {/* Display chosen files (with preview + remove) */}
                    {files.length > 0 && (
                        <div className="mt-4 text-left">
                            <h3 className="font-semibold mb-2">Files:</h3>
                            <ul className="space-y-4">
                                {files.map((file, i) => {
                                    const previewUrl = URL.createObjectURL(file);

                                    return (
                                        <li key={i} className="border p-2 rounded relative">
                                            <p className="font-medium mb-2">
                                                {file.name} ({(file.size / 1024).toFixed(2)} KB)
                                            </p>

                                            {/* Preview Image or Video */}
                                            {file.type.startsWith("image/") && (
                                                <img
                                                    src={previewUrl}
                                                    alt="Preview"
                                                    className="max-w-xs max-h-48 object-cover mb-2"
                                                />
                                            )}
                                            {file.type.startsWith("video/") && (
                                                <video
                                                    src={previewUrl}
                                                    controls
                                                    className="max-w-xs max-h-48 mb-2"
                                                />
                                            )}

                                            {/* Remove File Button */}
                                            <button
                                                onClick={() => removeFile(i)}
                                                className="absolute top-2 right-2 bg-red-500 text-white text-sm px-2 py-1 rounded"
                                            >
                                                Remove
                                            </button>
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
                        Modify Assets
                    </button>
                </div>
            </main>
        </div>
    );
}
