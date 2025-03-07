using Core.Entities;

namespace Core.Dtos
{
    public class GetPaginatedProjectAssetsReq
    {
        public int projectID {get; set; }
        public string assetType {get; set; }
        public int pageNumber {get; set; }
        public int assetsPerPage {get; set; }
        public string? postedBy {get; set; }
        public string? datePosted {get; set; }
    }

    public class GetPaginatedProjectAssetsRes
    {
        public int projectID {get; set; }
        public List<PaginatedProjectAsset> assets { get; set; }
        public ProjectAssetsPagination pagination {get; set; }
    }

    public class PaginatedProjectAsset
    {
        public int blobID { get; set; }
        public string thumbnailUrl { get; set; }
        public string filename { get; set; }
        public PaginatedProjectAssetUploadedBy uploadedBy { get; set; }
        public DateTime lastUpdated { get; set; }
        public int filesizeInKB { get; set; }
        public List<string> tags { get; set; }  
    }

    public class PaginatedProjectAssetUploadedBy
    {
        public int userID { get; set; }
        public string name { get; set; }
    }

    public class ProjectAssetsPagination
    {
        public int pageNumber { get; set; }
        public int assetsPerPage { get; set; }
        public int totalAssetsReturned { get; set; }
        public int totalPages { get; set; } // For updating frontend pagination
    } 
}
