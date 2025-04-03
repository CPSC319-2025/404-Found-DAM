using Microsoft.AspNetCore.Http;

namespace Core.Dtos
{
    public class GetPaginatedProjectAssetsReq
    {
        public int projectID {get; set; }
        public string assetType {get; set; }
        public int pageNumber {get; set; }
        public int assetsPerPage {get; set; }
        public int? postedBy {get; set; } // userID
        public string? tagName { get; set; }
        public DateTime? fromDate {get; set; }
        public DateTime? toDate {get; set; }
    }

    public class GetPaginatedProjectAssetsRes
    {
        public int projectID {get; set; }
        public List<PaginatedProjectAsset> assets { get; set; }
        public List<string> assetBlobSASUrlList { get; set; }
        public ProjectAssetsPagination pagination {get; set; }
        public List<GetAssetFileFromStorageReq> assetIdNameList {get; set; }
    }

    public class PaginatedProjectAsset
    {
        public string blobID { get; set; }
        public string filename { get; set; }
        public PaginatedProjectAssetUploadedBy uploadedBy { get; set; }
        public DateTime date { get; set; }
        public double filesizeInKB { get; set; }
        public List<string> tags { get; set; }  
        public string mimetype { get; set; }
    }

    public class PaginatedProjectAssetUploadedBy
    {
        public int userID { get; set; }
        public string name { get; set; }
        public string email { get; set; }
    }

    public class ProjectAssetsPagination
    {
        public int pageNumber { get; set; }
        public int assetsPerPage { get; set; }
        public int totalAssetsReturned { get; set; }
        public int totalPages { get; set; } // For updating frontend pagination
    } 
}