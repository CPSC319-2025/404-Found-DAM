"use client";

import React, { createContext, useContext, useState, ReactNode } from "react";

// Define metadata structure for images & videos
export type FileMetadata = {
    file: File;
    fileSize: string;
    description: string;
    location: string;
    tags: string[];
    width?: number;  // Only for images/videos
    height?: number; // Only for images/videos
    duration?: number; // Only for videos
};

// Define context type
type FileContextType = {
    files: FileMetadata[];
    // Setter expects a callback that takes previous array of FileMetadata
    setFiles: (
        setter: (prev) => (FileMetadata | File)[]
    ) => void;
    // Update metadata for a specific item by index
    updateMetadata: (index: number, metadata: Partial<FileMetadata>) => void;
};

// Create context
const FileContext = createContext<FileContextType>({
    files: [],
    setFiles: () => {},
    updateMetadata: () => {},
});

export function FileProvider({ children }: { children: ReactNode }) {
    const [files, setFiles] = useState<FileMetadata[]>([]);

    // Update metadata for a single file by index
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

// Custom hook for easy usage
export function useFileContext() {
    return useContext(FileContext);
}
