namespace Core.Dtos
{
    public class ArchiveProjectReq
    {
        public int projectID { get; set; }
    }

    public class ArchiveProjectRes
    {
       public int projectID { get; set; }
       public DateTime? archiveTimestampUTC { get; set; }
    }
}