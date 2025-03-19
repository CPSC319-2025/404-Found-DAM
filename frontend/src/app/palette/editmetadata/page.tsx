"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { useFileContext } from "@/app/context/FileContext";
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
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-red-500 text-lg">File not found in context.</p>
      </div>
    );
  }

  // Local state for editing
  const [description, setDescription] = useState(fileData.description || "");
  const [location, setLocation] = useState(fileData.location || "");
  const [tags, setTags] = useState(fileData.tags.join(", ") || "");

  function handleSave() {
    // Update context with new metadata
    updateMetadata(fileIndex, {
      description,
      location,
      tags: tags.split(",").map((t) => t.trim()),
    });

    // Navigate back to palette page
    router.push("/palette");
  }

  function handleEditImage() {
    // Navigate to a new page under /palette/ for image editing
    if (!fileName) {
      console.error("File name is missing!");
      return;
    }
    router.push(`/palette/editImage?file=${encodeURIComponent(fileName)}`);
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-4">
      <div className="w-full max-w-lg bg-white shadow-xl rounded-lg p-6">
        <h1 className="text-3xl font-bold text-gray-800 mb-6 text-center">
          Edit Image
        </h1>

        <div className="mb-6">
          <p className="text-lg font-medium text-gray-700 mb-2">
            Editing file:
          </p>
          <div className="bg-gray-100 p-3 rounded">
            <p className="text-gray-800 font-semibold">{fileData.file.name}</p>
          </div>
        </div>

        <div className="space-y-4">
          <div>
            <label className="block text-gray-700 mb-1">Description:</label>
            <input
              type="text"
              className="w-full border border-gray-300 rounded-lg p-2 focus:outline-none focus:ring-2 focus:ring-teal-400"
              placeholder="Enter description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>

          <div>
            <label className="block text-gray-700 mb-1">Location:</label>
            <input
              type="text"
              className="w-full border border-gray-300 rounded-lg p-2 focus:outline-none focus:ring-2 focus:ring-teal-400"
              placeholder="Where was this created?"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
            />
          </div>

          <div>
            <label className="block text-gray-700 mb-1">
              Tags (comma-separated):
            </label>
            <input
              type="text"
              className="w-full border border-gray-300 rounded-lg p-2 focus:outline-none focus:ring-2 focus:ring-teal-400"
              placeholder="e.g. big, lion, fly"
              value={tags}
              onChange={(e) => setTags(e.target.value)}
            />
          </div>

          <div className="mt-4">
            <p className="text-gray-700 font-medium">File Size:</p>
            <p className="text-gray-800">{fileData.fileSize}</p>
          </div>
        </div>

        <div className="mt-8 flex justify-center space-x-4">
          <button
            onClick={handleSave}
            className="bg-teal-500 hover:bg-teal-600 text-white font-semibold py-2 px-6 rounded-lg transition-colors duration-200"
          >
            Save Changes
          </button>
          <button
            onClick={handleEditImage}
            className="bg-blue-500 hover:bg-blue-600 text-white font-semibold py-2 px-6 rounded-lg transition-colors duration-200"
          >
            Edit Image
          </button>
        </div>
      </div>
    </div>
  );
}
