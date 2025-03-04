namespace Core.Dtos
{
    public class AddAssetsToProjectReq
    {
        public int projectID { get; set; }
        public List<int> blobIDs { get; set; }
    }

    public class AddAssetsToProjectRes
    {
        // public int projectID {get; set; }
        // public List<AssignedAsset> assignedAssets { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    // public class AssignedAsset
    // {
    //     public int id { get; set; }
    //     public string filename { get; set; }
    // }
}