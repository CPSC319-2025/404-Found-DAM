namespace Core.Dtos
{
    public class ArchiveProjectsReq
    {
        public List<int> projectIDs { get; set; }
    }

    public class ArchiveProjectsRes
    {
        public List<ArchivedProject> projectsNewlyArchived { get; set; }
        public List<ArchivedProject> projectsAlreadyArchived { get; set; }
        public List<int> unfoundProjectIDs { get; set; }
    }

    public class ArchivedProject
    {
       public int projectID { get; set; }
       public DateTime archiveTimestampUTC { get; set; }
    }
}