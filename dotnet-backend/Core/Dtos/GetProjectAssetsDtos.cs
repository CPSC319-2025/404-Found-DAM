namespace Core.Dtos
{
    public class GetProjectAssetsRes
    {
        public string projectId {get; set; }
        public List<ProjectAsset> assets { get; set; }
        public ProjectAssetsPagination pagination {get; set; }
    }

    public class ProjectAsset
    {
        public string id { get; set; }
        public string thumbnailUrl { get; set; }
        public string filename { get; set; }
        public ProjectAssetMd projectAssetMd { get; set; }
    }

    public class ProjectAssetMd
    {
        public string date { get; set; }
        public List<string> tags { get; set; }
    } 

    public class ProjectAssetsPagination
    {
        public int page { get; set; }
        public int limit { get; set; }
        public int total { get; set; }
    } 
}
