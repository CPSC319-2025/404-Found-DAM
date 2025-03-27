/**
 * Submits assets to a project
 * @param projectId The project ID to submit assets to
 * @param blobIds Array of blob IDs to submit
 * @returns True if successful, false otherwise
 */
export async function submitAssets(projectId: string, blobIds: string[]): Promise<boolean> {
  try {
    const response = await fetch(
      `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/${projectId}/submit-assets`,
      {
        method: "PATCH",
        headers: {
          Authorization: "Bearer MY_TOKEN",
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ blobIDs: blobIds }), // e.g. { "blobIDs": [123, 456] }
      }
    );

    if (!response.ok) {
      console.error("Submit assets failed:", response.status);
      return false;
    }

    const data = await response.json();
    console.log("Submission success:", data);
    
    return true;
  } catch (err) {
    console.error("Error submitting assets:", err);
    return false;
  }
} 