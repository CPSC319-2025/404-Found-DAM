"use client";

import { useSearchParams, useRouter } from "next/navigation";
import "react-image-crop/dist/ReactCrop.css";
import { useContext, useEffect, useState } from "react";
import ReactCrop, { type Crop } from "react-image-crop";
import { useFileContext } from "@/app/context/FileContext";


function CropImage({ src, setCroppedDimensions }: any) {
  const [crop, setCrop] = useState<Crop>({
    unit: "%",
    unit: "%",
    x: 25,
    y: 25,
    width: 50,
    height: 50,
  });

  function handleCropChange(c: Crop) {
    setCrop(c);
    setCroppedDimensions({ width: c.width, height: c.height });
  }

  return (
    <div className="flex flex-col items-center">
      <ReactCrop crop={crop} onChange={handleCropChange}>
        <img
          src={src}
          className="max-w-full h-auto"
          alt="Please pick an editable image!"
        />
      </ReactCrop>
    </div>
  );
  );
}

export default function EditImagePage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const fileName = searchParams.get("file");
  const [imageSource, setImageSource] = useState("");
  const { files, updateMetadata } = useFileContext();
  const [croppedDimensions, setCroppedDimensions] = useState({ width: 0, height: 0 });

  useEffect(() => {
    if (fileName) {
      const fileData = files.find((f) => f.file.name == fileName);
      if (fileData) {
        setImageSource(URL.createObjectURL(fileData.file));
      }
    }
  }, [fileName, files]);

  function handleSave() {
    if (!fileName) return;
    const fileIndex = files.findIndex((f) => f.file.name === fileName);
    if (fileIndex === -1) return;

    const fileData = files[fileIndex];

    updateMetadata(fileIndex, {
      fileName: fileName,
      description: "Cropped Image",
      fileSize: fileData.file.size.toString() + " bytes", // Converting file size to string
      width: croppedDimensions.width,
      height: croppedDimensions.height,
    });

    router.push("/palette"); // Navigate back after saving
  }

  return (
    <div className="min-h-screen flex flex-col items-center justify-center">
      <section className="imageEditor-section">
        <h1 className="text-3xl font-bold">Editing: {fileName}</h1>
        <p className="text-gray-600">Here you can implement an image editor.</p>

        {imageSource ? (
          <CropImage src={imageSource} setCroppedDimensions={setCroppedDimensions} />
        ) : (
          <p>"No image selected yet!"</p>
        )}

        <button
          onClick={handleSave}
          className="mt-4 bg-teal-500 hover:bg-teal-600 text-white font-semibold py-2 px-6 rounded-lg transition-colors duration-200"
        >
          Save
        </button>
      </section>
    </div>
  );
}
