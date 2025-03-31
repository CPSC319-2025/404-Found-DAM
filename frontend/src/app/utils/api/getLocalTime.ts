export function convertUtcToLocal(utcDateString) {
  if (!utcDateString.endsWith('Z')) {
    utcDateString += 'Z';
  }

  const utcDate = new Date(utcDateString);

  return utcDate.toLocaleString();
}