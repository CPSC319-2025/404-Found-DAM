"use client";

import React, { useState, ChangeEvent, useEffect, useCallback, useRef } from "react";
import { useRouter } from "next/navigation";
import { PencilIcon, TrashIcon, PlayIcon } from "@heroicons/react/24/outline";
import { useFileContext, FileMetadata } from "@/app/context/FileContext";
import { fetchWithAuth } from "@/app/utils/api/api";
import { loadFileContent } from "./Apis/fetchPaletteAssets";

// URL Registry to persist blob URLs across component re-renders
// This prevents URLs from being revoked when components unmount and remount
const URLRegistry = {
  urls: new Map<string, string>(),
  fileSizes: new Map<string, string>(), // Add storage for file sizes
  
  // Store a URL for a blobId
  register: (blobId: string, url: string, fileSize?: string): void => {
    if (!blobId) return;
    // If we already have a URL for this blobId, revoke it first
    if (URLRegistry.urls.has(blobId)) {
      const oldUrl = URLRegistry.urls.get(blobId);
      if (oldUrl && oldUrl.startsWith('blob:') && oldUrl !== url) {
        URL.revokeObjectURL(oldUrl);
      }
    }
    URLRegistry.urls.set(blobId, url);
    
    // Store file size if provided
    if (fileSize) {
      URLRegistry.fileSizes.set(blobId, fileSize);
    }
    
    console.log(`Registered URL for blobId ${blobId}: ${url.substring(0, 30)}...`);
  },
  
  // Get a URL for a blobId
  get: (blobId: string): string | undefined => {
    return URLRegistry.urls.get(blobId);
  },
  
  // Get file size for a blobId
  getFileSize: (blobId: string): string | undefined => {
    return URLRegistry.fileSizes.get(blobId);
  },
  
  // Remove a URL for a blobId
  remove: (blobId: string): void => {
    if (!blobId) return;
    const url = URLRegistry.urls.get(blobId);
    if (url && url.startsWith('blob:')) {
      // First remove from registry, then revoke the URL
      URLRegistry.urls.delete(blobId);
      URLRegistry.fileSizes.delete(blobId); // Also remove file size
      console.log(`Revoking URL for blobId ${blobId}: ${url.substring(0, 30)}...`);
      setTimeout(() => {
        URL.revokeObjectURL(url);
      }, 1000); // Small delay to ensure no components are still using it
    }
  },
  
  // Clean up all URLs
  clear: (): void => {
    URLRegistry.urls.forEach((url, blobId) => {
      if (url && url.startsWith('blob:')) {
        console.log(`Clearing URL for blobId ${blobId}`);
        URL.revokeObjectURL(url);
      }
    });
    URLRegistry.urls.clear();
    URLRegistry.fileSizes.clear(); // Also clear file sizes
  }
};

// Clean up all URLs when the window unloads
if (typeof window !== 'undefined') {
  window.addEventListener('beforeunload', () => {
    URLRegistry.clear();
  });
}

interface Project {
  projectID: number;
  projectName: string;
  location: string;
  description: string;
  creationTime: string;
  assetCount: number;
}

type FileTableProps = {
  files: FileMetadata[];
  removeFile: (index: number) => void;

  // Row selection from parent
  selectedIndices: number[];
  setSelectedIndices: React.Dispatch<React.SetStateAction<number[]>>;
  
  // blobId-based selection
  selectedBlobIds: string[];
  onSelectionChange: (indices: number[], blobIds: string[]) => void;
  
  projects: Project[];
  
  // Pagination props
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  
  // Edit metadata
  handleEditMetadata: (index: number) => void;
};

// Create a proper file size formatting function
function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 Bytes';
  // For more accurate size representation
  if (bytes < 1024) {
    return bytes + ' Bytes';
  } else if (bytes < 1024 * 1024) {
    return (bytes / 1024).toFixed(2) + ' KB';
  } else if (bytes < 1024 * 1024 * 1024) {
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  } else {
    return (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB';
  }
}

// Create a shared component for handling media files
function LazyMedia({ 
  fileMeta, 
  type,
  isImage,
  onClick 
}: { 
  fileMeta: FileMetadata, 
  type: 'image' | 'video',
  isImage: boolean,
  onClick: (url: string, type: string) => void 
}) {
  const [isLoaded, setIsLoaded] = useState(!!fileMeta.isLoaded);
  const [mediaSrc, setMediaSrc] = useState<string | null>(fileMeta.url || null);
  const { setFiles } = useFileContext();
  
  useEffect(() => {
    // Case 1: Check URL registry first
    if (fileMeta.blobId && URLRegistry.get(fileMeta.blobId)) {
      const registeredUrl = URLRegistry.get(fileMeta.blobId);
      const registeredFileSize = URLRegistry.getFileSize(fileMeta.blobId);
      
      if (registeredUrl && registeredUrl !== mediaSrc) {
        setMediaSrc(registeredUrl);
        setIsLoaded(true);
        
        // Update parent with registered file size if available
        if (registeredFileSize) {
          setFiles(prev => prev.map(file => {
            if (file.blobId === fileMeta.blobId) {
              return {
                ...file,
                url: registeredUrl,
                isLoaded: true,
                fileSize: registeredFileSize
              };
            }
            return file;
          }));
        }
        return;
      }
    }
    
    // Case 2: Use URL from fileMeta
    if (fileMeta.url && fileMeta.url !== mediaSrc) {
      setMediaSrc(fileMeta.url);
      setIsLoaded(true);
      
      if (fileMeta.blobId) {
        URLRegistry.register(fileMeta.blobId, fileMeta.url, fileMeta.fileSize);
      }
      return;
    }
    
    // Case 3: Create URL from raw file
    if (!mediaSrc && fileMeta.file instanceof File && fileMeta.file.size > 0) {
      const newUrl = URL.createObjectURL(fileMeta.file);
      setMediaSrc(newUrl);
      setIsLoaded(true);
      
      const fileSize = formatFileSize(fileMeta.file.size);
      
      if (fileMeta.blobId) {
        URLRegistry.register(fileMeta.blobId, newUrl, fileSize);
      }
      
      setFiles(prev => prev.map(file => {
        if (file === fileMeta || (fileMeta.blobId && file.blobId === fileMeta.blobId)) {
          return {
            ...file,
            url: newUrl,
            isLoaded: true,
            fileSize: fileSize
          };
        }
        return file;
      }));
      return;
    }
    
    // Case 4: Fetch from server using blobId
    if (fileMeta.blobId && !mediaSrc) {
      let isCancelled = false;
      
      const fetchMedia = async () => {
        try {
          const updatedFileMeta = await loadFileContent(fileMeta);
          
          if (!isCancelled && updatedFileMeta?.url) {
            if (fileMeta.blobId) {
              URLRegistry.register(fileMeta.blobId, updatedFileMeta.url, updatedFileMeta.fileSize);
            }
            
            setMediaSrc(updatedFileMeta.url);
            setIsLoaded(true);
            
            // Calculate file size if missing
            if (!updatedFileMeta.fileSize && updatedFileMeta.file instanceof File && updatedFileMeta.file.size > 0) {
              updatedFileMeta.fileSize = formatFileSize(updatedFileMeta.file.size);
            }
            
            setFiles(prev => prev.map(file => {
              if (file.blobId === fileMeta.blobId) {
                return updatedFileMeta;
              }
              return file;
            }));
          }
        } catch (error) {
          if (!isCancelled) {
            console.error(`Error loading ${type}:`, error);
          }
        }
      };
      
      fetchMedia();
      
      return () => {
        isCancelled = true;
      };
    }
  }, [fileMeta, mediaSrc, setFiles, type]);

  // Render different content based on type
  return (
    <div 
      className={`relative w-24 h-24 flex items-center justify-center overflow-hidden bg-gray-100 rounded-md ${!isImage ? 'cursor-pointer' : ''}`} 
      onClick={(e) => {
        e.stopPropagation();
        if (mediaSrc) {
          onClick(mediaSrc, fileMeta.file.type);
        }
      }}
    >
      {isLoaded && mediaSrc ? (
        isImage ? (
          <img
            src={mediaSrc}
            alt={fileMeta.file.name}
            className="object-contain max-w-full max-h-full"
          />
        ) : (
          <>
            <video
              src={mediaSrc}
              className="object-contain max-w-full max-h-full"
            />
            <div className="absolute inset-0 flex items-center justify-center">
              <div className="bg-indigo-500 bg-opacity-75 rounded-full p-2">
                <PlayIcon className="h-5 w-5 text-white" />
              </div>
            </div>
          </>
        )
      ) : (
        <div className="flex items-center justify-center">
          <svg className="animate-spin h-6 w-6 text-gray-400" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
        </div>
      )}
    </div>
  );
}

// Re-implement LazyImage and LazyVideo as wrappers around LazyMedia
function LazyImage({ fileMeta, onClick }: { fileMeta: FileMetadata, onClick: (url: string, type: string) => void }) {
  return <LazyMedia fileMeta={fileMeta} type="image" isImage={true} onClick={onClick} />;
}

function LazyVideo({ fileMeta, onClick }: { fileMeta: FileMetadata, onClick: (url: string, type: string) => void }) {
  return <LazyMedia fileMeta={fileMeta} type="video" isImage={false} onClick={onClick} />;
}

// Pagination component
const Pagination = ({ 
  currentPage, 
  totalPages,
  onPageChange 
}: { 
  currentPage: number, 
  totalPages: number,
  onPageChange: (page: number) => void 
}) => {
  const pages = [];
  
  // Logic to display max 5 page numbers with current page in the middle if possible
  let startPage = Math.max(1, currentPage - 2);
  let endPage = Math.min(totalPages, startPage + 4);
  
  // Adjust startPage if we're near the end
  if (endPage - startPage < 4) {
    startPage = Math.max(1, endPage - 4);
  }
  
  for (let i = startPage; i <= endPage; i++) {
    pages.push(i);
  }
  
  // Don't show pagination if there's only one page
  if (totalPages <= 1) {
    return null;
  }
  
  return (
    <div className="flex flex-col items-center my-6">
      <div className="text-sm text-gray-500 mb-2">
        Page {currentPage} of {totalPages}
      </div>
      <div className="flex border border-gray-300 rounded-md overflow-hidden">
        <button 
          className="px-3 py-1 border-r border-gray-300 bg-white text-gray-700 hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed" 
          onClick={() => onPageChange(1)}
          disabled={currentPage === 1}
        >
          «
        </button>
        <button 
          className="px-3 py-1 border-r border-gray-300 bg-white text-gray-700 hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed" 
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage === 1}
        >
          Previous
        </button>
        
        {startPage > 1 && (
          <>
            <button 
              className="px-3 py-1 border-r border-gray-300 bg-white text-gray-700 hover:bg-gray-100"
              onClick={() => onPageChange(1)}
            >
              1
            </button>
            {startPage > 2 && (
              <span className="px-3 py-1 border-r border-gray-300 bg-white text-gray-500">...</span>
            )}
          </>
        )}
        
        {pages.map(page => (
          <button 
            key={page} 
            className={`px-3 py-1 border-r border-gray-300 ${
              page === currentPage 
                ? 'bg-blue-500 text-white' 
                : 'bg-white text-gray-700 hover:bg-gray-100'
            }`}
            onClick={() => onPageChange(page)}
          >
            {page}
          </button>
        ))}
        
        {endPage < totalPages && (
          <>
            {endPage < totalPages - 1 && (
              <span className="px-3 py-1 border-r border-gray-300 bg-white text-gray-500">...</span>
            )}
            <button 
              className="px-3 py-1 border-r border-gray-300 bg-white text-gray-700 hover:bg-gray-100"
              onClick={() => onPageChange(totalPages)}
            >
              {totalPages}
            </button>
          </>
        )}
        
        <button 
          className="px-3 py-1 border-r border-gray-300 bg-white text-gray-700 hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed" 
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage === totalPages}
        >
          Next
        </button>
        <button 
          className="px-3 py-1 bg-white text-gray-700 hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed" 
          onClick={() => onPageChange(totalPages)}
          disabled={currentPage === totalPages}
        >
          »
        </button>
      </div>
    </div>
  );
};

export default function FileTable({
  files,
  removeFile,
  selectedIndices,
  setSelectedIndices,
  selectedBlobIds,
  onSelectionChange,
  projects,
  currentPage,
  totalPages,
  onPageChange,
  handleEditMetadata,
}: FileTableProps) {
  const router = useRouter();
  const { setFiles } = useFileContext();

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [previewType, setPreviewType] = useState<string | null>(null);
  const [previewBlobId, setPreviewBlobId] = useState<string | null>(null);

  function handleSelectAll(e: ChangeEvent<HTMLInputElement>) {
    if (e.target.checked) {
      // Get all blobIds from the current page
      const allBlobIds = files
        .map(file => file.blobId)
        .filter((blobId): blobId is string => !!blobId);
      
      // Get all indices from the current page
      const allIndices = files.map((_, idx) => idx);
      
      // Combine with previously selected blobIds from other pages
      const combinedBlobIds = new Set([...selectedBlobIds, ...allBlobIds]);
      const newBlobIds = Array.from(combinedBlobIds);
      
      console.log('Selecting all items, new selection:', newBlobIds);
      onSelectionChange(allIndices, newBlobIds);
      
      // Force persist to localStorage 
      localStorage.setItem('paletteSelections', JSON.stringify(newBlobIds));
    } else {
      // Get all blobIds from the current page
      const currentPageBlobIds = files
        .map(file => file.blobId)
        .filter((blobId): blobId is string => !!blobId);
      
      // Remove current page blobIds from selection, keep selections from other pages
      const newBlobIds = selectedBlobIds.filter(
        blobId => !currentPageBlobIds.includes(blobId)
      );
      
      console.log('Deselecting current page, new selection:', newBlobIds);
      onSelectionChange([], newBlobIds);
      
      // Force persist to localStorage
      localStorage.setItem('paletteSelections', JSON.stringify(newBlobIds));
    }
  }

  function handleSelectRow(index: number) {
    const file = files[index];
    const blobId = file.blobId;
    
    if (!blobId) return; // Can't select a file without a blobId
    
    let newIndices: number[];
    let newBlobIds: string[];
    
    if (selectedBlobIds.includes(blobId)) {
      // Deselect the file
      newIndices = selectedIndices.filter(i => i !== index);
      newBlobIds = selectedBlobIds.filter(id => id !== blobId);
      console.log('Deselecting item:', blobId);
    } else {
      // Select the file
      newIndices = [...selectedIndices, index];
      newBlobIds = [...selectedBlobIds, blobId];
      console.log('Selecting item:', blobId);
    }
    
    onSelectionChange(newIndices, newBlobIds);
    
    // Force persist to localStorage
    localStorage.setItem('paletteSelections', JSON.stringify(newBlobIds));
  }

  function handleRemoveTag(fileIndex: number, tagIndex: number) {
    const fileMeta = files[fileIndex];
    const tagToRemove = fileMeta.tags[tagIndex];
    const tagIdToRemove = fileMeta.tagIds[tagIndex];
    
    if (!fileMeta.blobId) {
      console.warn("File missing blobId:", fileMeta.file.name);
      return;
    }

    // Call API to delete the tag
    async function deleteTag() {
      try {
        const response = await fetchWithAuth(`/palette/assets/tags`, {
          method: "PATCH",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({
            BlobIds: [fileMeta.blobId],
            TagIds: [tagIdToRemove]
          })
        });

        if (!response.ok) {
          console.error("Failed to delete tag:", response.status);
          return;
        }

        // remove that tagid and corresponding tag from the fileMeta
        if (response.ok) {
          setFiles((prev) => {
            const updated = [...prev];
            if (updated[fileIndex]) {
              // Find the current index of the tagId we're removing
              // This is important because UI might have changed since API call was made
              const currentTagIdIndex = updated[fileIndex].tagIds.indexOf(tagIdToRemove);
              
              if (currentTagIdIndex !== -1) {
                const updatedTags = [...updated[fileIndex].tags];
                const updatedTagIds = [...updated[fileIndex].tagIds];
                
                // Remove the tag and tagId at the current index
                updatedTags.splice(currentTagIdIndex, 1);
                updatedTagIds.splice(currentTagIdIndex, 1);
                
                updated[fileIndex] = {
                  ...updated[fileIndex],
                  tags: updatedTags,
                  tagIds: updatedTagIds
                };
              }
            }
            return updated;
          });
          
          console.log(`Tag "${tagToRemove}" removed successfully`);
        }
        
      } catch (err) {
        console.error("Error deleting tag:", err);
      }
    }

    deleteTag();
  }

  // ----- Project Dropdown -----
  async function handleProjectChange(index: number, newProjectID: string) {
    if (!newProjectID) return; // Don't do anything if no project selected
    
    const fileMeta = files[index];
    if (!fileMeta.blobId) {
      console.warn("File missing blobId:", fileMeta.file.name);
      return;
    }

    // Clear existing tags first
    setFiles((prev) => {
      const updated = [...prev];
      updated[index] = {
        ...updated[index],
        tags: [] // Clear tags
      };
      return updated;
    });
    
    // Call the API to delete all tags from the backend if there are any
    if (fileMeta.tags.length > 0 && fileMeta.blobId) {
      try {
        const deleteTagsResponse = fetchWithAuth(`palette/assets/tags`, {
          method: "PATCH",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({
            BlobIds: [fileMeta.blobId],
            TagIds: fileMeta.tagIds
          })
        })
        console.log("Cleared all existing tags");
      } catch (err) {
        console.error("Error clearing tags:", err);
      }
    }
    
    // Update the UI state first for responsiveness
    setFiles((prev) => {
      const updated = [...prev];
      updated[index] = {
        ...updated[index],
        project: newProjectID,
      };
      return updated;
    });
    
    // Get any existing tags from the blob
    
    
    // Call API assign all project tags to the asset
    try {
      const token = localStorage.getItem("token");
      
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/asset/project-tags`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: token ? `Bearer ${token}` : "",
          },
          body: JSON.stringify({
            blobId: fileMeta.blobId,
            projectId: newProjectID
          })
        }
      );

      if (!response.ok) {
        console.error("Failed to update image tags:", response.status);
      }
    } catch (err) {
      console.error("Error updating image tags:", err); 
    }

    // Call the API to associate the asset with the project
    try {
      const response = await fetchWithAuth(`projects/${newProjectID}/associate-assets`, {
        method: "PATCH",
        body: JSON.stringify({
          projectID: parseInt(newProjectID),
          blobIDs: [fileMeta.blobId],
          tagIDs: [],
          metadataEntries: []
        }),
      })

      if (!response.ok) {
        console.error("Associate asset failed:", response.status);
        return;
      }
      
      const data = await response.json();
      console.log("Association success:", data);
      
      // Remove the file from the table if successfully associated
      if (data.successfulSubmissions?.includes(fileMeta.blobId)) {
        removeFile(index);
      }
    } catch (err) {
      console.error("Error associating asset with project:", err);
    }

    // Call the API to get asset details from the blobId
    try {
      const response = await fetchWithAuth(`palette/blob/${fileMeta.blobId}/details`);
      
      if (response.ok) {
        const data = await response.json();
        // If the blob has tags, keep them
        // console.log(data.tagIds);
        if (data.tags && data.tags.length > 0) {
          setFiles((prev) => {
            const updated = [...prev];
            updated[index] = {
              ...updated[index],
              tags: data.tags,
              tagIds: data.tagIds,
              description: projects.find(p => p.projectID.toString() === newProjectID)?.description || "",
              location: projects.find(p => p.projectID.toString() === newProjectID)?.location || "",
            };
            return updated;
          });
        }
      }
    } catch (err) {
      console.error("Error fetching blob details:", err);
    }
  }

  // ----- Modal Preview Logic -----
  function openPreview(url: string, fileType: string) {
    setPreviewUrl(url);
    setPreviewType(fileType);
    setIsModalOpen(true);
    
    // Try to find which blobId this URL corresponds to
    files.forEach(file => {
      if (file.url === url && file.blobId) {
        setPreviewBlobId(file.blobId);
      }
    });
  }
  
  function closeModal() {
    setIsModalOpen(false);
    setPreviewBlobId(null);
    setPreviewType(null);
    // We don't clear previewUrl here as it might cause a flash of no content
  }
  
  // Override the removeFile function to also clean up URLs from registry
  const handleRemoveFile = useCallback((index: number) => {
    const file = files[index];
    if (file.blobId) {
      // When a file is removed, remove its URL from the registry
      URLRegistry.remove(file.blobId);
    }
    removeFile(index);
  }, [files, removeFile]);

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
                    files.length > 0 && 
                    files.every(file => 
                      file.blobId && selectedBlobIds.includes(file.blobId)
                    )
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
              Project
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Tags
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Edit
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Remove
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {files.map((fileMeta, index) => {
            const rawFile = fileMeta.file;
            const displayName = fileMeta.fileName || rawFile.name;
            const isSelected = fileMeta.blobId ? selectedBlobIds.includes(fileMeta.blobId) : false;

            // detect if image or video
            const isImage = rawFile.type.startsWith("image/");
            const isVideo = rawFile.type.startsWith("video/");

            return (
              <tr
                key={index}
                className={`hover:bg-gray-50 cursor-pointer ${isSelected ? 'bg-blue-50' : ''}`}
                onClick={() => handleSelectRow(index)}
              >
                <td className="px-6 py-4 text-center">
                  <input
                    type="checkbox"
                    checked={isSelected}
                    onChange={(e) => e.stopPropagation()}
                    onClick={(e) => {
                      e.stopPropagation();
                      handleSelectRow(index);
                    }}
                  />
                </td>

                <td className="px-6 py-4">
                  {isImage && (
                    <LazyImage fileMeta={fileMeta} onClick={openPreview} />
                  )}
                  {isVideo && (
                    <LazyVideo fileMeta={fileMeta} onClick={openPreview} />
                  )}
                </td>

                <td className="px-6 py-4">{displayName}</td>

                {/* File Type */}
                <td className="px-6 py-4">{isImage ? "image/webp" : rawFile.type}</td>

                <td className="px-6 py-4">{fileMeta.fileSize}</td>

                <td className="px-6 py-4">
                  <select
                    className="border border-gray-300 rounded p-1"
                    value={fileMeta.project || ""}
                    onClick={(e) => e.stopPropagation()}
                    onChange={(e) => handleProjectChange(index, e.target.value)}
                  >
                    <option value="">Select project...</option>
                    {projects.map((p) => (
                      <option key={p.projectID} value={p.projectID.toString()}>
                        {p.projectName}
                      </option>
                    ))}
                  </select>
                </td>

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
                          onClick={(evt) => {
                            evt.stopPropagation();
                            handleRemoveTag(index, tagIndex);
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
                      handleEditMetadata(index);
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
                      handleRemoveFile(index);
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

      {/* Pagination */}
      <Pagination 
        currentPage={currentPage}
        totalPages={totalPages}
        onPageChange={onPageChange}
      />

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
