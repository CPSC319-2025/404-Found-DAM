"use client";

import React, { useCallback, useState, useEffect } from "react";
import { useDropzone } from "react-dropzone";
import { 
  fetchBlobDetails, 
  uploadFileChunked,
  UploadProgressCallbacks,
  Project
} from "../Apis";
import { FileMetadata, useFileContext } from "@/app/context/FileContext";

// Simple Progress component
const Progress = ({ 
  value 
}: { 
  value: number 
}) => (
  <div className="w-full bg-gray-200 rounded-full h-2.5 mb-4">
    <div 
      className="bg-indigo-600 h-2.5 rounded-full" 
      style={{ width: `${value}%` }}
    />
  </div>
);

interface UploadModalProps {
  projects: Project[];
  closeModal: () => void;
  createFileMetadata: (file: File) => FileMetadata;
  fetchAndUpdateBlobDetails: (blobId: string) => Promise<void>;
}

export default function UploadModal({
  projects,
  closeModal,
  createFileMetadata,
  fetchAndUpdateBlobDetails
}: UploadModalProps) {
  const { setFiles } = useFileContext();
  const [currentStep, setCurrentStep] = useState(1);
  const [uploadedFiles, setUploadedFiles] = useState<File[]>([]);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadingProgress, setUploadingProgress] = useState(0);
  const [uploadingFileName, setUploadingFileName] = useState("");
  const [selectedProject, setSelectedProject] = useState("");
  const [description, setDescription] = useState("");
  const [location, setLocation] = useState("");
  const [tagSuggestion, setTagSuggestion] = useState("");
  const [processingFiles, setProcessingFiles] = useState<{name: string, progress: number}[]>([]);
  const [processedFiles, setProcessedFiles] = useState<{name: string}[]>([]);
  
  // Auto-populate description and location when project is selected
  useEffect(() => {
    if (selectedProject) {
      const project = projects.find(p => p.projectID.toString() === selectedProject);
      if (project) {
        setDescription(project.description || "");
        setLocation(project.location || "");
      }
    }
  }, [selectedProject, projects]);
  
  // Handle dropping files
  const onModalDrop = useCallback((acceptedFiles: File[]) => {
    const newFiles = [...uploadedFiles, ...acceptedFiles];
    setUploadedFiles(newFiles);
    
    // Initialize processing files
    setProcessingFiles(prev => [
      ...prev,
      ...acceptedFiles.map(file => ({
        name: file.name, 
        progress: 0
      }))
    ]);
  }, [uploadedFiles]);
  
  const { getRootProps: getModalRootProps, getInputProps: getModalInputProps, isDragActive: isModalDragActive } = useDropzone({
    onDrop: onModalDrop,
    accept: { "image/*": [], "video/*": [], "application/pdf": [], "application/vnd.ms-powerpoint": [], 
             "application/vnd.openxmlformats-officedocument.presentationml.presentation": [], 
             "application/msword": [], "application/vnd.openxmlformats-officedocument.wordprocessingml.document": [] }
  });
  
  // Simulate progress for demo purposes
  useEffect(() => {
    if (processingFiles.length > 0) {
      // Simulate file processing
      const timer = setInterval(() => {
        setProcessingFiles(prev => {
          const updated = [...prev];
          const firstPending = updated.findIndex(f => f.progress < 100);
          
          if (firstPending >= 0) {
            const newProgress = Math.min(updated[firstPending].progress + 20, 100);
            updated[firstPending] = { ...updated[firstPending], progress: newProgress };
            
            // If file is complete, move to processedFiles but check for duplicates first
            if (newProgress === 100) {
              const fileName = updated[firstPending].name;
              setProcessedFiles(prev => {
                // Check if file is already in processedFiles to avoid duplicates
                if (!prev.some(f => f.name === fileName)) {
                  return [...prev, { name: fileName }];
                }
                return prev;
              });
            }
          } else {
            clearInterval(timer);
          }
          
          return updated;
        });
      }, 150); // Slower timer to reduce race conditions
      
      return () => clearInterval(timer);
    }
  }, [processingFiles.length]);
  
  // Remove a file from the upload list
  const removeUploadFile = useCallback((index: number) => {
    const fileToRemove = processingFiles[index];
    
    // Ensure we have a valid file to remove
    if (!fileToRemove) return;
    
    setUploadedFiles(prev => {
      return prev.filter(file => file.name !== fileToRemove.name);
    });
    
    setProcessingFiles(prev => {
      const newFiles = [...prev];
      newFiles.splice(index, 1);
      return newFiles;
    });
    
    // Also remove from processed files if it exists there
    setProcessedFiles(prev => {
      return prev.filter(file => file.name !== fileToRemove.name);
    });
  }, [processingFiles]);
  
  // Remove a processed file
  const removeProcessedFile = useCallback((index: number) => {
    const fileToRemove = processedFiles[index];
    
    // Ensure we have a valid file to remove
    if (!fileToRemove) return;
    
    setProcessedFiles(prev => {
      const newFiles = [...prev];
      newFiles.splice(index, 1);
      return newFiles;
    });
    
    // Also remove from uploaded files
    setUploadedFiles(prev => {
      return prev.filter(file => file.name !== fileToRemove.name);
    });
  }, [processedFiles]);
  
  // Handle confirming upload
  const handleConfirmUpload = useCallback(async () => {
    if (!selectedProject) {
      alert("Please select a project before uploading");
      return;
    }
    
    setIsUploading(true);
    
    for (const file of uploadedFiles) {
      // Create file metadata
      const fileMeta = createFileMetadata(file);
      fileMeta.description = description;
      fileMeta.location = location;
      fileMeta.project = selectedProject;
      
      // Upload file in chunks without adding to files state yet
      setUploadingFileName(file.name);
      setUploadingProgress(0);
      
      const callbacks: UploadProgressCallbacks = {
        onProgress: (progress: number, status: string) => {
          setUploadingProgress(progress);
        },
        onSuccess: async (blobId?: string) => {
          setUploadingProgress(100);
          
          if (blobId) {
            // Instead of using processFileMetadata (which adds to files state),
            // manually add the file with all metadata at once
            fileMeta.blobId = blobId;
            
            // Process dimensions if needed
            if (file.type.startsWith("image/")) {
              return new Promise<void>((resolve) => {
                const img = new Image();
                img.onload = () => {
                  fileMeta.width = img.width;
                  fileMeta.height = img.height;
                  setFiles(prev => [...prev, fileMeta]);
                  resolve();
                };
                img.src = URL.createObjectURL(file);
              });
            } else if (file.type.startsWith("video/")) {
              return new Promise<void>((resolve) => {
                const video = document.createElement("video");
                video.preload = "metadata";
                video.onloadedmetadata = () => {
                  fileMeta.width = video.videoWidth;
                  fileMeta.height = video.videoHeight;
                  fileMeta.duration = Math.floor(video.duration);
                  setFiles(prev => [...prev, fileMeta]);
                  resolve();
                };
                video.src = URL.createObjectURL(file);
              });
            } else {
              // For other file types, just add to files state
              setFiles(prev => [...prev, fileMeta]);
            }
            
            // Fetch and update blob details
            await fetchAndUpdateBlobDetails(blobId);
          }
        },
        onError: (error: string) => {
          alert(`Error uploading ${file.name}: ${error}`);
        }
      };
      
      await uploadFileChunked(file, callbacks);
    }
    
    setIsUploading(false);
    closeModal();
    setUploadedFiles([]);
    setCurrentStep(1);
    setProcessingFiles([]);
    setProcessedFiles([]);
  }, [uploadedFiles, selectedProject, description, location, createFileMetadata, fetchAndUpdateBlobDetails, setFiles, closeModal]);
  
  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl overflow-hidden">
        <div className="flex justify-between items-center p-4 border-b">
          <h2 className="text-xl font-semibold">Upload</h2>
          <button 
            onClick={closeModal}
            className="text-gray-500 hover:text-gray-700 text-2xl"
          >
            &times;
          </button>
        </div>
        
        <div className="p-6">
          {currentStep === 1 && (
            <div>
              <div 
                {...getModalRootProps()} 
                className="border-2 border-dashed border-gray-300 rounded-lg p-8 text-center cursor-pointer"
              >
                <input {...getModalInputProps()} />
                <div className="flex flex-col items-center justify-center">
                  <div className="mb-4">
                    <svg className="w-12 h-12 text-indigo-600" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path d="M7 16a4 4 0 0 0 4 4h2a4 4 0 0 0 4-4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                      <path d="M12 12V3M12 3L9 6M12 3L15 6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                  </div>
                  <h3 className="text-lg font-medium mb-2">Drag & drop files or <span className="text-indigo-600">Browse</span></h3>
                  <p className="text-sm text-gray-500">Supported formats: JPEG, PNG, GIF, MP4, PDF, PSD, AI, Word, PPT</p>
                </div>
              </div>
              
              <div className="mt-6 max-h-60 overflow-y-auto">
                {/* If there are files, show a header */}
                {(processingFiles.length > 0 || processedFiles.length > 0) && (
                  <h3 className="font-medium mb-4">Uploading - {uploadedFiles.length} files</h3>
                )}
                
                {/* Processing Files */}
                {processingFiles.filter(f => f.progress < 100).map((file, index) => (
                  <div key={`processing-${index}`} className="mb-4">
                    <div className="py-3 px-4 border rounded-lg mb-2 flex justify-between items-center">
                      <span>{file.name}</span>
                      <button 
                        onClick={() => removeUploadFile(index)}
                        className="text-gray-500 hover:text-red-500"
                      >
                        &times;
                      </button>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2.5">
                      <div 
                        className="bg-indigo-600 h-2.5 rounded-full" 
                        style={{ width: `${file.progress}%` }}
                      />
                    </div>
                  </div>
                ))}
                
                {/* Uploaded Files */}
                {processedFiles.length > 0 && (
                  <div className="mt-4">
                    <h3 className="font-medium mb-2">Uploaded</h3>
                    {processedFiles.map((file, index) => (
                      <div key={`uploaded-${index}`} className="py-3 px-4 border border-green-500 rounded-lg mb-2 flex justify-between items-center">
                        <span>{file.name}</span>
                        <button
                          onClick={() => removeProcessedFile(index)}
                          className="text-red-500 hover:text-red-700"
                        >
                          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                            <path fillRule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clipRule="evenodd" />
                          </svg>
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          )}
          
          {currentStep === 2 && (
            <div>
              <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Project <span className="text-red-500">*</span></label>
                <select 
                  className="w-full px-3 py-2 border rounded-md"
                  value={selectedProject}
                  onChange={(e) => setSelectedProject(e.target.value)}
                  required
                >
                  <option value="">Select Project</option>
                  {projects.map((project) => (
                    <option key={project.projectID} value={project.projectID.toString()}>
                      {project.projectName}
                    </option>
                  ))}
                </select>
              </div>
              
              <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Description</label>
                <textarea 
                  className={`w-full px-3 py-2 border rounded-md ${selectedProject ? 'bg-gray-100' : ''}`}
                  placeholder="Enter description..."
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  readOnly={!!selectedProject}
                />
                {selectedProject && (
                  <p className="mt-1 text-xs text-gray-500">Auto-populated from project settings</p>
                )}
              </div>
              
              <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Location</label>
                <input 
                  type="text"
                  className={`w-full px-3 py-2 border rounded-md ${selectedProject ? 'bg-gray-100' : ''}`}
                  placeholder="Enter Location..."
                  value={location}
                  onChange={(e) => setLocation(e.target.value)}
                  readOnly={!!selectedProject}
                />
                {selectedProject && (
                  <p className="mt-1 text-xs text-gray-500">Auto-populated from project settings</p>
                )}
              </div>
              
              <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Tag Suggestions</label>
                <input 
                  type="text"
                  className="w-full px-3 py-2 border rounded-md"
                  placeholder="Suggest tags to Project Admin..."
                  value={tagSuggestion}
                  onChange={(e) => setTagSuggestion(e.target.value)}
                />
              </div>
              
              <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Project Tags</label>
                <div className="w-full px-3 py-2 border rounded-md text-gray-400">
                  Project tags will be displayed here ...
                </div>
              </div>
            </div>
          )}
          
          {isUploading && (
            <div className="mt-4">
              <p className="text-sm">Uploading {uploadingFileName}</p>
              <Progress value={uploadingProgress} />
            </div>
          )}
          
          <div className="mt-6 flex justify-center">
            <button
              onClick={() => {
                if (currentStep === 1) {
                  // Only allow proceeding if there are files and they're all processed
                  if (uploadedFiles.length > 0 && processingFiles.every(f => f.progress === 100)) {
                    setCurrentStep(2);
                  }
                } else {
                  handleConfirmUpload();
                }
              }}
              disabled={isUploading || 
                       (currentStep === 1 && (uploadedFiles.length === 0 || !processingFiles.every(f => f.progress === 100)))}
              className="w-full py-3 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isUploading ? "Uploading..." : 
               currentStep === 2 ? "UPLOAD" : "CONTINUE"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
} 