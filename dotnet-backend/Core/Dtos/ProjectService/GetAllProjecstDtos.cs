namespace Core.Dtos
{
    public class GetAllProjecsRes
    {
        public int projectCount { get; set; }
        public List<FullProjectInfo> fullProjectInfos { get; set; }
    }

    public class FullProjectInfo
    {
        public int projectID { get; set; }
        public string projectName { get; set; }
        public string location { get; set; }
        public string description { get; set; }
        public DateTime creationTime { get; set; }
        public bool active { get; set; }
        public DateTime? archivedAt { get; set; } 
        public int assetCount { get; set; } 
        public HashSet<string> adminNames { get; set; } 
        public HashSet<string> regularUserNames { get; set; }         
    }
}