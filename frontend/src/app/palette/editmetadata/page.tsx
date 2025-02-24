"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { useFileContext } from "@/app/FileContext";
import { useState } from "react";

export default function EditMetadataPage() {
    const searchParams = useSearchParams();
    const router = useRouter();
    const { files, updateMetadata } = useFileContext();

    const fileName = searchParams.get("file");

    // Find file by comparing file.name
    const fileIndex = files.findIndex((f) => f.file.name === fileName);
    const fileData = files[fileIndex];

    if (!fileData) {
        return <p className="text-red-500 p-6">File not found in context.</p>;
    }

    // Local state for editing
    const [description, setDescription] = useState(fileData.description || "");
    const [location, setLocation] = useState(fileData.location || "");
    const [tags, setTags] = useState(fileData.tags.join(", ") || "");

    function handleSave() {
        // Update context
        updateMetadata(fileIndex, {
            description,
            location,
            tags: tags.split(",").map((t) => t.trim()),
        });

        // Navigate back to /palette
        router.push("/palette");
    }

    return (
        <div className="p-6 min-h-screen">
{/*             <h1 className="bg-gray-200 p-4 mb-4 rounded">Edit Metadata</h1>
            
 */}            
            <div className="bg-gray-200 p-4 mb-4 rounded">
                <p className="text-lg font-semibold mb-2">Editing metadata for:</p>
                <div className="flex items-center mb-4">
                <label className="block text-gray-700 w-32">File Name:</label>
                <p className="block text-gray-700 w-32">{fileData.file.name}</p>
                </div>
                {/* Description Input */}
                <div className="flex items-center mb-4">
                    <label className="block text-gray-700 w-32">Description:</label>
                    <input
                        type="text"
                        className="w-full p-2 border rounded"
                        placeholder="Enter description"
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                    />
                </div>

                {/* Location Input */}
                <div className="flex items-center mb-4">
                    <label className="block text-gray-700 w-32">Location:</label>
                    <input
                        type="text"
                        className="w-full p-2 border rounded"
                        placeholder="Where was this created?"
                        value={location}
                        onChange={(e) => setLocation(e.target.value)}
                    />
                </div>

                {/* Tags Input */}
                <div className="flex items-center mb-4">
                    <label className="block text-gray-700 w-32">Tags (comma-separated):</label>
                    <input
                        type="text"
                        className="w-full p-2 border rounded"
                        placeholder="e.g. big, lion, fly"
                        value={tags}
                        onChange={(e) => setTags(e.target.value)}
                    />
                </div>

                <p className="text-lg font-semibold mb-2">File size:</p>
                <p className="text-gray-800 mb-4">{fileData.fileSize}</p>

                <button
                    onClick={handleSave}
                    className="bg-teal-500 text-white px-4 py-2 rounded hover:bg-teal-600"
                >
                    Save Changes
                </button>
            </div>
        </div>
    );
}
