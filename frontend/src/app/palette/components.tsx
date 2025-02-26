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


    // State to manage dropdown for projects
    const [showDropdowns, setShowDropdowns] = useState<boolean[]>(new Array(files.length).fill(false));
    const [selectedProjects, setSelectedProjects] = useState<string[]>(new Array(files.length).fill(""));


    function handleSelectAll(e: ChangeEvent<HTMLInputElement>) {
        if (e.target.checked) {
            setSelectedIndices(files.map((_, idx) => idx));
        } else {
            setSelectedIndices([]);
        }
    }

    function handleSelectRow(index: number) {
        setSelectedIndices((prev) =>
            prev.includes(index)
                ? prev.filter((i) => i !== index)
                : [...prev, index]
        );
    }

    function handleRemoveTag(fileMeta: FileMetadata, tagIndex: number) {
        // Remove a specific tag from fileMeta
        setFiles((prevFiles) =>
            prevFiles.map((f) =>
                f.file.name === fileMeta.file.name
                    ? { ...f, tags: f.tags.filter((_, i) => i !== tagIndex) }
                    : f
            )
        );
    }

    function handleEditMetadata(rawFileName: string) {
        router.push(`/palette/editmetadata?file=${encodeURIComponent(rawFileName)}`);
    }

    function toggleDropdown(index: number) {
        const updatedDropdowns = [...showDropdowns];
        updatedDropdowns[index] = !updatedDropdowns[index];
        setShowDropdowns(updatedDropdowns);
    }

    // Handle project selection
    function handleProjectSelect(index: number, projectName: string) {
        const updatedProjects = [...selectedProjects];
        updatedProjects[index] = projectName;
        setSelectedProjects(updatedProjects);
        setShowDropdowns((prev) => prev.map((_, i) => (i === index ? false : _))); // Close dropdown after selection
    }


    return (
        <div className=" overflow-auto">
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
                    <th className="p-3">Projects</th>
                    <th className="p-3">Action</th>
                </tr>
                </thead>
                <tbody>
                {files.map((fileMeta, index) => {
                    const rawFile = fileMeta.file;
                    const displayName = rawFile.name;
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
                            {/* Checkbox */}
                            <td className="p-3 text-center">
                                <input
                                    type="checkbox"
                                    checked={selectedIndices.includes(index)}
                                    onChange={() => handleSelectRow(index)}
                                />
                            </td>

                            {/* Preview */}
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

                            {/* File name (custom) */}
                            <td className="p-3">{displayName}</td>

                            {/* File type */}
                            <td className="p-3">{rawFile.type}</td>

                            {/* File size */}
                            <td className="p-3">{fileMeta.fileSize}</td>

                            {/* Tags + remove tag buttons */}
                            <td className="p-3">
                                {fileMeta.tags.length > 0 ? (
                                    fileMeta.tags.map((tag, tagIndex) => (
                                        <span
                                            key={tagIndex}
                                            className="relative bg-red-400 text-white px-2 py-1 rounded mr-1 inline-flex items-center"
                                        >
                        {tag}
                                            <button
                                                onClick={() => handleRemoveTag(fileMeta, tagIndex)}
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

                             {/* Project */}
                             <td className="p-3">
                                    <button
                                        onClick={() => toggleDropdown(index)}
                                        className="bg-gray-600 text-white px-2 py-0.5 rounded"
                                    >
                                        {selectedProjects[index] || "Select Project"}
                                    </button>

                                    {showDropdowns[index] && (
                                        <div className="absolute bg-white border border-gray-300 rounded mt-2">
                                            <div
                                                className="px-3 py-2 hover:bg-gray-100 cursor-pointer"
                                                onClick={() => handleProjectSelect(index, "Project 1")}
                                            >
                                                Project 1
                                            </div>
                                            <div
                                                className="px-3 py-2 hover:bg-gray-100 cursor-pointer"
                                                onClick={() => handleProjectSelect(index, "Project 2")}
                                            >
                                                Project 2
                                            </div>
                                            <div
                                                className="px-3 py-2 hover:bg-gray-100 cursor-pointer"
                                                onClick={() => handleProjectSelect(index, "Project 3")}
                                            >
                                                Project 3
                                            </div>
                                        </div>
                                    )}
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
