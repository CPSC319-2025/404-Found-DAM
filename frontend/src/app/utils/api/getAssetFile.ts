import { ZstdCodec } from "zstd-codec";
import { fetchWithAuth } from "@/app/utils/api/api";
import { toast } from "react-toastify";


const loadImage = (src: string): Promise<HTMLImageElement> => {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.crossOrigin = "anonymous"; 
    img.onload = () => resolve(img);
    img.onerror = reject;
    img.src = src;
  });
};

export const getAssetFile = async (
  url: string,
  mimetype: string,
  shouldDecompress: boolean = false
): Promise<string> => {
  const response = await fetch(url);

  if (!response.ok) {
    throw new Error(`Fetch failed with status ${response.status}`);
  }

  const blob = await response.blob();
  const fileContent = new Uint8Array(await blob.arrayBuffer());

  if (!shouldDecompress) {
    const directBlob = new Blob([fileContent], { type: mimetype || "image/webp" });
    return URL.createObjectURL(directBlob);
  }

  return new Promise((resolve, reject) => {
    ZstdCodec.run((zstd: any) => {
      try {
        const simple = new zstd.Simple();
        const decompressed = simple.decompress(fileContent);

        const decompressedBlob = new Blob([decompressed], {
          type: mimetype || "image/webp",
        });

        const decompressedUrl = URL.createObjectURL(decompressedBlob);
        resolve(decompressedUrl);
      } catch (error) {
        reject(error);
      }
    });
  });
};

export const downloadAsset = async (asset: any, project: any, user: any, addWatermark: boolean = false) => {
  if (!asset.src) {
    throw new Error("Asset has not loaded yet! Please try again.")
  }




  try {
    const downloadLog = {
      userID: user.userID,
      changeType: "Downloaded",
      description:  `${user.email} downloaded '${asset.filename}' from project ${project.projectName}`,
      projID: Number(project.projectID),
      assetID: asset.blobID,
      isAdminAction: false
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

  if (addWatermark) {
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

      // const padding = 10;
      // const watermarkWidth = originalImage.width * 0.2;
      // const watermarkHeight =
      //   (watermarkImage.height / watermarkImage.width) * watermarkWidth;
      
      // Bound both width and height to prevent exceeding 20% of either dimension while preserving ratio 
      const maxWatermarkWidth = originalImage.width * 0.2;
      const maxWatermarkHeight = originalImage.height * 0.2;
      let watermarkWidth = maxWatermarkWidth;
      let watermarkHeight = (watermarkImage.height / watermarkImage.width) * watermarkWidth;
      
      if (watermarkHeight > maxWatermarkHeight) {
        watermarkHeight = maxWatermarkHeight;
        watermarkWidth = (watermarkImage.width / watermarkImage.height) * watermarkHeight;
      }

      // make padding proportional as well to avoid going out of bound
      const padding = Math.min(originalImage.width, watermarkHeight) * 0.05;

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
  } else {
    const a = document.createElement("a");
    a.href = asset.src;
    a.download = asset.filename || "downloaded-file";
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  }
};
