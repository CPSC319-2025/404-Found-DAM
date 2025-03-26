// using Microsoft.AspNetCore.Http;
using System.IO;
using System.IO.Compression;
using Core.Entities;


namespace Core.Dtos
{
    public class ImportProjectReq
    {
        // TODO
        // public IFormFile Folder { get; set; } 
        // metadata_file: [CSV/Excel file with metadata]
    }

    public class ImportProjectRes
    {
        public DateTime importedDate { get; set;}
        public GetProjectRes importedProjectInfo { get; set; }
        public List<UserCustomInfo> nonExistentUsers { get; set; }
    }

    public class ImportUserProfile
    {
        public required int userID { get; set;}
        public required string userName { get; set;}
        public required string userEmail { get; set;}
        public required bool userIsSuperAdmin { get; set;}
        public required ProjectMembership.UserRoleType userRole { get; set;}
    }
}