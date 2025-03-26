namespace Core.Dtos
{
    public class GetAssetFileFromStorageReq
    {
        public required string blobID { get; set; }
        public required string filename { get; set; }
    }
}