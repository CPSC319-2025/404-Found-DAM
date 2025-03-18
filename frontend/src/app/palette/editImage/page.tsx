"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { useFileContext } from "@/app/context/FileContext";
import { useEffect, useState } from "react";
import Cropper from "react-easy-crop";

export default function EditImagePage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const { files, updateMetadata, setFiles } = useFileContext();

  const fileName = searchParams.get("file");
  const fileIndex = files.findIndex((f) => f.file.name === fileName);
  const fileData = files[fileIndex];

  const [imageSource, setImageSource] = useState<string | null>(null);
  const [crop, setCrop] = useState({ x: 0, y: 0 });
  const [zoom, setZoom] = useState(1);
  const [rotation, setRotation] = useState(0);
  const [resize, setResize] = useState(1); // ✅ Resize Factor
  const [flip, setFlip] = useState({ horizontal: false, vertical: false });
  const [croppedAreaPixels, setCroppedAreaPixels] = useState<{ x: number; y: number; width: number; height: number } | null>(null);

  useEffect(() => {
    if (fileData) {
      const imageURL = URL.createObjectURL(fileData.file);
      setImageSource(imageURL);
    }
  }, [fileData]);

  const onCropComplete = (croppedArea: any, croppedPixels: { x: number; y: number; width: number; height: number }) => {
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

      let { x, y, width, height } = croppedAreaPixels;

      // ✅ Apply Resize in the Saving Process
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

      // ✅ Ensure the resized dimensions are used in `ctx.drawImage()`
      ctx.drawImage(image, x, y, width, height, 0, 0, resizedWidth, resizedHeight);

      canvas.toBlob((blob) => {
        if (blob) {
          const editedFile = new File([blob], fileData.file.name, { type: blob.type });

          // ✅ Update FileContext with resized image
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

          alert("Image saved successfully! Returning to palette...");
          router.push("/palette"); // ✅ Return to palette
        }
      });
    };
  };

  if (!fileData) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-red-500 text-lg">File not found in context.</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex flex-col items-center justify-center">
      <h1 className="text-3xl font-bold">Edit Image</h1>

      {imageSource ? (
        <div className="relative w-full max-w-md h-96 mt-4">
          <Cropper
            image={imageSource}
            crop={crop}
            zoom={zoom * resize}  // ✅ Resize applied to the Cropper in real-time
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

      {/* ✅ Resize Slider (Now Works in Real-Time) */}
      <div className="flex flex-col items-center gap-2 mt-4">
        <label className="text-gray-700 font-medium">Resize Image:</label>
        <input
          type="range"
          min="0.5"
          max="2"
          step="0.1"
          value={resize}
          onChange={(e) => setResize(parseFloat(e.target.value))}
          className="w-48"
        />
        <span className="text-gray-700">{Math.round(resize * 100)}%</span>
      </div>

      <div className="flex gap-4 mt-4">
        <button
          onClick={() => setRotation((prev) => prev + 90)}
          className="bg-blue-500 text-white py-2 px-4 rounded"
        >
          Rotate 90°
        </button>
        <button
          onClick={() => setFlip({ ...flip, horizontal: !flip.horizontal })}
          className="bg-blue-500 text-white py-2 px-4 rounded"
        >
          Flip Horizontally
        </button>
        <button
          onClick={() => setFlip({ ...flip, vertical: !flip.vertical })}
          className="bg-blue-500 text-white py-2 px-4 rounded"
        >
          Flip Vertically
        </button>
        <button
          onClick={handleSaveImage}
          className="bg-green-500 text-white py-2 px-4 rounded"
        >
          Save Image
        </button>
      </div>
    </div>
  );
}
