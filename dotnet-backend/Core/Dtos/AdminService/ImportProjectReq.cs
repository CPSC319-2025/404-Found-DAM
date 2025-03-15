// using Microsoft.AspNetCore.Http;
using System.IO;
using System.IO.Compression;


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
    }
}