namespace Core.Dtos
{
    public class SubmitAssetsReq
    {
        public List<string> blobIDs { get; set; }
    }

    public class SubmitAssetsRes
    {
        // public List<AssignedAsset> assignedAssets { get; set; }
         public int projectID {get; set; }
         public List<string> successfulSubmissions {get; set; }
         public List<string> failedSubmissions {get; set; }
         public DateTime submittedAt { get; set; }
    }
}