"use client";

import { useSearchParams } from "next/navigation";
import "react-image-crop/dist/ReactCrop.css";
import { useContext, useEffect, useState } from "react";
import ReactCrop, {type Crop} from 'react-image-crop';
import { useFileContext } from "@/app/context/FileContext";

function CropImage({ src } : any) {
  const [crop, setCrop] = useState<Crop>({
    unit : "%", 
    x: 25,
    y: 25,
    width: 50,
    height: 50
  })
  
  return (
    <div className="flex flex-col items-center">
      <ReactCrop crop={crop} onChange={c => setCrop(c)}>
        <img src={src} className="max-w-full h-auto" alt="Please pick an editable image!"/>
      </ReactCrop>
    </div>
  )
}


export default function EditImagePage() {
    const searchParams = useSearchParams();
    const fileName = searchParams.get("file");
    const [imageSource, setImageSource] = useState("");
    const {files} = useFileContext();


    useEffect(() =>{
      if(fileName) {
        const fileData = files.find((f) => f.file.name == fileName);
        if(fileData) {
          setImageSource(URL.createObjectURL(fileData.file));
        }
      }
    }, [fileName, files]);
      


    return (
      <div className="min-h-screen flex items-center justify-center">
        <section className="imageEditor-section">
        <h1 className="text-3xl font-bold">Editing: {fileName}</h1>
        <p className="text-gray-600">Here you can implement an image editor.</p>

        {imageSource ? <CropImage src = {imageSource} /> : <p> "No image selected yet!</p>}

        </section>
      </div>
    );
}
