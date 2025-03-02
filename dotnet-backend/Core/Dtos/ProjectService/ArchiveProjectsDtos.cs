namespace Core.Dtos
{
    public class ArchiveProjectsReq
    {
        public List<string> projectIds { get; set; }
    }

    public class ArchiveProjectsRes
    {
        public DateTime archiveTimestamp { get; set; }
    }
}