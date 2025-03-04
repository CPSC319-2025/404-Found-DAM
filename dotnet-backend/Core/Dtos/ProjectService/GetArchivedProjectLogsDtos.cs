namespace Core.Dtos
{
    public class GetArchivedProjectLogsRes
    {
        public List<ArchivedProjectLog> logs { get; set; }
    }

    public class ArchivedProjectLog
    {
        public int projectID { get; set; }
        public string projectName { get; set; }
        public DateTime archivedAt { get; set; }
        public string admin { get; set; }
    }
}