"use client";

import React, {
    createContext,
    useContext,
    useState,
    ReactNode,
    Dispatch,
    SetStateAction,
} from "react";

// Define metadata structure for images & videos
export type FileMetadata = {
    file: File;         // The raw File object
    fileName?: string;  // Optional custom name (defaults to file.name)
    fileSize: string;
    description: string;
    location: string;
    tags: string[];
    width?: number;     // Only for images/videos
    height?: number;    // Only for images/videos
    duration?: number;  // Only for videos
};

// Define context type using built-in React types for setFiles
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
