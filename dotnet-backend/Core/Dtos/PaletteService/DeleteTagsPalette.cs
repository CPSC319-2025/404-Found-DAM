namespace Core.Dtos
{
    public class RemoveTagsFromPaletteReq
    {
        public List<string> BlobIds { get; set; } = new List<string>();
        public List<int> TagIds { get; set; } = new List<int>();
    }

    public class AssetTagAssociationDto
    {
        public string BlobId { get; set; }
        public int TagId { get; set; }
    }

    public class RemoveTagsResult
    {
        public List<AssetTagAssociationDto> RemovedAssociations { get; set; } = new List<AssetTagAssociationDto>();
        public List<AssetTagAssociationDto> NotFoundAssociations { get; set; } = new List<AssetTagAssociationDto>();
    }
}
