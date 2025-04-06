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
        public required List<ProcessedAsset> SuccessfulUploads { get; set; }
        public required List<ProcessedAsset> FailedUploads { get; set; }
    }

    public class ProcessedAsset
    {
        public string? BlobID { get; set; }
        public required string FileName { get; set; }
        public required double SizeInKB { get; set; }
        public required bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
