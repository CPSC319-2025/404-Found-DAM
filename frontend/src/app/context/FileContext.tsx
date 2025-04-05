"use client";

import React, {
  createContext,
  useContext,
  useState,
  ReactNode,
  Dispatch,
  SetStateAction,
} from "react";

export type FileMetadata = {
  file: File; // The raw file object
  fileName?: string; // Optional custom name
  fileSize: string;
  description: string;
  location: string;
  tags: string[];
  tagIds: number[];
  width?: number; // For images/videos
  height?: number; // For images/videos
  duration?: number; // For videos
  project?: string; // Each file can store its own project name
  blobId?: string;
  metadata?: Record<number, any>; // For custom metadata fields
  url?: string; // Direct object URL for image display
  blobUri?: string; // URI to fetch the blob from the server
  isLoaded?: boolean; // Flag to indicate if file content has been loaded
};

type FileContextType = {
  files: FileMetadata[];
  setFiles: Dispatch<SetStateAction<FileMetadata[]>>;
  updateMetadata: (index: number, metadata: Partial<FileMetadata>) => void;
};

const FileContext = createContext<FileContextType>({
  files: [],
  setFiles: () => {},
  updateMetadata: () => {},
});

export function FileProvider({ children }: { children: ReactNode }) {
  const [files, setFiles] = useState<FileMetadata[]>([]);

  function updateMetadata(index: number, metadata: Partial<FileMetadata>) {
    setFiles((prevFiles) =>
      prevFiles.map((f, i) => (i === index ? { ...f, ...metadata } : f))
    );
  }

  return (
    <FileContext.Provider value={{ files, setFiles, updateMetadata }}>
      {children}
    </FileContext.Provider>
  );
}

export function useFileContext() {
  return useContext(FileContext);
}
