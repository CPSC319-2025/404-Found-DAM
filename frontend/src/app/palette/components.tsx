"use client";

import React, { useState, ChangeEvent } from "react";
import { useRouter } from "next/navigation";
import { useFileContext, FileMetadata } from "@/app/FileContext";

type FileTableProps = {
    files: FileMetadata[];
    removeFile: (index: number) => void;
};

export default function FileTable({ files, removeFile }: FileTableProps) {
    const router = useRouter();
    const { setFiles } = useFileContext();
    const [selectedIndices, setSelectedIndices] = useState<number[]>([]);

    // Toggle "Select All"
    function handleSelectAll(e: ChangeEvent<HTMLInputElement>) {
        if (e.target.checked) {
            setSelectedIndices(files.map((_, idx) => idx));
        } else {
            setSelectedIndices([]);
        }
    }

    // Toggle single row selection
    function handleSelectRow(index: number) {
        setSelectedIndices((prev) =>
            prev.includes(index)
                ? prev.filter((i) => i !== index)
                : [...prev, index]
        );
    }

    // Remove a single tag by index on a FileMetadata
    function handleRemoveTag(fileMeta: FileMetadata, tagIndex: number) {
        setFiles((prevFiles) =>
            prevFiles.map((f) =>
                f.file.name === fileMeta.file.name
                    ? { ...f, tags: f.tags.filter((_, i) => i !== tagIndex) }
                    : f
            )
        );
    }

    // Navigate to the editmetadata route (folder: app/palette/editmetadata/page.tsx)
    function handleEditMetadata(fileName: string) {
        router.push(`/palette/editmetadata?file=${encodeURIComponent(fileName)}`);
    }

    return (
        <div className="max-w-[1080px] overflow-auto">
            <table className="min-w-full bg-white border border-gray-200">
                <thead className="bg-gray-100 text-gray-600">
                <tr>
                    <th className="p-3 text-center w-[120px]">
                        <div className="flex items-center justify-center">
                            <input
                                type="checkbox"
                                checked={
                                    selectedIndices.length === files.length && files.length > 0
                                }
                                onChange={handleSelectAll}
                            />
                            <span className="ml-2">Select All</span>
                        </div>
                    </th>
                    <th className="p-3">Preview</th>
                    <th className="p-3">File Name</th>
                    <th className="p-3">File Type</th>
                    <th className="p-3">File Size</th>
                    <th className="p-3">Tags</th>
                    <th className="p-3">Edit Metadata</th>
                    <th className="p-3">Action</th>
                </tr>
                </thead>

                <tbody>
                {files.map((fileMeta, index) => {
                    const rawFile = fileMeta.file; // the actual File object
                    const isImage = rawFile.type.startsWith("image/");
                    const isVideo = rawFile.type.startsWith("video/");

                    let previewUrl: string | null = null;
                    try {
                        previewUrl = URL.createObjectURL(rawFile);
                    } catch (error) {
                        console.error("Failed to create object URL:", error);
                    }

                    return (
                        <tr key={index} className="border-b last:border-none">
                            {/* Row checkbox */}
                            <td className="p-3 text-center">
                                <input
                                    type="checkbox"
                                    checked={selectedIndices.includes(index)}
                                    onChange={() => handleSelectRow(index)}
                                />
                            </td>

                            {/* Preview column */}
                            <td className="p-3">
                                {isImage && previewUrl && (
                                    <img
                                        src={previewUrl}
                                        alt="Preview"
                                        className="max-w-[40px] max-h-[40px] object-cover"
                                    />
                                )}
                                {isVideo && previewUrl && (
                                    <video
                                        src={previewUrl}
                                        controls
                                        className="max-w-[40px] max-h-[40px]"
                                    />
                                )}
                            </td>

                            {/* File Info */}
                            <td className="p-3">{rawFile.name}</td>
                            <td className="p-3">{rawFile.type}</td>
                            <td className="p-3">{fileMeta.fileSize}</td>

                            {/* Tags */}
                            <td className="p-3">
                                {fileMeta.tags.length > 0 ? (
                                    fileMeta.tags.map((tag, tagIdx) => (
                                        <span
                                            key={tagIdx}
                                            className="relative bg-red-400 text-white px-2 py-1 rounded mr-1 inline-flex items-center"
                                        >
                        {tag}
                                            <button
                                                onClick={() => handleRemoveTag(fileMeta, tagIdx)}
                                                className="ml-1 text-xs bg-white text-red-500 rounded-full w-4 h-4 flex items-center justify-center font-bold"
                                            >
                          ✕
                        </button>
                      </span>
                                    ))
                                ) : (
                                    <span>No tags</span>
                                )}
                            </td>

                            {/* Edit Metadata */}
                            <td className="p-3">
                                <button
                                    onClick={() => handleEditMetadata(rawFile.name)}
                                    className="bg-gray-600 text-white px-2 py-0.5 rounded"
                                >
                                    Edit Metadata
                                </button>
                            </td>

                            {/* Remove File */}
                            <td className="p-3">
                                <button
                                    onClick={() => removeFile(index)}
                                    className="bg-red-500 text-white text-sm px-2 py-1 rounded"
                                >
                                    Remove
                                </button>
                            </td>
                        </tr>
                    );
                })}
                </tbody>
            </table>
        </div>
    );
}
