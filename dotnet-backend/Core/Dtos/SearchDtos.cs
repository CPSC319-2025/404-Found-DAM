namespace Core.Dtos
{
    public class SearchResultDto
    {
        public List<GetProjectRes> projects { get; set; } = new();
        public List<AssetSearchResultDto> assets { get; set; } = new();
    }

    public class AssetSearchResultUploadedBy
    {
        public int userID { get; set; }
        public string name { get; set; }
        public string email { get; set; }
    }

    public class AssetSearchResultDto
    {
        public string blobID { get; set; }
        public string filename { get; set; }
        public AssetSearchResultUploadedBy uploadedBy { get; set; }

        public List<string> tags { get; set; } = new();

        public int projectID { get; set; }
        
        public string projectName { get; set; }

        public string mimetype { get; set; }

        public DateTime date { get; set; }

        public double filesizeInKB { get; set; }
        
        public string BlobSASUrl { get; set; }
    }
}