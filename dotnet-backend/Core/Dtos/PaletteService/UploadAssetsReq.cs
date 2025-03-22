namespace Core.Dtos
{
    public class UploadAssetsReq
    {
        public required string UploadTaskName { get; set; }
        public required string AssetMimeType { get; set; }
        public required int UserId { get; set; }

        public int ProjectID { get; set; }
    }
}
