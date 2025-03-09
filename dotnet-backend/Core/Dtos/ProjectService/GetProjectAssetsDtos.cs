namespace Core.Dtos
{
    public class GetProjectAssetsRes
    {
        public int projectID {get; set; }
        public List<ProjectAsset> assets { get; set; }
        public ProjectAssetsPagination pagination {get; set; }
    }

    public class ProjectAsset
    {
        public int blobID { get; set; }
        public string thumbnailUrl { get; set; }
        public string filename { get; set; }
        public ProjectAssetMD projectAssetMD { get; set; }
    }

    public class ProjectAssetMD
    {
        public DateTime date { get; set; }
        public List<string> tags { get; set; }
    } 

    public class ProjectAssetsPagination
    {
        public int page { get; set; }
        public int limit { get; set; }
        public int total { get; set; }
    } 
}
