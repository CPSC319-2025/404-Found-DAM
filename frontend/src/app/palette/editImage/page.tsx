"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { useFileContext } from "@/app/context/FileContext";
import { useEffect, useState } from "react";
import Cropper from "react-easy-crop";

export default function EditImagePage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const { files, updateMetadata, setFiles } = useFileContext();

  const fileName = searchParams?.get("file") ?? "";
  const fileIndex = files.findIndex((f) => f.file.name === fileName);
  const fileData = files[fileIndex];

  const [imageSource, setImageSource] = useState<string | null>(null);
  const [crop, setCrop] = useState({ x: 0, y: 0 });
  const [zoom, setZoom] = useState(1);
  const [rotation, setRotation] = useState(0);
  const [resize, setResize] = useState(1); // Resize Factor
  const [flip, setFlip] = useState({ horizontal: false, vertical: false });
  const [croppedAreaPixels, setCroppedAreaPixels] = useState<{
    x: number;
    y: number;
    width: number;
    height: number;
  } | null>(null);

  
  const [loadedImage, setLoadedImage] = useState<HTMLImageElement | null>(null);

  useEffect(() => {
    if (imageSource) {
      const img = new Image();
      img.src = imageSource;
      img.onload = () => setLoadedImage(img);
    }
  }, [imageSource]);

  const flipImage = (image: HTMLImageElement, flipHorizontal: boolean, flipVertical: boolean): string => {
    // Create a canvas to manipulate the image
    const canvas = document.createElement("canvas");
    const ctx = canvas.getContext("2d");
    if (!ctx) return "";
  
    // Set canvas dimensions
    canvas.width = image.width;
    canvas.height = image.height;
  
    // Flip the image
    ctx.save();
    ctx.translate(
      flipHorizontal ? canvas.width : 0,
      flipVertical ? canvas.height : 0
    );
    ctx.scale(flipHorizontal ? -1 : 1, flipVertical ? -1 : 1);
    ctx.drawImage(image, 0, 0);
    ctx.restore();
  
    // Return the flipped image data URL
    return canvas.toDataURL();
  };
  

  const handleFlip = (direction: "horizontal" | "vertical") => {
    // Toggle the flip state
    const newFlip = {
      horizontal: direction === "horizontal" ? true : false,
      vertical: direction === "vertical" ? true : false,
    };
    
    // checking: Hoi fixed
    // console.log("in handleFlip, hori: " + newFlip.horizontal);
    // console.log("in handleFlip, verti: " + newFlip.vertical);
    setFlip(newFlip);
  
    // Directly update the image source using the loaded image
    if (loadedImage) {
      const flippedImage = flipImage(loadedImage, newFlip.horizontal, newFlip.vertical);
      setImageSource(flippedImage); // Update the state with the flipped image
    }
  };
  

  useEffect(() => {
    if (fileData) {
      // If the file already has a URL (from previous loading), use that
      if (fileData.url) {
        setImageSource(fileData.url);
      } 
      // Otherwise, if we have a raw file with size, create a new URL
      else if (fileData.file instanceof File && fileData.file.size > 0) {
        const imageURL = URL.createObjectURL(fileData.file);
        setImageSource(imageURL);
      } 
      // If we get here, we don't have usable file content
      else {
        console.error("File data is missing or incomplete");
        alert("Unable to load image for editing. Please try again from the palette view.");
        router.push("/palette");
      }
    } else {
      // No file data found, redirect back to palette
      console.error("File not found in context");
      alert("File not found. Returning to palette.");
      router.push("/palette");
    }
  }, [fileData, router]);
  

  
  const onCropComplete = (
    croppedArea: any,
    croppedPixels: { x: number; y: number; width: number; height: number }
  ) => {
    setCroppedAreaPixels(croppedPixels);
  };


  const handleSaveImage = async () => {
    if (!imageSource || !croppedAreaPixels || !fileData) return;

    const image = new Image();
    image.src = imageSource;
    image.onload = () => {
      const canvas = document.createElement("canvas");
      const ctx = canvas.getContext("2d");
      if (!ctx) return;

      if (!croppedAreaPixels) return;

      const { x, y, width, height } = croppedAreaPixels;

      // Apply Resize in the Saving Process
      const resizedWidth = Math.round(width * resize);
      const resizedHeight = Math.round(height * resize);

      canvas.width = resizedWidth;
      canvas.height = resizedHeight;

      // Flip transformations
      ctx.translate(
        flip.horizontal ? resizedWidth : 0,
        flip.vertical ? resizedHeight : 0
      );
      ctx.scale(flip.horizontal ? -1 : 1, flip.vertical ? -1 : 1);

      // Apply rotation
      ctx.translate(resizedWidth / 2, resizedHeight / 2);
      ctx.rotate((rotation * Math.PI) / 180);
      ctx.translate(-resizedWidth / 2, -resizedHeight / 2);

      // Ensure the resized dimensions are used in `ctx.drawImage()`
      ctx.drawImage(
        image,
        x,
        y,
        width,
        height,
        0,
        0,
        resizedWidth,
        resizedHeight
      );

      canvas.toBlob(async (blob) => {
        if (blob) {
          const editedFile = new File([blob], fileData.file.name, {
            type: blob.type,
          });

          // Update FileContext with resized image
          setFiles((prevFiles) =>
            prevFiles.map((file, index) =>
              index === fileIndex ? { ...file, file: editedFile } : file
            )
          );

          updateMetadata(fileIndex, {
            fileSize: `${blob.size} bytes`,
            width: resizedWidth,
            height: resizedHeight,
            description: "Resized & Edited Image",
          });

          // Use blobId directly from fileData
          const blobId = fileData.blobId;
          
          if (!blobId) {
            alert("Failed to identify blob ID - no blobId in file metadata");
            return;
          }

          // Save to backend via API - update existing image using blob ID
          try {
            const formData = new FormData();
            formData.append("file", editedFile);
            formData.append("mimeType", blob.type);
            formData.append("userId", "1"); // Using mockedUserId from backend
            
            console.log("blobId", blobId);
            // Get auth token
            const token = localStorage.getItem("token");
            
            // Using PUT request to update existing asset
            const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets/${blobId}`, {
              method: "PUT",
              headers: {
                Authorization: token ? `Bearer ${token}` : "",
              },
              body: formData,
            });

            if (!response.ok) {
              throw new Error(`Failed to update image: ${response.statusText}`);
            }

            const result = await response.json();
            
            if (result.error) {
              alert(`Error updating image: ${result.error}`);
              return;
            }

            alert("Image updated successfully! Returning to palette...");
            router.push("/palette"); // Return to palette
          } catch (error) {
            console.error("Error updating image:", error);
            alert(`Failed to update image: ${error instanceof Error ? error.message : 'Unknown error'}`);
          }
        }
      }, fileData.file.type);
    };
  };

  if (!fileData) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-red-500 text-lg">Please go back to palette after refresh.</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-gray-100 p-4">
        <h1 className="text-5xl font-extrabold text-blue-400 mb-6">
        Edit Your Image
        </h1>
  
    {imageSource ? (
      <div className="relative w-full max-w-4xl h-[600px] mt-4 shadow-lg rounded-lg border border-gray-300 overflow-hidden bg-white">
        <Cropper
          image={imageSource}
          crop={crop}
          zoom={zoom * resize}
          rotation={rotation}
          onCropChange={setCrop}
          onZoomChange={setZoom}
          onRotationChange={setRotation}
          onCropComplete={onCropComplete}
          objectFit="contain"
          restrictPosition={false}
          showGrid={false}
        />
      </div>
    ) : (
      <p className="text-gray-600">No image selected yet!</p>
    )}
  
    {/* Resize Slider */}
    <div className="flex flex-col items-center gap-2 mt-6">
      <label className="text-gray-700 font-medium">Resize Image:</label>
      <input
        type="range"
        min="0.5"
        max="2"
        step="0.1"
        value={resize}
        onChange={(e) => setResize(parseFloat(e.target.value))}
        className="w-64 h-2 bg-gray-300 rounded-lg"
      />
      <span className="text-gray-700 font-semibold">{Math.round(resize * 100)}%</span>
    </div>

    
  
    {/* Action Buttons */}
    <div className="flex gap-4 mt-6">
      <button
        onClick={() => setRotation((prev) => prev + 90)}
        className="bg-blue-600 text-white font-semibold py-2 px-4 rounded-lg shadow-md hover:bg-blue-700 transition duration-300"
      >
        Rotate 90°
      </button>
  
      <button
        onClick={() => handleFlip("horizontal")}
        className="bg-blue-600 text-white font-semibold py-2 px-4 rounded-lg shadow-md hover:bg-blue-700 transition duration-300"
      >
        Flip Horizontally
      </button>
  
      <button
        onClick={() => handleFlip("vertical")}
        className="bg-blue-600 text-white font-semibold py-2 px-4 rounded-lg shadow-md hover:bg-blue-700 transition duration-300"
      >
        Flip Vertically
      </button>
  
      <button
        onClick={handleSaveImage}
        className="bg-green-600 text-white font-semibold py-2 px-4 rounded-lg shadow-md hover:bg-green-700 transition duration-300"
      >
        Save Image
      </button>
    </div>
  </div>
  
  );
}
