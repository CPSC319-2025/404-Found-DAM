namespace Core.Dtos
{
    public class GetBlobProjectAndTagsReq
    {
        public int BlobId { get; set; }
    }

    public class GetBlobProjectAndTagsRes
    {
        public string BlobId { get; set; }
        public string FileName { get; set; }
        public ProjectInfo Project { get; set; }
        public List<string> Tags { get; set; }
    }

    public class ProjectInfo
    {
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
    }
} 