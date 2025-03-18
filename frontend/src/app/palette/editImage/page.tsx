"use client";

import { useSearchParams, useRouter } from "next/navigation";
import "react-image-crop/dist/ReactCrop.css";
import { useContext, useEffect, useState, useRef } from "react";
import ReactCrop, { type Crop } from "react-image-crop";
import { useFileContext } from "@/app/context/FileContext";


function CropImage({ src, setCroppedDimensions, setFinalImage, setIsEditing }: any) {
  const [crop, setCrop] = useState<Crop>({
    unit: "%",
    x: 25,
    y: 25,
    width: 50,
    height: 50,
  });
  const imageRef = useRef<HTMLImageElement | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

  function handleCropChange(c: Crop) {
    setCrop(c);
    setCroppedDimensions({ width: c.width, height: c.height });
  }

  function handleCropComplete() {
    if (!imageRef.current || !canvasRef.current) return;
    const image = imageRef.current;
    const canvas = canvasRef.current;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    const scaleX = image.naturalWidth / image.width;
    const scaleY = image.naturalHeight / image.height;
    const pixelCrop = {
      x: crop.x * scaleX,
      y: crop.y * scaleY,
      width: crop.width * scaleX,
      height: crop.height * scaleY,
    };

    canvas.width = pixelCrop.width;
    canvas.height = pixelCrop.height;
    ctx.drawImage(
      image,
      pixelCrop.x,
      pixelCrop.y,
      pixelCrop.width,
      pixelCrop.height,
      0,
      0,
      pixelCrop.width,
      pixelCrop.height
    );

    canvas.toBlob((blob) => {
      if (blob) {
        const croppedUrl = URL.createObjectURL(blob);
        setFinalImage(croppedUrl);
        setCroppedDimensions({ width: pixelCrop.width, height: pixelCrop.height });
      }
    });
  }

  return (
    <div className="flex flex-col items-center">
      <ReactCrop crop={crop} onChange={handleCropChange} onComplete={handleCropComplete}>
        <img
          ref={imageRef}
          src={src}
          className="max-w-full h-auto"
          alt="Please pick an editable image!"
        />
      </ReactCrop>
      <canvas ref={canvasRef} className="hidden" />
      <div className="mt-4 flex space-x-4">
        <button
          onClick={() => setIsEditing(true)}
          className="bg-blue-500 hover:bg-blue-600 text-white font-semibold py-2 px-6 rounded-lg transition-colors duration-200"
        >
          Edit Image
        </button>
        <button
          onClick={() => setFinalImage((prev : any) => prev || src)}
          className="bg-teal-500 hover:bg-teal-600 text-white font-semibold py-2 px-6 rounded-lg transition-colors duration-200"
        >
          Save
        </button>
      </div>
    </div>
  );
}

export default function EditImagePage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const fileName = searchParams.get("file");
  const [imageSource, setImageSource] = useState("");
  const { files, updateMetadata } = useFileContext();
  const [croppedDimensions, setCroppedDimensions] = useState({ width: 0, height: 0 });
  const [finalImage, setFinalImage] = useState<string | null>(null);
  const [isEditing, setIsEditing] = useState(false);

  useEffect(() => {
    if (fileName) {
      const fileData = files.find((f) => f.file.name == fileName);
      if (fileData) {
        setImageSource(URL.createObjectURL(fileData.file));
      }
    }
  }, [fileName, files]);

  function handleSave() {
    if (!fileName) return; // Ensure fileName exists
    const fileIndex = files.findIndex((f) => f.file.name === fileName);
    if (fileIndex === -1) return;

    const imageToSave = finalImage || imageSource; // ✅ Ensures an image is always saved

    if (imageToSave) {
        fetch(imageToSave)
            .then(res => res.blob())
            .then(blob => {
                const newFileSize = blob.size.toString() + " bytes";

                updateMetadata(fileIndex, {
                    fileName: fileName,
                    description: finalImage ? "Cropped & Edited Image" : "Original Unedited Image",
                    fileSize: newFileSize,
                    width: finalImage ? croppedDimensions.width : files[fileIndex].width ?? 0,
                    height: finalImage ? croppedDimensions.height : files[fileIndex].height ?? 0,
                });

                router.push("/palette"); // ✅ Always navigate back to palette
            })
            .catch((error) => {
              alert("An error occured!!! during image saving after Cropping!"); 
              console.error("Image saving failed:", error);
              router.push("/palette")
            }); // ✅ Ensures navigation even if fetch fails
    } else {
        router.push("/palette"); // ✅ Ensures navigation even if no image is processed
    }
  }

  return (
    <div className="min-h-screen flex flex-col items-center justify-center">
      <section className="imageEditor-section">
        <h1 className="text-3xl font-bold">Editing: {fileName}</h1>
        <p className="text-gray-600">Crop and edit your image below.</p>

        {imageSource ? (
          <CropImage src={imageSource} setCroppedDimensions={setCroppedDimensions} setFinalImage={setFinalImage} setIsEditing={setIsEditing} />
        ) : (
          <p>"No image selected yet!"</p>
        )}

        <div className="mt-4 flex space-x-4">
          <button
            onClick={handleSave}
            className="bg-teal-500 hover:bg-teal-600 text-white font-semibold py-2 px-6 rounded-lg transition-colors duration-200"
          >
            Cropping and Editting Done
          </button>
        </div>
      </section>
    </div>
  );
}
