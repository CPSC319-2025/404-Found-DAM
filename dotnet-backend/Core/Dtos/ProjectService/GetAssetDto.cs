using System.Text.Json;

namespace Core.Dtos
{
    public class GetAssetRes
    {
        public required string blobID { get; set; }
        public required string filename { get; set; }
        public required ProjectAssetUploadedBy uploadedBy { get; set; }
        public required DateTime date { get; set; }
        public required double filesizeInKB { get; set; }
        public required string mimetype { get; set; }
        public required List<string> tags { get; set; } = new List<string>();
        public required List<AssetMetadataCustomInfo> metadata { get; set; } = new List<AssetMetadataCustomInfo>();
    }

    public class ProjectAssetUploadedBy
    {
        public required int userID { get; set; }
        public required string name { get; set; }
        public required string email { get; set; }
    }

    public class AssetMetadataCustomInfo
    {
        public required int fieldID { get; set; }
        // public required string fieldName { get; set; }
        // public required JsonElement fieldValue { get; set; }
    }
}