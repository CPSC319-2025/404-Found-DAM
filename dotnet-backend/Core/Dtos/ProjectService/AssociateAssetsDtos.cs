using System.Text.Json;


namespace Core.Dtos
{
    public class AssociateAssetsWithProjectReq
    {
        public required List<string> BlobIDs { get; set; } = new List<string>();
        public required int ProjectID { get; set; }
        public required List<int> TagIDs { get; set; }
        public required List<AssetMetadataEntry> MetadataEntries { get; set; } = new List<AssetMetadataEntry>();
    }

    public class AssetMetadataEntry
    {
        public required int FieldId { get; set; }
        public required JsonElement FieldValue { get; set; }
    }

        public class AssociateAssetsWithProjectRes
    {
        public required int ProjectID { get; set; }
        public required List<string> UpdatedImages { get; set; } = new List<string>();
        public required List<string> FailedAssociations { get; set; } = new List<string>();
        public required string Message { get; set; }
    }
}