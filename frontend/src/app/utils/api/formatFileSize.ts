export function formatFileSize(sizeInKB: number) {
  if (sizeInKB >= 1024 * 1024) {
    return (sizeInKB / (1024 * 1024)).toFixed(2) + " GB";
  } else if (sizeInKB >= 1024) {
    return (sizeInKB / 1024).toFixed(2) + " MB";
  } else {
    return sizeInKB.toFixed(2) + " KB";
  }
}

