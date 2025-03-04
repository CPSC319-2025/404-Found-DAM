namespace Core.Dtos
{
    public class ArchiveProjectsReq
    {
        public List<int> projectIDs { get; set; }
    }

    public class ArchiveProjectsRes
    {
        public DateTime archiveTimestamp { get; set; }
    }
}