import { fetchWithAuth } from "@/app/utils/api/api";
import { toast } from "react-toastify";

/**
 * Submits assets to a project
 * @param projectId The project ID to submit assets to
 * @param blobIds Array of blob IDs to submit
 * @param autoNaming Optional parameter for automated naming convention
 * @returns True if successful, false otherwise
 */
export async function submitAssets(projectId: string, blobIds: string[], autoNaming: string = ""): Promise<boolean> {
  try {
    const url = `palette/${projectId}/submit-assets${autoNaming}`;
    const response = await fetchWithAuth(url, {
      method: "PATCH",
      body: JSON.stringify({ blobIDs: blobIds }), // e.g. { "blobIDs": [123, 456] }
    })

    

    if (!response.ok) {
      if (response.status === 403) {
        const errorData = await response.json();
        toast.error("Some assets were not submitted: " + errorData.detail + " Refreshing..."); // eg. "You cannot submit assets to archived project {projectName}"
        // console.log("sean0");
        setTimeout(() => {
          window.location.reload();
  
        }, 3000);
        
        // return false;
      } else {
        console.error("Submit assets failed:", response.status);
        return false;
      }
    }

    const data = await response.json();
    console.log("Submission success:", data);
    
    return true;
  } catch (err) {
    console.error("Error submitting assets:", err);
    return false;
  }
} 