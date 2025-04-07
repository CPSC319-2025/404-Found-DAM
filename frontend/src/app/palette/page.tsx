"use client";

import React, { useCallback, useState, useEffect, useRef } from "react";
import { useDropzone } from "react-dropzone";
import { useRouter } from "next/navigation";

import { useFileContext, FileMetadata } from "@/app/context/FileContext";
import FileTable from "./components";
import UploadModal from "./components/UploadModal";
import { 
  fetchPaletteAssets, 
  fetchBlobDetails, 
  fetchProjects, 
  removeFile as removeFileApi, 
  submitAssets,
  uploadFileChunked,
  UploadProgressCallbacks,
  Project,
  PaginatedFiles,
  loadFileContent
} from "./Apis";
import { useUser } from "@/app/context/UserContext";
import { formatFileSize } from "@/app/utils/api/formatFileSize";
import { toast } from "react-toastify";

// Simple Button component
const Button = ({ 
  children, 
  onClick,
  className = "",
  disabled = false
}: { 
  children: React.ReactNode; 
  onClick: () => void;
  className?: string;
  disabled?: boolean;
}) => (
  <button
    onClick={onClick}
    disabled={disabled}
    className={`px-6 py-3 rounded-lg font-medium transition-all duration-200 shadow-md hover:shadow-lg flex items-center ${className} ${
      disabled ? "opacity-50 cursor-not-allowed" : ""
    }`}
  >
    {children}
  </button>
);

// Simple Progress component
const Progress = ({ 
  value 
}: { 
  value: number 
}) => (
  <div className="w-full bg-gray-200 rounded-full h-2.5 mb-4">
    <div 
      className="bg-gradient-to-r from-blue-500 to-teal-500 h-2.5 rounded-full transition-all duration-300" 
      style={{ width: `${value}%` }}
    />
  </div>
);

export default function PalettePage() {
  const { user } = useUser();
  const router = useRouter();
  const { files, setFiles } = useFileContext();

  // Replace array indices with blobIds for selection
  const [selectedBlobIds, setSelectedBlobIds] = useState<string[]>([]);
  // Keep selectedIndices for backward compatibility
  const [selectedIndices, setSelectedIndices] = useState<number[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const didFetchRef = useRef(false);
  const [showModal, setShowModal] = useState(false);
  const [uploadStatus, setUploadStatus] = useState<string>("");
  const [uploadProgress, setUploadProgress] = useState<number>(0);
  const [autoNamingEnabled, setAutoNamingEnabled] = useState<boolean>(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [fileToDeleteIndex, setFileToDeleteIndex] = useState<number | null>(null);
  const [fileToDeleteBlobId, setFileToDeleteBlobId] = useState<string | null>(null);
  
  // Pagination states
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [pageSize] = useState<number>(6);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [bgUploadInProgress, setBgUploadInProgress] = useState<boolean>(false);

  // Update selectedIndices when files or selectedBlobIds change
  useEffect(() => {
    // Create a mapping from selected blobIds to current page indices
    const newSelectedIndices = files
      .map((file, index) => ({ index, blobId: file.blobId }))
      .filter(item => item.blobId && selectedBlobIds.includes(item.blobId))
      .map(item => item.index);
    
    setSelectedIndices(newSelectedIndices);
  }, [files, selectedBlobIds]);

  // Update selection storage in useEffect and localStorage
  useEffect(() => {
    // Load saved selections from localStorage when component mounts
    const savedSelections = localStorage.getItem('paletteSelections');
    if (savedSelections) {
      try {
        const parsedSelections = JSON.parse(savedSelections);
        setSelectedBlobIds(parsedSelections);
      } catch (error) {
        console.error('Error parsing saved selections:', error);
      }
    }
  }, []);

  // Save selections to localStorage whenever they change
  useEffect(() => {
    if (selectedBlobIds.length > 0) {
      localStorage.setItem('paletteSelections', JSON.stringify(selectedBlobIds));
    }
  }, [selectedBlobIds]);

  // Add component mount/unmount logging
  useEffect(() => {
    console.log('PalettePage mounted');
    // On component mount, try to restore selections
    const savedSelections = localStorage.getItem('paletteSelections');
    if (savedSelections) {
      try {
        const parsedSelections = JSON.parse(savedSelections);
        if (parsedSelections.length > 0) {
          console.log('Restoring selections on mount:', parsedSelections);
          setSelectedBlobIds(parsedSelections);
        }
      } catch (error) {
        console.error('Error parsing saved selections:', error);
      }
    }
    
    return () => {
      console.log('PalettePage unmounting, current selections:', selectedBlobIds);
      // Save selections on unmount as a backup
      if (selectedBlobIds.length > 0) {
        localStorage.setItem('paletteSelections', JSON.stringify(selectedBlobIds));
      }
    };
  }, []);

  // Handle clearing all selections
  const clearAllSelections = useCallback(() => {
    setSelectedBlobIds([]);
    setSelectedIndices([]);
    localStorage.removeItem('paletteSelections');
    console.log('All selections cleared');
  }, []);

  // Ensure selections are correctly loaded when returning to the palette
  useEffect(() => {
    // Listen for storage events from other tabs/windows
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'paletteSelections' && e.newValue) {
        try {
          const parsedSelections = JSON.parse(e.newValue);
          console.log('Storage event detected, updating selections:', parsedSelections);
          setSelectedBlobIds(parsedSelections);
        } catch (error) {
          console.error('Error parsing selections from storage event:', error);
        }
      }
    };
    
    window.addEventListener('storage', handleStorageChange);
    return () => {
      window.removeEventListener('storage', handleStorageChange);
    };
  }, []);

  // Helper function to get file dimensions from image/video and add to state
  const processFileMetadata = useCallback(async (fileMeta: FileMetadata): Promise<void> => {
    return new Promise((resolve) => {
      if (fileMeta.file.type.startsWith("image/")) {
        const img = new Image();
        img.onload = () => {
          fileMeta.width = img.width;
          fileMeta.height = img.height;
          
          // If the file already has a URL, mark it as loaded
          if (fileMeta.url) {
            fileMeta.isLoaded = true;
          }
          
          setFiles((prev) => [...prev, fileMeta]);
          resolve();
        };
        // Use existing URL if available
        img.src = fileMeta.url || URL.createObjectURL(fileMeta.file);
      } else if (fileMeta.file.type.startsWith("video/")) {
        const video = document.createElement("video");
        video.preload = "metadata";
        video.onloadedmetadata = () => {
          fileMeta.width = video.videoWidth;
          fileMeta.height = video.videoHeight;
          fileMeta.duration = Math.floor(video.duration);
          
          // If the file already has a URL, mark it as loaded
          if (fileMeta.url) {
            fileMeta.isLoaded = true;
          }
          
          setFiles((prev) => [...prev, fileMeta]);
          resolve();
        };
        // Use existing URL if available
        video.src = fileMeta.url || URL.createObjectURL(fileMeta.file);
      } else {
        // For other file types, just add them
        if (fileMeta.url) {
          fileMeta.isLoaded = true;
        }
        
        setFiles((prev) => [...prev, fileMeta]);
        resolve();
      }
    });
  }, [setFiles]);

  // Helper function to fetch blob details
  const fetchAndUpdateBlobDetails = useCallback(async (blobId: string): Promise<void> => {
    if (!blobId) return;
    
    try {
      const details = await fetchBlobDetails(blobId);
      
      // Update the file in our state with the details
      setFiles(prevFiles => prevFiles.map(file => {
        if (file.blobId === blobId) {
          return {
            ...file,
            project: details.project,
            tags: details.tags || [],
            tagIds: details.tagIds || [],
            description: details.description || "",
            location: details.location || ""
          };
        }
        return file;
      }));
    } catch (error) {
      console.error("Error fetching blob details:", error);
    }
  }, [setFiles]);

  // Load assets with pagination - optimize to reduce loading time
  const loadAssets = useCallback(async (page: number = 1) => {
    setIsLoading(true);
    
    try {
      // Clear files before loading new page (but don't clear selections)
      setFiles([]);
      
      // Use Promise.all to parallelize metadata fetching when possible
      const paginatedResult = await fetchPaletteAssets(page, pageSize);
      
      // Update state with pagination info
      setCurrentPage(paginatedResult.currentPage);
      setTotalPages(paginatedResult.totalPages);
      setTotalCount(paginatedResult.totalCount);
      
      // Update files state immediately
      setFiles(paginatedResult.files);
      
      // Fetch additional metadata in parallel
      if (paginatedResult.files.length > 0) {
        const metadataPromises = paginatedResult.files
          .filter(fileMeta => fileMeta.blobId)
          .map(fileMeta => fetchAndUpdateBlobDetails(fileMeta.blobId!));
            
        // Process in batch but don't wait for completion to show UI
        Promise.all(metadataPromises).catch(err => 
          console.error("Error fetching metadata batch:", err)
        );
      }
      
      // After loading assets, restore selections from localStorage if needed
      if (selectedBlobIds.length === 0) {
        const savedSelections = localStorage.getItem('paletteSelections');
        if (savedSelections) {
          try {
            const parsedSelections = JSON.parse(savedSelections);
            if (parsedSelections.length > 0) {
              setSelectedBlobIds(parsedSelections);
            }
          } catch (error) {
            console.error('Error parsing saved selections:', error);
          }
        }
      }
    } catch (error) {
      console.error("Error loading assets:", error);
    } finally {
      setIsLoading(false);
    }
  }, [pageSize, setFiles, fetchAndUpdateBlobDetails, selectedBlobIds.length]);

  // Handle page change
  const handlePageChange = useCallback((newPage: number) => {
    if (newPage !== currentPage) {
      // Save current selections before changing page
      if (selectedBlobIds.length > 0) {
        localStorage.setItem('paletteSelections', JSON.stringify(selectedBlobIds));
      }
      
      setCurrentPage(newPage);
      loadAssets(newPage);
    }
  }, [currentPage, loadAssets, selectedBlobIds]);

  // Load assets on initial mount and when currentPage changes
  useEffect(() => {
    // If this is initial mount, check if we're returning from editing
    if (didFetchRef.current === false) {
      // Check if we need to refresh to page 1 after upload
      const refreshPage = localStorage.getItem('paletteRefreshPage');
      if (refreshPage === '1') {
        // Clear the refresh flag
        localStorage.removeItem('paletteRefreshPage');
        
        // Set to page 1 and load assets
        setCurrentPage(1);
        loadAssets(1);
      } else {
        // Check if we're returning from editing metadata
        const savedPage = localStorage.getItem('palettePage');
        if (savedPage) {
          const page = parseInt(savedPage);
          setCurrentPage(page);
          
          // Clear the saved page now that we've used it
          localStorage.removeItem('palettePage');
          
          // Load assets for the saved page
          loadAssets(page);
        } else {
          // Just load the first page
          loadAssets(currentPage);
        }
      }
      
      didFetchRef.current = true;
    }
  }, [loadAssets, currentPage]);

  // Fetch projects once on mount
  useEffect(() => {
    const loadProjects = async () => {
      const projectsData = await fetchProjects();
      const userProjects = projectsData.filter((project) =>
        project.admins.some((admin) => admin.userID === user?.userID) ||
        project.regularUsers.some((regularUser) => regularUser.userID === user?.userID)
      );
      setProjects(userProjects);
    };

    loadProjects();
  }, [user]);

  // Remove a file by index
  const removeFile = useCallback((index: number) => {
    const fileMeta = files[index];
    
    // Only show confirmation if multiple files are selected and this one is selected
    if (selectedBlobIds.length > 1 && 
        fileMeta.blobId && 
        selectedBlobIds.includes(fileMeta.blobId)) {
      setFileToDeleteIndex(index);
      setFileToDeleteBlobId(fileMeta.blobId || null);
      setShowDeleteConfirm(true);
      return;
    }

    // If not selected or only one file is selected, proceed with normal delete
    deleteFile(index);
  }, [selectedBlobIds, files]);

  // Delete a single file
  const deleteFile = useCallback((index: number) => {
    const fileToRemove = files[index];
    const blobIdToRemove = fileToRemove.blobId;

    // Call the API to remove the file
    if (blobIdToRemove) {
      removeFileApi(fileToRemove);
      
      // Update selectedBlobIds to remove the deleted file
      const updatedSelections = selectedBlobIds.filter(id => id !== blobIdToRemove);
      setSelectedBlobIds(updatedSelections);
      
      // Update localStorage
      localStorage.setItem('paletteSelections', JSON.stringify(updatedSelections));
    }
    
    // Remove the file from state
    setFiles(prev => {
      const updated = [...prev];
      updated.splice(index, 1);
      return updated;
    });

    // Clear delete confirmation state
    setFileToDeleteIndex(null);
    setFileToDeleteBlobId(null);
    
    // If we've deleted all files on the current page and there are more pages,
    // go to the previous page - reduced timeout from 100ms to 50ms
    if (files.length <= 1) {
      // If this was the last file on the current page
      if (currentPage > 1 && totalPages > 1) {
        // Go to previous page if not on first page
        setTimeout(() => handlePageChange(currentPage - 1), 50);
      } else {
        // Reload first page if we're already on it
        setTimeout(() => loadAssets(1), 50);
      }
    } else if (currentPage === totalPages && files.length === pageSize) {
      // If we're on the last page and it was full, check if we need to add a file from next batch
      setTimeout(() => loadAssets(currentPage), 50);
    }
  }, [setFiles, files, currentPage, totalPages, pageSize, handlePageChange, loadAssets, selectedBlobIds]);

  // Delete all selected files
  const deleteAllSelected = useCallback(() => {
    // Show loading state immediately
    setIsLoading(true);
    
    // Show notification
    setUploadStatus(`Deleting ${selectedBlobIds.length} assets...`);
    
    // Get the files that will be deleted from the current page
    const filesToDelete = files.filter(file => 
      file.blobId && selectedBlobIds.includes(file.blobId)
    );
    
    // Get the blobIds that will be deleted from the current page
    const currentPageBlobIdsToDelete = filesToDelete
      .map(file => file.blobId)
      .filter((id): id is string => !!id);
    
    // Get blobIds that are selected but not on the current page
    const otherPageBlobIdsToDelete = selectedBlobIds.filter(
      id => !currentPageBlobIdsToDelete.includes(id)
    );
    
    // Track deletion completion
    let allDeletionsComplete = false;
    
    // Function to finalize deletion and refresh the view
    const finalizeDelete = () => {
      if (allDeletionsComplete) return;
      allDeletionsComplete = true;
      
      // Clear delete confirmation state
      setShowDeleteConfirm(false);
      setFileToDeleteIndex(null);
      setFileToDeleteBlobId(null);
      
      // Clear selections
      setSelectedBlobIds([]);
      setSelectedIndices([]);
      localStorage.removeItem('paletteSelections');
      
      // Clear status after all operations complete
      setTimeout(() => {
        setUploadStatus("");
        setUploadProgress(0);
        setIsLoading(false);
        
        // After deleting multiple files, we need to recalculate total pages
        const remainingFiles = totalCount - selectedBlobIds.length;
        const newTotalPages = Math.max(1, Math.ceil(remainingFiles / pageSize));
        
        // Decide which page to show after deletion
        let targetPage = currentPage;
        
        // If we've deleted all files on current page
        if (files.length === filesToDelete.length) {
          if (remainingFiles === 0) {
            // If no files left at all, go to page 1
            targetPage = 1;
          } else if (currentPage > newTotalPages) {
            // If current page is now beyond total pages, go to last available page
            targetPage = newTotalPages;
          } else if (currentPage > 1) {
            // If not on first page and current page still valid, stay there
            // otherwise go to previous page
            targetPage = currentPage;
          }
        }
        
        // Update total pages state
        setTotalPages(newTotalPages);
        setTotalCount(remainingFiles);
        
        // Reload the appropriate page
        loadAssets(targetPage);
      }, 500);
    };
      
    // Call the API to delete each file on the current page
    const currentPagePromises = filesToDelete.map(fileToRemove => {
      if (fileToRemove.blobId) {
        return removeFileApi(fileToRemove);
      }
      return Promise.resolve();
    });
    
    // Process files on other pages
    if (otherPageBlobIdsToDelete.length > 0) {
      // Process in batches to avoid overwhelming the API
      const processBatch = async (blobIds: string[], index = 0) => {
        if (index >= blobIds.length) {
          // When other page deletions are complete, check if all deletions are done
          Promise.all(currentPagePromises)
            .then(() => {
              finalizeDelete();
            })
            .catch(error => {
              console.error("Error during current page deletion:", error);
              finalizeDelete();
            });
          return;
        }
        
        try {
          const blobId = blobIds[index];
          
          // Create a minimal file object for deletion
          const dummyFile = new File([], "dummy");
          const fileToRemove = {
            file: dummyFile,
            blobId,
            fileSize: "0 B",
            description: "",
            location: "",
            tags: [],
            tagIds: []
          };
          
          await removeFileApi(fileToRemove);
          
          // Process next batch after a small delay
          setTimeout(() => processBatch(blobIds, index + 1), 10);
        } catch (error) {
          console.error(`Error deleting file ${blobIds[index]}:`, error);
          // Continue with next batch even if there's an error
          setTimeout(() => processBatch(blobIds, index + 1), 10);
        }
      };
      
      // Start processing batches
      processBatch(otherPageBlobIdsToDelete);
    } else {
      // If only current page files to delete, wait for them to complete
      Promise.all(currentPagePromises)
        .then(() => {
          finalizeDelete();
        })
        .catch(error => {
          console.error("Error during deletion:", error);
          finalizeDelete();
        });
    }
    
    // Remove the files from state immediately for UI feedback
    setFiles(prev => prev.filter(file => 
      !file.blobId || !selectedBlobIds.includes(file.blobId)
    ));
    
  }, [selectedBlobIds, files, setFiles, currentPage, loadAssets, setIsLoading, setUploadStatus, setUploadProgress]);

  // Handle selection change from FileTable
  const handleSelectionChange = useCallback((indices: number[], blobIds: string[]) => {
    setSelectedIndices(indices);
    setSelectedBlobIds(blobIds);
  }, []);

  // Prepare a file metadata object
  const createFileMetadata = useCallback((file: File): FileMetadata => {
    // Convert bytes to KB before formatting
    const fileSizeInKB = file.size / 1024;
    const fileSize = formatFileSize(fileSizeInKB);
    return {
      file,
      fileSize,
      description: "",
      location: "",
      tags: [],
      tagIds: [],
    };
  }, []);

  // Create callbacks for the chunked upload
  const createUploadCallbacks = useCallback((file: File, fileMeta: FileMetadata): UploadProgressCallbacks => ({
    onProgress: (progress: number, status: string) => {
      setUploadStatus(`Uploading ${file.name}: ${status}`);
      setUploadProgress(progress);
    },
    onSuccess: async (blobId?: string) => {
      setUploadStatus(`File ${file.name} uploaded successfully`);
      setUploadProgress(100);
      
      try {
        if (blobId) {
          // First, create a URL from the original file for preview
          const objectUrl = URL.createObjectURL(file);
          
          // Update the file metadata with the blobId and URL
          setFiles(prevFiles => {
            return prevFiles.map(f => {
              if (f.file === file) {
                return { 
                  ...f, 
                  blobId,
                  url: objectUrl,
                  isLoaded: true  // Mark as loaded to skip lazy loading
                };
              }
              return f;
            });
          });
          
          // Fetch and update blob details
          await fetchAndUpdateBlobDetails(blobId);
          
          // Set the flag for new files immediately
          localStorage.setItem('paletteHasNewFiles', 'true');
        }
      } catch (error) {
        console.error("Error setting up file preview:", error);
      }
      
      // Clear status immediately and refresh page - no delay
      setUploadStatus("");
      setUploadProgress(0);
      
      // After upload, return to first page to see the new files
      if (currentPage !== 1) {
        handlePageChange(1);
      } else {
        // Just reload the first page
        loadAssets(1);
      }
    },
    onError: (error: string) => {
      setUploadStatus(`Error uploading ${file.name}: ${error}`);
      
      // Clear error after a delay - reduced from 2000ms to 1000ms
      setTimeout(() => {
        setUploadStatus("");
        setUploadProgress(0);
      }, 1000);
    }
  }), [fetchAndUpdateBlobDetails, setFiles, loadAssets, currentPage, handlePageChange]);

  // Handle file drop with chunked upload
  const onDrop = useCallback(
    async (acceptedFiles: File[]) => {
      for (const file of acceptedFiles) {
        // Create file metadata
        const fileMeta = createFileMetadata(file);
        
        // Create a preview URL immediately
        const objectUrl = URL.createObjectURL(file);
        fileMeta.url = objectUrl;
        
        // Process file metadata (dimensions, etc.)
        await processFileMetadata(fileMeta);
        
        // Upload file in chunks
        setUploadStatus(`Starting upload of ${file.name}...`);
        setUploadProgress(0);
        
        const blobId = await uploadFileChunked(file, createUploadCallbacks(file, fileMeta));
        
        // If we got a blobId directly, update the file metadata
        if (blobId) {
          setFiles(prevFiles => {
            return prevFiles.map(f => {
              if (f.file === file) {
                return { 
                  ...f, 
                  blobId,
                  url: objectUrl,
                  isLoaded: true
                };
              }
              return f;
            });
          });
        }
      }
      
      // No need to reload here as each file upload already triggered a reload
      // This removes a redundant reload that could be causing additional delay
    },
    // Using only the dependencies that are actually needed
    [createFileMetadata, processFileMetadata, createUploadCallbacks, setFiles, setUploadStatus, setUploadProgress]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { "image/*": [], "video/*": [] },
  });

  const handleUploadNewDesign = useCallback(() => {
    setShowModal(true);
  }, []);

  // Add a function to handle newly uploaded files from the modal
  const handleFilesUploaded = useCallback((uploadedFiles: FileMetadata[]) => {
    console.log('Received uploaded files:', uploadedFiles);
    
    // Always navigate to page 1 to show new files
    if (currentPage !== 1) {
      // If not on page 1, navigate there first
      handlePageChange(1);
      
      // Then add files after a short delay to ensure page has changed
      setTimeout(() => {
        // Add the files to the beginning of the state
        setFiles(prev => {
          const newFiles = [...uploadedFiles, ...prev];
          return newFiles.slice(0, pageSize); // Keep only pageSize files
        });
        
        // Update total count
        setTotalCount(prev => prev + uploadedFiles.length);
        
        // Update total pages if needed
        const newTotalItems = totalCount + uploadedFiles.length;
        const newTotalPages = Math.ceil(newTotalItems / pageSize);
        if (newTotalPages > totalPages) {
          setTotalPages(newTotalPages);
        }
      }, 100);
    } else {
      // Already on page 1, just add files
      setFiles(prev => {
        const newFiles = [...uploadedFiles, ...prev];
        return newFiles.slice(0, pageSize); // Keep only pageSize files
      });
      
      // Update total count
      setTotalCount(prev => prev + uploadedFiles.length);
      
      // Update total pages if needed
      const newTotalItems = totalCount + uploadedFiles.length;
      const newTotalPages = Math.ceil(newTotalItems / pageSize);
      if (newTotalPages > totalPages) {
        setTotalPages(newTotalPages);
      }
    }
    
    // Set a subtle success message
    setUploadStatus(`Added ${uploadedFiles.length} new files to your palette.`);
    setTimeout(() => setUploadStatus(''), 3000);
  }, [setFiles, currentPage, handlePageChange, pageSize, totalCount, totalPages]);

  // Add a function to verify selection integrity
  const verifySelectionIntegrity = useCallback(async () => {
    // This function checks if all selectedBlobIds still exist on current page
    // and cleans up if any don't exist
    
    if (selectedBlobIds.length === 0) return selectedBlobIds;
    
    try {
      console.log("Verifying selection integrity...");
      
      const currentPageBlobIds = files
        .map(file => file.blobId)
        .filter((id): id is string => !!id);
      
      const currentPageSelections = selectedBlobIds.filter(id => 
        currentPageBlobIds.includes(id)
      );
      
      const otherPageSelections = selectedBlobIds.filter(id => 
        !currentPageBlobIds.includes(id)
      );
      
      const invalidSelections = currentPageSelections.filter(id => 
        !currentPageBlobIds.includes(id)
      );
      
      if (invalidSelections.length > 0) {
        console.log(`Found ${invalidSelections.length} invalid selections on current page`);
        
        const validSelections = selectedBlobIds.filter(id => 
          !invalidSelections.includes(id)
        );
        
        console.log('Updated valid selections:', validSelections);
        
        setSelectedBlobIds(validSelections);
        localStorage.setItem('paletteSelections', JSON.stringify(validSelections));
        
        return validSelections;
      }
      
      return selectedBlobIds;
    } catch (error) {
      console.error("Error verifying selection integrity:", error);
      return selectedBlobIds;
    }
  }, [selectedBlobIds, files]);

  useEffect(() => {
    if (!isLoading && files.length > 0) {
      verifySelectionIntegrity();
    }
  }, [files, isLoading, verifySelectionIntegrity]);

  const handleSubmitAssets = useCallback(async () => {
    const validSelections = await verifySelectionIntegrity();
    
    if (validSelections.length === 0) {
      toast.warn("No files selected!");
      return;
    }

    setIsLoading(true);
    
    setUploadStatus(`Submitting ${validSelections.length} assets...`);

    // Get project assignments for all selected blobIds
    const projectAssignments: Record<string, string[]> = {};
    const filesWithoutProject: string[] = [];
    
    // First check if we need to fetch details for any assets not on current page
    const currentPageBlobIds = files
      .map(file => file.blobId)
      .filter((id): id is string => !!id);
    
    const missingBlobIds = validSelections.filter(
      id => !currentPageBlobIds.includes(id)
    );
    
    // Get blobIds that will be submitted from the current page
    const currentPageBlobIdsToSubmit = files
      .filter(file => file.blobId && validSelections.includes(file.blobId))
      .map(file => file.blobId!)
      .filter(Boolean);
    
    // Fetch project info for assets not on current page
    if (missingBlobIds.length > 0) {
      try {
        const promises = missingBlobIds.map(async (blobId) => {
          try {
            const details = await fetchBlobDetails(blobId);
            return { blobId, project: details.project };
          } catch (err) {
            console.error(`Error fetching details for blobId ${blobId}:`, err);
            return { blobId, project: undefined };
          }
        });
        
        const results = await Promise.all(promises);
        
        // Sort results into projects or mark as missing project
        results.forEach(result => {
          if (result.project) {
            if (!projectAssignments[result.project]) {
              projectAssignments[result.project] = [];
            }
            projectAssignments[result.project].push(result.blobId);
          } else {
            filesWithoutProject.push(result.blobId);
          }
        });
      } catch (error) {
        console.error("Error fetching project details:", error);
        setIsLoading(false);
        setUploadStatus("");
        toast.error("Error fetching project details. Please try again.");
        return;
      }
    }
    
    files.forEach(file => {
      if (file.blobId && validSelections.includes(file.blobId)) {
        if (file.project) {
          if (!projectAssignments[file.project]) {
            projectAssignments[file.project] = [];
          }
          projectAssignments[file.project].push(file.blobId);
        } else {
          filesWithoutProject.push(file.blobId);
        }
      }
    });
    
    console.log('Project assignments:', projectAssignments);
    console.log('Files without project:', filesWithoutProject);
    
    if (filesWithoutProject.length > 0) {
      setIsLoading(false);
      setUploadStatus("");
      toast.warn(`Warning: ${filesWithoutProject.length} selected file(s) don't have a project assigned. Please select a project for all files before submitting.`);
      return;
    }

    try {
      let successCount = 0;
      
      for (const projectId in projectAssignments) {
        const blobIDs = projectAssignments[projectId];
        
        const success = await submitAssets(projectId, blobIDs, autoNamingEnabled ? "?Auto" : "");
        
        if (success) {
          successCount += blobIDs.length;
          
          setFiles((prev) =>
            prev.filter(
              (file) =>
                file.project !== projectId ||
                !file.blobId ||
                !blobIDs.includes(file.blobId)
            )
          );

          setSelectedBlobIds(prev => 
            prev.filter(blobId => !blobIDs.includes(blobId))
          );
        }
      }
      
      localStorage.setItem('paletteSelections', JSON.stringify(
        selectedBlobIds.filter(id => 
          !Object.values(projectAssignments).flat().includes(id)
        )
      ));
      
      const remainingFiles = totalCount - successCount;
      const newTotalPages = Math.max(1, Math.ceil(remainingFiles / pageSize));
      
      let targetPage = currentPage;
      
      if (currentPageBlobIdsToSubmit.length === files.length) {
        if (remainingFiles === 0) {
          targetPage = 1;
        } else if (currentPage > newTotalPages) {
          targetPage = newTotalPages;
        } else if (currentPage > 1) {
          targetPage = currentPage;
        }
      }
      
      setTotalPages(newTotalPages);
      setTotalCount(remainingFiles);
      
      if (successCount > 0) {
        setUploadStatus(`Successfully submitted ${successCount} asset(s).`);
        
        setTimeout(() => {
          setUploadStatus("");
          setUploadProgress(0);
          setIsLoading(false);
          
          loadAssets(targetPage);
        }, 1500);
      } else {
        setIsLoading(false);
        setUploadStatus("");
        toast.error("No assets were submitted. Please try again.");
      }
    } catch (error) {
      console.error("Error submitting assets:", error);
      setIsLoading(false);
      setUploadStatus("");
      toast.error("There was an error submitting assets. Please try again.");
    }
  }, [
    files, 
    selectedBlobIds, 
    setFiles, 
    autoNamingEnabled, 
    loadAssets, 
    currentPage,
    pageSize,
    totalCount,
    fetchBlobDetails, 
    verifySelectionIntegrity,
    setUploadStatus,
    setUploadProgress
  ]);

  const toggleAutoNaming = useCallback(() => {
    setAutoNamingEnabled(prev => !prev);
  }, []);

  // ----- Edit Metadata -----
  const handleEditMetadata = useCallback((index: number) => {
    const fileMeta = files[index];
    
    if (!fileMeta.project) {
      toast.warn("Please select a project before editing metadata.");
      return;
    }
    
    localStorage.setItem('palettePage', currentPage.toString());
    
    // Ensure selections are saved before navigating
    if (selectedBlobIds.length > 0) {
      localStorage.setItem('paletteSelections', JSON.stringify(selectedBlobIds));
      console.log('Saving selections before edit:', selectedBlobIds);
    }
    
    // Navigate to edit page
    router.push(
      `/palette/editmetadata?file=${encodeURIComponent(fileMeta.file.name)}&blobId=${fileMeta.blobId}`
    );
  }, [files, currentPage, router, selectedBlobIds]);

  // Check for background uploads
  useEffect(() => {
    const checkBackgroundUploads = () => {
      const inProgress = localStorage.getItem('bgUploadInProgress');
      setBgUploadInProgress(inProgress === 'true');
    };
    
    // Check immediately on mount
    checkBackgroundUploads();
    
    // Then check periodically
    const interval = setInterval(checkBackgroundUploads, 1000);
    
    return () => clearInterval(interval);
  }, []);
  
  // Check for newly uploaded files periodically
  useEffect(() => {
    const checkForNewFiles = () => {
      const hasNewFiles = localStorage.getItem('paletteHasNewFiles');
      if (hasNewFiles === 'true') {
        console.log('New files detected, available for refresh');
        // Since we're now automatically adding files, clear the flag
        localStorage.removeItem('paletteHasNewFiles');
        
        // And there's no need to show any refresh notification
        setBgUploadInProgress(false);
      }
    };
    
    // Check immediately on mount
    checkForNewFiles();
    
    // Then check more frequently (every 500ms instead of 3000ms)
    const interval = setInterval(checkForNewFiles, 500);
    
    return () => clearInterval(interval);
  }, []);
  
  // Listen for upload status updates from localStorage
  useEffect(() => {
    const checkUploadStatus = () => {
      const status = localStorage.getItem('uploadStatus');
      const progress = localStorage.getItem('uploadProgress');
      
      if (status) {
        setUploadStatus(status);
      }
      
      if (progress) {
        setUploadProgress(parseInt(progress, 10));
      }
    };
    
    // Check frequently for smoother progress updates
    const interval = setInterval(checkUploadStatus, 300);
    
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="min-h-screen bg-gradient-to-b p-6">
      <div className="max-w-8xl mx-auto"> 
        <div className="mb-8 bg-gradient-to-r from-blue-600 to-teal-500 px-6 py-6 rounded-xl shadow-lg text-white">
          <h1 className="text-3xl font-bold mb-2">Asset Palette</h1>
          <p className="text-white/80">Manage and submit your digital assets</p>
          {totalCount > 0 && (
            <div className="flex justify-between items-center">
              <p className="text-white/80">
                Total Assets: {totalCount} | 
                Selected: {selectedBlobIds.length} total, {selectedIndices.length} on this page
              </p>
              
              {selectedBlobIds.length > 0 && (
                <button 
                  onClick={clearAllSelections}
                  className="text-white/90 hover:text-white bg-red-500/30 hover:bg-red-500/50 px-3 py-1 rounded text-sm transition-colors duration-150"
                >
                  Clear All Selections
                </button>
              )}
            </div>
          )}
        </div>
        
        <div className="bg-white rounded-xl shadow-2xl overflow-hidden mb-8">
          {isLoading ? (
            <div className="flex justify-center items-center p-12">
              <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500" style={{ animationDuration: '0.6s' }}></div>
              <span className="ml-3 text-lg text-gray-700">Loading assets...</span>
            </div>
          ) : (
            <FileTable
              files={files}
              removeFile={removeFile}
              selectedIndices={selectedIndices}
              setSelectedIndices={setSelectedIndices}
              onSelectionChange={handleSelectionChange}
              selectedBlobIds={selectedBlobIds}
              projects={projects}
              currentPage={currentPage}
              totalPages={totalPages}
              onPageChange={handlePageChange}
              handleEditMetadata={handleEditMetadata}
            />
          )}
        </div>

        {/* Remove the refresh notification bar since files are now automatically added */}
        {uploadStatus && uploadStatus !== 'New files have been uploaded. Refresh to view them.' && (
          <div className="bg-white rounded-xl shadow-md p-6 mb-8">
            <h3 className="text-lg font-medium text-gray-700 mb-2">{uploadStatus}</h3>
            <Progress value={uploadProgress} />
          </div>
        )}

        <div className="bg-white p-8 rounded-xl shadow-2xl flex flex-col md:flex-row items-center justify-between gap-6">
          <div className="flex-1">
            <h2 className="text-xl font-semibold text-gray-800 mb-2">Ready to upload?</h2>
            <p className="text-gray-600">Add new assets to your palette or submit existing ones to projects</p>
          </div>
          
          <div className="flex flex-col sm:flex-row gap-4">
            <Button
              onClick={handleUploadNewDesign}
              className="bg-gradient-to-r from-blue-500 to-blue-600 hover:from-blue-600 hover:to-blue-700 text-white w-[220px] h-[50px] justify-center transition-all duration-150"
            >
              <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
              </svg>
              Upload Assets
            </Button>

            <div className="flex flex-col">
              <Button
                onClick={handleSubmitAssets}
                disabled={selectedBlobIds.length === 0}
                className={`w-[220px] h-[50px] justify-center transition-all duration-150 ${
                  selectedBlobIds.length > 0 
                    ? "bg-gradient-to-r from-teal-500 to-teal-600 hover:from-teal-600 hover:to-teal-700 text-white" 
                    : "bg-gray-300 text-gray-500"
                }`}
              >
                <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
                Submit Selected ({selectedBlobIds.length})
              </Button>
              
              <div className="flex items-center mt-2 justify-end">
                <button 
                  onClick={toggleAutoNaming}
                  className={`relative w-10 h-5 rounded-full transition-colors duration-150 focus:outline-none ${autoNamingEnabled ? 'bg-gradient-to-r from-teal-500 to-blue-500' : 'bg-gray-300'}`}
                  title="Auto rename files to [Project####__File###]"
                >
                  <span 
                    className={`absolute left-0.5 top-0.5 bg-white w-4 h-4 rounded-full shadow-md transform transition-transform duration-150 ${autoNamingEnabled ? 'translate-x-5' : ''}`}
                  />
                </button>
                <span className="text-xs font-medium ml-1 text-gray-700">Auto-naming</span>
              </div>
            </div>
          </div>
        </div>
        
        {showModal && (
          <UploadModal 
            projects={projects}
            closeModal={() => setShowModal(false)}
            createFileMetadata={createFileMetadata}
            fetchAndUpdateBlobDetails={fetchAndUpdateBlobDetails}
            onFilesUploaded={handleFilesUploaded}
          />
        )}
        
        {/* Delete Confirmation Modal */}
        {showDeleteConfirm && (
          <div className="fixed inset-0 flex items-center justify-center z-50 bg-black bg-opacity-50">
            <div className="bg-white rounded-lg p-6 shadow-xl max-w-md w-full">
              <h3 className="text-xl font-semibold text-gray-800 mb-4">Confirm Deletion</h3>
              <p className="text-gray-600 mb-6">
                This item is part of your selection. Would you like to delete all {selectedBlobIds.length} selected items?
              </p>
              <div className="flex justify-end space-x-3">
                <button 
                  className="px-4 py-2 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 transition-colors"
                  onClick={() => {
                    if (fileToDeleteIndex !== null) {
                      deleteFile(fileToDeleteIndex);
                    }
                    setShowDeleteConfirm(false);
                    setFileToDeleteIndex(null);
                    setFileToDeleteBlobId(null);
                  }}
                >
                  No, Just This Item
                </button>
                <button 
                  className="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600 transition-colors"
                  onClick={deleteAllSelected}
                >
                  Yes, Delete All Selected
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Remove the refresh notification floating bar */}
        {uploadStatus && uploadStatus.includes('Uploading') && (
          <div className="fixed bottom-4 right-4 bg-white p-4 rounded-lg shadow-lg z-50 w-80">
            <p className="text-sm font-medium mb-2">{uploadStatus}</p>
            <div className="w-full bg-gray-200 rounded-full h-2.5">
              <div 
                className="bg-gradient-to-r from-blue-500 to-teal-500 h-2.5 rounded-full transition-all duration-300" 
                style={{ width: `${uploadProgress}%` }}
              />
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
