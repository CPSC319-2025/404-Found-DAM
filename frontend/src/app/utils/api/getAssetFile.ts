// gets zstd compressed file and returns file from it
import { ZstdCodec } from "zstd-codec";

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

        const decompressedBlob = new Blob([decompressed], { type: mimetype || "image/webp" });

        const url = URL.createObjectURL(decompressedBlob);
        resolve(url);
      } catch (error) {
        reject(error);
      }
    });
  });
};

export const downloadAsset = (asset: any) => {
  if (!asset.src) {
    throw new Error("Asset has not loaded yet! Please try again.")
  }

  const a = document.createElement("a");
  a.href = asset.src;
  a.download = asset.filename || "downloaded-file";
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
};

