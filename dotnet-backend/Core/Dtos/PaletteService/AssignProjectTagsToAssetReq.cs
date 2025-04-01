namespace Core.Dtos
{
    public class AssignProjectTagsToAssetReq
    {
        public string BlobId { get; set; }
        public int ProjectId { get; set; }
    }

    public class AssignProjectTagsResult
    {
        public bool Success { get; set; }
        public string BlobId { get; set; }
        public List<int> AssignedTagIds { get; set; } = new List<int>();
        public string Message { get; set; }
    }
} 