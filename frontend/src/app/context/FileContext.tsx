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
  file?: File; // Optional raw file object (used during upload only)
  filePath: string; // Path to the file in storage
  fileName: string; // Display name of the file
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
  mimeType: string; // Store the file type
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
