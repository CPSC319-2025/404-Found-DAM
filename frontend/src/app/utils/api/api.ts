export async function fetchWithAuth(
  endpoint: string,
  options: RequestInit = {}
) {
  const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL;
  const url = new URL(endpoint, baseUrl).toString();

  console.log(`Fetching ${url}`);

  const token =
    typeof window !== "undefined" ? localStorage.getItem("token") : null;

  const AUTH_EXCLUDED_ROUTES = ["/auth/login"];

  const headers = new Headers(
    options.headers || { "Content-Type": "application/json" }
  );

  if (options.body instanceof FormData) {
    headers.delete("Content-Type"); // Ensure we don't overwrite FormData's Content-Type
  }

  if (token && !AUTH_EXCLUDED_ROUTES.includes(new URL(url).pathname)) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  return await fetch(url, { ...options, headers });
}
