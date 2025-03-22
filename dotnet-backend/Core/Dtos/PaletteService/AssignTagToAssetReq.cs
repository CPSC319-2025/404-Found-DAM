namespace Core.Dtos
{
    public class AssignTagToAssetReq
    {
        public string BlobId { get; set; }
        public int TagId { get; set; }
    }

    public class AssignTagResult
    {
        public bool Success { get; set; }
        public string BlobId { get; set; }
        public int TagId { get; set; }
        public string Message { get; set; }
    }
} 