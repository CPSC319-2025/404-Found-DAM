namespace Core.Dtos
{
    public class SearchResultDto
    {
        public List<GetProjectRes> projects { get; set; } = new();
        public List<AssetSearchResultDto> assets { get; set; } = new();
    }

    public class AssetSearchResultDto
    {
        public int blobID { get; set; }
        public string fileName { get; set; }
        public string thumbnailUrl { get; set; }

        public List<TagCustomInfo> tags { get; set; } = new();

        public int projectID { get; set; }

        public string projectName { get; set; }

    }
}