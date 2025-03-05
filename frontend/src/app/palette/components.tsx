"use client";

import React, { useState, ChangeEvent } from "react";
import { useRouter } from "next/navigation";
import { PencilIcon, TrashIcon } from "@heroicons/react/24/outline";
import { useFileContext, FileMetadata } from "@/app/FileContext";

type FileTableProps = {
  files: FileMetadata[];
  removeFile: (index: number) => void;
};

export default function FileTable({ files, removeFile }: FileTableProps) {
  const router = useRouter();
  const { setFiles } = useFileContext();

  const [selectedIndices, setSelectedIndices] = useState<number[]>([]);

  // Modal states
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [previewType, setPreviewType] = useState<string | null>(null);

  function handleSelectAll(e: ChangeEvent<HTMLInputElement>) {
    if (e.target.checked) {
      setSelectedIndices(files.map((_, idx) => idx));
    } else {
      setSelectedIndices([]);
    }
  }

  function handleSelectRow(index: number) {
    setSelectedIndices((prev) =>
      prev.includes(index) ? prev.filter((i) => i !== index) : [...prev, index]
    );
  }

  function handleRemoveTag(fileMeta: FileMetadata, tagIndex: number) {
    setFiles((prevFiles) =>
      prevFiles.map((f) =>
        f.file.name === fileMeta.file.name
          ? { ...f, tags: f.tags.filter((_, i) => i !== tagIndex) }
          : f
      )
    );
  }

  function handleEditMetadata(rawFileName: string) {
    router.push(
      `/palette/editmetadata?file=${encodeURIComponent(rawFileName)}`
    );
  }

  // Open modal to show full preview
  function openPreview(url: string, fileType: string) {
    setPreviewUrl(url);
    setPreviewType(fileType);
    setIsModalOpen(true);
  }

  // Close modal
  function closeModal() {
    setIsModalOpen(false);
    setPreviewUrl(null);
    setPreviewType(null);
  }

  return (
    <div className="overflow-auto">
      <table className="min-w-full bg-white border border-gray-200">
        <thead className="bg-gray-50">
          <tr>
            <th className="px-6 py-3 text-center">
              <div className="flex items-center justify-center">
                <input
                  type="checkbox"
                  checked={
                    selectedIndices.length === files.length && files.length > 0
                  }
                  onChange={handleSelectAll}
                />
                <span className="ml-2 text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Select All
                </span>
              </div>
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Preview
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              File Name
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              File Type
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              File Size
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Tags
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Edit
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Action
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {files.map((fileMeta, index) => {
            const rawFile = fileMeta.file;
            const displayName = rawFile.name;
            const isImage = rawFile.type.startsWith("image/");
            const isVideo = rawFile.type.startsWith("video/");

            let previewUrlObj: string | null = null;
            try {
              previewUrlObj = URL.createObjectURL(rawFile);
            } catch (error) {
              console.error("Failed to create object URL:", error);
            }

            return (
              <tr key={index} className="hover:bg-gray-50 cursor-pointer">
                {/* Checkbox */}
                <td className="px-6 py-4 text-center">
                  <input
                    type="checkbox"
                    checked={selectedIndices.includes(index)}
                    onChange={() => handleSelectRow(index)}
                  />
                </td>

                {/* Preview */}
                <td className="px-6 py-4">
                  {isImage && previewUrlObj && (
                    <div
                      className="h-20 w-20 relative"
                      onClick={() => openPreview(previewUrlObj, rawFile.type)}
                    >
                      <img
                        src={previewUrlObj}
                        alt="Preview"
                        className="object-cover rounded w-full h-full"
                      />
                    </div>
                  )}
                  {isVideo && previewUrlObj && (
                    <div
                      className="h-20 w-20 relative"
                      onClick={() => openPreview(previewUrlObj, rawFile.type)}
                    >
                      <video
                        src={previewUrlObj}
                        className="object-cover rounded w-full h-full"
                      />
                    </div>
                  )}
                </td>

                {/* File Name */}
                <td className="px-6 py-4">{displayName}</td>

                {/* File Type */}
                <td className="px-6 py-4">{rawFile.type}</td>

                {/* File Size */}
                <td className="px-6 py-4">{fileMeta.fileSize}</td>

                {/* Tags */}
                <td className="px-6 py-4">
                  {fileMeta.tags.length > 0 ? (
                    fileMeta.tags.map((tag, tagIndex) => (
                      <span
                        key={tagIndex}
                        className="inline-flex items-center px-2 py-1 mr-1 rounded-full text-xs font-semibold bg-blue-100 text-blue-800"
                      >
                        {tag}
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleRemoveTag(fileMeta, tagIndex);
                          }}
                          className="ml-1 text-red-500 hover:text-red-700"
                        >
                          ×
                        </button>
                      </span>
                    ))
                  ) : (
                    <span className="text-gray-400 text-xs">No tags</span>
                  )}
                </td>

                {/* Edit button */}
                <td className="px-6 py-4">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      handleEditMetadata(rawFile.name);
                    }}
                    className="text-indigo-600 hover:text-indigo-900"
                  >
                    <PencilIcon className="h-5 w-5" />
                  </button>
                </td>

                {/* Remove button */}
                <td className="px-6 py-4">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      removeFile(index);
                    }}
                    className="text-red-600 hover:text-red-900"
                  >
                    <TrashIcon className="h-5 w-5" />
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>

      {/* Modal for full preview */}
      {isModalOpen && previewUrl && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
          <div className="relative bg-white p-4 rounded shadow-lg max-w-3xl max-h-[80vh] overflow-auto">
            {/* Close Button */}
            <button
              onClick={closeModal}
              className="absolute top-2 right-2 text-gray-500 hover:text-gray-700"
            >
              ✕
            </button>

            {/* Image or Video Preview */}
            {previewType?.startsWith("image/") && (
              <img
                src={previewUrl}
                alt="Full Preview"
                className="max-w-full max-h-[70vh]"
              />
            )}
            {previewType?.startsWith("video/") && (
              <video
                src={previewUrl}
                controls
                className="max-w-full max-h-[70vh]"
              />
            )}
          </div>
        </div>
      )}
    </div>
  );
}
