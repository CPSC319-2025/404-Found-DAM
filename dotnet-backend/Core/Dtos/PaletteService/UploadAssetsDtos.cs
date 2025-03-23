namespace Core.Dtos
{
    public class UploadAssetsReq
    {
        public required string UploadTaskName { get; set; }
        public required string AssetMimeType { get; set; }
        public required int UserId { get; set; }

        public int ProjectID { get; set; }
    }

    public class UploadAssetsRes
    {
        public required bool Success { get; set; }
        public string? BlobID { get; set; }
        public required string FileName { get; set; }
        public required double SizeInKB { get; set; }

    }
}
