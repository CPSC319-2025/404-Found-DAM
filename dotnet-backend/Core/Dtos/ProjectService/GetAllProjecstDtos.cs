namespace Core.Dtos
{
    public class GetAllProjecsRes
    {
        public int projectCount { get; set; }
        public List<GetAllProject> getAllProjects { get; set; }
    }

    public class GetAllProject
    {
        public int projectID { get; set; }
        public required string projectName { get; set; }
        public required string location { get; set; }
        public required string description { get; set; }
        public DateTime creationTime { get; set; }
        public bool active { get; set; }
        public DateTime archivedAt { get; set; } 
        public int assetCount { get; set; } 
        public List<string> userNames { get; set; } 
        
    }
}