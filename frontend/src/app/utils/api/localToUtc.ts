export function getStartOfDayUtc(date: string): string {
  const [year, month, day] = date.split("-").map(Number);

  const localDate = new Date(year, month - 1, day, 0, 0, 0, 0);

  return localDate.toISOString();
}


export function getEndOfDayUtc(date: string): string {
  const [year, month, day] = date.split("-").map(Number);

  const localDate = new Date(year, month - 1, day, 23, 59, 59, 999);

  return localDate.toISOString();
}