export function convertUtcToLocal(utcDateString: string): string {
  if (!utcDateString.endsWith('Z')) {
    utcDateString += 'Z';
  }

  const utcDate = new Date(utcDateString);

  return utcDate.toLocaleString();
}