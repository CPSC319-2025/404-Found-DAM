"use client";

import { ZstdCodec } from "zstd-codec";

export async function compressFileZstd(file: File): Promise<File> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();

    reader.onload = (e) => {
      const result = e?.target?.result;
      if (!result) {
        reject(new Error("Failed to read file array buffer."));
        return;
      }

      const inputData = new Uint8Array(result as ArrayBuffer);

      // Initialize zstd by calling ZstdCodec.run(...)
      ZstdCodec.run((zstd: any) => {
        try {
          const simpleZstd = new zstd.Simple();
          const compressedData = simpleZstd.compress(inputData);

          const compressedBlob = new Blob([compressedData], {
            type: "application/zstd",
          });
          const compressedFile = new File(
            [compressedBlob],
            file.name + ".zst",
            {
              type: "application/zstd",
              lastModified: Date.now(),
            }
          );

          resolve(compressedFile);
        } catch (error) {
          reject(error);
        }
      });
    };

    reader.onerror = reject;
    reader.readAsArrayBuffer(file);
  });
}
