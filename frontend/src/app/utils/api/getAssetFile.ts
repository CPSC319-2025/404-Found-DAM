import { ZstdCodec } from "zstd-codec";
import { fetchWithAuth } from "@/app/utils/api/api";


const loadImage = (src: string): Promise<HTMLImageElement> => {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.crossOrigin = "anonymous"; 
    img.onload = () => resolve(img);
    img.onerror = reject;
    img.src = src;
  });
};

export const getAssetFile = async (url: string, mimetype: string) => {
  const response = await fetch(url);

  if (!response.ok) {
    throw new Error(`Fetch failed with status ${response.status}`);
  }

  const blob = await response.blob();
  const fileContent = new Uint8Array(await blob.arrayBuffer());

  return new Promise((resolve, reject) => {
    ZstdCodec.run((zstd: any) => {
      try {
        const simple = new zstd.Simple();
        const decompressed = simple.decompress(fileContent);

        const decompressedBlob = new Blob([decompressed], {
          type: mimetype || "image/webp",
        });

        const url = URL.createObjectURL(decompressedBlob);
        resolve(url);
      } catch (error) {
        reject(error);
      }
    });
  });
};

export const downloadAsset = async (asset: any, project: any, user: any) => {
  if (!asset.src) {
    throw new Error("Asset has not loaded yet! Please try again.");
  }


  try {
    const downloadLog = {
      userID: user.userID,
      changeType: "Downloaded",
      description: `${user.email} downloaded '${asset.filename}' from project ${project.projectName}`,
      projID: Number(project.projectID),
      assetID: asset.blobID,
      isAdminAction: false,
    };

    const response = await fetchWithAuth("addLog", {
      method: "POST",
      body: JSON.stringify(downloadLog),
    });

    if (!response.ok) {
      throw new Error("Unable to download asset!");
    }
  } catch (error) {
    throw new Error((error as Error).message);
  }

  try {
    const originalImage = await loadImage(asset.src);
    const watermarkImage = await loadImage("/images/ae_logo_blue.svg");

    const canvas = document.createElement("canvas");
    canvas.width = originalImage.width;
    canvas.height = originalImage.height;
    const ctx = canvas.getContext("2d");
    if (!ctx) {
      throw new Error("Could not get canvas context");
    }

    
    ctx.drawImage(originalImage, 0, 0);

    // Define watermark properties
    const padding = 10;
    const watermarkWidth = originalImage.width * 0.2; // 20% of asset width
    const watermarkHeight =
      (watermarkImage.height / watermarkImage.width) * watermarkWidth;
    const x = originalImage.width - watermarkWidth - padding;
    const y = originalImage.height - watermarkHeight - padding;

    ctx.globalAlpha = 0.5;
    ctx.drawImage(watermarkImage, x, y, watermarkWidth, watermarkHeight);
    ctx.globalAlpha = 1; 

    canvas.toBlob((blob) => {
      if (!blob) {
        throw new Error("Canvas is empty");
      }
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = asset.filename || "downloaded-file";
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
    
      URL.revokeObjectURL(url);
    }, asset.mimetype || "image/webp");
  } catch (error) {
    console.error("Error processing watermark:", error);
    throw new Error("Failed to apply watermark");
  }
};
