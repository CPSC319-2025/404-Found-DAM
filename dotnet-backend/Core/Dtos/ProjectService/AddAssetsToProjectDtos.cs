namespace Core.Dtos
{
    public class AddAssetsToProjectReq
    {
        public string projectId { get; set; }
        public List<string> ImageIds { get; set; }
    }

    public class AddAssetsToProjectRes
    {
        // public string projectId {get; set; }
        // public List<AssignedAsset> assignedAssets { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    // public class AssignedAsset
    // {
    //     public string id { get; set; }
    //     public string filename { get; set; }
    // }
}