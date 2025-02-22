"use client";

import React, { createContext, useContext, useState, ReactNode } from "react";

// Shape of our context data
type FileContextType = {
    files: File[];
    setFiles: (files: (prevFiles) => any[]) => void;
};

const FileContext = createContext<FileContextType>({
    files: [],
    setFiles: () => {},
});

export function FileProvider({ children }: { children: ReactNode }) {
    const [files, setFiles] = useState<File[]>([]);

    return (
        <FileContext.Provider value={{ files, setFiles }}>
            {children}
        </FileContext.Provider>
    );
}

// Custom hook for easy usage
export function useFileContext() {
    return useContext(FileContext);
}
