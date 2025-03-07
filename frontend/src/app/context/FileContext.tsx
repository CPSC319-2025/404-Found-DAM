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
  file: File;            // The raw file object
  fileName?: string;     // Optional custom name
  fileSize: string;
  description: string;
  location: string;
  tags: string[];
  width?: number;        // For images/videos
  height?: number;       // For images/videos
  duration?: number;     // For videos
  project?: string;      // Each file can store its own project name
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
