// gets zstd compressed file and returns file from it
import { ZstdCodec } from "zstd-codec";
import { fetchWithAuth } from "@/app/utils/api/api";

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

export const downloadAsset = async (asset: any) => {
  if (!asset.src) {
    throw new Error("Asset has not loaded yet! Please try again.")
  }

  // TODO: take in user, project
  try {
    const downloadLog = {
      //   userID = user.userID,
      //   changeType = "Downloaded",
      // // TODO: fix this
      //   description = `${user.email} downloaded ${asset.filename} from project ${project.name}`,
      // // TODO: fix
      //   projID = project.projectID,
      //   assetID = asset.blobID,
      //   isAdminAction = false
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

  const a = document.createElement("a");
  a.href = asset.src;
  a.download = asset.filename || "downloaded-file";
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
};
