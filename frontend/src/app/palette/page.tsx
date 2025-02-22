"use client";

import { useFileContext } from "../FileContext"; // adjust path if needed
import { useRouter } from "next/navigation";

export default function PalettePage() {
    const router = useRouter();
    const { files, setFiles } = useFileContext();

    // Example: remove one file by index
    function removeFile(index: number) {
        const updated = [...files];
        updated.splice(index, 1);
        setFiles(updated);
    }

    function handleGoBack() {
        router.push("/upload"); // Or home, whichever route you prefer
    }

    return (
        <div className="p-6 min-h-screen">
            <h1 className="text-2xl fond-bold mb-4">Palette</h1>

            {/* If no files, prompt the user to go back */}
            {files.length === 0 ? (
                <>
                    <p className="mb-4">No files to modify!</p>
                    <button
                        onClick={handleGoBack}
                        className="px-4 py-2 bg-teal-500 text-white rounded"
                    >
                        Go Back
                    </button>
                </>
            ) : (
                <div className="space-y-4">
                    {files.map((file, index) => {
                        // Create a temporary URL to preview the file
                        const previewUrl = URL.createObjectURL(file);
                        const isImage = file.type.startsWith("image/");
                        const isVideo = file.type.startsWith("video/");

                        return (
                            <div key={index} className="bg-white p-4 rounded shadow relative">
                                <p className="font-medium mb-2">{file.name}</p>

                                {/* Preview image or video */}
                                {isImage && (
                                    <img
                                        src={previewUrl}
                                        alt="Preview"
                                        className="max-w-md max-h-60 object-cover mb-2"
                                    />
                                )}
                                {isVideo && (
                                    <video
                                        src={previewUrl}
                                        controls
                                        className="max-w-md max-h-60 mb-2"
                                    />
                                )}

                                {/* Remove File button */}
                                <button
                                    onClick={() => removeFile(index)}
                                    className="absolute top-2 right-2 bg-red-500 text-white text-sm px-2 py-1 rounded"
                                >
                                    Remove
                                </button>
                            </div>
                        );
                    })}

                    <button
                        onClick={handleGoBack}
                        className="px-4 py-2 bg-teal-500 text-white rounded"
                    >
                        Done / Go Back
                    </button>
                </div>
            )}
        </div>
    );
}

