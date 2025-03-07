namespace Core.Dtos
{
    public class SubmitAssetsReq
    {
        public List<int> blobIDs { get; set; }
    }

    public class SubmitAssetsRes
    {
        // public int projectID {get; set; }
        // public List<AssignedAsset> assignedAssets { get; set; }
        public DateTime submittedAt { get; set; }
    }

    // public class AssignedAsset
    // {
    //     public int id { get; set; }
    //     public string filename { get; set; }
    // }
}